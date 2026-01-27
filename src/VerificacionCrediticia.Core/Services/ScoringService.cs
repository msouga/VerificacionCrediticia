using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public class ScoringService : IScoringService
{
    private const decimal SCORE_INICIAL = 100m;
    private const decimal PENALIZACION_SCORE_BAJO = 20m;
    private const decimal PENALIZACION_MOROSIDAD = 25m;
    private const decimal PENALIZACION_DEUDA_CASTIGADA = 35m;
    private const decimal PENALIZACION_EMPRESA_INACTIVA = 30m;
    private const decimal PENALIZACION_DEUDA_VENCIDA_BASE = 10m;
    private const decimal UMBRAL_SCORE_BAJO = 500m;

    public ResultadoEvaluacionDto EvaluarRed(ResultadoExploracionDto exploracion)
    {
        var alertas = new List<Alerta>();

        var personasConProblemas = 0;
        var empresasConProblemas = 0;
        decimal montoTotalDeudas = 0;
        decimal montoTotalDeudasVencidas = 0;
        var scoresValidos = new List<decimal>();

        // Desglose de scores por categoria
        var desglose = new DesgloseScoreDto { ScoreBase = SCORE_INICIAL };
        decimal penalizacionSolicitante = 0;
        decimal penalizacionEmpresa = 0;
        decimal penalizacionRelaciones = 0;
        int relacionesConProblemas = 0;
        int totalRelaciones = 0;

        foreach (var nodo in exploracion.Grafo.Values)
        {
            // Factor de impacto segun nivel de profundidad (mas cercano = mas impacto)
            var factorProfundidad = 1m / (nodo.NivelProfundidad + 1);

            var tieneProblemas = false;
            var esSolicitante = nodo.Identificador == exploracion.DniSolicitante;
            var esEmpresaPrincipal = nodo.Identificador == exploracion.RucEmpresa;
            var esRelacion = !esSolicitante && !esEmpresaPrincipal;

            if (esRelacion) totalRelaciones++;

            // Evaluar score crediticio
            if (nodo.Score.HasValue)
            {
                scoresValidos.Add(nodo.Score.Value);

                if (nodo.Score < UMBRAL_SCORE_BAJO)
                {
                    var penalizacion = PENALIZACION_SCORE_BAJO * factorProfundidad;
                    tieneProblemas = true;

                    var motivo = $"Score bajo: {nodo.Score}";
                    AgregarPenalizacion(ref penalizacionSolicitante, ref penalizacionEmpresa, ref penalizacionRelaciones,
                        desglose, esSolicitante, esEmpresaPrincipal, penalizacion, motivo);

                    alertas.Add(new Alerta
                    {
                        Tipo = TipoAlerta.ScoreBajo,
                        Mensaje = $"{nodo.Tipo} {nodo.Nombre} ({nodo.Identificador}) tiene score bajo: {nodo.Score}",
                        Severidad = nodo.Score < 300 ? Severidad.Critica : Severidad.Alta,
                        NivelProfundidad = nodo.NivelProfundidad,
                        IdentificadorEntidad = nodo.Identificador,
                        TipoEntidad = nodo.Tipo
                    });
                }
            }

            // Evaluar estado crediticio
            switch (nodo.EstadoCredito)
            {
                case EstadoCrediticio.Moroso:
                    {
                        var penalizacion = PENALIZACION_MOROSIDAD * factorProfundidad;
                        tieneProblemas = true;

                        var motivo = "Morosidad activa";
                        AgregarPenalizacion(ref penalizacionSolicitante, ref penalizacionEmpresa, ref penalizacionRelaciones,
                            desglose, esSolicitante, esEmpresaPrincipal, penalizacion, motivo);

                        alertas.Add(new Alerta
                        {
                            Tipo = TipoAlerta.Morosidad,
                            Mensaje = $"{nodo.Tipo} {nodo.Nombre} ({nodo.Identificador}) esta en morosidad",
                            Severidad = nodo.NivelProfundidad == 0 ? Severidad.Critica : Severidad.Alta,
                            NivelProfundidad = nodo.NivelProfundidad,
                            IdentificadorEntidad = nodo.Identificador,
                            TipoEntidad = nodo.Tipo
                        });
                    }
                    break;

                case EstadoCrediticio.Castigado:
                    {
                        var penalizacion = PENALIZACION_DEUDA_CASTIGADA * factorProfundidad;
                        tieneProblemas = true;

                        var motivo = "Deuda castigada";
                        AgregarPenalizacion(ref penalizacionSolicitante, ref penalizacionEmpresa, ref penalizacionRelaciones,
                            desglose, esSolicitante, esEmpresaPrincipal, penalizacion, motivo);

                        alertas.Add(new Alerta
                        {
                            Tipo = TipoAlerta.DeudaVencida,
                            Mensaje = $"{nodo.Tipo} {nodo.Nombre} ({nodo.Identificador}) tiene deuda castigada",
                            Severidad = Severidad.Critica,
                            NivelProfundidad = nodo.NivelProfundidad,
                            IdentificadorEntidad = nodo.Identificador,
                            TipoEntidad = nodo.Tipo
                        });
                    }
                    break;
            }

            // Evaluar deudas
            foreach (var deuda in nodo.Deudas)
            {
                montoTotalDeudas += deuda.SaldoActual;

                if (deuda.EstaVencida)
                {
                    montoTotalDeudasVencidas += deuda.SaldoActual;
                    var penalizacionDeuda = CalcularPenalizacionDeuda(deuda) * factorProfundidad;
                    tieneProblemas = true;

                    var motivo = $"Deuda vencida S/ {deuda.SaldoActual:N0} ({deuda.DiasVencidos} dias)";
                    AgregarPenalizacion(ref penalizacionSolicitante, ref penalizacionEmpresa, ref penalizacionRelaciones,
                        desglose, esSolicitante, esEmpresaPrincipal, penalizacionDeuda, motivo);

                    if (nodo.NivelProfundidad <= 1)
                    {
                        alertas.Add(new Alerta
                        {
                            Tipo = TipoAlerta.DeudaVencida,
                            Mensaje = $"{nodo.Tipo} {nodo.Nombre}: Deuda vencida de S/ {deuda.SaldoActual:N2} con {deuda.Entidad} ({deuda.DiasVencidos} dias)",
                            Severidad = deuda.DiasVencidos > 90 ? Severidad.Critica : Severidad.Alta,
                            NivelProfundidad = nodo.NivelProfundidad,
                            IdentificadorEntidad = nodo.Identificador,
                            TipoEntidad = nodo.Tipo
                        });
                    }
                }
            }

            // Evaluar alertas del nodo
            foreach (var alertaNodo in nodo.Alertas)
            {
                if (!alertas.Any(a => a.Mensaje.Contains(alertaNodo)))
                {
                    alertas.Add(new Alerta
                    {
                        Tipo = TipoAlerta.ProblemaDetectado,
                        Mensaje = $"{nodo.Tipo} {nodo.Nombre}: {alertaNodo}",
                        Severidad = DeterminarSeveridadAlerta(alertaNodo),
                        NivelProfundidad = nodo.NivelProfundidad,
                        IdentificadorEntidad = nodo.Identificador,
                        TipoEntidad = nodo.Tipo
                    });
                }
            }

            if (tieneProblemas)
            {
                if (nodo.Tipo == TipoNodo.Persona)
                    personasConProblemas++;
                else
                    empresasConProblemas++;

                if (esRelacion)
                    relacionesConProblemas++;
            }
        }

        // Calcular scores finales por categoria
        desglose.PenalizacionSolicitante = Math.Round(penalizacionSolicitante, 2);
        desglose.PenalizacionEmpresa = Math.Round(penalizacionEmpresa, 2);
        desglose.PenalizacionRelaciones = Math.Round(penalizacionRelaciones, 2);
        desglose.ScoreSolicitante = Math.Max(0, Math.Round(SCORE_INICIAL - penalizacionSolicitante, 2));
        desglose.ScoreEmpresa = Math.Max(0, Math.Round(SCORE_INICIAL - penalizacionEmpresa, 2));
        desglose.ScoreRelaciones = Math.Max(0, Math.Round(SCORE_INICIAL - penalizacionRelaciones, 2));
        desglose.TotalRelacionesAnalizadas = totalRelaciones;
        desglose.RelacionesConProblemas = relacionesConProblemas;

        // Score total es el promedio ponderado de las tres categorias
        var scoreTotal = SCORE_INICIAL - penalizacionSolicitante - penalizacionEmpresa - penalizacionRelaciones;
        scoreTotal = Math.Max(0, scoreTotal);

        // Obtener informacion del solicitante y empresa
        var nodoPrincipal = exploracion.Grafo.GetValueOrDefault(exploracion.DniSolicitante);
        var nodoEmpresa = exploracion.Grafo.GetValueOrDefault(exploracion.RucEmpresa);

        return new ResultadoEvaluacionDto
        {
            DniSolicitante = exploracion.DniSolicitante,
            NombreSolicitante = nodoPrincipal?.Nombre ?? "No encontrado",
            RucEmpresa = exploracion.RucEmpresa,
            RazonSocialEmpresa = nodoEmpresa?.Nombre ?? "No encontrada",
            ScoreFinal = Math.Round(scoreTotal, 2),
            DesgloseScore = desglose,
            Recomendacion = DeterminarRecomendacion(scoreTotal, alertas),
            Resumen = new ResumenRedDto
            {
                TotalPersonasAnalizadas = exploracion.TotalPersonas,
                TotalEmpresasAnalizadas = exploracion.TotalEmpresas,
                PersonasConProblemas = personasConProblemas,
                EmpresasConProblemas = empresasConProblemas,
                ScorePromedioRed = scoresValidos.Any() ? Math.Round(scoresValidos.Average(), 2) : null,
                MontoTotalDeudas = montoTotalDeudas,
                MontoTotalDeudasVencidas = montoTotalDeudasVencidas
            },
            Alertas = alertas.OrderByDescending(a => a.Severidad)
                            .ThenBy(a => a.NivelProfundidad)
                            .ToList(),
            FechaEvaluacion = DateTime.UtcNow
        };
    }

    private void AgregarPenalizacion(
        ref decimal penalizacionSolicitante,
        ref decimal penalizacionEmpresa,
        ref decimal penalizacionRelaciones,
        DesgloseScoreDto desglose,
        bool esSolicitante,
        bool esEmpresaPrincipal,
        decimal penalizacion,
        string motivo)
    {
        if (esSolicitante)
        {
            penalizacionSolicitante += penalizacion;
            if (!desglose.MotivosSolicitante.Contains(motivo))
                desglose.MotivosSolicitante.Add(motivo);
        }
        else if (esEmpresaPrincipal)
        {
            penalizacionEmpresa += penalizacion;
            if (!desglose.MotivosEmpresa.Contains(motivo))
                desglose.MotivosEmpresa.Add(motivo);
        }
        else
        {
            penalizacionRelaciones += penalizacion;
            if (!desglose.MotivosRelaciones.Contains(motivo))
                desglose.MotivosRelaciones.Add(motivo);
        }
    }

    private decimal CalcularPenalizacionDeuda(DeudaRegistrada deuda)
    {
        var penalizacionBase = PENALIZACION_DEUDA_VENCIDA_BASE;

        // Aumentar penalización según días vencidos
        if (deuda.DiasVencidos > 90)
            penalizacionBase *= 2;
        else if (deuda.DiasVencidos > 60)
            penalizacionBase *= 1.5m;
        else if (deuda.DiasVencidos > 30)
            penalizacionBase *= 1.2m;

        // Aumentar penalización según monto
        if (deuda.SaldoActual > 50000)
            penalizacionBase *= 1.5m;
        else if (deuda.SaldoActual > 10000)
            penalizacionBase *= 1.2m;

        return penalizacionBase;
    }

    private Recomendacion DeterminarRecomendacion(decimal score, List<Alerta> alertas)
    {
        var alertasCriticasNivel0y1 = alertas.Count(a =>
            a.Severidad == Severidad.Critica && a.NivelProfundidad <= 1);

        var alertasAltasNivel0 = alertas.Count(a =>
            a.Severidad == Severidad.Alta && a.NivelProfundidad == 0);

        // Rechazar si hay alertas críticas cercanas o score muy bajo
        if (alertasCriticasNivel0y1 > 0 || score < 40)
            return Recomendacion.Rechazar;

        // Revisar manualmente si hay alertas altas o score moderado
        if (alertasAltasNivel0 > 1 || score < 60)
            return Recomendacion.RevisarManualmente;

        // Aprobar si todo está en orden
        return Recomendacion.Aprobar;
    }

    private Severidad DeterminarSeveridadAlerta(string alerta)
    {
        var alertaLower = alerta.ToLower();

        if (alertaLower.Contains("castigad") || alertaLower.Contains("crítico"))
            return Severidad.Critica;

        if (alertaLower.Contains("morosi") || alertaLower.Contains("vencid"))
            return Severidad.Alta;

        if (alertaLower.Contains("bajo") || alertaLower.Contains("inactiv"))
            return Severidad.Media;

        return Severidad.Baja;
    }
}

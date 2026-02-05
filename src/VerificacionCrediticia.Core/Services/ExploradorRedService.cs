using VerificacionCrediticia.Core.DTOs;
using VerificacionCrediticia.Core.Entities;
using VerificacionCrediticia.Core.Enums;
using VerificacionCrediticia.Core.Interfaces;

namespace VerificacionCrediticia.Core.Services;

public class ExploradorRedService : IExploradorRedService
{
    private readonly IEquifaxApiClient _equifaxClient;

    public ExploradorRedService(IEquifaxApiClient equifaxClient)
    {
        _equifaxClient = equifaxClient;
    }

    public async Task<ResultadoExploracionDto> ExplorarRedAsync(
        string dniSolicitante,
        string rucEmpresaSolicitante,
        int profundidadMaxima = 2,
        CancellationToken cancellationToken = default)
    {
        var nodosVisitados = new HashSet<string>();
        var grafo = new Dictionary<string, NodoRed>();
        var cola = new Queue<(string id, string tipoDoc, TipoNodo tipoNodo, int nivel)>();

        cola.Enqueue((dniSolicitante, "1", TipoNodo.Persona, 0));
        cola.Enqueue((rucEmpresaSolicitante, "6", TipoNodo.Empresa, 0));

        while (cola.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (id, tipoDoc, tipoNodo, nivel) = cola.Dequeue();

            if (nodosVisitados.Contains(id) || nivel > profundidadMaxima)
                continue;

            nodosVisitados.Add(id);

            var reporte = await _equifaxClient.ConsultarReporteCrediticioAsync(
                tipoDoc, id, cancellationToken);

            if (reporte == null) continue;

            var nodo = ConstruirNodo(reporte, tipoNodo, nivel);
            grafo[id] = nodo;

            if (nivel < profundidadMaxima)
            {
                foreach (var conexion in nodo.Conexiones)
                {
                    if (!nodosVisitados.Contains(conexion.Identificador))
                    {
                        var tipoDocConexion = conexion.Tipo == TipoNodo.Persona ? "1" : "6";
                        cola.Enqueue((conexion.Identificador, tipoDocConexion,
                            conexion.Tipo, nivel + 1));
                    }
                }
            }
        }

        return new ResultadoExploracionDto
        {
            DniSolicitante = dniSolicitante,
            RucEmpresa = rucEmpresaSolicitante,
            Grafo = grafo,
            TotalNodos = grafo.Count,
            TotalPersonas = grafo.Values.Count(n => n.Tipo == TipoNodo.Persona),
            TotalEmpresas = grafo.Values.Count(n => n.Tipo == TipoNodo.Empresa),
            FechaConsulta = DateTime.UtcNow
        };
    }

    private NodoRed ConstruirNodo(ReporteCrediticioDto reporte, TipoNodo tipoNodo, int nivel)
    {
        var conexiones = new List<ConexionNodo>();
        var alertas = new List<string>();

        var nivelRiesgo = reporte.NivelRiesgo;
        var scoreNumerico = NivelRiesgoMapper.ToScoreNumerico(nivelRiesgo);
        var estadoCredito = NivelRiesgoMapper.ToEstadoCrediticio(nivelRiesgo);
        var nivelRiesgoTexto = reporte.NivelRiesgoTexto;

        string nombre;

        if (tipoNodo == TipoNodo.Persona)
        {
            nombre = reporte.DatosPersona?.Nombres ?? reporte.NumeroDocumento;

            // RepresentantesDe: empresas donde esta persona es representante legal
            foreach (var rep in reporte.RepresentantesDe)
            {
                conexiones.Add(new ConexionNodo
                {
                    Identificador = rep.NumeroDocumento,
                    Tipo = TipoNodo.Empresa,
                    Nombre = rep.Nombre,
                    TipoRelacion = rep.Cargo ?? "Representante Legal"
                });
            }

            // Alertas de persona
            if (estadoCredito == EstadoCrediticio.Moroso)
                alertas.Add("Persona con riesgo alto");

            if (estadoCredito == EstadoCrediticio.Castigado)
                alertas.Add("Persona con riesgo muy alto");

            var deudasVencidas = reporte.Deudas.Where(d => d.EstaVencida).ToList();
            if (deudasVencidas.Any())
            {
                var montoVencido = deudasVencidas.Sum(d => d.SaldoActual);
                alertas.Add($"Deudas vencidas por S/ {montoVencido:N2}");
            }
        }
        else
        {
            nombre = reporte.DatosEmpresa?.RazonSocial ?? reporte.NumeroDocumento;

            // RepresentadoPor: personas que representan esta empresa
            foreach (var rep in reporte.RepresentadoPor)
            {
                conexiones.Add(new ConexionNodo
                {
                    Identificador = rep.NumeroDocumento,
                    Tipo = TipoNodo.Persona,
                    Nombre = rep.Nombre,
                    TipoRelacion = rep.Cargo ?? "Representante Legal"
                });
            }

            // EmpresasRelacionadas: otras empresas vinculadas
            foreach (var empRel in reporte.EmpresasRelacionadas)
            {
                conexiones.Add(new ConexionNodo
                {
                    Identificador = empRel.NumeroDocumento,
                    Tipo = TipoNodo.Empresa,
                    Nombre = empRel.Nombre,
                    TipoRelacion = empRel.Relacion ?? "Empresa Relacionada"
                });
            }

            // Alertas de empresa
            var estadoContrib = reporte.DatosEmpresa?.EstadoContribuyente;
            if (estadoContrib != null && estadoContrib.ToUpper() != "ACTIVO")
                alertas.Add($"Empresa con estado: {estadoContrib}");

            if (estadoCredito == EstadoCrediticio.Moroso)
                alertas.Add("Empresa con riesgo alto");

            if (estadoCredito == EstadoCrediticio.Castigado)
                alertas.Add("Empresa con riesgo muy alto");

            var deudasVencidas = reporte.Deudas.Where(d => d.EstaVencida).ToList();
            if (deudasVencidas.Any())
            {
                var montoVencido = deudasVencidas.Sum(d => d.SaldoActual);
                alertas.Add($"Deudas vencidas por S/ {montoVencido:N2}");
            }
        }

        return new NodoRed
        {
            Identificador = reporte.NumeroDocumento,
            Tipo = tipoNodo,
            Nombre = nombre,
            NivelProfundidad = nivel,
            Score = scoreNumerico,
            NivelRiesgoTexto = nivelRiesgoTexto,
            EstadoCredito = estadoCredito,
            Alertas = alertas,
            Deudas = reporte.Deudas,
            Conexiones = conexiones
        };
    }
}

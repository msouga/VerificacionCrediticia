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
        var cola = new Queue<(string id, TipoNodo tipo, int nivel)>();

        // Iniciar exploración con DNI solicitante y RUC empresa
        cola.Enqueue((dniSolicitante, TipoNodo.Persona, 0));
        cola.Enqueue((rucEmpresaSolicitante, TipoNodo.Empresa, 0));

        while (cola.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (id, tipo, nivel) = cola.Dequeue();

            if (nodosVisitados.Contains(id) || nivel > profundidadMaxima)
                continue;

            nodosVisitados.Add(id);

            var nodo = await ObtenerInformacionNodoAsync(id, tipo, nivel, cancellationToken);
            if (nodo != null)
            {
                grafo[id] = nodo;

                // Agregar conexiones a la cola para explorar siguiente nivel
                if (nivel < profundidadMaxima)
                {
                    foreach (var conexion in nodo.Conexiones)
                    {
                        if (!nodosVisitados.Contains(conexion.Identificador))
                        {
                            cola.Enqueue((conexion.Identificador, conexion.Tipo, nivel + 1));
                        }
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

    private async Task<NodoRed?> ObtenerInformacionNodoAsync(
        string id,
        TipoNodo tipo,
        int nivel,
        CancellationToken cancellationToken)
    {
        if (tipo == TipoNodo.Persona)
        {
            return await ObtenerNodoPersonaAsync(id, nivel, cancellationToken);
        }
        else
        {
            return await ObtenerNodoEmpresaAsync(id, nivel, cancellationToken);
        }
    }

    private async Task<NodoRed?> ObtenerNodoPersonaAsync(
        string dni,
        int nivel,
        CancellationToken cancellationToken)
    {
        var persona = await _equifaxClient.ConsultarPersonaAsync(dni, cancellationToken);
        if (persona == null) return null;

        var empresas = await _equifaxClient.ObtenerEmpresasDondeEsSocioAsync(dni, cancellationToken);

        var alertas = GenerarAlertasPersona(persona);

        return new NodoRed
        {
            Identificador = dni,
            Tipo = TipoNodo.Persona,
            Nombre = persona.NombreCompleto,
            NivelProfundidad = nivel,
            Score = persona.ScoreCrediticio,
            EstadoCredito = persona.Estado,
            Alertas = alertas,
            Deudas = persona.Deudas,
            Conexiones = empresas.Select(e => new ConexionNodo
            {
                Identificador = e.Ruc,
                Tipo = TipoNodo.Empresa,
                Nombre = e.RazonSocialEmpresa,
                TipoRelacion = e.TipoRelacion
            }).ToList()
        };
    }

    private async Task<NodoRed?> ObtenerNodoEmpresaAsync(
        string ruc,
        int nivel,
        CancellationToken cancellationToken)
    {
        var empresa = await _equifaxClient.ConsultarEmpresaAsync(ruc, cancellationToken);
        if (empresa == null) return null;

        var socios = await _equifaxClient.ObtenerSociosDeEmpresaAsync(ruc, cancellationToken);

        var alertas = GenerarAlertasEmpresa(empresa);

        return new NodoRed
        {
            Identificador = ruc,
            Tipo = TipoNodo.Empresa,
            Nombre = empresa.RazonSocial,
            NivelProfundidad = nivel,
            Score = empresa.ScoreCrediticio,
            EstadoCredito = empresa.EstadoCredito,
            Alertas = alertas,
            Deudas = empresa.Deudas,
            Conexiones = socios.Select(s => new ConexionNodo
            {
                Identificador = s.Dni,
                Tipo = TipoNodo.Persona,
                Nombre = s.NombrePersona,
                TipoRelacion = s.TipoRelacion
            }).ToList()
        };
    }

    private List<string> GenerarAlertasPersona(Persona persona)
    {
        var alertas = new List<string>();

        if (persona.ScoreCrediticio.HasValue && persona.ScoreCrediticio < 500)
            alertas.Add($"Score crediticio bajo: {persona.ScoreCrediticio}");

        if (persona.Estado == EstadoCrediticio.Moroso)
            alertas.Add("Persona en estado de morosidad");

        if (persona.Estado == EstadoCrediticio.Castigado)
            alertas.Add("Persona con deuda castigada");

        var deudasVencidas = persona.Deudas.Where(d => d.EstaVencida).ToList();
        if (deudasVencidas.Any())
        {
            var montoVencido = deudasVencidas.Sum(d => d.SaldoActual);
            alertas.Add($"Deudas vencidas por S/ {montoVencido:N2}");
        }

        return alertas;
    }

    private List<string> GenerarAlertasEmpresa(Empresa empresa)
    {
        var alertas = new List<string>();

        if (empresa.Estado?.ToUpper() != "ACTIVO")
            alertas.Add($"Empresa con estado: {empresa.Estado}");

        if (empresa.ScoreCrediticio.HasValue && empresa.ScoreCrediticio < 500)
            alertas.Add($"Score crediticio bajo: {empresa.ScoreCrediticio}");

        if (empresa.EstadoCredito == EstadoCrediticio.Moroso)
            alertas.Add("Empresa en estado de morosidad");

        var deudasVencidas = empresa.Deudas.Where(d => d.EstaVencida).ToList();
        if (deudasVencidas.Any())
        {
            var montoVencido = deudasVencidas.Sum(d => d.SaldoActual);
            alertas.Add($"Deudas vencidas por S/ {montoVencido:N2}");
        }

        return alertas;
    }
}

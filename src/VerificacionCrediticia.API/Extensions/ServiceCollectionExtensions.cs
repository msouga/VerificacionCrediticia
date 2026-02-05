using VerificacionCrediticia.Core.Interfaces;
using VerificacionCrediticia.Core.Services;
using VerificacionCrediticia.Infrastructure.DocumentIntelligence;
using VerificacionCrediticia.Infrastructure.Equifax;

namespace VerificacionCrediticia.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración de Equifax
        var equifaxSettings = configuration.GetSection(EquifaxSettings.SectionName);
        services.Configure<EquifaxSettings>(equifaxSettings);

        // Configuración de Document Intelligence
        services.Configure<DocumentIntelligenceSettings>(
            configuration.GetSection(DocumentIntelligenceSettings.SectionName));
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();

        // Cache en memoria
        services.AddMemoryCache();

        // Determinar si usar mock o cliente real
        var useMock = equifaxSettings.GetValue<bool>("UseMock");

        if (useMock)
        {
            // Usar cliente mock para desarrollo/pruebas
            services.AddScoped<IEquifaxApiClient, EquifaxApiClientMock>();
        }
        else
        {
            // Usar cliente real de Equifax
            services.AddHttpClient<IEquifaxAuthService, EquifaxAuthService>();
            services.AddHttpClient<IEquifaxApiClient, EquifaxApiClient>();
        }

        // Servicios de dominio
        services.AddScoped<IExploradorRedService, ExploradorRedService>();
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<IVerificacionService, VerificacionService>();

        return services;
    }
}

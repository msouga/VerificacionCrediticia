using VerificacionCrediticia.Core.Interfaces;
using VerificacionCrediticia.Core.Services;
using VerificacionCrediticia.Infrastructure.ContentUnderstanding;
using VerificacionCrediticia.Infrastructure.Equifax;
using VerificacionCrediticia.Infrastructure.Reniec;

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

        // Configuración de Content Understanding
        var cuSettings = configuration.GetSection(ContentUnderstandingSettings.SectionName);
        services.Configure<ContentUnderstandingSettings>(cuSettings);

        if (cuSettings.GetValue<bool>("UseMock"))
        {
            services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceServiceMock>();
        }
        else
        {
            services.AddHttpClient<IDocumentIntelligenceService, ContentUnderstandingService>();
        }

        // Configuración de RENIEC
        var reniecSettings = configuration.GetSection(ReniecSettings.SectionName);
        services.Configure<ReniecSettings>(reniecSettings);

        if (reniecSettings.GetValue<bool>("UseMock"))
        {
            services.AddScoped<IReniecValidationService, ReniecValidationServiceMock>();
        }
        else
        {
            // TODO: Registrar cliente real de RENIEC cuando se tenga acceso
            services.AddScoped<IReniecValidationService, ReniecValidationServiceMock>();
        }

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

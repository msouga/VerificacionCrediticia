using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using VerificacionCrediticia.Core.Interfaces;
using VerificacionCrediticia.Core.Services;
using VerificacionCrediticia.Infrastructure.ContentUnderstanding;
using VerificacionCrediticia.Infrastructure.Equifax;
using VerificacionCrediticia.Infrastructure.Persistence;
using VerificacionCrediticia.Infrastructure.Persistence.Repositories;
using VerificacionCrediticia.Infrastructure.Reniec;
using VerificacionCrediticia.Infrastructure.BackgroundProcessing;
using VerificacionCrediticia.Infrastructure.Storage;

namespace VerificacionCrediticia.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración de Entity Framework
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (connectionString!.Contains("Data Source="))
            {
                // SQLite para desarrollo local
                options.UseSqlite(connectionString, sqliteOptions =>
                {
                    sqliteOptions.MigrationsAssembly("VerificacionCrediticia.Infrastructure");
                });
            }
            else
            {
                // SQL Server para Azure
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    // Retry logic para transient failures
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);

                    // Command timeout
                    sqlOptions.CommandTimeout(30);

                    // Migraciones en proyecto Infrastructure
                    sqlOptions.MigrationsAssembly("VerificacionCrediticia.Infrastructure");
                });
            }
        });

        // Azure Blob Storage
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        // Background document processing
        services.AddSingleton<IDocumentProcessingQueue, DocumentProcessingQueue>();
        services.AddHostedService<BackgroundDocumentProcessor>();

        // Repositorios
        services.AddScoped<IExpedienteRepository, ExpedienteRepository>();
        services.AddScoped<IDocumentoProcesadoRepository, DocumentoProcesadoRepository>();
        services.AddScoped<ITipoDocumentoRepository, TipoDocumentoRepository>();
        services.AddScoped<IReglaEvaluacionRepository, ReglaEvaluacionRepository>();
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
            services.AddHttpClient<IDocumentIntelligenceService, ContentUnderstandingService>()
                .AddResilienceHandler("content-understanding", (builder, context) =>
                {
                    var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ContentUnderstanding.Resilience");

                    builder.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 5,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromSeconds(2),
                        UseJitter = true,
                        ShouldHandle = args => ValueTask.FromResult(
                            args.Outcome.Result?.StatusCode == HttpStatusCode.TooManyRequests
                            || args.Outcome.Result?.StatusCode == HttpStatusCode.ServiceUnavailable
                            || args.Outcome.Result?.StatusCode == HttpStatusCode.RequestTimeout),
                        DelayGenerator = args =>
                        {
                            if (args.Outcome.Result?.Headers.RetryAfter is { } retryAfter)
                            {
                                TimeSpan? delay = retryAfter.Delta
                                    ?? (retryAfter.Date.HasValue
                                        ? retryAfter.Date.Value - DateTimeOffset.UtcNow
                                        : null);

                                if (delay.HasValue && delay.Value > TimeSpan.Zero)
                                {
                                    logger.LogInformation(
                                        "Retry-After header indica esperar {Delay}", delay.Value);
                                    return ValueTask.FromResult<TimeSpan?>(delay.Value);
                                }
                            }

                            // Sin Retry-After: usar el backoff exponencial por defecto
                            return ValueTask.FromResult<TimeSpan?>(null);
                        },
                        OnRetry = args =>
                        {
                            logger.LogWarning(
                                "Retry {AttemptNumber} para Content Understanding. Status: {Status}. Esperando {Delay}",
                                args.AttemptNumber,
                                args.Outcome.Result?.StatusCode,
                                args.RetryDelay);
                            return ValueTask.CompletedTask;
                        }
                    });
                });
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
        services.AddScoped<IMotorReglasService, MotorReglasService>();
        services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();
        services.AddScoped<IExpedienteService, ExpedienteService>();

        return services;
    }
}

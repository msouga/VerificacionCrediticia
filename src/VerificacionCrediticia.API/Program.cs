using Serilog;
using VerificacionCrediticia.API.Extensions;
using VerificacionCrediticia.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog desde appsettings
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

// Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Verificación Crediticia API",
        Version = "v1",
        Description = "API para verificación crediticia y análisis de red empresarial"
    });
});

// Agregar servicios de la aplicación
builder.Services.AddApplicationServices(builder.Configuration);

// CORS para el Dashboard
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configurar pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verificación Crediticia API v1");
    });
}

// Solo usar HTTPS redirection en producción
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseSerilogRequestLogging();
app.UseCors("AllowDashboard");
app.UseAuthorization();
app.MapControllers();

// Seed database en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
    var seeder = new DatabaseSeeder(context, logger);

    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        log.LogError(ex, "Error durante el seeding de la base de datos");
    }
}

app.Run();

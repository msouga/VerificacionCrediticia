using VerificacionCrediticia.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDashboard", policy =>
    {
        policy.AllowAnyOrigin()
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
app.UseCors("AllowDashboard");
app.UseAuthorization();
app.MapControllers();

app.Run();

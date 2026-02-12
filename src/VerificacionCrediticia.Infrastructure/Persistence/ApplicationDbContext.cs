using Microsoft.EntityFrameworkCore;
using System.Reflection;
using VerificacionCrediticia.Core.Entities;

namespace VerificacionCrediticia.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<TipoDocumento> TiposDocumento { get; set; }
    public DbSet<Expediente> Expedientes { get; set; }
    public DbSet<DocumentoProcesado> DocumentosProcesados { get; set; }
    public DbSet<ResultadoEvaluacionPersistido> ResultadosEvaluacion { get; set; }
    public DbSet<ReglaEvaluacion> ReglasEvaluacion { get; set; }
    public DbSet<ParametroLineaCredito> ParametrosLineaCredito { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de TipoDocumento
        modelBuilder.Entity<TipoDocumento>(entity =>
        {
            entity.ToTable("TiposDocumento");
            entity.HasKey(t => t.Id);

            entity.HasIndex(t => t.Codigo)
                .IsUnique()
                .HasDatabaseName("UX_TiposDocumento_Codigo");

            entity.HasIndex(t => t.Orden)
                .HasDatabaseName("IX_TiposDocumento_Orden");

            entity.Property(t => t.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(t => t.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(t => t.AnalyzerId).HasMaxLength(50);
            entity.Property(t => t.Descripcion).HasMaxLength(500);
        });

        // Configuración de Expediente
        modelBuilder.Entity<Expediente>(entity =>
        {
            entity.ToTable("Expedientes");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.DniSolicitante)
                .HasDatabaseName("IX_Expedientes_DniSolicitante");

            entity.HasIndex(e => e.RucEmpresa)
                .HasDatabaseName("IX_Expedientes_RucEmpresa");

            entity.HasIndex(e => e.FechaCreacion)
                .HasDatabaseName("IX_Expedientes_FechaCreacion");

            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(40);
            entity.Property(e => e.DniSolicitante).HasMaxLength(8);
            entity.Property(e => e.NombresSolicitante).HasMaxLength(100);
            entity.Property(e => e.ApellidosSolicitante).HasMaxLength(100);
            entity.Property(e => e.RucEmpresa).HasMaxLength(11);
            entity.Property(e => e.RazonSocialEmpresa).HasMaxLength(200);

            // Enum como entero
            entity.Property(e => e.Estado)
                .HasConversion<int>();
        });

        // Configuración de DocumentoProcesado
        modelBuilder.Entity<DocumentoProcesado>(entity =>
        {
            entity.ToTable("DocumentosProcesados");
            entity.HasKey(d => d.Id);

            entity.HasIndex(d => new { d.ExpedienteId, d.TipoDocumentoId })
                .HasDatabaseName("IX_DocumentosProcesados_ExpedienteId_TipoDocumentoId");

            entity.HasIndex(d => d.FechaProcesado)
                .HasDatabaseName("IX_DocumentosProcesados_FechaProcesado");

            entity.Property(d => d.NombreArchivo).IsRequired().HasMaxLength(255);
            entity.Property(d => d.DatosExtraidosJson).HasColumnType("TEXT");
            entity.Property(d => d.ConfianzaPromedio).HasColumnType("decimal(5,4)");
            entity.Property(d => d.ErrorMensaje).HasMaxLength(1000);
            entity.Property(d => d.BlobUri).HasMaxLength(500);

            // Enum como entero
            entity.Property(d => d.Estado)
                .HasConversion<int>();

            // Relaciones
            entity.HasOne(d => d.Expediente)
                .WithMany(e => e.Documentos)
                .HasForeignKey(d => d.ExpedienteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_DocumentosProcesados_Expediente");

            entity.HasOne(d => d.TipoDocumento)
                .WithMany(t => t.DocumentosProcesados)
                .HasForeignKey(d => d.TipoDocumentoId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_DocumentosProcesados_TipoDocumento");
        });

        // Configuración de ResultadoEvaluacionPersistido
        modelBuilder.Entity<ResultadoEvaluacionPersistido>(entity =>
        {
            entity.ToTable("ResultadosEvaluacion");
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => r.ExpedienteId)
                .IsUnique()
                .HasDatabaseName("UX_ResultadosEvaluacion_ExpedienteId");

            entity.HasIndex(r => r.FechaEvaluacion)
                .HasDatabaseName("IX_ResultadosEvaluacion_FechaEvaluacion");

            entity.Property(r => r.ScoreFinal).HasColumnType("decimal(10,2)");
            entity.Property(r => r.ResultadoCompletoJson).HasColumnType("TEXT").IsRequired();

            // Enums como enteros
            entity.Property(r => r.Recomendacion)
                .HasConversion<int>();

            entity.Property(r => r.NivelRiesgo)
                .HasConversion<int>();

            // Relación 1:1 con Expediente
            entity.HasOne(r => r.Expediente)
                .WithOne(e => e.ResultadoEvaluacion)
                .HasForeignKey<ResultadoEvaluacionPersistido>(r => r.ExpedienteId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ResultadosEvaluacion_Expediente");
        });

        // Configuración de ReglaEvaluacion
        modelBuilder.Entity<ReglaEvaluacion>(entity =>
        {
            entity.ToTable("ReglasEvaluacion");
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => r.Orden)
                .HasDatabaseName("IX_ReglasEvaluacion_Orden");

            entity.HasIndex(r => r.Activa)
                .HasDatabaseName("IX_ReglasEvaluacion_Activa");

            entity.Property(r => r.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Descripcion).HasMaxLength(500);
            entity.Property(r => r.Campo).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Valor).HasColumnType("decimal(18,6)");
            entity.Property(r => r.Peso).HasColumnType("decimal(5,4)");

            // Enums como enteros
            entity.Property(r => r.Operador)
                .HasConversion<int>();

            entity.Property(r => r.Resultado)
                .HasConversion<int>();
        });

        // Configuración de ParametroLineaCredito
        modelBuilder.Entity<ParametroLineaCredito>(entity =>
        {
            entity.ToTable("ParametrosLineaCredito");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PorcentajeCapitalTrabajo).HasColumnType("decimal(5,2)");
            entity.Property(p => p.PorcentajePatrimonio).HasColumnType("decimal(5,2)");
            entity.Property(p => p.PorcentajeUtilidadNeta).HasColumnType("decimal(5,2)");
            entity.Property(p => p.PesoRedNivel0).HasColumnType("decimal(5,2)");
            entity.Property(p => p.PesoRedNivel1).HasColumnType("decimal(5,2)");
            entity.Property(p => p.PesoRedNivel2).HasColumnType("decimal(5,2)");

            // Seed con valores por defecto
            entity.HasData(new ParametroLineaCredito
            {
                Id = 1,
                PorcentajeCapitalTrabajo = 20m,
                PorcentajePatrimonio = 30m,
                PorcentajeUtilidadNeta = 100m,
                PesoRedNivel0 = 100m,
                PesoRedNivel1 = 50m,
                PesoRedNivel2 = 25m
            });
        });

        // Configuraciones adicionales
        ConfigurarPrecisionDecimales(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        #if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        #endif
    }

    private static void ConfigurarPrecisionDecimales(ModelBuilder modelBuilder)
    {
        // Configurar precision para todos los decimales que no tengan configuración específica
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            if (string.IsNullOrEmpty(property.GetColumnType()))
            {
                property.SetColumnType("decimal(18,2)");
            }
        }
    }
}
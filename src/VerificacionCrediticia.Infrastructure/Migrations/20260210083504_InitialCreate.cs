using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VerificacionCrediticia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expedientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DniSolicitante = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    NombresSolicitante = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApellidosSolicitante = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RucEmpresa = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: true),
                    RazonSocialEmpresa = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expedientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReglasEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Campo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Operador = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Peso = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasEvaluacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnalyzerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EsObligatorio = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResultadosEvaluacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    ScoreFinal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Recomendacion = table.Column<int>(type: "int", nullable: false),
                    NivelRiesgo = table.Column<int>(type: "int", nullable: false),
                    ResultadoCompletoJson = table.Column<string>(type: "TEXT", nullable: false),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultadosEvaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResultadosEvaluacion_Expediente",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentosProcesados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpedienteId = table.Column<int>(type: "int", nullable: false),
                    TipoDocumentoId = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FechaProcesado = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    DatosExtraidosJson = table.Column<string>(type: "TEXT", nullable: true),
                    ConfianzaPromedio = table.Column<decimal>(type: "decimal(5,4)", nullable: true),
                    ErrorMensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosProcesados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosProcesados_Expediente",
                        column: x => x.ExpedienteId,
                        principalTable: "Expedientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentosProcesados_TipoDocumento",
                        column: x => x.TipoDocumentoId,
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosProcesados_ExpedienteId_TipoDocumentoId",
                table: "DocumentosProcesados",
                columns: new[] { "ExpedienteId", "TipoDocumentoId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosProcesados_FechaProcesado",
                table: "DocumentosProcesados",
                column: "FechaProcesado");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosProcesados_TipoDocumentoId",
                table: "DocumentosProcesados",
                column: "TipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_DniSolicitante",
                table: "Expedientes",
                column: "DniSolicitante");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_FechaCreacion",
                table: "Expedientes",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_Expedientes_RucEmpresa",
                table: "Expedientes",
                column: "RucEmpresa");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasEvaluacion_Activa",
                table: "ReglasEvaluacion",
                column: "Activa");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasEvaluacion_Orden",
                table: "ReglasEvaluacion",
                column: "Orden");

            migrationBuilder.CreateIndex(
                name: "IX_ResultadosEvaluacion_FechaEvaluacion",
                table: "ResultadosEvaluacion",
                column: "FechaEvaluacion");

            migrationBuilder.CreateIndex(
                name: "UX_ResultadosEvaluacion_ExpedienteId",
                table: "ResultadosEvaluacion",
                column: "ExpedienteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposDocumento_Orden",
                table: "TiposDocumento",
                column: "Orden");

            migrationBuilder.CreateIndex(
                name: "UX_TiposDocumento_Codigo",
                table: "TiposDocumento",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosProcesados");

            migrationBuilder.DropTable(
                name: "ReglasEvaluacion");

            migrationBuilder.DropTable(
                name: "ResultadosEvaluacion");

            migrationBuilder.DropTable(
                name: "TiposDocumento");

            migrationBuilder.DropTable(
                name: "Expedientes");
        }
    }
}

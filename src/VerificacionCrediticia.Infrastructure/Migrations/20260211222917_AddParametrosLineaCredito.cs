using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VerificacionCrediticia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddParametrosLineaCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParametrosLineaCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PorcentajeCapitalTrabajo = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PorcentajePatrimonio = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PorcentajeUtilidadNeta = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosLineaCredito", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ParametrosLineaCredito",
                columns: new[] { "Id", "PorcentajeCapitalTrabajo", "PorcentajePatrimonio", "PorcentajeUtilidadNeta" },
                values: new object[] { 1, 20m, 30m, 100m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParametrosLineaCredito");
        }
    }
}

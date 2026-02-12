using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VerificacionCrediticia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPesosRedRelaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PesoRedNivel0",
                table: "ParametrosLineaCredito",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoRedNivel1",
                table: "ParametrosLineaCredito",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoRedNivel2",
                table: "ParametrosLineaCredito",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "ParametrosLineaCredito",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "PesoRedNivel0", "PesoRedNivel1", "PesoRedNivel2" },
                values: new object[] { 100m, 50m, 25m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PesoRedNivel0",
                table: "ParametrosLineaCredito");

            migrationBuilder.DropColumn(
                name: "PesoRedNivel1",
                table: "ParametrosLineaCredito");

            migrationBuilder.DropColumn(
                name: "PesoRedNivel2",
                table: "ParametrosLineaCredito");
        }
    }
}

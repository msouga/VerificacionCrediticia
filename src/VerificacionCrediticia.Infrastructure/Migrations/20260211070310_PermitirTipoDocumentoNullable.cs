using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VerificacionCrediticia.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PermitirTipoDocumentoNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumentoId",
                table: "DocumentosProcesados",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TipoDocumentoId",
                table: "DocumentosProcesados",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

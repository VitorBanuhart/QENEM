using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qenem.Migrations
{
    /// <inheritdoc />
    public partial class simulado2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "RespostasUsuario",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "RespostasUsuario");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace qenem.Migrations
{
    /// <inheritdoc />
    public partial class AddSimuladoIdToRespostaUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RespostasUsuario_Simulados_SimuladoId",
                table: "RespostasUsuario");

            migrationBuilder.AlterColumn<int>(
                name: "SimuladoId",
                table: "RespostasUsuario",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RespostasUsuario_Simulados_SimuladoId",
                table: "RespostasUsuario",
                column: "SimuladoId",
                principalTable: "Simulados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RespostasUsuario_Simulados_SimuladoId",
                table: "RespostasUsuario");

            migrationBuilder.AlterColumn<int>(
                name: "SimuladoId",
                table: "RespostasUsuario",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_RespostasUsuario_Simulados_SimuladoId",
                table: "RespostasUsuario",
                column: "SimuladoId",
                principalTable: "Simulados",
                principalColumn: "Id");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ion8600ClockSync.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medidor",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    porta = table.Column<int>(type: "int", nullable: false),
                    endereco_dnp = table.Column<int>(type: "int", nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false),
                    dnp_habilitado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medidor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sincronismo_historico",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    id_medidor = table.Column<int>(type: "int", nullable: false),
                    data_hora_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    sucesso = table.Column<bool>(type: "bit", nullable: false),
                    delay_medido_ms = table.Column<int>(type: "int", nullable: true),
                    horario_enviado = table.Column<DateTime>(type: "datetime2", nullable: true),
                    horario_medidor_apos = table.Column<DateTime>(type: "datetime2", nullable: true),
                    desvio_ms = table.Column<long>(type: "bigint", nullable: true),
                    mensagem_erro = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sincronismo_historico", x => x.id);
                    table.ForeignKey(
                        name: "FK_sincronismo_historico_medidor",
                        column: x => x.id_medidor,
                        principalTable: "medidor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sincronismo_historico_id_medidor",
                table: "sincronismo_historico",
                column: "id_medidor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sincronismo_historico");

            migrationBuilder.DropTable(
                name: "medidor");
        }
    }
}

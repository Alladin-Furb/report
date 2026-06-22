using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransporteEscolar.Relatorios.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMariaDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "alunos_snapshot",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ExternalId = table.Column<long>(type: "bigint", nullable: false),
                    Nome = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alunos_snapshot", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "rotas_historicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    DistanciaKm = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    QuantidadeAlunosTransportados = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rotas_historicas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "solicitacoes_relatorio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ano = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    ProfileIdSolicitante = table.Column<long>(type: "bigint", nullable: true),
                    PapelSolicitante = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultadoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Erro = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tentativas = table.Column<int>(type: "int", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IniciadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConcluidoEm = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_solicitacoes_relatorio", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "presencas_historicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AlunoId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    ConfirmouPresenca = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CancelouPresenca = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataConfirmacao = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DataCancelamento = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EnderecoUtilizado = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_presencas_historicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_presencas_historicas_alunos_snapshot_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "alunos_snapshot",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_alunos_snapshot_ExternalId",
                table: "alunos_snapshot",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_presencas_historicas_AlunoId_Data",
                table: "presencas_historicas",
                columns: new[] { "AlunoId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_relatorio_CriadoEm",
                table: "solicitacoes_relatorio",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_relatorio_ProfileIdSolicitante_CriadoEm",
                table: "solicitacoes_relatorio",
                columns: new[] { "ProfileIdSolicitante", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_solicitacoes_relatorio_Status",
                table: "solicitacoes_relatorio",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "presencas_historicas");

            migrationBuilder.DropTable(
                name: "rotas_historicas");

            migrationBuilder.DropTable(
                name: "solicitacoes_relatorio");

            migrationBuilder.DropTable(
                name: "alunos_snapshot");
        }
    }
}

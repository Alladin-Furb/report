using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

#nullable disable

namespace TransporteEscolar.Relatorios.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RelatoriosDbContext))]
[Migration("20260619000000_AddAsyncReports")]
public partial class AddAsyncReports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "solicitacoes_relatorio",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Ano = table.Column<int>(type: "integer", nullable: false),
                Mes = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ResultadoJson = table.Column<string>(type: "jsonb", nullable: true),
                Erro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                Tentativas = table.Column<int>(type: "integer", nullable: false),
                CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                IniciadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ConcluidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_solicitacoes_relatorio", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_solicitacoes_relatorio_CriadoEm",
            table: "solicitacoes_relatorio",
            column: "CriadoEm");

        migrationBuilder.CreateIndex(
            name: "IX_solicitacoes_relatorio_Status",
            table: "solicitacoes_relatorio",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_alunos_snapshot_ExternalId",
            table: "alunos_snapshot",
            column: "ExternalId",
            unique: true,
            filter: "\"ExternalId\" <> 0");

        migrationBuilder.Sql(
            """
            DELETE FROM "presencas_historicas" a
            USING "presencas_historicas" b
            WHERE a.ctid < b.ctid
              AND a."AlunoId" = b."AlunoId"
              AND a."Data" = b."Data";
            """);

        migrationBuilder.CreateIndex(
            name: "IX_presencas_historicas_AlunoId_Data",
            table: "presencas_historicas",
            columns: new[] { "AlunoId", "Data" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "solicitacoes_relatorio");

        migrationBuilder.DropIndex(
            name: "IX_alunos_snapshot_ExternalId",
            table: "alunos_snapshot");

        migrationBuilder.DropIndex(
            name: "IX_presencas_historicas_AlunoId_Data",
            table: "presencas_historicas");
    }
}

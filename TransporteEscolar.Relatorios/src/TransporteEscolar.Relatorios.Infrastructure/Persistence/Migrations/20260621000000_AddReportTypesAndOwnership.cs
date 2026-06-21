using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

#nullable disable

namespace TransporteEscolar.Relatorios.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RelatoriosDbContext))]
[Migration("20260621000000_AddReportTypesAndOwnership")]
public partial class AddReportTypesAndOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Tipo",
            table: "solicitacoes_relatorio",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "ResumoMensal");

        migrationBuilder.AddColumn<long>(
            name: "ProfileIdSolicitante",
            table: "solicitacoes_relatorio",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PapelSolicitante",
            table: "solicitacoes_relatorio",
            type: "character varying(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "ROLE_ADMIN");

        migrationBuilder.CreateIndex(
            name: "IX_solicitacoes_relatorio_ProfileIdSolicitante_CriadoEm",
            table: "solicitacoes_relatorio",
            columns: new[] { "ProfileIdSolicitante", "CriadoEm" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_solicitacoes_relatorio_ProfileIdSolicitante_CriadoEm",
            table: "solicitacoes_relatorio");
        migrationBuilder.DropColumn(name: "Tipo", table: "solicitacoes_relatorio");
        migrationBuilder.DropColumn(name: "ProfileIdSolicitante", table: "solicitacoes_relatorio");
        migrationBuilder.DropColumn(name: "PapelSolicitante", table: "solicitacoes_relatorio");
    }
}

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

#nullable disable

namespace TransporteEscolar.Relatorios.Infrastructure.Persistence.Migrations;

[DbContext(typeof(RelatoriosDbContext))]
[Migration("20260623031500_RepairLegacyMariaDbSchema")]
public sealed class RepairLegacyMariaDbSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // O primeiro deploy MariaDB reutilizou um banco criado antes de o modelo
        // assíncrono completo existir. IF NOT EXISTS mantém esta correção segura
        // tanto para bancos legados quanto para instalações novas.
        migrationBuilder.Sql(
            """
            ALTER TABLE `alunos_snapshot`
                ADD COLUMN IF NOT EXISTS `ExternalId` BIGINT NOT NULL DEFAULT 0;
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE `solicitacoes_relatorio`
                ADD COLUMN IF NOT EXISTS `Tipo` VARCHAR(30) NOT NULL DEFAULT 'ResumoMensal',
                ADD COLUMN IF NOT EXISTS `ProfileIdSolicitante` BIGINT NULL,
                ADD COLUMN IF NOT EXISTS `PapelSolicitante` VARCHAR(30) NOT NULL DEFAULT 'ROLE_ADMIN';
            """);

        // Normaliza valores que podem ter sido gravados em formato de API.
        migrationBuilder.Sql(
            """
            UPDATE `solicitacoes_relatorio`
            SET `Tipo` = CASE UPPER(`Tipo`)
                WHEN 'RESUMO_MENSAL' THEN 'ResumoMensal'
                WHEN 'FREQUENCIA_ALUNOS' THEN 'FrequenciaAlunos'
                WHEN 'PRESENCAS_DETALHADAS' THEN 'PresencasDetalhadas'
                WHEN 'DESEMPENHO_ROTAS' THEN 'DesempenhoRotas'
                WHEN 'FREQUENCIA_PROPRIA' THEN 'FrequenciaPropria'
                ELSE `Tipo`
            END;
            """);

        migrationBuilder.Sql(
            """
            UPDATE `solicitacoes_relatorio`
            SET `Status` = CASE UPPER(`Status`)
                WHEN 'PENDENTE' THEN 'Pendente'
                WHEN 'ENFILEIRADO' THEN 'Enfileirado'
                WHEN 'PROCESSANDO' THEN 'Processando'
                WHEN 'CONCLUIDO' THEN 'Concluido'
                WHEN 'ERRO' THEN 'Erro'
                ELSE `Status`
            END;
            """);

        // ExternalId=0 era o default do modelo antigo. Valores negativos
        // preservam esses snapshots e liberam o índice único para IDs reais.
        migrationBuilder.Sql("SET @external_id_repair_row = 0;");
        migrationBuilder.Sql(
            """
            UPDATE `alunos_snapshot`
            SET `ExternalId` = -(@external_id_repair_row := @external_id_repair_row + 1)
            WHERE `ExternalId` = 0;
            """);

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS `IX_alunos_snapshot_ExternalId`
            ON `alunos_snapshot` (`ExternalId`);
            """);

        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS `IX_solicitacoes_relatorio_ProfileIdSolicitante_CriadoEm`
            ON `solicitacoes_relatorio` (`ProfileIdSolicitante`, `CriadoEm`);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Migração de reparo intencionalmente irreversível: remover colunas
        // restauraria o estado que causava HTTP 500 e poderia apagar dados.
    }
}

using System.Text.Json;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.DTOs;

public class SolicitacaoRelatorioDto
{
    public Guid RelatorioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string UrlConsulta { get; set; } = string.Empty;
}

public class CriarSolicitacaoRelatorioDto
{
    public string Tipo { get; set; } = string.Empty;
    public int Ano { get; set; }
    public int Mes { get; set; }
}

public class ConsultaRelatorioDto
{
    public Guid RelatorioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public Guid? ProfileIdSolicitante { get; set; }
    public int Ano { get; set; }
    public int Mes { get; set; }
    public string Status { get; set; } = string.Empty;
    public JsonElement? Resultado { get; set; }
    public string? Erro { get; set; }
    public int Tentativas { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }

    public static ConsultaRelatorioDto FromEntity(
        SolicitacaoRelatorio solicitacao)
    {
        JsonElement? resultado = null;
        if (!string.IsNullOrWhiteSpace(solicitacao.ResultadoJson))
        {
            using var document = JsonDocument.Parse(solicitacao.ResultadoJson);
            resultado = document.RootElement.Clone();
        }

        return new ConsultaRelatorioDto
        {
            RelatorioId = solicitacao.Id,
            Tipo = TipoRelatorioTexto.ParaApi(solicitacao.Tipo),
            ProfileIdSolicitante = solicitacao.ProfileIdSolicitante,
            Ano = solicitacao.Ano,
            Mes = solicitacao.Mes,
            Status = solicitacao.Status.ToString().ToUpperInvariant(),
            Resultado = resultado,
            Erro = solicitacao.Erro,
            Tentativas = solicitacao.Tentativas,
            CriadoEm = solicitacao.CriadoEm,
            AtualizadoEm = solicitacao.AtualizadoEm,
            ConcluidoEm = solicitacao.ConcluidoEm
        };
    }
}

public static class TipoRelatorioTexto
{
    public static string ParaApi(TipoRelatorio tipo) => tipo switch
    {
        TipoRelatorio.ResumoMensal => "RESUMO_MENSAL",
        TipoRelatorio.FrequenciaAlunos => "FREQUENCIA_ALUNOS",
        TipoRelatorio.PresencasDetalhadas => "PRESENCAS_DETALHADAS",
        TipoRelatorio.DesempenhoRotas => "DESEMPENHO_ROTAS",
        TipoRelatorio.FrequenciaPropria => "FREQUENCIA_PROPRIA",
        _ => throw new ArgumentOutOfRangeException(nameof(tipo))
    };

    public static bool TentarConverter(string? valor, out TipoRelatorio tipo)
    {
        tipo = valor?.Trim().ToUpperInvariant() switch
        {
            "RESUMO_MENSAL" => TipoRelatorio.ResumoMensal,
            "FREQUENCIA_ALUNOS" => TipoRelatorio.FrequenciaAlunos,
            "PRESENCAS_DETALHADAS" => TipoRelatorio.PresencasDetalhadas,
            "DESEMPENHO_ROTAS" => TipoRelatorio.DesempenhoRotas,
            "FREQUENCIA_PROPRIA" => TipoRelatorio.FrequenciaPropria,
            _ => (TipoRelatorio)(-1)
        };
        return Enum.IsDefined(tipo);
    }
}

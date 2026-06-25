using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Exceptions;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Infrastructure.Exporting;

public class ExportadorRelatorioService : IExportadorRelatorioService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public ExportadorRelatorioService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public ArquivoRelatorioDto Exportar(SolicitacaoRelatorio solicitacao, string formato)
    {
        if (string.IsNullOrWhiteSpace(solicitacao.ResultadoJson))
            throw new ConflictException("O relatório não possui resultado para exportação.");

        var modelo = CriarModelo(solicitacao);
        var nomeBase = $"{TipoRelatorioTexto.ParaApi(solicitacao.Tipo).ToLowerInvariant()}-{solicitacao.Ano}-{solicitacao.Mes:00}";

        return formato.Trim().ToLowerInvariant() switch
        {
            "pdf" => new ArquivoRelatorioDto
            {
                Conteudo = GerarPdf(modelo),
                ContentType = "application/pdf",
                NomeArquivo = $"{nomeBase}.pdf"
            },
            "csv" => new ArquivoRelatorioDto
            {
                Conteudo = GerarCsv(modelo),
                ContentType = "text/csv; charset=utf-8",
                NomeArquivo = $"{nomeBase}.csv"
            },
            _ => throw new BusinessException("Formato inválido. Use pdf ou csv.")
        };
    }

    private static ModeloExportacao CriarModelo(SolicitacaoRelatorio solicitacao)
    {
        var json = solicitacao.ResultadoJson!;
        var modelo = solicitacao.Tipo switch
        {
            TipoRelatorio.ResumoMensal => ModeloResumo(
                Desserializar<RelatorioMensalDto>(json)),
            TipoRelatorio.FrequenciaAlunos => ModeloFrequencias(
                "Relatório de frequência dos alunos",
                Desserializar<FrequenciaAlunosRelatorioDto>(json).Alunos),
            TipoRelatorio.FrequenciaPropria => ModeloFrequencias(
                "Relatório da minha frequência",
                [Desserializar<FrequenciaPropriaRelatorioDto>(json).Aluno]),
            TipoRelatorio.PresencasDetalhadas => ModeloPresencas(
                Desserializar<PresencasDetalhadasRelatorioDto>(json)),
            TipoRelatorio.DesempenhoRotas => ModeloRotas(
                Desserializar<DesempenhoRotasRelatorioDto>(json)),
            _ => throw new BusinessException("Tipo de relatório inválido.")
        };
        return modelo with { Periodo = $"{solicitacao.Mes:00}/{solicitacao.Ano}" };
    }

    private static ModeloExportacao ModeloResumo(RelatorioMensalDto item) => new(
        "Resumo mensal do transporte escolar",
        [
            $"Confirmações: {item.TotalConfirmacoes}",
            $"Cancelamentos: {item.TotalCancelamentos}",
            $"Rotas: {item.TotalRotas}",
            $"Média diária: {item.MediaKmPorDia.ToString("N2", PtBr)} km"
        ],
        ["Indicador", "Valor"],
        [
            ["Confirmações", item.TotalConfirmacoes.ToString(PtBr)],
            ["Cancelamentos", item.TotalCancelamentos.ToString(PtBr)],
            ["Total de rotas", item.TotalRotas.ToString(PtBr)],
            ["Média diária de quilômetros", item.MediaKmPorDia.ToString("N2", PtBr)]
        ]);

    private static ModeloExportacao ModeloFrequencias(
        string titulo,
        IEnumerable<FrequenciaAlunoDto> itens)
    {
        var lista = itens.ToList();
        return new ModeloExportacao(
            titulo,
            [$"Alunos no relatório: {lista.Count}"],
            ["Nome", "ID externo", "Dias confirmados", "Dias cancelados", "Frequência (%)"],
            lista.Select(x => new[]
            {
                x.NomeAluno,
                x.AlunoExternalId.ToString(),
                x.DiasConfirmados.ToString(PtBr),
                x.DiasCancelados.ToString(PtBr),
                x.PercentualFrequencia.ToString("N2", PtBr)
            }).ToList());
    }

    private static ModeloExportacao ModeloPresencas(PresencasDetalhadasRelatorioDto relatorio) => new(
        "Relatório de presenças detalhadas",
        [$"Registros no período: {relatorio.Presencas.Count}"],
        ["Aluno", "ID externo", "Data", "Situação", "Confirmação", "Cancelamento", "Endereço"],
        relatorio.Presencas.Select(x => new[]
        {
            x.NomeAluno,
            x.AlunoExternalId.ToString(),
            x.Data.ToString("dd/MM/yyyy", PtBr),
            x.Situacao,
            FormatarDataHora(x.DataConfirmacao),
            FormatarDataHora(x.DataCancelamento),
            x.EnderecoUtilizado ?? string.Empty
        }).ToList());

    private static ModeloExportacao ModeloRotas(DesempenhoRotasRelatorioDto relatorio) => new(
        "Relatório de desempenho das rotas",
        [
            $"Distância total: {relatorio.DistanciaTotalKm.ToString("N2", PtBr)} km",
            $"Média por rota: {relatorio.MediaKmPorRota.ToString("N2", PtBr)} km",
            $"Alunos transportados: {relatorio.TotalAlunosTransportados}"
        ],
        ["Data", "Distância (km)", "Alunos transportados"],
        relatorio.Rotas.Select(x => new[]
        {
            x.Data.ToString("dd/MM/yyyy", PtBr),
            x.DistanciaKm.ToString("N2", PtBr),
            x.AlunosTransportados.ToString(PtBr)
        }).ToList());

    private static byte[] GerarCsv(ModeloExportacao modelo)
    {
        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            bufferSize: 1024,
            leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(PtBr)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            NewLine = "\r\n"
        }))
        {
            foreach (var cabecalho in modelo.Cabecalhos)
                csv.WriteField(cabecalho);
            csv.NextRecord();

            foreach (var linha in modelo.Linhas)
            {
                foreach (var valor in linha)
                    csv.WriteField(valor);
                csv.NextRecord();
            }
        }
        return stream.ToArray();
    }

    private static byte[] GerarPdf(ModeloExportacao modelo)
    {
        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Header().Column(header =>
                {
                    header.Item().Text(modelo.Titulo).SemiBold().FontSize(18).FontColor(Colors.Blue.Darken3);
                    header.Item().Text($"Período: {modelo.Periodo} · Emitido em {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);
                });
                page.Content().PaddingTop(18).Column(column =>
                {
                    column.Spacing(12);
                    if (modelo.Indicadores.Count > 0)
                    {
                        column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(indicadores =>
                        {
                            foreach (var indicador in modelo.Indicadores)
                                indicadores.Item().Text(indicador);
                        });
                    }
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in modelo.Cabecalhos)
                                columns.RelativeColumn();
                        });
                        table.Header(header =>
                        {
                            foreach (var cabecalho in modelo.Cabecalhos)
                                header.Cell().Background(Colors.Blue.Darken3).Padding(5)
                                    .Text(cabecalho).FontColor(Colors.White).SemiBold();
                        });
                        foreach (var linha in modelo.Linhas)
                        {
                            foreach (var valor in linha)
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text(valor ?? string.Empty);
                        }
                    });
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Página ");
                    text.CurrentPageNumber();
                });
            });
        }).GeneratePdf();
    }

    private static T Desserializar<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException("Resultado JSON inválido.");

    private static string FormatarDataHora(DateTime? valor) =>
        valor?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", PtBr) ?? string.Empty;

    private sealed record ModeloExportacao(
        string Titulo,
        IReadOnlyCollection<string> Indicadores,
        IReadOnlyCollection<string> Cabecalhos,
        IReadOnlyCollection<string[]> Linhas)
    {
        public string Periodo { get; init; } = string.Empty;
    }
}

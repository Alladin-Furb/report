using System.Text;
using System.Text.Json;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Services;
using TransporteEscolar.Relatorios.Domain.Entities;
using TransporteEscolar.Relatorios.Infrastructure.Exporting;

namespace TransporteEscolar.Relatorios.UnitTests;

public class NovosRelatoriosTests
{
    private static readonly Guid AlunoExternalId = Guid.Parse("00000000-0000-0000-0000-000000000007");

    [Theory]
    [InlineData(TipoRelatorio.ResumoMensal, typeof(RelatorioMensalDto))]
    [InlineData(TipoRelatorio.FrequenciaAlunos, typeof(FrequenciaAlunosRelatorioDto))]
    [InlineData(TipoRelatorio.PresencasDetalhadas, typeof(PresencasDetalhadasRelatorioDto))]
    [InlineData(TipoRelatorio.DesempenhoRotas, typeof(DesempenhoRotasRelatorioDto))]
    [InlineData(TipoRelatorio.FrequenciaPropria, typeof(FrequenciaPropriaRelatorioDto))]
    public async Task Gerador_GeraOsCincoTipos(TipoRelatorio tipo, Type tipoEsperado)
    {
        var gerador = CriarGerador();
        var resultado = await gerador.GerarAsync(new SolicitacaoRelatorio
        {
            Tipo = tipo,
            Ano = 2026,
            Mes = 6,
            ProfileIdSolicitante = AlunoExternalId
        });

        Assert.IsType(tipoEsperado, resultado);
    }

    [Fact]
    public void Exportador_GeraPdfComAssinaturaValida()
    {
        var arquivo = new ExportadorRelatorioService().Exportar(
            NovaSolicitacaoConcluida(), "pdf");

        Assert.Equal("%PDF", Encoding.ASCII.GetString(arquivo.Conteudo, 0, 4));
        Assert.Equal("application/pdf", arquivo.ContentType);
    }

    [Fact]
    public void Exportador_GeraCsvComBomCabecalhoELinhas()
    {
        var arquivo = new ExportadorRelatorioService().Exportar(
            NovaSolicitacaoConcluida(), "csv");
        var texto = Encoding.UTF8.GetString(arquivo.Conteudo);

        Assert.Equal([0xEF, 0xBB, 0xBF], arquivo.Conteudo[..3]);
        Assert.Contains("Indicador;Valor", texto);
        Assert.Equal(5, texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
    }

    private static GeradorRelatorioService CriarGerador()
    {
        var aluno = new AlunoSnapshot
        {
            Id = Guid.NewGuid(),
            ExternalId = AlunoExternalId,
            Nome = "Helena",
            Ativo = true
        };
        var presenca = new PresencaHistorica
        {
            Id = Guid.NewGuid(),
            AlunoId = aluno.Id,
            Data = new DateOnly(2026, 6, 10),
            ConfirmouPresenca = true
        };
        var rota = new RotaHistorica
        {
            Id = Guid.NewGuid(),
            Data = new DateOnly(2026, 6, 10),
            DistanciaKm = 12.5m,
            QuantidadeAlunosTransportados = 1
        };
        var alunos = new AlunosFake(aluno);
        var presencas = new PresencasFake(presenca);
        return new GeradorRelatorioService(
            new MensalFake(),
            new FrequenciaAlunoService(presencas, alunos),
            presencas,
            new RotasFake(rota),
            alunos);
    }

    private static SolicitacaoRelatorio NovaSolicitacaoConcluida() => new()
    {
        Id = Guid.NewGuid(),
        Tipo = TipoRelatorio.ResumoMensal,
        Ano = 2026,
        Mes = 6,
        Status = StatusSolicitacaoRelatorio.Concluido,
        ResultadoJson = JsonSerializer.Serialize(new RelatorioMensalDto
        {
            Ano = 2026,
            Mes = 6,
            TotalConfirmacoes = 10,
            TotalCancelamentos = 2,
            TotalRotas = 4,
            MediaKmPorDia = 12.5m
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web))
    };

    private sealed class MensalFake : IRelatorioMensalService
    {
        public Task<RelatorioMensalDto> GerarAsync(
            int ano, int mes, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RelatorioMensalDto { Ano = ano, Mes = mes });
    }

    private sealed class AlunosFake(params AlunoSnapshot[] itens) : IAlunoSnapshotRepository
    {
        public Task<AlunoSnapshot?> ObterPorIdAsync(Guid alunoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(itens.FirstOrDefault(x => x.Id == alunoId));
        public Task<IReadOnlyCollection<AlunoSnapshot>> ObterTodosAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<AlunoSnapshot>>(itens);
        public Task<AlunoSnapshot?> BuscarPorExternalIdAsync(Guid externalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(itens.FirstOrDefault(x => x.ExternalId == externalId));
        public Task AdicionarAsync(AlunoSnapshot aluno, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AtualizarAsync(AlunoSnapshot aluno, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class PresencasFake(params PresencaHistorica[] itens) : IPresencaHistoricaRepository
    {
        public Task<IReadOnlyCollection<PresencaHistorica>> ObterPorPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PresencaHistorica>>(itens);
        public Task<IReadOnlyCollection<PresencaHistorica>> ObterPorAlunoEPeriodoAsync(
            Guid alunoId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<PresencaHistorica>>(itens.Where(x => x.AlunoId == alunoId).ToList());
        public Task<bool> ExistePorAlunoEDataAsync(Guid alunoId, DateOnly data, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
        public Task AdicionarAsync(PresencaHistorica presenca, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class RotasFake(params RotaHistorica[] itens) : IRotaHistoricaRepository
    {
        public Task<IReadOnlyCollection<RotaHistorica>> ObterPorPeriodoAsync(
            DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<RotaHistorica>>(itens);
    }
}

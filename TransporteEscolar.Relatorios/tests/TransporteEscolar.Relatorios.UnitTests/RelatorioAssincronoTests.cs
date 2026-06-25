using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Exceptions;
using TransporteEscolar.Relatorios.Application.Services;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.UnitTests;

public class RelatorioAssincronoTests
{
    [Fact]
    public async Task SolicitarAsync_PersisteSolicitacaoPendente()
    {
        var repository = new SolicitacaoRepositoryFake();
        var service = new SolicitacaoRelatorioService(repository, new CacheFake(), new ExportadorFake());

        var resposta = await service.SolicitarAsync(
            TipoRelatorio.ResumoMensal, 2026, 6, "ROLE_ADMIN", null, "/api/relatorios/id");

        var persistida = await repository.ObterPorIdAsync(resposta.RelatorioId);
        Assert.NotNull(persistida);
        Assert.Equal(StatusSolicitacaoRelatorio.Pendente, persistida.Status);
        Assert.Equal("PENDENTE", resposta.Status);
    }

    [Fact]
    public async Task SolicitarAsync_RejeitaPeriodoInvalido()
    {
        var service = new SolicitacaoRelatorioService(
            new SolicitacaoRepositoryFake(),
            new CacheFake(),
            new ExportadorFake());

        await Assert.ThrowsAsync<BusinessException>(
            () => service.SolicitarAsync(
                TipoRelatorio.ResumoMensal, 2026, 13, "ROLE_ADMIN", null, string.Empty));
    }

    [Fact]
    public async Task ConsultarAsync_UsaCacheAntesDoBanco()
    {
        var id = Guid.NewGuid();
        var cache = new CacheFake
        {
            Valor = new ConsultaRelatorioDto
            {
                RelatorioId = id,
                Status = "CONCLUIDO"
            }
        };
        var service = new SolicitacaoRelatorioService(
            new SolicitacaoRepositoryFake(),
            cache,
            new ExportadorFake());

        var resposta = await service.ConsultarAsync(id, "ROLE_ADMIN", null);

        Assert.Equal("CONCLUIDO", resposta.Status);
    }

    [Fact]
    public async Task ProcessarAsync_GeraResultadoESeTornaIdempotente()
    {
        var solicitacao = NovaSolicitacao();
        var repository = new SolicitacaoRepositoryFake(solicitacao);
        var gerador = new GeradorFake();
        var processador = new ProcessadorRelatorioService(
            repository,
            gerador,
            new CacheFake());

        await processador.ProcessarAsync(solicitacao.Id);
        await processador.ProcessarAsync(solicitacao.Id);

        Assert.Equal(1, gerador.Chamadas);
        Assert.Equal(StatusSolicitacaoRelatorio.Concluido, solicitacao.Status);
        Assert.NotNull(solicitacao.ResultadoJson);
        Assert.Equal(1, solicitacao.Tentativas);
    }

    [Fact]
    public async Task ProcessarAsync_MarcaErroNaTerceiraFalha()
    {
        var solicitacao = NovaSolicitacao();
        var repository = new SolicitacaoRepositoryFake(solicitacao);
        var processador = new ProcessadorRelatorioService(
            repository,
            new GeradorFake { Falhar = true },
            new CacheFake());

        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => processador.ProcessarAsync(solicitacao.Id));
        }

        Assert.Equal(3, solicitacao.Tentativas);
        Assert.Equal(StatusSolicitacaoRelatorio.Erro, solicitacao.Status);
        Assert.Equal("Falha simulada", solicitacao.Erro);
    }

    [Fact]
    public async Task ConsultarAsync_RetornaNotFoundParaIdInexistente()
    {
        var service = new SolicitacaoRelatorioService(
            new SolicitacaoRepositoryFake(),
            new CacheFake(),
            new ExportadorFake());

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ConsultarAsync(Guid.NewGuid(), "ROLE_ADMIN", null));
    }

    private static SolicitacaoRelatorio NovaSolicitacao()
    {
        return new SolicitacaoRelatorio
        {
            Id = Guid.NewGuid(),
            Ano = 2026,
            Mes = 6,
            Tipo = TipoRelatorio.ResumoMensal,
            PapelSolicitante = "ROLE_ADMIN",
            Status = StatusSolicitacaoRelatorio.Enfileirado,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow
        };
    }

    private sealed class SolicitacaoRepositoryFake : ISolicitacaoRelatorioRepository
    {
        private readonly Dictionary<Guid, SolicitacaoRelatorio> _itens = [];

        public SolicitacaoRepositoryFake(params SolicitacaoRelatorio[] solicitacoes)
        {
            foreach (var solicitacao in solicitacoes)
                _itens[solicitacao.Id] = solicitacao;
        }

        public Task AdicionarAsync(
            SolicitacaoRelatorio solicitacao,
            CancellationToken cancellationToken = default)
        {
            _itens[solicitacao.Id] = solicitacao;
            return Task.CompletedTask;
        }

        public Task<SolicitacaoRelatorio?> ObterPorIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_itens.GetValueOrDefault(id));
        }

        public Task<bool> TentarIniciarProcessamentoAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!_itens.TryGetValue(id, out var solicitacao) ||
                solicitacao.Status is StatusSolicitacaoRelatorio.Concluido
                    or StatusSolicitacaoRelatorio.Processando ||
                solicitacao.Tentativas >= 3)
            {
                return Task.FromResult(false);
            }

            solicitacao.Status = StatusSolicitacaoRelatorio.Processando;
            solicitacao.Tentativas++;
            solicitacao.IniciadoEm ??= DateTime.UtcNow;
            solicitacao.AtualizadoEm = DateTime.UtcNow;
            solicitacao.Erro = null;
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<SolicitacaoRelatorio>> ListarRecentesAsync(
            string papelSolicitante,
            Guid? profileIdSolicitante,
            int limite,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<SolicitacaoRelatorio> resultado = _itens.Values
                .OrderByDescending(x => x.CriadoEm)
                .Take(limite)
                .ToList();
            return Task.FromResult(resultado);
        }

        public Task<IReadOnlyCollection<SolicitacaoRelatorio>> ObterParaEnfileirarAsync(
            int limite,
            DateTime reenfileirarAntesDe,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<SolicitacaoRelatorio> resultado = _itens.Values
                .Take(limite)
                .ToList();
            return Task.FromResult(resultado);
        }

        public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class CacheFake : IRelatorioCacheService
    {
        public ConsultaRelatorioDto? Valor { get; set; }

        public Task<ConsultaRelatorioDto?> ObterAsync(
            Guid relatorioId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Valor);

        public Task ArmazenarAsync(
            ConsultaRelatorioDto relatorio,
            CancellationToken cancellationToken = default)
        {
            Valor = relatorio;
            return Task.CompletedTask;
        }
    }

    private sealed class GeradorFake : IGeradorRelatorioService
    {
        public int Chamadas { get; private set; }
        public bool Falhar { get; set; }

        public Task<object> GerarAsync(
            SolicitacaoRelatorio solicitacao,
            CancellationToken cancellationToken = default)
        {
            Chamadas++;
            if (Falhar)
                throw new InvalidOperationException("Falha simulada");

            return Task.FromResult<object>(new RelatorioMensalDto
            {
                Ano = solicitacao.Ano,
                Mes = solicitacao.Mes,
                TotalConfirmacoes = 10,
                TotalCancelamentos = 2,
                MediaKmPorDia = 15.5m,
                TotalRotas = 4
            });
        }
    }

    private sealed class ExportadorFake : IExportadorRelatorioService
    {
        public ArquivoRelatorioDto Exportar(SolicitacaoRelatorio solicitacao, string formato)
            => new()
            {
                Conteudo = [1, 2, 3],
                ContentType = formato == "pdf" ? "application/pdf" : "text/csv",
                NomeArquivo = $"relatorio.{formato}"
            };
    }
}

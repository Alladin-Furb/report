using System.Text.Json;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Services;

public class ProcessadorRelatorioService : IProcessadorRelatorioService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ISolicitacaoRelatorioRepository _repository;
    private readonly IGeradorRelatorioService _geradorRelatorioService;
    private readonly IRelatorioCacheService _cache;

    public ProcessadorRelatorioService(
        ISolicitacaoRelatorioRepository repository,
        IGeradorRelatorioService geradorRelatorioService,
        IRelatorioCacheService cache)
    {
        _repository = repository;
        _geradorRelatorioService = geradorRelatorioService;
        _cache = cache;
    }

    public async Task ProcessarAsync(
        Guid relatorioId,
        CancellationToken cancellationToken = default)
    {
        var iniciou = await _repository.TentarIniciarProcessamentoAsync(
            relatorioId,
            cancellationToken);
        if (!iniciou)
            return;

        var solicitacao = await _repository.ObterPorIdAsync(relatorioId, cancellationToken)
            ?? throw new InvalidOperationException("Solicitação desapareceu durante o processamento.");

        try
        {
            var resultado = await _geradorRelatorioService.GerarAsync(
                solicitacao,
                cancellationToken);

            solicitacao.ResultadoJson = JsonSerializer.Serialize(resultado, JsonOptions);
            solicitacao.Status = StatusSolicitacaoRelatorio.Concluido;
            solicitacao.ConcluidoEm = DateTime.UtcNow;
            solicitacao.AtualizadoEm = DateTime.UtcNow;
            solicitacao.Erro = null;
            await _repository.SalvarAlteracoesAsync(cancellationToken);

            await _cache.ArmazenarAsync(
                ConsultaRelatorioDto.FromEntity(solicitacao),
                cancellationToken);
        }
        catch (Exception ex)
        {
            solicitacao.Erro = ex.Message;
            solicitacao.Status = solicitacao.Tentativas >= 3
                ? StatusSolicitacaoRelatorio.Erro
                : StatusSolicitacaoRelatorio.Enfileirado;
            solicitacao.AtualizadoEm = DateTime.UtcNow;
            await _repository.SalvarAlteracoesAsync(cancellationToken);
            throw;
        }
    }
}

using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface ISolicitacaoRelatorioRepository
{
    Task AdicionarAsync(
        SolicitacaoRelatorio solicitacao,
        CancellationToken cancellationToken = default);

    Task<SolicitacaoRelatorio?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SolicitacaoRelatorio>> ListarRecentesAsync(
        string papelSolicitante,
        Guid? profileIdSolicitante,
        int limite,
        CancellationToken cancellationToken = default);

    Task<bool> TentarIniciarProcessamentoAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SolicitacaoRelatorio>> ObterParaEnfileirarAsync(
        int limite,
        DateTime reenfileirarAntesDe,
        CancellationToken cancellationToken = default);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}

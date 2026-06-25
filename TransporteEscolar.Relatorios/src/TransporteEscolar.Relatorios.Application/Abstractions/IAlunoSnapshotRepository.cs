using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface IAlunoSnapshotRepository
{
    Task<AlunoSnapshot?> ObterPorIdAsync(
        Guid alunoId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AlunoSnapshot>> ObterTodosAsync(
        CancellationToken cancellationToken = default);

    Task<AlunoSnapshot?> BuscarPorExternalIdAsync(
        Guid externalId,
        CancellationToken cancellationToken = default);

    Task AdicionarAsync(
        AlunoSnapshot aluno,
        CancellationToken cancellationToken = default);

    Task AtualizarAsync(
        AlunoSnapshot aluno,
        CancellationToken cancellationToken = default);
}
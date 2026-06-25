using Microsoft.EntityFrameworkCore;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Domain.Entities;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

namespace TransporteEscolar.Relatorios.Infrastructure.Persistence.Repositories;

public class SolicitacaoRelatorioRepository : ISolicitacaoRelatorioRepository
{
    private readonly RelatoriosDbContext _context;

    public SolicitacaoRelatorioRepository(RelatoriosDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(
        SolicitacaoRelatorio solicitacao,
        CancellationToken cancellationToken = default)
    {
        _context.SolicitacoesRelatorio.Add(solicitacao);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<SolicitacaoRelatorio?> ObterPorIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.SolicitacoesRelatorio
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<SolicitacaoRelatorio>> ListarRecentesAsync(
        string papelSolicitante,
        Guid? profileIdSolicitante,
        int limite,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.SolicitacoesRelatorio.AsNoTracking();
        if (papelSolicitante == "ROLE_ALUNO")
        {
            consulta = consulta.Where(x =>
                x.Tipo == TipoRelatorio.FrequenciaPropria &&
                x.ProfileIdSolicitante == profileIdSolicitante);
        }
        else
        {
            consulta = consulta.Where(x => x.Tipo != TipoRelatorio.FrequenciaPropria);
        }

        return await consulta
            .OrderByDescending(x => x.CriadoEm)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TentarIniciarProcessamentoAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var agora = DateTime.UtcNow;
        var atualizadas = await _context.SolicitacoesRelatorio
            .Where(x =>
                x.Id == id &&
                x.Status != StatusSolicitacaoRelatorio.Concluido &&
                x.Status != StatusSolicitacaoRelatorio.Processando &&
                x.Tentativas < 3)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, StatusSolicitacaoRelatorio.Processando)
                .SetProperty(x => x.Tentativas, x => x.Tentativas + 1)
                .SetProperty(x => x.IniciadoEm, x => x.IniciadoEm ?? agora)
                .SetProperty(x => x.AtualizadoEm, agora)
                .SetProperty(x => x.Erro, (string?)null),
                cancellationToken);

        return atualizadas == 1;
    }

    public async Task<IReadOnlyCollection<SolicitacaoRelatorio>> ObterParaEnfileirarAsync(
        int limite,
        DateTime reenfileirarAntesDe,
        CancellationToken cancellationToken = default)
    {
        return await _context.SolicitacoesRelatorio
            .Where(x =>
                x.Status == StatusSolicitacaoRelatorio.Pendente ||
                ((x.Status == StatusSolicitacaoRelatorio.Enfileirado ||
                  x.Status == StatusSolicitacaoRelatorio.Processando) &&
                 x.AtualizadoEm <= reenfileirarAntesDe))
            .OrderBy(x => x.CriadoEm)
            .Take(limite)
            .ToListAsync(cancellationToken);
    }

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}

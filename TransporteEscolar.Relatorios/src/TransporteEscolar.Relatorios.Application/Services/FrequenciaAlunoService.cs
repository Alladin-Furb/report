using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Exceptions;

namespace TransporteEscolar.Relatorios.Application.Services;

public class FrequenciaAlunoService : IFrequenciaAlunoService
{
    private readonly IPresencaHistoricaRepository _presencaHistoricaRepository;
    private readonly IAlunoSnapshotRepository _alunoSnapshotRepository;

    public FrequenciaAlunoService(
        IPresencaHistoricaRepository presencaHistoricaRepository,
        IAlunoSnapshotRepository alunoSnapshotRepository)
    {
        _presencaHistoricaRepository = presencaHistoricaRepository;
        _alunoSnapshotRepository = alunoSnapshotRepository;
    }

    public async Task<FrequenciaAlunoDto> CalcularAsync(
        Guid alunoId,
        int ano,
        int mes,
        CancellationToken cancellationToken = default)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);

        var aluno = await _alunoSnapshotRepository.ObterPorIdAsync(alunoId, cancellationToken);

        var presencas = await _presencaHistoricaRepository.ObterPorAlunoEPeriodoAsync(
            alunoId,
            inicio,
            fim,
            cancellationToken);

        var diasConfirmados = presencas.Count(x => x.ConfirmouPresenca);
        var diasCancelados = presencas.Count(x => x.CancelouPresenca);
        var totalRegistros = presencas.Count;

        var percentualFrequencia = totalRegistros == 0
            ? 0
            : decimal.Round((decimal)diasConfirmados / totalRegistros * 100m, 2);

        return new FrequenciaAlunoDto
        {
            AlunoId = alunoId,
            AlunoExternalId = aluno?.ExternalId ?? 0,
            NomeAluno = aluno?.Nome ?? "Aluno não encontrado",
            DiasConfirmados = diasConfirmados,
            DiasCancelados = diasCancelados,
            PercentualFrequencia = percentualFrequencia
        };
    }

    public async Task<FrequenciaAlunoDto> CalcularPorExternalIdAsync(
        long externalId,
        int ano,
        int mes,
        CancellationToken cancellationToken = default)
    {
        var aluno = await _alunoSnapshotRepository.BuscarPorExternalIdAsync(
            externalId,
            cancellationToken);

        if (aluno is null)
            throw new NotFoundException("Aluno não encontrado.");

        return await CalcularAsync(aluno.Id, ano, mes, cancellationToken);
    }

    public async Task<IReadOnlyCollection<FrequenciaAlunoDto>> CalcularTodosAsync(
        int ano,
        int mes,
        CancellationToken cancellationToken = default)
    {
        var alunos = await _alunoSnapshotRepository.ObterTodosAsync(cancellationToken);
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        var presencas = await _presencaHistoricaRepository.ObterPorPeriodoAsync(
            inicio,
            fim,
            cancellationToken);
        var porAluno = presencas
            .GroupBy(x => x.AlunoId)
            .ToDictionary(x => x.Key, x => x.ToList());

        return alunos
            .Select(aluno =>
            {
                var registros = porAluno.GetValueOrDefault(aluno.Id) ?? [];
                var confirmados = registros.Count(x => x.ConfirmouPresenca);
                var cancelados = registros.Count(x => x.CancelouPresenca);

                return new FrequenciaAlunoDto
                {
                    AlunoId = aluno.Id,
                    AlunoExternalId = aluno.ExternalId,
                    NomeAluno = aluno.Nome,
                    DiasConfirmados = confirmados,
                    DiasCancelados = cancelados,
                    PercentualFrequencia = registros.Count == 0
                        ? 0
                        : decimal.Round((decimal)confirmados / registros.Count * 100m, 2)
                };
            })
            .OrderByDescending(x => x.PercentualFrequencia)
            .ThenBy(x => x.NomeAluno)
            .ToList();
    }
}

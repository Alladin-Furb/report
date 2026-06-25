using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Exceptions;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Services;

public class GeradorRelatorioService : IGeradorRelatorioService
{
    private readonly IRelatorioMensalService _mensal;
    private readonly IFrequenciaAlunoService _frequencia;
    private readonly IPresencaHistoricaRepository _presencas;
    private readonly IRotaHistoricaRepository _rotas;
    private readonly IAlunoSnapshotRepository _alunos;

    public GeradorRelatorioService(
        IRelatorioMensalService mensal,
        IFrequenciaAlunoService frequencia,
        IPresencaHistoricaRepository presencas,
        IRotaHistoricaRepository rotas,
        IAlunoSnapshotRepository alunos)
    {
        _mensal = mensal;
        _frequencia = frequencia;
        _presencas = presencas;
        _rotas = rotas;
        _alunos = alunos;
    }

    public async Task<object> GerarAsync(
        SolicitacaoRelatorio solicitacao,
        CancellationToken cancellationToken = default)
    {
        return solicitacao.Tipo switch
        {
            TipoRelatorio.ResumoMensal => await _mensal.GerarAsync(
                solicitacao.Ano, solicitacao.Mes, cancellationToken),
            TipoRelatorio.FrequenciaAlunos => await GerarFrequenciaAsync(
                solicitacao.Ano, solicitacao.Mes, cancellationToken),
            TipoRelatorio.PresencasDetalhadas => await GerarPresencasAsync(
                solicitacao.Ano, solicitacao.Mes, cancellationToken),
            TipoRelatorio.DesempenhoRotas => await GerarRotasAsync(
                solicitacao.Ano, solicitacao.Mes, cancellationToken),
            TipoRelatorio.FrequenciaPropria => await GerarFrequenciaPropriaAsync(
                solicitacao, cancellationToken),
            _ => throw new BusinessException("Tipo de relatório inválido.")
        };
    }

    private async Task<FrequenciaAlunosRelatorioDto> GerarFrequenciaAsync(
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        return new FrequenciaAlunosRelatorioDto
        {
            Ano = ano,
            Mes = mes,
            Alunos = await _frequencia.CalcularTodosAsync(ano, mes, cancellationToken)
        };
    }

    private async Task<FrequenciaPropriaRelatorioDto> GerarFrequenciaPropriaAsync(
        SolicitacaoRelatorio solicitacao,
        CancellationToken cancellationToken)
    {
        if (solicitacao.ProfileIdSolicitante is null)
            throw new BusinessException("Relatório individual sem perfil de aluno.");

        return new FrequenciaPropriaRelatorioDto
        {
            Ano = solicitacao.Ano,
            Mes = solicitacao.Mes,
            Aluno = await _frequencia.CalcularPorExternalIdAsync(
                solicitacao.ProfileIdSolicitante.Value,
                solicitacao.Ano,
                solicitacao.Mes,
                cancellationToken)
        };
    }

    private async Task<PresencasDetalhadasRelatorioDto> GerarPresencasAsync(
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        var presencas = await _presencas.ObterPorPeriodoAsync(inicio, fim, cancellationToken);
        var alunos = (await _alunos.ObterTodosAsync(cancellationToken))
            .ToDictionary(x => x.Id);

        return new PresencasDetalhadasRelatorioDto
        {
            Ano = ano,
            Mes = mes,
            Presencas = presencas
                .OrderBy(x => x.Data)
                .ThenBy(x => alunos.GetValueOrDefault(x.AlunoId)?.Nome)
                .Select(x =>
                {
                    var aluno = alunos.GetValueOrDefault(x.AlunoId);
                    return new PresencaDetalhadaDto
                    {
                        AlunoExternalId = aluno?.ExternalId ?? Guid.Empty,
                        NomeAluno = aluno?.Nome ?? "Aluno não encontrado",
                        Data = x.Data,
                        Situacao = x.ConfirmouPresenca
                            ? "CONFIRMADA"
                            : x.CancelouPresenca ? "CANCELADA" : "PENDENTE",
                        DataConfirmacao = x.DataConfirmacao,
                        DataCancelamento = x.DataCancelamento,
                        EnderecoUtilizado = x.EnderecoUtilizado
                    };
                })
                .ToList()
        };
    }

    private async Task<DesempenhoRotasRelatorioDto> GerarRotasAsync(
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1).AddDays(-1);
        var rotas = await _rotas.ObterPorPeriodoAsync(inicio, fim, cancellationToken);
        var distanciaTotal = rotas.Sum(x => x.DistanciaKm);

        return new DesempenhoRotasRelatorioDto
        {
            Ano = ano,
            Mes = mes,
            DistanciaTotalKm = decimal.Round(distanciaTotal, 2),
            MediaKmPorRota = rotas.Count == 0
                ? 0
                : decimal.Round(distanciaTotal / rotas.Count, 2),
            TotalAlunosTransportados = rotas.Sum(x => x.QuantidadeAlunosTransportados),
            Rotas = rotas
                .OrderBy(x => x.Data)
                .Select(x => new DesempenhoRotaDto
                {
                    Data = x.Data,
                    DistanciaKm = x.DistanciaKm,
                    AlunosTransportados = x.QuantidadeAlunosTransportados
                })
                .ToList()
        };
    }
}

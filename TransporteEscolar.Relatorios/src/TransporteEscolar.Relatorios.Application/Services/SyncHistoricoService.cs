using System.Net.Http.Json;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.DTOs.Externos;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Services;

public class SyncHistoricoService : ISyncHistoricoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAlunoSnapshotRepository _alunoRepository;
    private readonly IPresencaHistoricaRepository _presencaRepository;

    public SyncHistoricoService(
        IHttpClientFactory httpClientFactory,
        IAlunoSnapshotRepository alunoRepository,
        IPresencaHistoricaRepository presencaRepository)
    {
        _httpClientFactory = httpClientFactory;
        _alunoRepository = alunoRepository;
        _presencaRepository = presencaRepository;
    }

    public async Task<SyncResultDto> ImportarPresencasAsync(
    DateOnly dataInicio,
    DateOnly dataFim,
    CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("presenca-service");

        var alunos = await _alunoRepository.ObterTodosAsync(cancellationToken);

        var processados = 0;

        foreach (var aluno in alunos)
        {
            var url = $"/api/v1/presencas/aluno/{aluno.ExternalId}/periodo?dataInicio={dataInicio:yyyy-MM-dd}&dataFim={dataFim:yyyy-MM-dd}";

            var presencas = await client.GetFromJsonAsync<List<PresencaExternaDto>>(
                url, cancellationToken) ?? [];

            foreach (var presencaExterna in presencas)
            {
                var jaExiste = await _presencaRepository.ExistePorAlunoEDataAsync(
                    aluno.Id, presencaExterna.DataPresenca, cancellationToken);

                if (jaExiste) continue;

                await _presencaRepository.AdicionarAsync(new PresencaHistorica
                {
                    Id = Guid.NewGuid(),
                    AlunoId = aluno.Id,
                    Data = presencaExterna.DataPresenca,
                    ConfirmouPresenca = presencaExterna.Status == "PRESENTE",
                    CancelouPresenca = presencaExterna.Status == "CANCELADO"
                }, cancellationToken);

                processados++;
            }
        }

        return new SyncResultDto
        {
            Operacao = "Importação de presenças",
            RegistrosProcessados = processados,
            Sucesso = true,
            Mensagem = $"{processados} presença(s) sincronizada(s)."
        };
    }

    public Task<SyncResultDto> ImportarRotasAsync(DateOnly dataInicio, DateOnly dataFim, CancellationToken cancellationToken = default)
        => Task.FromResult(new SyncResultDto { Operacao = "Importação de rotas", Sucesso = true, Mensagem = "Sem fonte de dados disponível." });

    public async Task<SyncResultDto> ImportarAlunosAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("presenca-service");

        var alunos = await client.GetFromJsonAsync<List<AlunoExternoDto>>(
            "/api/v1/alunos", cancellationToken) ?? [];

        var processados = 0;

        foreach (var alunoexterno in alunos)
        {
            var existente = await _alunoRepository.BuscarPorExternalIdAsync(alunoexterno.Id, cancellationToken);

            if (existente is null)
            {
                await _alunoRepository.AdicionarAsync(new AlunoSnapshot
                {
                    Id = Guid.NewGuid(),
                    ExternalId = alunoexterno.Id,
                    Nome = alunoexterno.Nome,
                    Ativo = alunoexterno.Ativo
                }, cancellationToken);
            }
            else
            {
                existente.Nome = alunoexterno.Nome;
                existente.Ativo = alunoexterno.Ativo;
                await _alunoRepository.AtualizarAsync(existente, cancellationToken);
            }

            processados++;
        }

        return new SyncResultDto
        {
            Operacao = "Importação de alunos",
            RegistrosProcessados = processados,
            Sucesso = true,
            Mensagem = $"{processados} aluno(s) sincronizado(s)."
        };
    }

    public Task<SyncResultDto> ImportarPeriodoAsync(DateOnly dataInicio, DateOnly dataFim, CancellationToken cancellationToken = default)
        => Task.FromResult(new SyncResultDto { Operacao = "Importação por período", Sucesso = true, Mensagem = "Não implementado ainda." });
}
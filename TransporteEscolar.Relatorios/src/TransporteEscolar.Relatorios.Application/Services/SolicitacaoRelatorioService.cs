using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Exceptions;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Services;

public class SolicitacaoRelatorioService : ISolicitacaoRelatorioService
{
    private const string RoleAdmin = "ROLE_ADMIN";
    private const string RoleMotorista = "ROLE_MOTORISTA";
    private const string RoleAluno = "ROLE_ALUNO";

    private readonly ISolicitacaoRelatorioRepository _repository;
    private readonly IRelatorioCacheService _cache;
    private readonly IExportadorRelatorioService _exportador;

    public SolicitacaoRelatorioService(
        ISolicitacaoRelatorioRepository repository,
        IRelatorioCacheService cache,
        IExportadorRelatorioService exportador)
    {
        _repository = repository;
        _cache = cache;
        _exportador = exportador;
    }

    public async Task<SolicitacaoRelatorioDto> SolicitarAsync(
        TipoRelatorio tipo,
        int ano,
        int mes,
        string papelSolicitante,
        long? profileIdSolicitante,
        string urlConsulta,
        CancellationToken cancellationToken = default)
    {
        ValidarPeriodo(ano, mes);
        ValidarCriacao(tipo, papelSolicitante, profileIdSolicitante);

        var agora = DateTime.UtcNow;
        var solicitacao = new SolicitacaoRelatorio
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Ano = ano,
            Mes = mes,
            ProfileIdSolicitante = profileIdSolicitante,
            PapelSolicitante = papelSolicitante,
            Status = StatusSolicitacaoRelatorio.Pendente,
            CriadoEm = agora,
            AtualizadoEm = agora
        };

        await _repository.AdicionarAsync(solicitacao, cancellationToken);

        return new SolicitacaoRelatorioDto
        {
            RelatorioId = solicitacao.Id,
            Tipo = TipoRelatorioTexto.ParaApi(tipo),
            Status = solicitacao.Status.ToString().ToUpperInvariant(),
            UrlConsulta = urlConsulta
        };
    }

    public async Task<ConsultaRelatorioDto> ConsultarAsync(
        Guid relatorioId,
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var cacheado = await _cache.ObterAsync(relatorioId, cancellationToken);
        if (cacheado is not null)
        {
            ValidarAcesso(
                cacheado.Tipo,
                cacheado.ProfileIdSolicitante,
                papelSolicitante,
                profileIdSolicitante);
            return cacheado;
        }

        var solicitacao = await ObterEValidarAsync(
            relatorioId, papelSolicitante, profileIdSolicitante, cancellationToken);
        var resposta = ConsultaRelatorioDto.FromEntity(solicitacao);

        if (solicitacao.Status == StatusSolicitacaoRelatorio.Concluido)
            await _cache.ArmazenarAsync(resposta, cancellationToken);

        return resposta;
    }

    public async Task<IReadOnlyCollection<ConsultaRelatorioDto>> ListarAsync(
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        ValidarPapel(papelSolicitante);
        var itens = await _repository.ListarRecentesAsync(
            papelSolicitante, profileIdSolicitante, 50, cancellationToken);
        return itens.Select(ConsultaRelatorioDto.FromEntity).ToList();
    }

    public async Task<ArquivoRelatorioDto> BaixarAsync(
        Guid relatorioId,
        string formato,
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken = default)
    {
        var solicitacao = await ObterEValidarAsync(
            relatorioId, papelSolicitante, profileIdSolicitante, cancellationToken);

        if (solicitacao.Status != StatusSolicitacaoRelatorio.Concluido ||
            string.IsNullOrWhiteSpace(solicitacao.ResultadoJson))
        {
            throw new ConflictException("O relatório ainda não está concluído.");
        }

        return _exportador.Exportar(solicitacao, formato);
    }

    private async Task<SolicitacaoRelatorio> ObterEValidarAsync(
        Guid relatorioId,
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken)
    {
        var solicitacao = await _repository.ObterPorIdAsync(relatorioId, cancellationToken)
            ?? throw new NotFoundException("Solicitação de relatório não encontrada.");
        ValidarAcesso(
            TipoRelatorioTexto.ParaApi(solicitacao.Tipo),
            solicitacao.ProfileIdSolicitante,
            papelSolicitante,
            profileIdSolicitante);
        return solicitacao;
    }

    private static void ValidarCriacao(
        TipoRelatorio tipo,
        string papelSolicitante,
        long? profileIdSolicitante)
    {
        ValidarPapel(papelSolicitante);
        if (papelSolicitante == RoleAluno)
        {
            if (tipo != TipoRelatorio.FrequenciaPropria || profileIdSolicitante is null)
                throw new ForbiddenException("Aluno pode solicitar somente a própria frequência.");
            return;
        }

        if (tipo == TipoRelatorio.FrequenciaPropria)
            throw new BusinessException("FREQUENCIA_PROPRIA é exclusiva do perfil de aluno.");
    }

    private static void ValidarAcesso(
        string tipo,
        long? proprietario,
        string papelSolicitante,
        long? profileIdSolicitante)
    {
        ValidarPapel(papelSolicitante);
        if (papelSolicitante != RoleAluno)
        {
            if (tipo == "FREQUENCIA_PROPRIA")
                throw new ForbiddenException("Relatórios individuais pertencem somente ao aluno solicitante.");
            return;
        }

        if (tipo != "FREQUENCIA_PROPRIA" ||
            proprietario is null ||
            proprietario != profileIdSolicitante)
        {
            throw new ForbiddenException("Você não possui acesso a este relatório.");
        }
    }

    private static void ValidarPapel(string papelSolicitante)
    {
        if (papelSolicitante is not (RoleAdmin or RoleMotorista or RoleAluno))
            throw new ForbiddenException("Papel sem permissão para relatórios.");
    }

    private static void ValidarPeriodo(int ano, int mes)
    {
        if (ano <= 0 || mes is < 1 or > 12)
            throw new BusinessException("Ano ou mês inválido.");
    }
}

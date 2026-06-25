using Microsoft.AspNetCore.Mvc;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Application.Exceptions;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Api.Controllers;

[ApiController]
[Route("api/relatorios")]
public class RelatoriosController : ControllerBase
{
    private readonly ISolicitacaoRelatorioService _solicitacaoService;

    public RelatoriosController(ISolicitacaoRelatorioService solicitacaoService)
    {
        _solicitacaoService = solicitacaoService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SolicitacaoRelatorioDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SolicitarRelatorio(
        [FromBody] CriarSolicitacaoRelatorioDto requisicao,
        CancellationToken cancellationToken)
    {
        if (!TipoRelatorioTexto.TentarConverter(requisicao.Tipo, out var tipo))
            throw new BusinessException("Tipo de relatório inválido.");

        var solicitacao = await CriarAsync(
            tipo, requisicao.Ano, requisicao.Mes, cancellationToken);
        return Accepted(solicitacao.UrlConsulta, solicitacao);
    }

    [HttpPost("mensal")]
    [ProducesResponseType(typeof(SolicitacaoRelatorioDto), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SolicitarRelatorioMensal(
        [FromQuery] int ano,
        [FromQuery] int mes,
        CancellationToken cancellationToken)
    {
        var solicitacao = await CriarAsync(
            TipoRelatorio.ResumoMensal, ano, mes, cancellationToken);
        return Accepted(solicitacao.UrlConsulta, solicitacao);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<ConsultaRelatorioDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarRelatorios(CancellationToken cancellationToken)
    {
        var resultado = await _solicitacaoService.ListarAsync(
            ObterPapel(), ObterProfileId(), cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{relatorioId:guid}")]
    [ProducesResponseType(typeof(ConsultaRelatorioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConsultarRelatorio(
        [FromRoute] Guid relatorioId,
        CancellationToken cancellationToken)
    {
        var resultado = await _solicitacaoService.ConsultarAsync(
            relatorioId, ObterPapel(), ObterProfileId(), cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{relatorioId:guid}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BaixarRelatorio(
        [FromRoute] Guid relatorioId,
        [FromQuery] string formato,
        CancellationToken cancellationToken)
    {
        var arquivo = await _solicitacaoService.BaixarAsync(
            relatorioId,
            formato,
            ObterPapel(),
            ObterProfileId(),
            cancellationToken);
        return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeArquivo);
    }

    private async Task<SolicitacaoRelatorioDto> CriarAsync(
        TipoRelatorio tipo,
        int ano,
        int mes,
        CancellationToken cancellationToken)
    {
        var solicitacao = await _solicitacaoService.SolicitarAsync(
            tipo,
            ano,
            mes,
            ObterPapel(),
            ObterProfileId(),
            string.Empty,
            cancellationToken);
        solicitacao.UrlConsulta = Url.Action(
            nameof(ConsultarRelatorio),
            values: new { relatorioId = solicitacao.RelatorioId })!;
        return solicitacao;
    }

    private string ObterPapel() =>
        Request.Headers["X-User-Role"].FirstOrDefault()
        ?? throw new ForbiddenException("Cabeçalho de papel não informado.");

    private Guid? ObterProfileId()
    {
        var valor = Request.Headers["X-Profile-Id"].FirstOrDefault();
        return Guid.TryParse(valor, out var profileId) ? profileId : null;
    }
}

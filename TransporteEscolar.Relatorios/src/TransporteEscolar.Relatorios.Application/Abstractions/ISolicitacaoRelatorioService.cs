using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface ISolicitacaoRelatorioService
{
    Task<SolicitacaoRelatorioDto> SolicitarAsync(
        TipoRelatorio tipo,
        int ano,
        int mes,
        string papelSolicitante,
        long? profileIdSolicitante,
        string urlConsulta,
        CancellationToken cancellationToken = default);

    Task<ConsultaRelatorioDto> ConsultarAsync(
        Guid relatorioId,
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ConsultaRelatorioDto>> ListarAsync(
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken = default);

    Task<ArquivoRelatorioDto> BaixarAsync(
        Guid relatorioId,
        string formato,
        string papelSolicitante,
        long? profileIdSolicitante,
        CancellationToken cancellationToken = default);
}

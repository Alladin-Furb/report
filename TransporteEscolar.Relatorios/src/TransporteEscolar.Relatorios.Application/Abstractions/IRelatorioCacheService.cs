using TransporteEscolar.Relatorios.Application.DTOs;

namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface IRelatorioCacheService
{
    Task<ConsultaRelatorioDto?> ObterAsync(
        Guid relatorioId,
        CancellationToken cancellationToken = default);

    Task ArmazenarAsync(
        ConsultaRelatorioDto relatorio,
        CancellationToken cancellationToken = default);
}

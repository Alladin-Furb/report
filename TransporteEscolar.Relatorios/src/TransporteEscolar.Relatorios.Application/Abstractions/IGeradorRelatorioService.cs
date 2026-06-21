using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface IGeradorRelatorioService
{
    Task<object> GerarAsync(
        SolicitacaoRelatorio solicitacao,
        CancellationToken cancellationToken = default);
}

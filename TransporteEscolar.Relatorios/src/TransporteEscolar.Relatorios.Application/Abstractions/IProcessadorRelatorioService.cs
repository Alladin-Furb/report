namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface IProcessadorRelatorioService
{
    Task ProcessarAsync(
        Guid relatorioId,
        CancellationToken cancellationToken = default);
}

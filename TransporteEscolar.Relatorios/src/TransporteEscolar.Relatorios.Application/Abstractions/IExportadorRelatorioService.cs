using TransporteEscolar.Relatorios.Application.DTOs;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Application.Abstractions;

public interface IExportadorRelatorioService
{
    ArquivoRelatorioDto Exportar(SolicitacaoRelatorio solicitacao, string formato);
}

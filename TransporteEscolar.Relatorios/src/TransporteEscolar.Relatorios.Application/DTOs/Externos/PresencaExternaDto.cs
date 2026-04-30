namespace TransporteEscolar.Relatorios.Application.DTOs.Externos;

public class PresencaExternaDto
{
    public long Id { get; set; }
    public long AlunoId { get; set; }
    public DateOnly DataPresenca { get; set; }
    public string Status { get; set; } = string.Empty;
}
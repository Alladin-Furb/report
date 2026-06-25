namespace TransporteEscolar.Relatorios.Application.DTOs.Externos;

public class PresencaExternaDto
{
    public Guid Id { get; set; }
    public Guid AlunoId { get; set; }
    public DateOnly DataPresenca { get; set; }
    public string Status { get; set; } = string.Empty;
}
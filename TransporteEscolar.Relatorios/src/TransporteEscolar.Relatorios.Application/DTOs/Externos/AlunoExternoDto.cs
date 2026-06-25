namespace TransporteEscolar.Relatorios.Application.DTOs.Externos;

public class AlunoExternoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}
namespace TransporteEscolar.Relatorios.Application.DTOs.Externos;

public class AlunoExternoDto
{
    public long Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; }
}
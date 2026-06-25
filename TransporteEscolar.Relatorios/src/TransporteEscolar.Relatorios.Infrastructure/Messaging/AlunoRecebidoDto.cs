namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class AlunoRecebidoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string? Email { get; set; }
}
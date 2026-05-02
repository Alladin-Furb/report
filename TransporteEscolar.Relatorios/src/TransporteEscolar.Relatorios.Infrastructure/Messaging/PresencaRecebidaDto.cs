namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class PresencaRecebidaDto
{
    public long Id { get; set; }
    public long AlunoId { get; set; }
    public string AlunoNome { get; set; } = string.Empty;
    public DateOnly DataPresenca { get; set; }
    public string Status { get; set; } = string.Empty;
}
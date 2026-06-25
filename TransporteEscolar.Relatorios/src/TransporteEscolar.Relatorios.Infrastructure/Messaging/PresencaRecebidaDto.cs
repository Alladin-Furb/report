namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class PresencaRecebidaDto
{
    public Guid Id { get; set; }
    public Guid AlunoId { get; set; }
    public string AlunoNome { get; set; } = string.Empty;
    public string DataPresenca { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
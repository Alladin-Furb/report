namespace TransporteEscolar.Relatorios.Domain.Entities;

public class SolicitacaoRelatorio
{
    public Guid Id { get; set; }
    public TipoRelatorio Tipo { get; set; } = TipoRelatorio.ResumoMensal;
    public int Ano { get; set; }
    public int Mes { get; set; }
    public long? ProfileIdSolicitante { get; set; }
    public string PapelSolicitante { get; set; } = string.Empty;
    public StatusSolicitacaoRelatorio Status { get; set; } = StatusSolicitacaoRelatorio.Pendente;
    public string? ResultadoJson { get; set; }
    public string? Erro { get; set; }
    public int Tentativas { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public DateTime? IniciadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }
}

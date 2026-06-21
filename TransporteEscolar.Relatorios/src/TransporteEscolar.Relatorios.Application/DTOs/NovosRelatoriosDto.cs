namespace TransporteEscolar.Relatorios.Application.DTOs;

public class FrequenciaAlunosRelatorioDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public IReadOnlyCollection<FrequenciaAlunoDto> Alunos { get; set; } = [];
}

public class PresencasDetalhadasRelatorioDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public IReadOnlyCollection<PresencaDetalhadaDto> Presencas { get; set; } = [];
}

public class PresencaDetalhadaDto
{
    public long AlunoExternalId { get; set; }
    public string NomeAluno { get; set; } = string.Empty;
    public DateOnly Data { get; set; }
    public string Situacao { get; set; } = string.Empty;
    public DateTime? DataConfirmacao { get; set; }
    public DateTime? DataCancelamento { get; set; }
    public string? EnderecoUtilizado { get; set; }
}

public class DesempenhoRotasRelatorioDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public decimal DistanciaTotalKm { get; set; }
    public decimal MediaKmPorRota { get; set; }
    public int TotalAlunosTransportados { get; set; }
    public IReadOnlyCollection<DesempenhoRotaDto> Rotas { get; set; } = [];
}

public class DesempenhoRotaDto
{
    public DateOnly Data { get; set; }
    public decimal DistanciaKm { get; set; }
    public int AlunosTransportados { get; set; }
}

public class FrequenciaPropriaRelatorioDto
{
    public int Ano { get; set; }
    public int Mes { get; set; }
    public FrequenciaAlunoDto Aluno { get; set; } = new();
}

public class ArquivoRelatorioDto
{
    public required byte[] Conteudo { get; init; }
    public required string ContentType { get; init; }
    public required string NomeArquivo { get; init; }
}

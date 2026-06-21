namespace TransporteEscolar.Relatorios.Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

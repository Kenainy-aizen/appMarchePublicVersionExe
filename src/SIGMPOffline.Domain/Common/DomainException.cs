namespace SIGMPOffline.Domain.Common;

/// <summary>Exception métier de base, à utiliser pour les erreurs de règles de gestion.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}

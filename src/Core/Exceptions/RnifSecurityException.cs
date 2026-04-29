namespace RNIF2.Core.Exceptions;

public sealed class RnifSecurityException : Exception
{
    public RnifSecurityException(string message) : base(message) { }
    public RnifSecurityException(string message, Exception inner) : base(message, inner) { }
}

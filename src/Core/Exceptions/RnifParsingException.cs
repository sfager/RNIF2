namespace RNIF2.Core.Exceptions;

public sealed class RnifParsingException : Exception
{
    public RnifParsingException(string message) : base(message) { }
    public RnifParsingException(string message, Exception inner) : base(message, inner) { }
}

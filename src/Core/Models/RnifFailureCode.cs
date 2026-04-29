namespace RNIF2.Core.Models;

public enum RnifFailureCode
{
    Unknown = 0,
    ParseError,
    ValidationError,
    AuthenticationFailed,
    UnknownPip,
    SystemError
}

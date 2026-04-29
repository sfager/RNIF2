using RNIF2.Core.Models;

namespace RNIF2.Core.Interfaces;

public interface IMessageValidator
{
    IReadOnlyList<string> Validate(RnifMessage message);
}

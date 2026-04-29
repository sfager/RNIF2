using System.Xml.Linq;

namespace RNIF2.Core.Constants;

public static class RnifNamespaces
{
    public const string Rnif20 = "urn:rosettanet:specifications:rnif:xsd:02.00";
    public static readonly XNamespace Rnif20Ns = XNamespace.Get(Rnif20);
}

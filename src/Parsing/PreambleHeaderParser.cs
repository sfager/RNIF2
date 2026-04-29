using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Models;

namespace RNIF2.Parsing;

public static class PreambleHeaderParser
{
    public static PreambleHeader Parse(ParsedMimePart part)
    {
        var xml = LoadXml(part);
        var ns = RnifNamespaces.Rnif20Ns;

        var version = xml.Root?.Element(ns + "standardVersion")?.Value
            ?? xml.Root?.Element("standardVersion")?.Value
            ?? throw new RnifParsingException("Preamble: missing <standardVersion> element.");

        var usageCode = xml.Root?.Element(ns + "GlobalUsageCode")?.Value
            ?? xml.Root?.Element("GlobalUsageCode")?.Value
            ?? "Production";

        var tsValue = xml.Root?.Element(ns + "GlobalDateTimeStamp")?.Value
            ?? xml.Root?.Element("GlobalDateTimeStamp")?.Value
            ?? DateTimeOffset.UtcNow.ToString("o");

        if (!DateTimeOffset.TryParse(tsValue, out var ts))
            ts = DateTimeOffset.UtcNow;

        return new PreambleHeader
        {
            RnifVersionIdentifier = version,
            GlobalUsageCode = usageCode,
            TimeDelivered = ts
        };
    }

    private static XDocument LoadXml(ParsedMimePart part)
    {
        try
        {
            return XDocument.Parse(System.Text.Encoding.UTF8.GetString(part.Data));
        }
        catch (Exception ex)
        {
            throw new RnifParsingException("Preamble: invalid XML.", ex);
        }
    }
}

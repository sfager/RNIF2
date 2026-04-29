using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Models;

namespace RNIF2.Parsing;

public static class ServiceHeaderParser
{
    public static ServiceHeader Parse(ParsedMimePart part)
    {
        if (part.IsSmime)
        {
            return new ServiceHeader
            {
                PipCode = string.Empty,
                PipVersion = string.Empty,
                PipInstanceId = string.Empty,
                IsEncrypted = true
            };
        }

        XDocument xml;
        try
        {
            xml = XDocument.Parse(System.Text.Encoding.UTF8.GetString(part.Data));
        }
        catch (Exception ex)
        {
            throw new RnifParsingException("ServiceHeader: invalid XML.", ex);
        }

        var ns = RnifNamespaces.Rnif20Ns;
        var root = xml.Root;

        var pipCode = FindValue(root, ns, "PipCode")
            ?? throw new RnifParsingException("ServiceHeader: missing PipCode.");
        var pipVersion = FindValue(root, ns, "PipVersion") ?? string.Empty;
        var pipInstanceId = FindValue(root, ns, "PipInstanceId")
            ?? FindValue(root, ns, "PipInstanceID")
            ?? Guid.NewGuid().ToString();

        var globalProcessCode = FindValue(root, ns, "GlobalProcessCode");
        var initiatorRole = FindValue(root, ns, "InitiatorRole");
        var responderRole = FindValue(root, ns, "ResponderRole");

        // Detect signal messages by root element name
        var rootName = root?.Name.LocalName ?? string.Empty;
        var isSignal = rootName.EndsWith("Signal", StringComparison.OrdinalIgnoreCase);
        var signalType = isSignal ? rootName : null;

        return new ServiceHeader
        {
            PipCode = pipCode,
            PipVersion = pipVersion,
            PipInstanceId = pipInstanceId,
            GlobalProcessCode = globalProcessCode,
            InitiatorRole = initiatorRole,
            ResponderRole = responderRole,
            IsSignalMessage = isSignal,
            SignalType = signalType
        };
    }

    private static string? FindValue(XElement? root, XNamespace ns, string localName) =>
        root?.Descendants(ns + localName).FirstOrDefault()?.Value
        ?? root?.Descendants(localName).FirstOrDefault()?.Value;
}

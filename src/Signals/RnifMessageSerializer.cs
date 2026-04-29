using System.Text;
using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Models;

namespace RNIF2.Signals;

public sealed class RnifMessageSerializer : IRnifMessageSerializer
{
    private static readonly string Boundary = "rnif-boundary";

    public string GetContentType(RnifMessage message) =>
        $"multipart/related; type=\"application/xml\"; boundary=\"{Boundary}\"";

    public async Task SerializeAsync(RnifMessage message, Stream output, CancellationToken ct)
    {
        using var writer = new StreamWriter(output, Encoding.UTF8, leaveOpen: true);

        await WritePart(writer, BuildPreambleXml(message.Preamble), ct);
        await WritePart(writer, BuildDeliveryXml(message.Delivery), ct);
        await WritePart(writer, BuildServiceHeaderXml(message.Service), ct);
        await WritePart(writer, message.Content.PayloadXml.ToString(SaveOptions.None), ct);

        await writer.WriteAsync($"--{Boundary}--\r\n");
        await writer.FlushAsync(ct);
    }

    private static async Task WritePart(StreamWriter writer, string xmlContent, CancellationToken ct)
    {
        await writer.WriteAsync($"--{Boundary}\r\n");
        await writer.WriteAsync("Content-Type: application/xml; charset=utf-8\r\n\r\n");
        await writer.WriteAsync(xmlContent);
        await writer.WriteAsync("\r\n");
    }

    private static string BuildPreambleXml(PreambleHeader p)
    {
        var ns = RnifNamespaces.Rnif20Ns;
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "Preamble",
                new XAttribute(XNamespace.Xmlns + "rnif", RnifNamespaces.Rnif20),
                new XElement(ns + "standardVersion", p.RnifVersionIdentifier),
                new XElement(ns + "GlobalUsageCode", p.GlobalUsageCode),
                new XElement(ns + "GlobalDateTimeStamp", p.TimeDelivered.ToString("o"))
            )
        ).ToString(SaveOptions.None);
    }

    private static string BuildDeliveryXml(DeliveryHeader d)
    {
        var ns = RnifNamespaces.Rnif20Ns;
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "DeliveryHeader",
                new XAttribute(XNamespace.Xmlns + "rnif", RnifNamespaces.Rnif20),
                new XElement(ns + "FromRole",
                    new XElement(ns + "PartnerIdentification",
                        new XElement(ns + "GlobalBusinessIdentifier",
                            d.FromPartner.GlobalBusinessIdentifier))),
                new XElement(ns + "ToRole",
                    new XElement(ns + "PartnerIdentification",
                        new XElement(ns + "GlobalBusinessIdentifier",
                            d.ToPartner.GlobalBusinessIdentifier))),
                new XElement(ns + "MessageTrackingID", d.MessageTrackingId),
                new XElement(ns + "MessageDateTime", d.MessageDateTime.ToString("o"))
            )
        ).ToString(SaveOptions.None);
    }

    private static string BuildServiceHeaderXml(ServiceHeader s)
    {
        var ns = RnifNamespaces.Rnif20Ns;
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "ServiceHeader",
                new XAttribute(XNamespace.Xmlns + "rnif", RnifNamespaces.Rnif20),
                new XElement(ns + "ProcessControl",
                    new XElement(ns + "PipCode", s.PipCode),
                    new XElement(ns + "PipVersion", s.PipVersion),
                    new XElement(ns + "PipInstanceId", s.PipInstanceId)),
                s.SignalType != null
                    ? new XElement(ns + "SignalType", s.SignalType)
                    : null
            )
        ).ToString(SaveOptions.None);
    }
}

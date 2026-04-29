using System.Xml.Linq;
using RNIF2.Core.Exceptions;
using RNIF2.Core.Models;

namespace RNIF2.Parsing;

public static class ServiceContentParser
{
    public static ServiceContent Parse(ParsedMimePart part)
    {
        if (part.IsSmime)
        {
            return new ServiceContent
            {
                PayloadXml = new XDocument(),
                IsEncrypted = true
            };
        }

        // Service content may be plain XML or a nested multipart/mixed with attachments.
        // If ContentType is application/xml or text/xml, parse directly.
        if (part.ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
        {
            return new ServiceContent
            {
                PayloadXml = ParseXml(part.Data)
            };
        }

        // Nested multipart — treat first XML part as payload, rest as attachments.
        // In this basic implementation, attempt to parse the whole data as XML.
        try
        {
            return new ServiceContent { PayloadXml = ParseXml(part.Data) };
        }
        catch
        {
            return new ServiceContent
            {
                PayloadXml = new XDocument(new XElement("ServiceContent")),
                Attachments = [new ServiceAttachment
                {
                    ContentType = part.ContentType,
                    Data = part.Data,
                    ContentId = part.ContentId
                }]
            };
        }
    }

    private static XDocument ParseXml(byte[] data)
    {
        try
        {
            return XDocument.Parse(System.Text.Encoding.UTF8.GetString(data));
        }
        catch (Exception ex)
        {
            throw new RnifParsingException("ServiceContent: invalid XML.", ex);
        }
    }
}

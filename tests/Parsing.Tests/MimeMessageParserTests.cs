using RNIF2.Parsing;
using Xunit;

namespace RNIF2.Parsing.Tests;

public class MimeMessageParserTests
{
    private static readonly string SampleFilePath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "sample_rnif_message.txt");

    private static readonly string ContentType =
        "multipart/related; type=\"application/xml\"; boundary=\"rnif-boundary\"";

    [Fact]
    public async Task ParseAsync_Returns_Four_Parts()
    {
        var parser = new MimeMessageParser();
        using var body = File.OpenRead(SampleFilePath);

        var parts = await parser.ParseAsync(body, ContentType, CancellationToken.None);

        Assert.Equal(4, parts.Count);
    }

    [Fact]
    public async Task ParseAsync_Part0_Is_Application_Xml()
    {
        var parser = new MimeMessageParser();
        using var body = File.OpenRead(SampleFilePath);

        var parts = await parser.ParseAsync(body, ContentType, CancellationToken.None);

        Assert.Contains("xml", parts[0].ContentType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ParseAsync_Part0_Contains_Preamble_Root()
    {
        var parser = new MimeMessageParser();
        using var body = File.OpenRead(SampleFilePath);

        var parts = await parser.ParseAsync(body, ContentType, CancellationToken.None);
        var xml = System.Text.Encoding.UTF8.GetString(parts[0].Data);

        Assert.Contains("Preamble", xml);
        Assert.Contains("V02.00", xml);
    }
}

public class PreambleHeaderParserTests
{
    private static Stream MakePart(string xml) =>
        new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));

    [Fact]
    public void Parse_Extracts_Version_And_UsageCode()
    {
        var part = new RNIF2.Core.Models.ParsedMimePart
        {
            ContentType = "application/xml",
            Data = System.Text.Encoding.UTF8.GetBytes(
                """
                <?xml version="1.0"?>
                <rnif:Preamble xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
                  <rnif:standardVersion>V02.00</rnif:standardVersion>
                  <rnif:GlobalUsageCode>Test</rnif:GlobalUsageCode>
                  <rnif:GlobalDateTimeStamp>2024-01-15T10:30:00Z</rnif:GlobalDateTimeStamp>
                </rnif:Preamble>
                """)
        };

        var result = PreambleHeaderParser.Parse(part);

        Assert.Equal("V02.00", result.RnifVersionIdentifier);
        Assert.Equal("Test", result.GlobalUsageCode);
    }
}

public class DeliveryHeaderParserTests
{
    [Fact]
    public void Parse_Extracts_TrackingId_And_Partners()
    {
        var part = new RNIF2.Core.Models.ParsedMimePart
        {
            ContentType = "application/xml",
            Data = System.Text.Encoding.UTF8.GetBytes(
                """
                <?xml version="1.0"?>
                <rnif:DeliveryHeader xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
                  <rnif:FromRole>
                    <rnif:PartnerIdentification>
                      <rnif:GlobalBusinessIdentifier>urn:duns:111</rnif:GlobalBusinessIdentifier>
                    </rnif:PartnerIdentification>
                  </rnif:FromRole>
                  <rnif:ToRole>
                    <rnif:PartnerIdentification>
                      <rnif:GlobalBusinessIdentifier>urn:duns:222</rnif:GlobalBusinessIdentifier>
                    </rnif:PartnerIdentification>
                  </rnif:ToRole>
                  <rnif:MessageTrackingID>uuid:abc-123</rnif:MessageTrackingID>
                  <rnif:MessageDateTime>2024-01-15T10:30:00Z</rnif:MessageDateTime>
                </rnif:DeliveryHeader>
                """)
        };

        var result = DeliveryHeaderParser.Parse(part);

        Assert.Equal("uuid:abc-123", result.MessageTrackingId);
        Assert.Equal("urn:duns:111", result.FromPartner.GlobalBusinessIdentifier);
        Assert.Equal("urn:duns:222", result.ToPartner.GlobalBusinessIdentifier);
    }
}

public class RnifMessageExtractorTests
{
    private static readonly string SampleFilePath =
        Path.Combine(AppContext.BaseDirectory, "TestData", "sample_rnif_message.txt");
    private static readonly string ContentType =
        "multipart/related; type=\"application/xml\"; boundary=\"rnif-boundary\"";

    [Fact]
    public async Task ExtractAsync_Returns_Fully_Populated_Message()
    {
        var parser = new MimeMessageParser();
        using var body = File.OpenRead(SampleFilePath);
        var parts = await parser.ParseAsync(body, ContentType, CancellationToken.None);

        var extractor = new RnifMessageExtractor();
        var message = await extractor.ExtractAsync(parts, CancellationToken.None);

        Assert.Equal("V02.00", message.Preamble.RnifVersionIdentifier);
        Assert.Equal("uuid:550e8400-e29b-41d4-a716-446655440000", message.Delivery.MessageTrackingId);
        Assert.Equal("urn:duns:987654321", message.Delivery.FromPartner.GlobalBusinessIdentifier);
        Assert.Equal("3A4", message.Service.PipCode);
        Assert.Equal("V02.02", message.Service.PipVersion);
        Assert.NotNull(message.Content.PayloadXml.Root);
    }
}

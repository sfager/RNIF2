using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IHost = Microsoft.Extensions.Hosting.IHost;
using RNIF2.Core.Configuration;
using RNIF2.Core.Interfaces;
using RNIF2.Core.Validation;
using RNIF2.Handlers;
using RNIF2.Parsing;
using RNIF2.Security;
using RNIF2.Signals;
using RNIF2.Transport;
using Xunit;

namespace RNIF2.Integration.Tests;

public class RnifServerIntegrationTests : IAsyncLifetime
{
    private static readonly int Port = 18080 + Random.Shared.Next(0, 1000);
    private IHost? _host;

    private static readonly string SampleBody = """
        --rnif-boundary
        Content-Type: application/xml; charset=utf-8

        <?xml version="1.0" encoding="utf-8"?>
        <rnif:Preamble xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
          <rnif:standardVersion>V02.00</rnif:standardVersion>
          <rnif:GlobalUsageCode>Test</rnif:GlobalUsageCode>
          <rnif:GlobalDateTimeStamp>2024-01-15T10:30:00Z</rnif:GlobalDateTimeStamp>
        </rnif:Preamble>
        --rnif-boundary
        Content-Type: application/xml; charset=utf-8

        <?xml version="1.0" encoding="utf-8"?>
        <rnif:DeliveryHeader xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
          <rnif:FromRole>
            <rnif:PartnerIdentification>
              <rnif:GlobalBusinessIdentifier>urn:duns:987654321</rnif:GlobalBusinessIdentifier>
            </rnif:PartnerIdentification>
          </rnif:FromRole>
          <rnif:ToRole>
            <rnif:PartnerIdentification>
              <rnif:GlobalBusinessIdentifier>urn:duns:123456789</rnif:GlobalBusinessIdentifier>
            </rnif:PartnerIdentification>
          </rnif:ToRole>
          <rnif:MessageTrackingID>uuid:integration-test-001</rnif:MessageTrackingID>
          <rnif:MessageDateTime>2024-01-15T10:30:00Z</rnif:MessageDateTime>
        </rnif:DeliveryHeader>
        --rnif-boundary
        Content-Type: application/xml; charset=utf-8

        <?xml version="1.0" encoding="utf-8"?>
        <rnif:ServiceHeader xmlns:rnif="urn:rosettanet:specifications:rnif:xsd:02.00">
          <rnif:ProcessControl>
            <rnif:PipCode>3A4</rnif:PipCode>
            <rnif:PipVersion>V02.02</rnif:PipVersion>
            <rnif:PipInstanceId>uuid:instance-integration-001</rnif:PipInstanceId>
          </rnif:ProcessControl>
        </rnif:ServiceHeader>
        --rnif-boundary
        Content-Type: application/xml; charset=utf-8

        <?xml version="1.0" encoding="utf-8"?>
        <PurchaseOrderRequest xmlns="urn:rosettanet:specifications:domain:purchasing:xsd:02.00">
          <requestingOrganization>
            <businessName>Integration Test Buyer</businessName>
          </requestingOrganization>
        </PurchaseOrderRequest>
        --rnif-boundary--
        """;

    public async Task InitializeAsync()
    {
        var prefix = $"http://localhost:{Port}/rnif/";
        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Warning))
            .ConfigureServices(services =>
            {
                services.Configure<RnifOptions>(opt =>
                {
                    opt.ListenerPrefix = prefix;
                    opt.LocalTradingPartnerId = "urn:duns:123456789";
                    opt.LocalBusinessName = "Test Corp";
                    opt.MaxConcurrentRequests = 5;
                });
                services.AddSingleton<IMimeParser, MimeMessageParser>();
                services.AddSingleton<RnifMessageExtractor>();
                services.AddSingleton<IMessageValidator, RnifMessageValidator>();
                services.AddSingleton<ISecurityService, SmimeSecurityService>();
                services.AddSingleton<ISignalGenerator, SignalGenerator>();
                services.AddSingleton<IRnifMessageSerializer, RnifMessageSerializer>();
                services.AddSingleton<IMessageRouter, MessageRouter>();
                services.AddDefaultRnifHandlers();
                services.AddSingleton<IRnifRequestPipeline, RnifRequestPipeline>();
                services.AddSingleton<HttpListenerServer>();
                services.AddHostedService<RNIF2.Host.Worker>();
            })
            .Build();

        await _host.StartAsync();
        await Task.Delay(200); // let listener start
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(3));
            _host.Dispose();
        }
    }

    private static HttpContent BuildRnifContent(string body)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(body);
        var content = new ByteArrayContent(bytes);
        content.Headers.TryAddWithoutValidation(
            "Content-Type",
            "multipart/related; type=\"application/xml\"; boundary=\"rnif-boundary\"");
        return content;
    }

    [Fact]
    public async Task Post_ValidRnifMessage_Returns_200_With_ReceiptAck()
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(
            $"http://localhost:{Port}/rnif/", BuildRnifContent(SampleBody));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.Contains("ReceiptAcknowledgmentSignal", responseBody);
    }

    [Fact]
    public async Task Post_ValidRnifMessage_Response_Contains_InResponseToTrackingId()
    {
        using var client = new HttpClient();
        var response = await client.PostAsync(
            $"http://localhost:{Port}/rnif/", BuildRnifContent(SampleBody));
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Contains("uuid:integration-test-001", responseBody);
    }

    [Fact]
    public async Task Get_Returns_MethodNotAllowed()
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"http://localhost:{Port}/rnif/");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}

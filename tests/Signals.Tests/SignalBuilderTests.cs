using System.Xml.Linq;
using RNIF2.Core.Constants;
using RNIF2.Core.Models;
using RNIF2.Signals;
using Xunit;

namespace RNIF2.Signals.Tests;

public class ReceiptAcknowledgmentBuilderTests
{
    private static RnifMessage BuildSampleMessage() => new()
    {
        Preamble = new PreambleHeader
        {
            RnifVersionIdentifier = "V02.00",
            GlobalUsageCode = "Test",
            TimeDelivered = DateTimeOffset.UtcNow
        },
        Delivery = new DeliveryHeader
        {
            FromPartner = new TradingPartnerIdentity { GlobalBusinessIdentifier = "urn:duns:111" },
            ToPartner = new TradingPartnerIdentity { GlobalBusinessIdentifier = "urn:duns:222" },
            MessageTrackingId = "uuid:test-tracking-id",
            MessageDateTime = DateTimeOffset.UtcNow
        },
        Service = new ServiceHeader
        {
            PipCode = "3A4",
            PipVersion = "V02.02",
            PipInstanceId = "uuid:instance-id"
        },
        Content = new ServiceContent
        {
            PayloadXml = new XDocument(new XElement("payload"))
        }
    };

    [Fact]
    public void Build_RootElement_Is_ReceiptAcknowledgmentSignal()
    {
        var message = BuildSampleMessage();
        var doc = ReceiptAcknowledgmentBuilder.Build(message, "urn:duns:222", "Test Corp");

        Assert.Equal("ReceiptAcknowledgmentSignal", doc.Root?.Name.LocalName);
        Assert.Equal(RnifNamespaces.Rnif20, doc.Root?.Name.NamespaceName);
    }

    [Fact]
    public void Build_Contains_InResponseToTrackingId()
    {
        var message = BuildSampleMessage();
        var doc = ReceiptAcknowledgmentBuilder.Build(message, "urn:duns:222", "Test Corp");

        var ns = RnifNamespaces.Rnif20Ns;
        var trackingId = doc.Root?.Element(ns + "inResponseToMessageTrackingID")?.Value;

        Assert.Equal("uuid:test-tracking-id", trackingId);
    }

    [Fact]
    public void Build_Contains_PipCode_In_ActionCode()
    {
        var message = BuildSampleMessage();
        var doc = ReceiptAcknowledgmentBuilder.Build(message, "urn:duns:222", "Test Corp");

        var ns = RnifNamespaces.Rnif20Ns;
        var actionCode = doc.Root?.Element(ns + "inResponseToGlobalBusinessActionCode")?.Value;

        Assert.Equal("3A4", actionCode);
    }
}

public class ExceptionSignalBuilderTests
{
    [Fact]
    public void Build_RootElement_Is_ExceptionSignal()
    {
        var doc = ExceptionSignalBuilder.Build(
            null, RnifFailureCode.ParseError, "Bad message", "urn:duns:222");

        Assert.Equal("ExceptionSignal", doc.Root?.Name.LocalName);
    }

    [Fact]
    public void Build_Contains_ExceptionCode()
    {
        var doc = ExceptionSignalBuilder.Build(
            null, RnifFailureCode.ValidationError, "Field missing", "urn:duns:222");

        var ns = RnifNamespaces.Rnif20Ns;
        var code = doc.Root?.Element(ns + "ExceptionCode")?.Value;

        Assert.Equal("ValidationError", code);
    }

    [Fact]
    public void Build_Contains_ExceptionDescription()
    {
        var doc = ExceptionSignalBuilder.Build(
            null, RnifFailureCode.SystemError, "Unexpected error", "urn:duns:222");

        var ns = RnifNamespaces.Rnif20Ns;
        var desc = doc.Root?.Element(ns + "ExceptionDescription")?.Value;

        Assert.Equal("Unexpected error", desc);
    }
}

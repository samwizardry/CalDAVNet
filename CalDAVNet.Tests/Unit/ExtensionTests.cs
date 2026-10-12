using System.Xml.Linq;
using FluentAssertions;

namespace CalDAVNet.Tests.Unit;

public class ExtensionTests
{
    [Fact]
    public void HttpRequestMessageExtensions_WithBasicAuthorization_SetsAuthorizationHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        request.WithBasicAuthorization("dGVzdDp0ZXN0");

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        request.Headers.Authorization.Parameter.Should().Be("dGVzdDp0ZXN0");
    }

    [Fact]
    public void HttpRequestMessageExtensions_WithPrefer_SetsPreferHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        request.WithPrefer("return-minimal");

        request.Headers.Contains("Prefer").Should().BeTrue();
        request.Headers.GetValues("Prefer").Should().Contain("return-minimal");
    }

    [Fact]
    public void HttpRequestMessageExtensions_WithDepth_SetsDepthHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        request.WithDepth(1);

        request.Headers.Contains("Depth").Should().BeTrue();
        request.Headers.GetValues("Depth").Should().Contain("1");
    }

    [Theory]
    [InlineData("ctag-123", "\"ctag-123\"")]
    [InlineData("\"ctag-123\"", "\"ctag-123\"")]
    public void HttpRequestMessageExtensions_WithVersion_SetsIfMatchHeaderWithSingleQuotes(string inputVersion, string expectedHeader)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        request.WithVersion(inputVersion);

        request.Headers.Contains("If-Match").Should().BeTrue();
        request.Headers.GetValues("If-Match").Should().Contain(expectedHeader);
    }

    [Fact]
    public async Task XDocumentExtensions_ToStringContent_CreatesApplicationXmlContent()
    {
        var doc = new XDocument(new XElement("root", "content"));
        var stringContent = doc.ToStringContent();

        stringContent.Headers.ContentType!.MediaType.Should().Be("application/xml");
        var text = await stringContent.ReadAsStringAsync();
        text.Should().Contain("<root>content</root>");
    }

    [Theory]
    [InlineData("HTTP/1.1 200 OK", 200)]
    [InlineData("HTTP/1.1 404 Not Found", 404)]
    [InlineData("207 Multi-Status", 207)]
    [InlineData("Status 500", 500)]
    public void XElementExtensions_GetStatusCodeOrDefault_ExtractsThreeDigitCode(string statusText, int expected)
    {
        var element = new XElement("item", new XElement(XNames.Status, statusText));
        var code = element.GetStatusCodeOrDefault(999);
        code.Should().Be(expected);
    }

    [Fact]
    public void XElementExtensions_GetStatusCodeOrDefault_ReturnsDefault_WhenStatusMissingOrInvalid()
    {
        var missingStatus = new XElement("item");
        missingStatus.GetStatusCodeOrDefault(500).Should().Be(500);

        var emptyStatus = new XElement("item", new XElement(XNames.Status, ""));
        emptyStatus.GetStatusCodeOrDefault(500).Should().Be(500);

        var invalidStatus = new XElement("item", new XElement(XNames.Status, "No numbers here"));
        invalidStatus.GetStatusCodeOrDefault(500).Should().Be(500);
    }
}

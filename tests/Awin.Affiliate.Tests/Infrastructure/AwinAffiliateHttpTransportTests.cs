using System.Net;
using Awin.Affiliate.Infrastructure;
using FluentAssertions;

namespace Awin.Affiliate.Tests.Infrastructure;

public class AwinAffiliateHttpTransportTests
{
    private static AwinAffiliateHttpTransport BuildTransport(
        FakeHttpMessageHandler handler,
        int retries = 1,
        TimeSpan? timeout = null)
    {
        return new AwinAffiliateHttpTransport(
            new HttpClient(handler),
            new Uri("https://api.awin.com"),
            "test-token",
            timeout ?? TimeSpan.FromSeconds(10),
            retries);
    }

    [Fact]
    public async Task GetJson_ReturnsBody_OnSuccess()
    {
        var handler = FakeHttpMessageHandler.Json("[]");
        var transport = BuildTransport(handler);

        var response = await transport.GetJsonAsync("/publishers/1/transactions/", default);

        response.StatusCode.Should().Be(200);
        response.Body.Should().Be("[]");
        handler.Requests.Should().HaveCount(1);
        handler.Requests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Requests[0].Headers.Authorization!.Parameter.Should().Be("test-token");
    }

    [Fact]
    public async Task GetJson_MapsHttp401_ToAuthException()
    {
        var handler = FakeHttpMessageHandler.Json("{\"error\":\"bad token\"}", HttpStatusCode.Unauthorized);
        var transport = BuildTransport(handler, retries: 0);

        var act = async () => await transport.GetJsonAsync("/x", default);
        var ex = await act.Should().ThrowAsync<AwinAffiliateAuthException>();
        ex.Which.StatusCode.Should().Be(401);
        ex.Which.ResponseBody.Should().Contain("bad token");
    }

    [Fact]
    public async Task GetJson_MapsHttp429_ToRateLimitException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("too many", System.Text.Encoding.UTF8, "application/json")
        };
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(7));

        var handler = new FakeHttpMessageHandler(response);
        var transport = BuildTransport(handler, retries: 0);

        var act = async () => await transport.GetJsonAsync("/x", default);
        var ex = await act.Should().ThrowAsync<AwinAffiliateRateLimitException>();
        ex.Which.RetryAfter.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public async Task GetJson_RetriesOnce_On5xx()
    {
        var fail = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("server error", System.Text.Encoding.UTF8, "application/json")
        };
        var ok = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
        };

        var handler = FakeHttpMessageHandler.Sequence(fail, ok);
        var transport = BuildTransport(handler);

        var response = await transport.GetJsonAsync("/x", default);

        response.Body.Should().Be("[]");
        handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetJson_Throws_OnPersistent5xxAfterRetry()
    {
        var fail1 = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("bad gateway", System.Text.Encoding.UTF8, "application/json")
        };
        var fail2 = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("bad gateway", System.Text.Encoding.UTF8, "application/json")
        };

        var handler = FakeHttpMessageHandler.Sequence(fail1, fail2);
        var transport = BuildTransport(handler, retries: 1);

        var act = async () => await transport.GetJsonAsync("/x", default);
        var ex = await act.Should().ThrowAsync<AwinAffiliateApiException>();
        ex.Which.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task GetJson_DoesNotRetry_On4xx()
    {
        var fail = FakeHttpMessageHandler.Json("nope", HttpStatusCode.BadRequest);
        var transport = BuildTransport(fail);

        var act = async () => await transport.GetJsonAsync("/x", default);
        await act.Should().ThrowAsync<AwinAffiliateApiException>();

        fail.Requests.Should().HaveCount(1);
    }
}

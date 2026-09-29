using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using GalaShow.ChzzkProxy;
using ChzzkFunction = GalaShow.ChzzkProxy.Function;

namespace GalaShow.Token.Tests;

[Collection("Environment configuration")]
public sealed class ChzzkIntegrationTests : ConfigurationEnvironment
{
    private const string Success = "{\"code\":200,\"message\":null,\"content\":{}}";
    private readonly RecordingHandler _http = new();
    private int _credentialReads;

    private ChzzkFunction CreateFunction() => new(new ChzzkIntegrationService(new HttpClient(_http), () =>
    {
        _credentialReads++;
        return Task.FromResult(new ChzzkCredentials("server-id", "server-secret"));
    }));

    private static APIGatewayProxyRequest Request(string method, string path, string? body = null, bool bearer = false) => new()
    {
        HttpMethod = method, Path = path, Body = body,
        Headers = bearer ? new Dictionary<string, string> { ["Authorization"] = "Bearer user-token" } : new Dictionary<string, string>()
    };

    [Fact]
    public async Task Config_OnlyExposesPublicClientId()
    {
        var response = await CreateFunction().FunctionHandler(Request("GET", "/chzzk/config"), null!);
        Assert.Equal(200, response.StatusCode);
        Assert.Equal("server-id", JsonDocument.Parse(response.Body).RootElement.GetProperty("content").GetProperty("clientId").GetString());
        Assert.DoesNotContain("server-secret", response.Body);
        Assert.Equal("no-store", response.Headers["Cache-Control"]);
        Assert.Empty(_http.Requests);
    }

    [Theory]
    [InlineData("/chzzk/auth/token", "{\"code\":\"login-code\",\"state\":\"login-state\"}", "authorization_code")]
    [InlineData("/chzzk/auth/refresh", "{\"refreshToken\":\"refresh-token\"}", "refresh_token")]
    public async Task TokenRequests_InjectServerCredentials(string path, string body, string grantType)
    {
        _http.Enqueue(Success);
        var response = await CreateFunction().FunctionHandler(Request("POST", path, body), null!);
        Assert.Equal(200, response.StatusCode);
        var sent = Assert.Single(_http.Requests);
        Assert.Equal("https://openapi.chzzk.naver.com/auth/v1/token", sent.Url);
        Assert.Equal("POST", sent.Method);
        var json = JsonDocument.Parse(sent.Body!).RootElement;
        Assert.Equal(grantType, json.GetProperty("grantType").GetString());
        Assert.Equal("server-id", json.GetProperty("clientId").GetString());
        Assert.Equal("server-secret", json.GetProperty("clientSecret").GetString());
        Assert.Null(sent.Authorization);
        Assert.Equal(body.Contains("login-code") ? "login-code" : "refresh-token", json.GetProperty(grantType == "authorization_code" ? "code" : "refreshToken").GetString());
    }

    [Theory]
    [InlineData("GET", "/chzzk/open/v1/channels", null, 404)]
    [InlineData("GET", "/chzzk/auth/token", null, 405)]
    [InlineData("POST", "/chzzk/auth/token", "{}", 400)]
    [InlineData("POST", "/chzzk/auth/token", "not-json", 400)]
    [InlineData("POST", "/chzzk/auth/token", "[]", 400)]
    [InlineData("POST", "/chzzk/auth/token", "{\"code\":\"c\",\"state\":\"s\",\"ClientSecret\":\"bad\"}", 400)]
    [InlineData("POST", "/chzzk/auth/token", "{\"code\":\"c\",\"state\":\"s\",\"client_id\":\"bad\"}", 400)]
    [InlineData("GET", "/chzzk/users/me", null, 401)]
    [InlineData("POST", "/chzzk/sessions/events/subscribe/anything", "{\"sessionKey\":\"key\"}", 404)]
    public async Task InvalidRequests_DoNotReadSecretsOrCallUpstream(string method, string path, string? body, int status)
    {
        var response = await CreateFunction().FunctionHandler(Request(method, path, body), null!);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(status, JsonDocument.Parse(response.Body).RootElement.GetProperty("code").GetInt32());
        Assert.Equal(0, _credentialReads);
        Assert.Empty(_http.Requests);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BrowserCredentialHeaders_AreRejectedCaseInsensitively(bool multiValue)
    {
        var request = Request("GET", "/chzzk/config");
        if (multiValue) request.MultiValueHeaders = new Dictionary<string, IList<string>> { ["cLiEnT-sEcReT"] = ["bad"] };
        else request.Headers["cLiEnT-iD"] = "bad";
        var response = await CreateFunction().FunctionHandler(request, null!);
        Assert.Equal(400, response.StatusCode);
        Assert.Equal(0, _credentialReads);
    }

    [Fact]
    public async Task Channels_AuthenticatesUserBeforeSpendingApplicationQuota()
    {
        _http.Enqueue(Success);
        _http.Enqueue(Success);
        var request = Request("GET", "/chzzk/channels", bearer: true);
        request.QueryStringParameters = new Dictionary<string, string> { ["channelIds"] = "channel-a,channel-b" };
        Assert.Equal(200, (await CreateFunction().FunctionHandler(request, null!)).StatusCode);
        Assert.Equal(2, _http.Requests.Count);
        Assert.Equal("https://openapi.chzzk.naver.com/open/v1/users/me", _http.Requests[0].Url);
        Assert.Equal("Bearer user-token", _http.Requests[0].Authorization);
        Assert.Null(_http.Requests[0].ClientSecret);
        var channels = _http.Requests[1];
        Assert.Equal("https://openapi.chzzk.naver.com/open/v1/channels?channelIds=channel-a%2Cchannel-b", channels.Url);
        Assert.Equal("server-id", channels.ClientId);
        Assert.Equal("server-secret", channels.ClientSecret);
        Assert.Null(channels.Authorization);
    }

    [Fact]
    public async Task Channels_InvalidBearerStopsBeforeCredentialRead()
    {
        _http.Enqueue("{\"code\":401,\"message\":\"invalid token\"}", HttpStatusCode.Unauthorized);
        var request = Request("GET", "/chzzk/channels", bearer: true);
        request.QueryStringParameters = new Dictionary<string, string> { ["channelIds"] = "channel-a" };
        Assert.Equal(401, (await CreateFunction().FunctionHandler(request, null!)).StatusCode);
        Assert.Single(_http.Requests);
        Assert.Equal(0, _credentialReads);
    }

    [Theory]
    [InlineData("GET", "/chzzk/users/me", "GET", "/open/v1/users/me")]
    [InlineData("POST", "/chzzk/sessions", "GET", "/open/v1/sessions/auth")]
    [InlineData("POST", "/chzzk/sessions/events/subscribe/chat", "POST", "/open/v1/sessions/events/subscribe/chat?sessionKey=a%2Bb%26c")]
    [InlineData("POST", "/chzzk/sessions/events/subscribe/donation", "POST", "/open/v1/sessions/events/subscribe/donation?sessionKey=a%2Bb%26c")]
    [InlineData("POST", "/chzzk/sessions/events/subscribe/subscription", "POST", "/open/v1/sessions/events/subscribe/subscription?sessionKey=a%2Bb%26c")]
    public async Task UserApis_UseOnlyUserBearer(string method, string path, string upstreamMethod, string upstreamPath)
    {
        _http.Enqueue(Success);
        var request = Request(method, path, path.Contains("subscribe") ? "{\"sessionKey\":\"a+b&c\"}" : null, true);
        request.Headers["Cookie"] = "private-cookie";
        request.Headers["X-Forwarded-Host"] = "other-host";
        var response = await CreateFunction().FunctionHandler(request, null!);
        Assert.Equal(200, response.StatusCode);
        var sent = Assert.Single(_http.Requests);
        Assert.Equal(upstreamMethod, sent.Method);
        Assert.Equal("https://openapi.chzzk.naver.com" + upstreamPath, sent.Url);
        Assert.Equal("Bearer user-token", sent.Authorization);
        Assert.Null(sent.ClientSecret);
        Assert.Null(sent.Body);
        Assert.DoesNotContain("Cookie", sent.HeaderNames);
        Assert.DoesNotContain("X-Forwarded-Host", sent.HeaderNames);
        Assert.Equal(0, _credentialReads);
    }

    [Fact]
    public async Task Revoke_UsesOfficialCamelCasePayload()
    {
        _http.Enqueue(Success);
        var request = Request("POST", "/chzzk/auth/revoke", "{\"token\":\"user-token\",\"tokenTypeHint\":\"access_token\"}");
        Assert.Equal(200, (await CreateFunction().FunctionHandler(request, null!)).StatusCode);
        var sent = Assert.Single(_http.Requests);
        Assert.EndsWith("/auth/v1/token/revoke", sent.Url);
        var json = JsonDocument.Parse(sent.Body!).RootElement;
        Assert.Equal("user-token", json.GetProperty("token").GetString());
        Assert.Equal("access_token", json.GetProperty("tokenTypeHint").GetString());
        Assert.Equal("server-secret", json.GetProperty("clientSecret").GetString());
        Assert.False(json.TryGetProperty("grantType", out _));
    }

    [Theory]
    [InlineData("broken", false, 502)]
    [InlineData("<html>error</html>", false, 502)]
    [InlineData("{\"code\":200}", false, 502)]
    [InlineData("{\"code\":200,\"message\":null}", false, 502)]
    [InlineData("{\"code\":200,\"message\":123,\"content\":{}}", false, 502)]
    [InlineData("{}", true, 502)]
    public async Task UnusableUpstreamResponse_IsSanitized(string body, bool redirect, int status)
    {
        _http.Enqueue(body, redirect ? HttpStatusCode.Redirect : HttpStatusCode.OK);
        var response = await CreateFunction().FunctionHandler(Request("GET", "/chzzk/users/me", bearer: true), null!);
        Assert.Equal(status, response.StatusCode);
        Assert.DoesNotContain("error</html>", response.Body);
        Assert.False(response.Headers.ContainsKey("Set-Cookie"));
    }

    [Theory]
    [InlineData(false, 502)]
    [InlineData(true, 504)]
    public async Task TransportFailure_PreservesDevCorsWithoutLeakingException(bool timeout, int status)
    {
        Set("STAGE", "dev");
        _http.Failure = timeout ? new TaskCanceledException("sensitive value") : new HttpRequestException("sensitive value");
        var request = Request("GET", "/chzzk/users/me", bearer: true);
        request.Headers["Origin"] = "http://localhost:51234";
        var response = await CreateFunction().FunctionHandler(request, null!);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("http://localhost:51234", response.Headers["Access-Control-Allow-Origin"]);
        Assert.Equal("no-store", response.Headers["Cache-Control"]);
        Assert.DoesNotContain("sensitive", response.Body);
    }

    [Fact]
    public async Task MissingConfiguration_Returns503()
    {
        var service = new ChzzkIntegrationService(new HttpClient(_http), new ChzzkCredentialsProvider().GetAsync);
        var response = await new ChzzkFunction(service).FunctionHandler(Request("GET", "/chzzk/config"), null!);
        Assert.Equal(503, response.StatusCode);
        Assert.Empty(_http.Requests);
    }

    [Fact]
    public async Task Base64Json_IsSupported_AndMalformedBase64Rejected()
    {
        _http.Enqueue(Success);
        var request = Request("POST", "/chzzk/auth/token", Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"code\":\"c\",\"state\":\"s\"}")));
        request.IsBase64Encoded = true;
        var function = CreateFunction();
        Assert.Equal(200, (await function.FunctionHandler(request, null!)).StatusCode);
        request.Body = "!invalid!";
        Assert.Equal(400, (await function.FunctionHandler(request, null!)).StatusCode);
        Assert.Single(_http.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a,,b")]
    [InlineData("channel&a=override")]
    [InlineData("a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u")]
    public async Task InvalidChannelIds_StopBeforeAuthentication(string ids)
    {
        var request = Request("GET", "/chzzk/channels", bearer: true);
        request.QueryStringParameters = new Dictionary<string, string> { ["channelIds"] = ids };
        Assert.Equal(400, (await CreateFunction().FunctionHandler(request, null!)).StatusCode);
        Assert.Empty(_http.Requests);
        Assert.Equal(0, _credentialReads);
    }

    [Fact]
    public async Task UnverifiedUserEnvelope_DoesNotReadAppCredentials()
    {
        _http.Enqueue("{\"code\":401,\"message\":\"invalid token\"}");
        var request = Request("GET", "/chzzk/channels", bearer: true);
        request.QueryStringParameters = new Dictionary<string, string> { ["channelIds"] = "channel-a" };
        var response = await CreateFunction().FunctionHandler(request, null!);
        Assert.Contains("invalid token", response.Body);
        Assert.Single(_http.Requests);
        Assert.Equal(0, _credentialReads);
    }

    [Fact]
    public async Task UpstreamScopeFailure_PreservesStatusAndBodyWithoutPrivateHeaders()
    {
        const string error = "{\"code\":403,\"message\":\"scope unavailable\"}";
        _http.Enqueue(error, HttpStatusCode.Forbidden);
        var response = await CreateFunction().FunctionHandler(Request("POST", "/chzzk/sessions/events/subscribe/donation", "{\"sessionKey\":\"key\"}", true), null!);
        Assert.Equal(403, response.StatusCode);
        Assert.Equal(error, response.Body);
        Assert.False(response.Headers.ContainsKey("Set-Cookie"));
        Assert.Equal("no-store", response.Headers["Cache-Control"]);
    }

    private sealed record SentRequest(string Method, string Url, string? Body, string? Authorization, string? ClientId, string? ClientSecret, string[] HeaderNames);

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public readonly List<SentRequest> Requests = [];
        private readonly Queue<HttpResponseMessage> _responses = new();
        public Exception? Failure;

        public void Enqueue(string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            response.Headers.Add("Set-Cookie", "upstream-private-cookie");
            _responses.Enqueue(response);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(new(request.Method.Method, request.RequestUri!.AbsoluteUri,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
                request.Headers.Authorization?.ToString(), Header("Client-Id"), Header("Client-Secret"),
                request.Headers.Select(h => h.Key).ToArray()));
            if (Failure is not null) throw Failure;
            return _responses.Dequeue();
            string? Header(string name) => request.Headers.TryGetValues(name, out var values) ? values.Single() : null;
        }
    }
}

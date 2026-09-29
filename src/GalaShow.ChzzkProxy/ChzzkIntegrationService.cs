using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.APIGatewayEvents;

namespace GalaShow.ChzzkProxy;

/// <summary>Explicit CHZZK integration operations. Incoming URLs and headers are never proxied.</summary>
public sealed class ChzzkIntegrationService(HttpClient httpClient, Func<Task<ChzzkCredentials>> getCredentials)
{
    private const string ApiBase = "https://openapi.chzzk.naver.com";
    private const string SubscribePrefix = "/chzzk/sessions/events/subscribe/";
    private static readonly Regex BearerPattern = new(@"\ABearer ([A-Za-z0-9\-._~+/]+=*)\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request)
    {
        var path = request.Path ?? "";
        var expectedMethod = path switch
        {
            "/chzzk/config" or "/chzzk/users/me" or "/chzzk/channels" => "GET",
            "/chzzk/auth/token" or "/chzzk/auth/refresh" or "/chzzk/auth/revoke" or "/chzzk/sessions" => "POST",
            SubscribePrefix + "chat" or SubscribePrefix + "donation" or SubscribePrefix + "subscription" => "POST",
            _ => null
        };
        if (expectedMethod is null) return Error(404, "NOT_FOUND");
        if (request.HttpMethod != expectedMethod) return Error(405, "METHOD_NOT_ALLOWED");
        if (HeaderValues(request, "Client-Id").Any() || HeaderValues(request, "Client-Secret").Any())
            return Error(400, "CLIENT_CREDENTIALS_NOT_ACCEPTED");

        try
        {
            if (path == "/chzzk/config")
            {
                RequireEmptyBody(request);
                var credentials = await getCredentials();
                return Json(200, JsonSerializer.Serialize(new { code = 200, message = (string?)null, content = new { clientId = credentials.ClientId } }));
            }

            if (path.StartsWith("/chzzk/auth/", StringComparison.Ordinal))
            {
                var fields = ReadBody(request, path switch
                {
                    "/chzzk/auth/token" => ["code", "state"],
                    "/chzzk/auth/refresh" => ["refreshToken"],
                    _ => ["token", "tokenTypeHint"]
                });
                var payload = new Dictionary<string, string>();
                if (path == "/chzzk/auth/token")
                {
                    payload["code"] = Required(fields, "code");
                    payload["state"] = Required(fields, "state");
                    payload["grantType"] = "authorization_code";
                }
                else if (path == "/chzzk/auth/refresh")
                {
                    payload["refreshToken"] = Required(fields, "refreshToken");
                    payload["grantType"] = "refresh_token";
                }
                else
                {
                    payload["token"] = Required(fields, "token");
                    if (fields.TryGetValue("tokenTypeHint", out var hint))
                    {
                        if (hint is not ("access_token" or "refresh_token")) throw new InvalidRequestException();
                        payload["tokenTypeHint"] = hint;
                    }
                }
                var credentials = await getCredentials();
                payload["clientId"] = credentials.ClientId;
                payload["clientSecret"] = credentials.ClientSecret;
                using var outbound = new HttpRequestMessage(HttpMethod.Post, ApiBase + (path == "/chzzk/auth/revoke" ? "/auth/v1/token/revoke" : "/auth/v1/token"))
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                return await SendAsync(outbound);
            }

            var authorization = HeaderValues(request, "Authorization").Distinct(StringComparer.Ordinal).ToArray();
            if (authorization.Length != 1 || !BearerPattern.IsMatch(authorization[0])) return Error(401, "UNAUTHORIZED");
            var token = BearerPattern.Match(authorization[0]).Groups[1].Value;

            if (path == "/chzzk/channels")
            {
                RequireEmptyBody(request);
                var ids = ChannelIds(request);
                using var authenticate = UserRequest(HttpMethod.Get, "/open/v1/users/me", token);
                var user = await SendAsync(authenticate);
                if (user.StatusCode != 200) return user;
                using var userJson = JsonDocument.Parse(user.Body);
                if (userJson.RootElement.GetProperty("code").GetInt32() != 200) return user;
                var credentials = await getCredentials();
                using var channels = new HttpRequestMessage(HttpMethod.Get, ApiBase + "/open/v1/channels?channelIds=" + Uri.EscapeDataString(ids));
                channels.Headers.Add("Client-Id", credentials.ClientId);
                channels.Headers.Add("Client-Secret", credentials.ClientSecret);
                return await SendAsync(channels);
            }

            if (path.StartsWith(SubscribePrefix, StringComparison.Ordinal))
            {
                var sessionKey = Required(ReadBody(request, ["sessionKey"]), "sessionKey");
                var eventType = path[SubscribePrefix.Length..];
                using var subscribe = UserRequest(HttpMethod.Post, $"/open/v1/sessions/events/subscribe/{eventType}?sessionKey={Uri.EscapeDataString(sessionKey)}", token);
                return await SendAsync(subscribe);
            }

            RequireEmptyBody(request);
            using var userRequest = UserRequest(HttpMethod.Get, path == "/chzzk/users/me" ? "/open/v1/users/me" : "/open/v1/sessions/auth", token);
            return await SendAsync(userRequest);
        }
        catch (InvalidRequestException) { return Error(400, "INVALID_REQUEST"); }
        catch (ChzzkConfigurationException) { return Error(503, "CHZZK_NOT_CONFIGURED"); }
        catch (OperationCanceledException) { return Error(504, "CHZZK_TIMEOUT"); }
        catch (HttpRequestException) { return Error(502, "CHZZK_UNAVAILABLE"); }
    }

    private async Task<APIGatewayProxyResponse> SendAsync(HttpRequestMessage request)
    {
        using var response = await httpClient.SendAsync(request);
        var status = (int)response.StatusCode;
        if (status is >= 300 and < 400) return Error(502, "INVALID_CHZZK_RESPONSE");
        var body = await response.Content.ReadAsStringAsync();
        if (status == 204 && string.IsNullOrEmpty(body)) return Json(status, "");
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.Number || !code.TryGetInt32(out var resultCode) ||
                !root.TryGetProperty("message", out var message) || message.ValueKind is not (JsonValueKind.Null or JsonValueKind.String) ||
                (status is >= 200 and < 300 && resultCode == 200 && !root.TryGetProperty("content", out _)))
                return Error(502, "INVALID_CHZZK_RESPONSE");
        }
        catch (JsonException) { return Error(502, "INVALID_CHZZK_RESPONSE"); }
        return Json(status, body);
    }

    private static HttpRequestMessage UserRequest(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, ApiBase + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static IEnumerable<string> HeaderValues(APIGatewayProxyRequest request, string name)
    {
        if (request.Headers is not null)
            foreach (var entry in request.Headers)
                if (entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase)) yield return entry.Value ?? "";
        if (request.MultiValueHeaders is not null)
            foreach (var entry in request.MultiValueHeaders)
                if (entry.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    foreach (var value in entry.Value) yield return value ?? "";
    }

    private static Dictionary<string, string> ReadBody(APIGatewayProxyRequest request, string[] allowed)
    {
        try
        {
            var body = request.IsBase64Encoded ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body ?? "")) : request.Body;
            if (string.IsNullOrWhiteSpace(body) || body.Length > 16384) throw new InvalidRequestException();
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidRequestException();
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in json.RootElement.EnumerateObject())
            {
                if (!allowed.Contains(property.Name) || property.Value.ValueKind != JsonValueKind.String || !fields.TryAdd(property.Name, property.Value.GetString()!))
                    throw new InvalidRequestException();
            }
            return fields;
        }
        catch (Exception ex) when (ex is JsonException or FormatException) { throw new InvalidRequestException(); }
    }

    private static string Required(Dictionary<string, string> fields, string name)
    {
        if (!fields.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
            throw new InvalidRequestException();
        return value;
    }

    private static string ChannelIds(APIGatewayProxyRequest request)
    {
        var values = new List<string>();
        if (request.QueryStringParameters?.TryGetValue("channelIds", out var single) == true) values.Add(single);
        if (request.MultiValueQueryStringParameters?.TryGetValue("channelIds", out var multiple) == true) values.AddRange(multiple);
        var distinct = values.Distinct(StringComparer.Ordinal).ToArray();
        if (distinct.Length != 1 || string.IsNullOrEmpty(distinct[0]) || distinct[0].Length > 2048) throw new InvalidRequestException();
        var ids = distinct[0].Split(',');
        if (ids.Length is < 1 or > 20 || ids.Any(id => id.Length == 0 || id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_')))
            throw new InvalidRequestException();
        return distinct[0];
    }

    private static void RequireEmptyBody(APIGatewayProxyRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Body)) throw new InvalidRequestException();
    }

    public static APIGatewayProxyResponse Error(int status, string message) => Json(status, JsonSerializer.Serialize(new { code = status, message }));

    public static APIGatewayProxyResponse Json(int status, string body) => new()
    {
        StatusCode = status, Body = body,
        Headers = new Dictionary<string, string> { ["Content-Type"] = "application/json; charset=utf-8", ["Cache-Control"] = "no-store" }
    };

    private sealed class InvalidRequestException : Exception;
}

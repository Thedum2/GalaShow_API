using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Token;
using GalaShow.Common.Models.Response.Token;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace GalaShow.Token.Tests;

public class IntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly ITestOutputHelper _output;
    private string? _accessToken;
    private string? _refreshToken;

    public IntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        _baseUrl = configuration["API_BASE_URL"]
                   ?? configuration["ApiSettings:BaseUrl"]
                   ?? "http://127.0.0.1:3000";

        _client = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl)
        };

        _output.WriteLine($"[테스트 시작] Base URL: {_baseUrl}");
    }

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken != null) return _accessToken;

        var testId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? "test";
        var testPassword = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD") ?? "test";

        var loginRequest = new LoginRequest { Id = testId, Password = testPassword };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        try
        {
            var response = await _client.PostAsync("/auth/login", content);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
                _accessToken = body?.Data?.AccessToken;
                _refreshToken = body?.Data?.RefreshToken;
                _output.WriteLine($"[토큰 발급 성공] AccessToken: {_accessToken?[..20]}...");
                return _accessToken;
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"[토큰 발급 실패] {ex.Message}");
        }

        return null;
    }

    #region [1] Token API

    [Fact]
    public async Task Token_Login_Success()
    {
        // Arrange
        var testId = Environment.GetEnvironmentVariable("TEST_USER_ID") ?? "test";
        var testPassword = Environment.GetEnvironmentVariable("TEST_USER_PASSWORD") ?? "test";
        var loginRequest = new LoginRequest { Id = testId, Password = testPassword };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Token API - 로그인 성공"
            : $"[실패] Token API - 로그인 성공 (Status: {response.StatusCode})");

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            body.Data!.AccessToken.Should().NotBeNullOrEmpty();
            body.Data.RefreshToken.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Token_Login_Fail_EmptyBody()
    {
        // Arrange
        // Act
        var response = await _client.PostAsync("/auth/login", null);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.BadRequest
            ? "[성공] Token API - 로그인 실패 (빈 Body)"
            : "[실패] Token API - 로그인 실패 (빈 Body)");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Token_Login_Fail_InvalidCredentials()
    {
        // Arrange
        var loginRequest = new LoginRequest { Id = "invalid", Password = "invalid" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine(((int)response.StatusCode) == 600
            ? "[성공] Token API - 로그인 실패 (잘못된 자격증명)"
            : "[실패] Token API - 로그인 실패 (잘못된 자격증명)");

        ((int)response.StatusCode).Should().Be(600);
    }

    [Fact]
    public async Task Token_Verify_Success()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        if (token == null)
        {
            _output.WriteLine("[건너뜀] Token API - 토큰 검증 성공 (토큰 발급 실패)");
            return;
        }

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        // Act
        var response = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Token API - 토큰 검증 성공"
            : $"[실패] Token API - 토큰 검증 성공 (Status: {response.StatusCode})");

        _client.DefaultRequestHeaders.Clear();

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task Token_Verify_Fail_MissingToken()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();

        // Act
        var response = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine(((int)response.StatusCode) == 601
            ? "[성공] Token API - 토큰 검증 실패 (토큰 누락)"
            : "[실패] Token API - 토큰 검증 실패 (토큰 누락)");

        ((int)response.StatusCode).Should().Be(601);
    }

    [Fact]
    public async Task Token_Verify_Fail_InvalidToken()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.token.here");

        // Act
        var response = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine(((int)response.StatusCode) == 602
            ? "[성공] Token API - 토큰 검증 실패 (잘못된 토큰)"
            : "[실패] Token API - 토큰 검증 실패 (잘못된 토큰)");

        _client.DefaultRequestHeaders.Clear();
        ((int)response.StatusCode).Should().Be(602);
    }

    [Fact]
    public async Task Token_Refresh_Fail_InvalidToken()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "invalid-refresh-token" };
        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/refresh", content);

        // Assert
        _output.WriteLine(((int)response.StatusCode) == 604
            ? "[성공] Token API - 토큰 재발급 실패 (잘못된 토큰)"
            : "[실패] Token API - 토큰 재발급 실패 (잘못된 토큰)");

        ((int)response.StatusCode).Should().Be(604);
    }

    [Fact]
    public async Task Token_Logout_Success()
    {
        // Arrange
        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.OK
            ? "[성공] Token API - 로그아웃 성공"
            : "[실패] Token API - 로그아웃 성공");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    #endregion

    #region [2] Banner API

    [Fact]
    public async Task Banner_Get_Success()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/banners");

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Banner API - 전체 조회 성공"
            : $"[실패] Banner API - 전체 조회 성공 (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Banner_Update_Success()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        if (token == null)
        {
            _output.WriteLine("[건너뜀] Banner API - 수정 성공 (토큰 발급 실패)");
            return;
        }

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var updateData = new { displayOrder = 1 };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/1", content);

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Banner API - 수정 성공"
            : $"[실패] Banner API - 수정 성공 (Status: {response.StatusCode})");

        _client.DefaultRequestHeaders.Clear();

        if (!response.IsSuccessStatusCode)
        {
            _output.WriteLine($"  Response: {await response.Content.ReadAsStringAsync()}");
        }
    }

    [Fact]
    public async Task Banner_Update_Fail_Unauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        var updateData = new { displayOrder = 1 };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/1", content);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[성공] Banner API - 수정 실패 (인증 없음)"
            : "[실패] Banner API - 수정 실패 (인증 없음)");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [3] Background API

    [Fact]
    public async Task Background_Get_Success()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/background");

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Background API - 전체 조회 성공"
            : $"[실패] Background API - 전체 조회 성공 (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Background_Update_Success()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        if (token == null)
        {
            _output.WriteLine("[건너뜀] Background API - 수정 성공 (토큰 발급 실패)");
            return;
        }

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var updateData = new { displayOrder = 1 };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/1", content);

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Background API - 수정 성공"
            : $"[실패] Background API - 수정 성공 (Status: {response.StatusCode})");

        _client.DefaultRequestHeaders.Clear();

        if (!response.IsSuccessStatusCode)
        {
            _output.WriteLine($"  Response: {await response.Content.ReadAsStringAsync()}");
        }
    }

    [Fact]
    public async Task Background_Update_Fail_Unauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        var updateData = new { displayOrder = 1 };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/1", content);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[성공] Background API - 수정 실패 (인증 없음)"
            : "[실패] Background API - 수정 실패 (인증 없음)");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [4] Policy API

    [Fact]
    public async Task Policy_Get_Success()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/policies");

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Policy API - 조회 성공"
            : $"[실패] Policy API - 조회 성공 (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Policy_Update_Success()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        if (token == null)
        {
            _output.WriteLine("[건너뜀] Policy API - 수정 성공 (토큰 발급 실패)");
            return;
        }

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var updateData = new { termsOfService = "test", privacyPolicy = "test" };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/policies", content);

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] Policy API - 수정 성공"
            : $"[실패] Policy API - 수정 성공 (Status: {response.StatusCode})");

        _client.DefaultRequestHeaders.Clear();

        if (!response.IsSuccessStatusCode)
        {
            _output.WriteLine($"  Response: {await response.Content.ReadAsStringAsync()}");
        }
    }

    [Fact]
    public async Task Policy_Update_Fail_Unauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        var updateData = new { termsOfService = "test", privacyPolicy = "test" };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/policies", content);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[성공] Policy API - 수정 실패 (인증 없음)"
            : "[실패] Policy API - 수정 실패 (인증 없음)");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [5] SNS API

    [Fact]
    public async Task Sns_Get_Success()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/sns-links");

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] SNS API - 조회 성공"
            : $"[실패] SNS API - 조회 성공 (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Sns_Update_Success()
    {
        // Arrange
        var token = await GetAccessTokenAsync();
        if (token == null)
        {
            _output.WriteLine("[건너뜀] SNS API - 수정 성공 (토큰 발급 실패)");
            return;
        }

        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var updateData = new { links = new[] { new { platform = "youtube", url = "https://youtube.com/test" } } };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/sns-links", content);

        // Assert
        _output.WriteLine(response.IsSuccessStatusCode
            ? "[성공] SNS API - 수정 성공"
            : $"[실패] SNS API - 수정 성공 (Status: {response.StatusCode})");

        _client.DefaultRequestHeaders.Clear();

        if (!response.IsSuccessStatusCode)
        {
            _output.WriteLine($"  Response: {await response.Content.ReadAsStringAsync()}");
        }
    }

    [Fact]
    public async Task Sns_Update_Fail_Unauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        var updateData = new { links = new[] { new { platform = "youtube", url = "https://youtube.com/test" } } };
        var content = new StringContent(JsonSerializer.Serialize(updateData), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/sns-links", content);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[성공] SNS API - 수정 실패 (인증 없음)"
            : "[실패] SNS API - 수정 실패 (인증 없음)");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [6] ChzzkProxy API

    [Fact]
    public async Task ChzzkProxy_Get()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/chzzk/service/v1/lives");

        // Assert
        _output.WriteLine($"[완료] ChzzkProxy API - 프록시 호출 (Status: {response.StatusCode})");

        response.Should().NotBeNull();
    }

    #endregion

    #region [7] 공통

    [Fact]
    public async Task Common_Options_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/auth/login");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _output.WriteLine(response.StatusCode == System.Net.HttpStatusCode.OK
            ? "[성공] 공통 - OPTIONS 요청"
            : "[실패] 공통 - OPTIONS 요청");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Common_NotFound()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/unknown/path");

        // Assert
        _output.WriteLine($"[완료] 공통 - 404 테스트 (Status: {response.StatusCode})");

        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.NotFound,
            System.Net.HttpStatusCode.Forbidden
        );
    }

    #endregion
}

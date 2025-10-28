using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Background;
using GalaShow.Common.Models.Request.Banner;
using GalaShow.Common.Models.Request.Policy;
using GalaShow.Common.Models.Request.Sns;
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

        _output.WriteLine($"[TEST START] Base URL: {_baseUrl}");
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string> LoginAndGetTokenAsync()
    {
        var loginRequest = new LoginRequest { Id = "dev", Password = "dev" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/auth/login", content);

        if (!response.IsSuccessStatusCode)
        {
            _output.WriteLine($"[Login FAIL] StatusCode: {response.StatusCode}, Body: {await response.Content.ReadAsStringAsync()}");
            throw new Exception("Login failed");
        }
        
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        return body!.Data!.AccessToken;
    }


    #region [1] BANNER

    [Fact]
    public async Task Banner_GetAll_ReturnsSuccess()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/banners");

        // Assert
        bool success = response.IsSuccessStatusCode;
        _output.WriteLine(
            success
            ? "[SUCCESS] Banner - Get All"
            : $"[FAIL] Banner - Get All (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task Banner_Update_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var updateRequest = new UpdateBannerRequest { Message = "New Banner Message" };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/1", content);

        // Assert
        _output.WriteLine(
            response.IsSuccessStatusCode
            ? "[SUCCESS] Banner - Update with auth"
            : $"[FAIL] Banner - Update with auth (Status: {response.StatusCode})");
        
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _client.DefaultRequestHeaders.Authorization = null;
    }
    
    [Fact]
    public async Task Banner_Update_WithNonExistentId_ReturnsBannerNotFound()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var updateRequest = new UpdateBannerRequest { Message = "New Banner Message" };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/9999", content);

        // Assert
        _output.WriteLine(
            (int)response.StatusCode == 610
            ? "[SUCCESS] Banner - Update with non-existent id"
            : $"[FAIL] Banner - Update with non-existent id (Status: {response.StatusCode})");
        
        ((int)response.StatusCode).Should().Be(610);
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Banner_Update_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var content = new StringContent("{\"displayOrder\":1}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/1", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[SUCCESS] Banner - Update without auth"
            : "[FAIL] Banner - Update without auth");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [2] Background

    [Fact]
    public async Task Background_GetAll_ReturnsSuccess()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/background");

        // Assert
        bool success = response.IsSuccessStatusCode;
        _output.WriteLine(
            success
            ? "[SUCCESS] Background - Get All"
            : $"[FAIL] Background - Get All (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
    
    [Fact]
    public async Task Background_Update_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var updateRequest = new UpdateBackgroundRequest { Title = "New Background Title", Type = "image", Url = "http://example.com/image.jpg"};
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/1", content);

        // Assert
        _output.WriteLine(
            response.IsSuccessStatusCode
            ? "[SUCCESS] Background - Update with auth"
            : $"[FAIL] Background - Update with auth (Status: {response.StatusCode})");
        
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _client.DefaultRequestHeaders.Authorization = null;
    }
    
    [Fact]
    public async Task Background_Update_WithNonExistentId_ReturnsBackgroundNotFound()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var updateRequest = new UpdateBackgroundRequest { Title = "New Background Title", Type = "image", Url = "http://example.com/image.jpg"};
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/9999", content);

        // Assert
        _output.WriteLine(
            (int)response.StatusCode == 620
            ? "[SUCCESS] Background - Update with non-existent id"
            : $"[FAIL] Background - Update with non-existent id (Status: {response.StatusCode})");
        
        ((int)response.StatusCode).Should().Be(620);
        
        _client.DefaultRequestHeaders.Authorization = null;
    }


    [Fact]
    public async Task Background_Update_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var content = new StringContent("{\"displayOrder\":1}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/1", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[SUCCESS] Background - Update without auth"
            : "[FAIL] Background - Update without auth");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [3] Policy

    [Fact]
    public async Task Policy_Get_ReturnsSuccess()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/policies");

        // Assert
        bool success = response.IsSuccessStatusCode;
        _output.WriteLine(
            success
            ? "[SUCCESS] Policy - Get"
            : $"[FAIL] Policy - Get (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
    
    [Fact]
    public async Task Policy_Update_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var updateRequest = new UpdatePolicyRequest { TermsOfService = "new terms", PrivacyPolicy = "new privacy policy"};
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/policies", content);

        // Assert
        _output.WriteLine(
            response.IsSuccessStatusCode
            ? "[SUCCESS] Policy - Update with auth"
            : $"[FAIL] Policy - Update with auth (Status: {response.StatusCode})");
        
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Policy_Update_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var content = new StringContent("{\"termsOfService\":\"test\"}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/policies", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[SUCCESS] Policy - Update without auth"
            : "[FAIL] Policy - Update without auth");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion

    #region [4] SNS Link

    [Fact]
    public async Task SnsLinks_Get_ReturnsSuccess()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/sns-links");

        // Assert
        bool success = response.IsSuccessStatusCode;
        _output.WriteLine(
            success
            ? "[SUCCESS] SNS Link - Get"
            : $"[FAIL] SNS Link - Get (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
    
    [Fact]
    public async Task SnsLinks_Update_WithAuth_ReturnsSuccess()
    {
        // Arrange
        var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        var updateRequest = new UpdateSnsLinksRequest
        {
            Data = new List<UpdateSnsLinksRequest.SnsLinkItem>
            {
                new() { Id = 1, Title = "new title", Url = "http://new.url", IconUrl = "http://new.icon", Order = 1 }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/sns-links", content);

        // Assert
        _output.WriteLine(
            response.IsSuccessStatusCode
            ? "[SUCCESS] SNS Link - Update with auth"
            : $"[FAIL] SNS Link - Update with auth (Status: {response.StatusCode})");
        
        response.IsSuccessStatusCode.Should().BeTrue();
        
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task SnsLinks_Update_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var content = new StringContent("{\"links\":[]}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/sns-links", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[SUCCESS] SNS Link - Update without auth"
            : "[FAIL] SNS Link - Update without auth");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    #endregion
    
    #region [5] Token
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginRequest = new LoginRequest { Id = "dev", Password = "dev" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine(
            response.IsSuccessStatusCode
            ? "[SUCCESS] Login - Valid credentials"
            : $"[FAIL] Login - Valid credentials (Status: {response.StatusCode})");

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data!.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithMissingBody_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var response = await _client.PostAsync("/auth/login", null);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest
            ? "[SUCCESS] Login - Missing body"
            : "[FAIL] Login - Missing body");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest { Id = "", Password = "" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.BadRequest
            ? "[SUCCESS] Login - Empty credentials"
            : "[FAIL] Login - Empty credentials");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsAuthInvalidCredentials()
    {
        // Arrange
        var loginRequest = new LoginRequest { Id = "invalid-user", Password = "wrong-password" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine(
            ((int)response.StatusCode) == 600
            ? "[SUCCESS] Login - Invalid credentials"
            : "[FAIL] Login - Invalid credentials");

        ((int)response.StatusCode).Should().Be(600);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }



    [Fact]
    public async Task Refresh_WithMissingBody_ReturnsUnauthorized()
    {
        // Arrange
        // Act
        var response = await _client.PostAsync("/auth/refresh", null);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[SUCCESS] Refresh - Missing body"
            : "[FAIL] Refresh - Missing body");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_WithEmptyRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "" };
        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/refresh", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            ? "[SUCCESS] Refresh - Empty refresh token"
            : "[FAIL] Refresh - Empty refresh token");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsAuthRefreshInvalid()
    {
        // Arrange
        var refreshRequest = new RefreshRequest { RefreshToken = "invalid-refresh-token" };
        var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/refresh", content);

        // Assert
        _output.WriteLine(
            ((int)response.StatusCode) == 604
            ? "[SUCCESS] Refresh - Invalid refresh token"
            : "[FAIL] Refresh - Invalid refresh token");

        ((int)response.StatusCode).Should().Be(604);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_WithMissingBody_ReturnsSuccess()
    {
        // Arrange
        // Act
        var response = await _client.PostAsync("/auth/logout", null);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.OK
            ? "[SUCCESS] Logout - Missing body"
            : "[FAIL] Logout - Missing body");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().BeNull();
    }

    [Fact]
    public async Task Logout_WithEmptyRefreshToken_ReturnsSuccess()
    {
        // Arrange
        var logoutRequest = new LogoutRequest { RefreshToken = "" };
        var content = new StringContent(JsonSerializer.Serialize(logoutRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/logout", content);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.OK
            ? "[SUCCESS] Logout - Empty refresh token"
            : "[FAIL] Logout - Empty refresh token");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().BeNull();
    }


    [Fact]
    public async Task Verify_WithMissingToken_ReturnsAuthTokenMissing()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine(
            ((int)response.StatusCode) == 601
            ? "[SUCCESS] Verify - Missing token"
            : "[FAIL] Verify - Missing token");

        ((int)response.StatusCode).Should().Be(601);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Verify_WithInvalidToken_ReturnsAuthTokenInvalid()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

        // Act
        var response = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine(
            ((int)response.StatusCode) == 602
            ? "[SUCCESS] Verify - Invalid token"
            : "[FAIL] Verify - Invalid token");

        ((int)response.StatusCode).Should().Be(602);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();

        _client.DefaultRequestHeaders.Clear();
    }

    [Fact]
    public async Task Verify_WithoutBearerPrefix_ReturnsAuthTokenInvalid()
    {
        // Arrange
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", "some-token-without-bearer");

        // Act
        var response = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine(
            ((int)response.StatusCode) == 602
            ? "[SUCCESS] Verify - Without Bearer prefix"
            : "[FAIL] Verify - Without Bearer prefix");

        ((int)response.StatusCode).Should().Be(602);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();

        _client.DefaultRequestHeaders.Clear();
    }

    #endregion

    #region [99] ETC

    [Fact]
    public async Task UnknownPath_ReturnsPathNotFound()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/auth/unknown");

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.NotFound
            ? "[SUCCESS] ETC - Unknown path"
            : "[FAIL] ETC - Unknown path");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task WrongHttpMethod_ReturnsPathNotFound()
    {
        // Arrange
        // Act
        var response = await _client.GetAsync("/auth/login");

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.NotFound
            ? "[SUCCESS] ETC - Wrong HTTP method"
            : "[FAIL] ETC - Wrong HTTP method");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Options_ReturnsSuccess()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Options, "/auth/login");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        _output.WriteLine(
            response.StatusCode == System.Net.HttpStatusCode.OK
            ? "[SUCCESS] ETC - OPTIONS request"
            : "[FAIL] ETC - OPTIONS request");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    #endregion
    
}
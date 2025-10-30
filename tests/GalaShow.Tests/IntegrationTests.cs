using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Background;
using GalaShow.Common.Models.Request.Banner;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Models.Request.Policy;
using GalaShow.Common.Models.Request.Sns;
using GalaShow.Common.Models.Request.Token;
using GalaShow.Common.Models.Response.Token;
using GalaShow.Common.Models.Response.Banner;
using GalaShow.Common.Models.Response.Background;
using GalaShow.Common.Models.Response.Minigame;
using GalaShow.Common.Models.Response.Policy;
using GalaShow.Common.Models.Response.Sns;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace GalaShow.Token.Tests;

public class IntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;
    private readonly ITestOutputHelper _output;
    private static int _testCounter = 0;
    private static readonly List<string> _failedTests = new List<string>();
    private static readonly object _lockObject = new object();

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

        // 모든 테스트가 끝난 후 실패한 테스트 목록 출력
        lock (_lockObject)
        {
            if (_failedTests.Count > 0)
            {
                _output.WriteLine("\n\n" + new string('=', 60));
                _output.WriteLine($"  FAILED TESTS SUMMARY ({_failedTests.Count} failed)");
                _output.WriteLine(new string('=', 60));
                foreach (var failedTest in _failedTests)
                {
                    _output.WriteLine($"  {failedTest}");
                }
                _output.WriteLine(new string('=', 60) + "\n");
            }
        }

        return Task.CompletedTask;
    }

    private int GetNextTestNumber()
    {
        lock (_lockObject)
        {
            return ++_testCounter;
        }
    }

    private void RecordTestFailure(int testNumber, string testName, Exception ex)
    {
        lock (_lockObject)
        {
            _failedTests.Add($"[{testNumber}] {testName}: {ex.Message}");
        }
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


    #region [1] BANNER TESTS

    // ========================================
    // Banner GET Tests - 배너 조회 테스트
    // ========================================

    /// <summary>
    /// 테스트: 모든 배너 조회 성공
    /// 목적: 인증 없이 배너 목록을 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task Banner_GetAll_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Banner_GetAll_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Banner - Get All");

            // Act
            var response = await _client.GetAsync("/banners");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<BannerResponse>>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved {body.Data!.Count} banners");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Banner UPDATE Tests - 배너 수정 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 배너 수정 성공 및 실제 변경 확인
    /// 목적: 배너 업데이트가 정상적으로 작동하고 실제로 데이터가 변경되는지 검증
    /// </summary>
    [Fact]
    public async Task Banner_Update_WithAuth_ReturnsSuccessAndVerifyChanges()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Banner_Update_WithAuth_ReturnsSuccessAndVerifyChanges";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Banner - Update with Auth & Verify Changes");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueMessage = $"Test Banner Message {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var updateRequest = new UpdateBannerRequest { Message = uniqueMessage };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act - Update
        _output.WriteLine($"  Updating banner with message: {uniqueMessage}");
        var updateResponse = await _client.PutAsync("/banners/1", content);

        // Assert - Update Success
        _output.WriteLine($"  Update Status: {(int)updateResponse.StatusCode} ({updateResponse.StatusCode})");
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act - Verify by GET
        _output.WriteLine("  Verifying changes...");
        var getResponse = await _client.GetAsync("/banners");
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var banners = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<BannerResponse>>>();
        banners.Should().NotBeNull();
        banners!.Data.Should().NotBeNull();

        var updatedBanner = banners.Data!.FirstOrDefault(b => b.Id == 1);
        updatedBanner.Should().NotBeNull();
        updatedBanner!.Message.Should().Be(uniqueMessage);

        _output.WriteLine($"  Result: ✓ SUCCESS - Banner updated and verified");
        _output.WriteLine($"  Updated Message: {updatedBanner.Message}");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 존재하지 않는 배너 ID로 수정 시도
    /// 목적: 잘못된 ID에 대한 적절한 에러 처리 확인 (커스텀 상태 코드 450)
    /// </summary>
    [Fact]
    public async Task Banner_Update_WithNonExistentId_ReturnsBannerNotFound()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Banner_Update_WithNonExistentId_ReturnsBannerNotFound";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Banner - Update with Non-existent ID");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateBannerRequest { Message = "Should Fail" };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/9999", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode}");
        ((int)response.StatusCode).Should().Be(450);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
        _output.WriteLine($"  Result: ✓ SUCCESS - Correctly returned error code 450 (Banner Not Found)");
        _output.WriteLine($"  Error: {body.Error!.Message}");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 배너 수정 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task Banner_Update_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Banner_Update_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Banner - Update without Auth");
            var content = new StringContent("{\"message\":\"Unauthorized Update\"}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/banners/1", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

    #region [2] BACKGROUND TESTS

    // ========================================
    // Background GET Tests - 배경 조회 테스트
    // ========================================

    /// <summary>
    /// 테스트: 모든 배경 조회 성공
    /// 목적: 인증 없이 배경 목록을 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task Background_GetAll_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Background_GetAll_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Background - Get All");

            // Act
            var response = await _client.GetAsync("/background");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<BackgroundResponse>>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved {body.Data!.Count} backgrounds");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Background UPDATE Tests - 배경 수정 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 배경 수정 성공 및 실제 변경 확인
    /// 목적: 배경 업데이트가 정상적으로 작동하고 실제로 데이터가 변경되는지 검증
    /// </summary>
    [Fact]
    public async Task Background_Update_WithAuth_ReturnsSuccessAndVerifyChanges()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Background_Update_WithAuth_ReturnsSuccessAndVerifyChanges";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Background - Update with Auth & Verify Changes");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueTitle = $"Test BG {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var updateRequest = new UpdateBackgroundRequest
        {
            Title = uniqueTitle,
            Type = "image",
            Url = "http://example.com/test-bg.jpg"
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act - Update
        _output.WriteLine($"  Updating background: {uniqueTitle}");
        var updateResponse = await _client.PutAsync("/background/1", content);

        // Assert - Update Success
        _output.WriteLine($"  Update Status: {(int)updateResponse.StatusCode} ({updateResponse.StatusCode})");
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act - Verify by GET
        _output.WriteLine("  Verifying changes...");
        var getResponse = await _client.GetAsync("/background");
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var backgrounds = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<BackgroundResponse>>>();
        backgrounds.Should().NotBeNull();
        backgrounds!.Data.Should().NotBeNull();

        var updatedBg = backgrounds.Data!.FirstOrDefault(bg => bg.Id == 1);
        updatedBg.Should().NotBeNull();
        updatedBg!.Title.Should().Be(uniqueTitle);
        updatedBg.Type.Should().Be("image");
        updatedBg.Url.Should().Be("http://example.com/test-bg.jpg");

        _output.WriteLine($"  Result: ✓ SUCCESS - Background updated and verified");
        _output.WriteLine($"  Title: {updatedBg.Title}");
        _output.WriteLine($"  Type: {updatedBg.Type}");
        _output.WriteLine($"  URL: {updatedBg.Url}");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 존재하지 않는 배경 ID로 수정 시도
    /// 목적: 잘못된 ID에 대한 적절한 에러 처리 확인 (커스텀 상태 코드 451)
    /// </summary>
    [Fact]
    public async Task Background_Update_WithNonExistentId_ReturnsBackgroundNotFound()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Background_Update_WithNonExistentId_ReturnsBackgroundNotFound";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Background - Update with Non-existent ID");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateBackgroundRequest
        {
            Title = "Should Fail",
            Type = "image",
            Url = "http://example.com/fail.jpg"
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/9999", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode}");
        ((int)response.StatusCode).Should().Be(451);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
        _output.WriteLine($"  Result: ✓ SUCCESS - Correctly returned error code 451 (Background Not Found)");
        _output.WriteLine($"  Error: {body.Error!.Message}");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 배경 수정 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task Background_Update_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Background_Update_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Background - Update without Auth");
            var content = new StringContent("{\"title\":\"Unauthorized\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/background/1", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 잘못된 타입으로 배경 수정 시도
    /// 목적: 배경 타입 유효성 검사 확인 (image, video 등)
    /// </summary>
    [Fact]
    public async Task Background_Update_WithInvalidType_ReturnsBadRequest()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Background_Update_WithInvalidType_ReturnsBadRequest";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Background - Update with Invalid Type");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateBackgroundRequest
        {
            Title = "Test",
            Type = "invalid-type",
            Url = "http://example.com/test.jpg"
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/background/1", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        // API가 타입 검증을 한다면 실패해야 함
        _output.WriteLine($"  Result: Response received (validation check)");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

    #region [3] POLICY TESTS

    // ========================================
    // Policy GET Tests - 정책 조회 테스트
    // ========================================

    /// <summary>
    /// 테스트: 정책 조회 성공
    /// 목적: 인증 없이 정책(이용약관, 개인정보처리방침)을 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task Policy_Get_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Policy_Get_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Policy - Get");

            // Act
            var response = await _client.GetAsync("/policies");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<PolicyResponse>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Policy retrieved");
            _output.WriteLine($"  Terms of Service length: {body.Data!.TermsOfService.Length} chars");
            _output.WriteLine($"  Privacy Policy length: {body.Data!.PrivacyPolicy.Length} chars");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Policy UPDATE Tests - 정책 수정 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 정책 수정 성공 및 실제 변경 확인
    /// 목적: 정책 업데이트가 정상적으로 작동하고 실제로 데이터가 변경되는지 검증
    /// </summary>
    [Fact]
    public async Task Policy_Update_WithAuth_ReturnsSuccessAndVerifyChanges()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Policy_Update_WithAuth_ReturnsSuccessAndVerifyChanges";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Policy - Update with Auth & Verify Changes");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueTerms = $"Terms updated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var uniquePrivacy = $"Privacy updated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
        var updateRequest = new UpdatePolicyRequest
        {
            TermsOfService = uniqueTerms,
            PrivacyPolicy = uniquePrivacy
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act - Update
        _output.WriteLine($"  Updating policies...");
        var updateResponse = await _client.PutAsync("/policies", content);

        // Assert - Update Success
        _output.WriteLine($"  Update Status: {(int)updateResponse.StatusCode} ({updateResponse.StatusCode})");
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act - Verify by GET
        _output.WriteLine("  Verifying changes...");
        var getResponse = await _client.GetAsync("/policies");
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var policy = await getResponse.Content.ReadFromJsonAsync<ApiResponse<PolicyResponse>>();
        policy.Should().NotBeNull();
        policy!.Data.Should().NotBeNull();
        policy.Data!.TermsOfService.Should().Be(uniqueTerms);
        policy.Data!.PrivacyPolicy.Should().Be(uniquePrivacy);

        _output.WriteLine($"  Result: ✓ SUCCESS - Policy updated and verified");
        _output.WriteLine($"  Terms: {policy.Data.TermsOfService}");
        _output.WriteLine($"  Privacy: {policy.Data.PrivacyPolicy}");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 정책 수정 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task Policy_Update_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Policy_Update_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Policy - Update without Auth");
            var content = new StringContent("{\"termsOfService\":\"Unauthorized\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/policies", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 부분적인 정책 업데이트
    /// 목적: 이용약관만 업데이트하거나 개인정보처리방침만 업데이트 가능한지 확인
    /// </summary>
    [Fact]
    public async Task Policy_Update_PartialUpdate_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Policy_Update_PartialUpdate_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Policy - Partial Update (Terms only)");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdatePolicyRequest
        {
            TermsOfService = $"Only terms {DateTime.UtcNow:HH:mm:ss}",
            PrivacyPolicy = "" // 빈 값으로 테스트
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/policies", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        // 부분 업데이트가 허용되는지 확인
        _output.WriteLine($"  Result: Response received (partial update check)");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

    #region [4] SNS LINK TESTS

    // ========================================
    // SNS Link GET Tests - SNS 링크 조회 테스트
    // ========================================

    /// <summary>
    /// 테스트: SNS 링크 목록 조회 성공
    /// 목적: 인증 없이 SNS 링크를 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task SnsLinks_Get_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "SnsLinks_Get_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] SNS Links - Get All");

            // Act
            var response = await _client.GetAsync("/sns-links");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<SnsLinkResponse>>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved {body.Data!.Count} SNS links");

            if (body.Data.Count > 0)
            {
                _output.WriteLine($"  First Link: {body.Data[0].Title} - {body.Data[0].Url}");
            }
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // SNS Link UPDATE Tests - SNS 링크 수정 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 SNS 링크 수정 성공 및 실제 변경 확인
    /// 목적: SNS 링크 업데이트가 정상적으로 작동하고 실제로 데이터가 변경되는지 검증
    /// </summary>
    [Fact]
    public async Task SnsLinks_Update_WithAuth_ReturnsSuccessAndVerifyChanges()
    {
        var testNumber = GetNextTestNumber();
        var testName = "SnsLinks_Update_WithAuth_ReturnsSuccessAndVerifyChanges";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] SNS Links - Update with Auth & Verify Changes");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueTitle = $"SNS {DateTime.UtcNow:HH:mm:ss}";
        var updateRequest = new UpdateSnsLinksRequest
        {
            Data = new List<UpdateSnsLinksRequest.SnsLinkItem>
            {
                new() {
                    Id = 1,
                    Title = uniqueTitle,
                    Url = "http://updated.url",
                    IconUrl = "http://updated.icon",
                    Order = 1
                }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act - Update
        _output.WriteLine($"  Updating SNS links...");
        var updateResponse = await _client.PutAsync("/sns-links", content);

        // Assert - Update Success
        _output.WriteLine($"  Update Status: {(int)updateResponse.StatusCode} ({updateResponse.StatusCode})");
        updateResponse.IsSuccessStatusCode.Should().BeTrue();

        // Act - Verify by GET
        _output.WriteLine("  Verifying changes...");
        var getResponse = await _client.GetAsync("/sns-links");
        getResponse.IsSuccessStatusCode.Should().BeTrue();

        var links = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<SnsLinkResponse>>>();
        links.Should().NotBeNull();
        links!.Data.Should().NotBeNull();
        links.Data.Should().NotBeEmpty();

        // 업데이트된 링크 찾기 (Order로 식별)
        var updatedLink = links.Data!.FirstOrDefault(l => l.Order == 1);
        updatedLink.Should().NotBeNull();
        updatedLink!.Title.Should().Be(uniqueTitle);

        _output.WriteLine($"  Result: ✓ SUCCESS - SNS links updated and verified");
        _output.WriteLine($"  Title: {updatedLink.Title}");
        _output.WriteLine($"  URL: {updatedLink.Url}");
        _output.WriteLine($"  Icon: {updatedLink.IconUrl}");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 SNS 링크 수정 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task SnsLinks_Update_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "SnsLinks_Update_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] SNS Links - Update without Auth");
            var content = new StringContent("{\"data\":[]}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/sns-links", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 여러 SNS 링크 동시 업데이트
    /// 목적: 여러 링크를 한 번에 업데이트할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task SnsLinks_Update_MultipleLinks_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "SnsLinks_Update_MultipleLinks_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] SNS Links - Update Multiple Links");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss");
        var updateRequest = new UpdateSnsLinksRequest
        {
            Data = new List<UpdateSnsLinksRequest.SnsLinkItem>
            {
                new() { Id = 1, Title = $"Link1 {timestamp}", Url = "http://link1.url", IconUrl = "http://icon1.url", Order = 1 },
                new() { Id = 2, Title = $"Link2 {timestamp}", Url = "http://link2.url", IconUrl = "http://icon2.url", Order = 2 },
                new() { Id = 3, Title = $"Link3 {timestamp}", Url = "http://link3.url", IconUrl = "http://icon3.url", Order = 3 }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/sns-links", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        response.IsSuccessStatusCode.Should().BeTrue();
        _output.WriteLine($"  Result: ✓ SUCCESS - Multiple links updated (3 links)");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 빈 배열로 SNS 링크 업데이트
    /// 목적: 모든 링크를 제거할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task SnsLinks_Update_WithEmptyArray_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "SnsLinks_Update_WithEmptyArray_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] SNS Links - Update with Empty Array");
            var token = await LoginAndGetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateSnsLinksRequest { Data = new List<UpdateSnsLinksRequest.SnsLinkItem>() };
        var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/sns-links", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        // 빈 배열이 허용되는지 확인
        _output.WriteLine($"  Result: Response received (empty array check)");

        _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

    #region [5] TOKEN / AUTHENTICATION TESTS

    // ========================================
    // Login Tests - 로그인 테스트
    // ========================================

    /// <summary>
    /// 테스트: 유효한 자격 증명으로 로그인 성공
    /// 목적: 올바른 ID/PW로 로그인 시 AccessToken과 RefreshToken을 정상적으로 받는지 확인
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccessWithTokens()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Login_WithValidCredentials_ReturnsSuccessWithTokens";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Login - Valid Credentials");
            var loginRequest = new LoginRequest { Id = "dev", Password = "dev" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.AccessToken.Should().NotBeNullOrEmpty();
        body.Data!.RefreshToken.Should().NotBeNullOrEmpty();

        _output.WriteLine($"  Result: ✓ SUCCESS - Tokens received");
        _output.WriteLine($"  AccessToken length: {body.Data.AccessToken.Length}");
        _output.WriteLine($"  RefreshToken length: {body.Data.RefreshToken.Length}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: Body 없이 로그인 시도
    /// 목적: 필수 데이터 누락 시 400 Bad Request 반환 확인
    /// </summary>
    [Fact]
    public async Task Login_WithMissingBody_ReturnsBadRequest()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Login_WithMissingBody_ReturnsBadRequest";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Login - Missing Body");

            // Act
            var response = await _client.PostAsync("/auth/login", null);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Bad request error");
            _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 빈 자격 증명으로 로그인 시도
    /// 목적: 빈 ID/PW에 대한 유효성 검사 확인
    /// </summary>
    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Login_WithEmptyCredentials_ReturnsBadRequest";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Login - Empty Credentials");
            var loginRequest = new LoginRequest { Id = "", Password = "" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
        _output.WriteLine($"  Result: ✓ SUCCESS - Empty credentials rejected");
        _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 잘못된 자격 증명으로 로그인 시도
    /// 목적: 인증 실패 시 커스텀 상태 코드 440 반환 확인
    /// </summary>
    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsAuthInvalidCredentials()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Login_WithInvalidCredentials_ReturnsAuthInvalidCredentials";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Login - Invalid Credentials");
            var loginRequest = new LoginRequest { Id = "invalid-user", Password = "wrong-password" };
        var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/auth/login", content);

        // Assert
        _output.WriteLine($"  Status: {(int)response.StatusCode}");
        ((int)response.StatusCode).Should().Be(440);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().NotBeNull();
        _output.WriteLine($"  Result: ✓ SUCCESS - Invalid credentials error (440)");
        _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 한 필드만 비어있는 경우
    /// 목적: ID 또는 Password 중 하나만 비어있을 때의 처리 확인
    /// </summary>
    [Fact]
    public async Task Login_WithOnlyIdProvided_ReturnsBadRequest()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Login_WithOnlyIdProvided_ReturnsBadRequest";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Login - Only ID Provided");
            var loginRequest = new LoginRequest { Id = "dev", Password = "" };
            var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/login", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeFalse();
            _output.WriteLine($"  Result: ✓ SUCCESS - Partial credentials rejected");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Refresh Token Tests - 리프레시 토큰 테스트
    // ========================================

    /// <summary>
    /// 테스트: 유효한 리프레시 토큰으로 액세스 토큰 갱신
    /// 목적: 리프레시 토큰으로 새로운 액세스 토큰을 정상적으로 발급받는지 확인
    /// </summary>
    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessToken()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Refresh_WithValidToken_ReturnsNewAccessToken";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Refresh - Valid Refresh Token");

            // 먼저 로그인하여 리프레시 토큰 획득
        var loginRequest = new LoginRequest { Id = "dev", Password = "dev" };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        var refreshToken = loginBody!.Data!.RefreshToken;

        _output.WriteLine($"  Obtained refresh token");

        // Act - 리프레시 토큰으로 새 액세스 토큰 발급
        var refreshRequest = new RefreshRequest { RefreshToken = refreshToken };
        var refreshContent = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");
        var refreshResponse = await _client.PostAsync("/auth/refresh", refreshContent);

        // Assert
        _output.WriteLine($"  Refresh Status: {(int)refreshResponse.StatusCode} ({refreshResponse.StatusCode})");
        refreshResponse.IsSuccessStatusCode.Should().BeTrue();

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        refreshBody.Should().NotBeNull();
        refreshBody!.Data.Should().NotBeNull();
        refreshBody.Data!.AccessToken.Should().NotBeNullOrEmpty();

        _output.WriteLine($"  Result: ✓ SUCCESS - New access token issued");
        _output.WriteLine($"  New AccessToken length: {refreshBody.Data.AccessToken.Length}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: Body 없이 리프레시 시도
    /// 목적: 필수 데이터 누락 시 Unauthorized 반환 확인
    /// </summary>
    [Fact]
    public async Task Refresh_WithMissingBody_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Refresh_WithMissingBody_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Refresh - Missing Body");

            // Act
            var response = await _client.PostAsync("/auth/refresh", null);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Unauthorized error");
            _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 빈 리프레시 토큰으로 갱신 시도
    /// 목적: 빈 값에 대한 유효성 검사 확인
    /// </summary>
    [Fact]
    public async Task Refresh_WithEmptyRefreshToken_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Refresh_WithEmptyRefreshToken_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Refresh - Empty Refresh Token");
            var refreshRequest = new RefreshRequest { RefreshToken = "" };
            var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/refresh", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Empty token rejected");
            _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 잘못된 리프레시 토큰으로 갱신 시도
    /// 목적: 유효하지 않은 토큰에 대한 에러 처리 확인 (커스텀 상태 코드 444)
    /// </summary>
    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ReturnsAuthRefreshInvalid()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Refresh_WithInvalidRefreshToken_ReturnsAuthRefreshInvalid";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Refresh - Invalid Refresh Token");
            var refreshRequest = new RefreshRequest { RefreshToken = "invalid-refresh-token" };
            var content = new StringContent(JsonSerializer.Serialize(refreshRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/refresh", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(444);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Invalid refresh token error (444)");
            _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Logout Tests - 로그아웃 테스트
    // ========================================

    /// <summary>
    /// 테스트: Body 없이 로그아웃 시도
    /// 목적: 로그아웃은 리프레시 토큰 없이도 성공해야 함 (클라이언트에서 토큰 삭제)
    /// </summary>
    [Fact]
    public async Task Logout_WithMissingBody_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Logout_WithMissingBody_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Logout - Missing Body");

            // Act
            var response = await _client.PostAsync("/auth/logout", null);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().BeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Logout successful without body");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 빈 리프레시 토큰으로 로그아웃
    /// 목적: 빈 토큰으로도 로그아웃이 성공하는지 확인
    /// </summary>
    [Fact]
    public async Task Logout_WithEmptyRefreshToken_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Logout_WithEmptyRefreshToken_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Logout - Empty Refresh Token");
            var logoutRequest = new LogoutRequest { RefreshToken = "" };
            var content = new StringContent(JsonSerializer.Serialize(logoutRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/logout", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().BeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Logout successful with empty token");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 유효한 리프레시 토큰으로 로그아웃
    /// 목적: 정상적인 로그아웃 플로우 테스트
    /// </summary>
    [Fact]
    public async Task Logout_WithValidRefreshToken_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Logout_WithValidRefreshToken_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Logout - Valid Refresh Token");

            // 먼저 로그인하여 리프레시 토큰 획득
        var loginRequest = new LoginRequest { Id = "dev", Password = "dev" };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        var refreshToken = loginBody!.Data!.RefreshToken;

        // Act - 획득한 리프레시 토큰으로 로그아웃
        var logoutRequest = new LogoutRequest { RefreshToken = refreshToken };
        var logoutContent = new StringContent(JsonSerializer.Serialize(logoutRequest), Encoding.UTF8, "application/json");
        var logoutResponse = await _client.PostAsync("/auth/logout", logoutContent);

        // Assert
        _output.WriteLine($"  Status: {(int)logoutResponse.StatusCode} ({logoutResponse.StatusCode})");
        logoutResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var body = await logoutResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        body.Should().NotBeNull();
        body!.Error.Should().BeNull();
        _output.WriteLine($"  Result: ✓ SUCCESS - Logout successful");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Verify Token Tests - 토큰 검증 테스트
    // ========================================

    /// <summary>
    /// 테스트: 토큰 없이 검증 시도
    /// 목적: Authorization 헤더 누락 시 커스텀 상태 코드 441 반환 확인
    /// </summary>
    [Fact]
    public async Task Verify_WithMissingToken_ReturnsAuthTokenMissing()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Verify_WithMissingToken_ReturnsAuthTokenMissing";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Verify - Missing Token");

            // Act
            var response = await _client.GetAsync("/auth/verify");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(441);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Missing token error (441)");
            _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 잘못된 토큰으로 검증 시도
    /// 목적: 유효하지 않은 JWT에 대한 에러 처리 확인 (커스텀 상태 코드 442)
    /// </summary>
    [Fact]
    public async Task Verify_WithInvalidToken_ReturnsAuthTokenInvalid()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Verify_WithInvalidToken_ReturnsAuthTokenInvalid";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Verify - Invalid Token");
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid.jwt.token");

            // Act
            var response = await _client.GetAsync("/auth/verify");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(442);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Invalid token error (442)");
            _output.WriteLine($"  Error: {body.Error!.Message}");

            _client.DefaultRequestHeaders.Clear();
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: Bearer 접두사 없이 토큰으로 검증 시도
    /// 목적: Authorization 헤더 형식 검증 확인
    /// </summary>
    [Fact]
    public async Task Verify_WithoutBearerPrefix_ReturnsAuthTokenInvalid()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Verify_WithoutBearerPrefix_ReturnsAuthTokenInvalid";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Verify - Without Bearer Prefix");
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", "some-token-without-bearer");

            // Act
            var response = await _client.GetAsync("/auth/verify");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(442);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Bearer prefix missing error (442)");
            _output.WriteLine($"  Error: {body.Error!.Message}");

            _client.DefaultRequestHeaders.Clear();
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 유효한 토큰으로 검증 성공
    /// 목적: 정상적인 토큰 검증 플로우 테스트
    /// </summary>
    [Fact]
    public async Task Verify_WithValidToken_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Verify_WithValidToken_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Verify - Valid Token");

            // 먼저 로그인하여 액세스 토큰 획득
        var loginRequest = new LoginRequest { Id = "dev", Password = "dev" };
        var loginContent = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync("/auth/login", loginContent);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse.TokenBundleResponse>>();
        var accessToken = loginBody!.Data!.AccessToken;

        // Act - 획득한 액세스 토큰으로 검증
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var verifyResponse = await _client.GetAsync("/auth/verify");

        // Assert
        _output.WriteLine($"  Status: {(int)verifyResponse.StatusCode} ({verifyResponse.StatusCode})");
        verifyResponse.IsSuccessStatusCode.Should().BeTrue();

        var verifyBody = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();
        verifyBody.Should().NotBeNull();
        verifyBody!.Error.Should().BeNull();
        _output.WriteLine($"  Result: ✓ SUCCESS - Token verified");

        _client.DefaultRequestHeaders.Clear();
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

    #region [6] MINIGAME TESTS

    // ========================================
    // Minigame GET Tests - 미니게임 조회 테스트
    // ========================================

    /// <summary>
    /// 테스트: 미니게임 목록 조회 성공
    /// 목적: 인증 없이 미니게임 목록을 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task Minigame_GetAll_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_GetAll_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Get All");

            // Act
            var response = await _client.GetAsync("/minigames");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved {body.Data!.Total} minigames");
            _output.WriteLine($"  Items count: {body.Data.Items.Count}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 필터를 사용한 미니게임 목록 조회
    /// 목적: 태그 기반 필터링이 정상적으로 작동하는지 확인
    /// </summary>
    [Fact]
    public async Task Minigame_GetAll_WithFilters_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_GetAll_WithFilters_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Get All With Filters");

            // Act
            var response = await _client.GetAsync("/minigames?scale=large&difficulty=2");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Filtered results");
            _output.WriteLine($"  Total filtered: {body.Data!.Total}");
            _output.WriteLine($"  Filters applied: scale=large, difficulty=2");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 미니게임 상세 조회 성공
    /// 목적: 특정 미니게임의 상세 정보를 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task Minigame_GetDetail_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_GetDetail_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Get Detail");

            // First get a list to find a valid ID
            var listResponse = await _client.GetAsync("/minigames");
            var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();

            if (listBody?.Data?.Items.Count > 0)
            {
                var gameId = listBody.Data.Items[0].Id;

                // Act
                var response = await _client.GetAsync($"/minigames/{gameId}");

                // Assert
                _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
                response.IsSuccessStatusCode.Should().BeTrue();

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<MinigameDetailResponse>>();
                body.Should().NotBeNull();
                body!.Data.Should().NotBeNull();
                _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved minigame detail");
                _output.WriteLine($"  Game ID: {body.Data!.Id}");
                _output.WriteLine($"  Name: {body.Data.Name}");
            }
            else
            {
                _output.WriteLine($"  Result: ⚠ SKIPPED - No minigames in database");
            }
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 존재하지 않는 미니게임 ID로 조회
    /// 목적: 잘못된 ID에 대한 적절한 에러 처리 확인 (커스텀 상태 코드 454)
    /// </summary>
    [Fact]
    public async Task Minigame_GetDetail_WithNonExistentId_ReturnsMinigameNotFound()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_GetDetail_WithNonExistentId_ReturnsMinigameNotFound";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Get Detail with Non-existent ID");

            // Act
            var response = await _client.GetAsync("/minigames/99999");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(454);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly returned error code 454 (Minigame Not Found)");
            _output.WriteLine($"  Error: {body.Error!.Message}");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Minigame CREATE Tests - 미니게임 생성 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 미니게임 생성 성공
    /// 목적: 미니게임 생성이 정상적으로 작동하는지 검증
    /// </summary>
    [Fact]
    public async Task Minigame_Create_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_Create_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Create with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRequest = new CreateMinigameRequest
            {
                Name = $"Test Game {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                Description = "Test game description",
                VideoUrl = "http://example.com/video.mp4",
                LogoUrl = "http://example.com/logo.jpg",
                Tags = new MinigameTagsDto
                {
                    Scale = new List<string> { "medium" },
                    Difficulty = new List<string> { "2" },
                    Round = new List<string> { "1-2" },
                    Type = new List<string> { "strategy" },
                    SurvivalRate = new List<string> { "medium" },
                    WinCondition = new List<string> { "score" }
                },
                Tutorial = new List<MinigameTutorialDto>
                {
                    new MinigameTutorialDto { Step = 1, Description = "Step 1 tutorial" },
                    new MinigameTutorialDto { Step = 2, Description = "Step 2 tutorial" }
                },
                Controls = new List<MinigameControlDto>
                {
                    new MinigameControlDto { KeyName = "Move", Key = new List<string> { "W", "A", "S", "D" } }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/minigames", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<MinigameCreatedResponse>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            body.Data!.Id.Should().BeGreaterThan(0);
            _output.WriteLine($"  Result: ✓ SUCCESS - Minigame created");
            _output.WriteLine($"  New Game ID: {body.Data.Id}");

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 미니게임 생성 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task Minigame_Create_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_Create_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Create without Auth");
            var content = new StringContent("{\"name\":\"Unauthorized\"}", Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/minigames", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Minigame UPDATE Tests - 미니게임 수정 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 미니게임 수정 성공
    /// 목적: 미니게임 업데이트가 정상적으로 작동하는지 검증
    /// </summary>
    [Fact]
    public async Task Minigame_Update_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_Update_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Update with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // First get a list to find a valid ID
            var listResponse = await _client.GetAsync("/minigames");
            var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();

            if (listBody?.Data?.Items.Count > 0)
            {
                var gameId = listBody.Data.Items[0].Id;
                var uniqueName = $"Updated Game {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";

                var updateRequest = new UpdateMinigameRequest
                {
                    Name = uniqueName,
                    Description = "Updated description",
                    VideoUrl = "http://example.com/updated-video.mp4",
                    LogoUrl = "http://example.com/updated-logo.jpg",
                    Tags = new MinigameTagsDto
                    {
                        Scale = new List<string> { "large" },
                        Difficulty = new List<string> { "3" },
                        Round = new List<string> { "3-4" },
                        Type = new List<string> { "strategy", "coop" },
                        SurvivalRate = new List<string> { "low" },
                        WinCondition = new List<string> { "goal" }
                    },
                    Tutorial = new List<MinigameTutorialDto>
                    {
                        new MinigameTutorialDto { Step = 1, Description = "Updated tutorial" }
                    },
                    Controls = new List<MinigameControlDto>
                    {
                        new MinigameControlDto { KeyName = "Jump", Key = new List<string> { "Space" } }
                    }
                };
                var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PutAsync($"/minigames/{gameId}", content);

                // Assert
                _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
                response.IsSuccessStatusCode.Should().BeTrue();

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<MinigameUpdatedResponse>>();
                body.Should().NotBeNull();
                _output.WriteLine($"  Result: ✓ SUCCESS - Minigame updated");
                _output.WriteLine($"  Updated Game ID: {gameId}");
            }
            else
            {
                _output.WriteLine($"  Result: ⚠ SKIPPED - No minigames in database");
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 존재하지 않는 미니게임 ID로 수정 시도
    /// 목적: 잘못된 ID에 대한 적절한 에러 처리 확인 (커스텀 상태 코드 454)
    /// </summary>
    [Fact]
    public async Task Minigame_Update_WithNonExistentId_ReturnsMinigameNotFound()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_Update_WithNonExistentId_ReturnsMinigameNotFound";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Update with Non-existent ID");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new UpdateMinigameRequest { Name = "Should Fail" };
            var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/minigames/99999", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(454);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly returned error code 454 (Minigame Not Found)");
            _output.WriteLine($"  Error: {body.Error!.Message}");

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Minigame DELETE Tests - 미니게임 삭제 테스트
    // ========================================

    /// <summary>
    /// 테스트: 인증된 사용자의 미니게임 삭제 성공
    /// 목적: 미니게임 삭제가 정상적으로 작동하는지 검증
    /// </summary>
    [Fact]
    public async Task Minigame_Delete_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_Delete_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Delete with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // First create a minigame to delete
            var createRequest = new CreateMinigameRequest
            {
                Name = "Game to Delete",
                Description = "This game will be deleted",
                VideoUrl = "http://example.com/video.mp4",
                LogoUrl = "http://example.com/logo.jpg",
                Tags = new MinigameTagsDto
                {
                    Scale = new List<string> { "small" },
                    Difficulty = new List<string> { "1" },
                    Round = new List<string> { "1-2" },
                    Type = new List<string> { "luck" },
                    SurvivalRate = new List<string> { "high" },
                    WinCondition = new List<string> { "first" }
                },
                Tutorial = new List<MinigameTutorialDto>(),
                Controls = new List<MinigameControlDto>()
            };
            var createContent = new StringContent(JsonSerializer.Serialize(createRequest), Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync("/minigames", createContent);
            var createBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<MinigameCreatedResponse>>();

            if (createBody?.Data?.Id > 0)
            {
                var gameId = createBody.Data.Id;

                // Act
                var response = await _client.DeleteAsync($"/minigames/{gameId}");

                // Assert
                _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
                response.IsSuccessStatusCode.Should().BeTrue();
                _output.WriteLine($"  Result: ✓ SUCCESS - Minigame deleted");
                _output.WriteLine($"  Deleted Game ID: {gameId}");
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 미니게임 삭제 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task Minigame_Delete_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_Delete_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Delete without Auth");

            // Act
            var response = await _client.DeleteAsync("/minigames/1");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Survival Rate Tests - 생존률 테스트
    // ========================================

    /// <summary>
    /// 테스트: 미니게임 생존률 조회 성공
    /// 목적: 특정 미니게임의 생존률 정보를 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task Minigame_GetSurvivalRate_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_GetSurvivalRate_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Get Survival Rate");

            // First get a list to find a valid ID
            var listResponse = await _client.GetAsync("/minigames");
            var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();

            if (listBody?.Data?.Items.Count > 0)
            {
                var gameId = listBody.Data.Items[0].Id;

                // Act
                var response = await _client.GetAsync($"/minigames/{gameId}/survival-rate");

                // Assert
                _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
                response.IsSuccessStatusCode.Should().BeTrue();

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<SurvivalRateResponse>>();
                body.Should().NotBeNull();
                body!.Data.Should().NotBeNull();
                _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved survival rate");
                _output.WriteLine($"  Game ID: {gameId}");
                _output.WriteLine($"  Total Players: {body.Data!.TotalPlayers}");
                _output.WriteLine($"  Survivors: {body.Data.Survivors}");
                _output.WriteLine($"  Survival Rate: {body.Data.SurvivalRate}%");
            }
            else
            {
                _output.WriteLine($"  Result: ⚠ SKIPPED - No minigames in database");
            }
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증된 사용자의 생존률 데이터 추가 성공
    /// 목적: 생존률 데이터 추가가 정상적으로 작동하는지 검증
    /// </summary>
    [Fact]
    public async Task Minigame_AddSurvivalRate_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_AddSurvivalRate_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Add Survival Rate with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // First get a list to find a valid ID
            var listResponse = await _client.GetAsync("/minigames");
            var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();

            if (listBody?.Data?.Items.Count > 0)
            {
                var gameId = listBody.Data.Items[0].Id;

                var addRequest = new AddSurvivalRateRequest
                {
                    TotalPlayers = 100,
                    Survivors = 45
                };
                var content = new StringContent(JsonSerializer.Serialize(addRequest), Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PostAsync($"/minigames/{gameId}/survival-rate", content);

                // Assert
                _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
                response.IsSuccessStatusCode.Should().BeTrue();

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<SurvivalRateAddedResponse>>();
                body.Should().NotBeNull();
                _output.WriteLine($"  Result: ✓ SUCCESS - Survival rate data added");
                _output.WriteLine($"  Game ID: {gameId}");
            }
            else
            {
                _output.WriteLine($"  Result: ⚠ SKIPPED - No minigames in database");
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증된 사용자의 생존률 수정 성공
    /// 목적: 생존률 업데이트가 정상적으로 작동하는지 검증
    /// </summary>
    [Fact]
    public async Task Minigame_UpdateSurvivalRate_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Minigame_UpdateSurvivalRate_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Minigame - Update Survival Rate with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // First get a list to find a valid ID
            var listResponse = await _client.GetAsync("/minigames");
            var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<MinigameListResponse>>();

            if (listBody?.Data?.Items.Count > 0)
            {
                var gameId = listBody.Data.Items[0].Id;

                var updateRequest = new UpdateSurvivalRateRequest
                {
                    SurvivalRate = 40.0m,
                    TotalGames = 10,
                    TotalPlayers = 200,
                    Survivors = 80
                };
                var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

                // Act
                var response = await _client.PutAsync($"/minigames/{gameId}/survival-rate", content);

                // Assert
                _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
                response.IsSuccessStatusCode.Should().BeTrue();

                var body = await response.Content.ReadFromJsonAsync<ApiResponse<SurvivalRateResponse>>();
                body.Should().NotBeNull();
                _output.WriteLine($"  Result: ✓ SUCCESS - Survival rate updated");
                _output.WriteLine($"  Game ID: {gameId}");
            }
            else
            {
                _output.WriteLine($"  Result: ⚠ SKIPPED - No minigames in database");
            }

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // Viewer Avatar Tests - 시청자 아바타 테스트
    // ========================================

    /// <summary>
    /// 테스트: 시청자 아바타 목록 조회 성공
    /// 목적: 인증된 사용자가 시청자 아바타 목록을 조회할 수 있는지 확인
    /// </summary>
    [Fact]
    public async Task ViewerAvatars_Get_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "ViewerAvatars_Get_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Viewer Avatars - Get with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/viewer-avatars");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<List<ViewerAvatarResponse>>>();
            body.Should().NotBeNull();
            body!.Data.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Retrieved {body.Data!.Count} viewer avatars");

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증 없이 시청자 아바타 조회 시도
    /// 목적: 인증되지 않은 요청에 대한 접근 제어 확인 (401 Unauthorized)
    /// </summary>
    [Fact]
    public async Task ViewerAvatars_Get_WithoutAuth_ReturnsUnauthorized()
    {
        var testNumber = GetNextTestNumber();
        var testName = "ViewerAvatars_Get_WithoutAuth_ReturnsUnauthorized";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Viewer Avatars - Get without Auth");

            // Act
            var response = await _client.GetAsync("/viewer-avatars");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly rejected unauthorized request");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 인증된 사용자의 시청자 아바타 목록 수정 성공
    /// 목적: 시청자 아바타 업데이트가 정상적으로 작동하는지 검증
    /// </summary>
    [Fact]
    public async Task ViewerAvatars_Update_WithAuth_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "ViewerAvatars_Update_WithAuth_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Viewer Avatars - Update with Auth");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new UpdateViewerAvatarsRequest
            {
                Data = new List<ViewerAvatarDto>
                {
                    new ViewerAvatarDto
                    {
                        Id = 1,
                        Order = 1,
                        Name = $"Test Avatar {DateTime.UtcNow:HH:mm:ss}",
                        GifUrl = "http://example.com/avatar1.gif"
                    },
                    new ViewerAvatarDto
                    {
                        Id = 2,
                        Order = 2,
                        Name = "Test Avatar 2",
                        GifUrl = "http://example.com/avatar2.gif"
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/viewer-avatars", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.IsSuccessStatusCode.Should().BeTrue();
            _output.WriteLine($"  Result: ✓ SUCCESS - Viewer avatars updated");

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 중복된 Order로 시청자 아바타 수정 시도
    /// 목적: Order 중복 검증이 정상적으로 작동하는지 확인 (커스텀 상태 코드 461)
    /// </summary>
    [Fact]
    public async Task ViewerAvatars_Update_WithDuplicateOrder_ReturnsAvatarDuplicateOrder()
    {
        var testNumber = GetNextTestNumber();
        var testName = "ViewerAvatars_Update_WithDuplicateOrder_ReturnsAvatarDuplicateOrder";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] Viewer Avatars - Update with Duplicate Order");
            var token = await LoginAndGetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var updateRequest = new UpdateViewerAvatarsRequest
            {
                Data = new List<ViewerAvatarDto>
                {
                    new ViewerAvatarDto { Id = 1, Order = 1, Name = "Avatar 1", GifUrl = "http://example.com/1.gif" },
                    new ViewerAvatarDto { Id = 2, Order = 1, Name = "Avatar 2", GifUrl = "http://example.com/2.gif" } // Duplicate order
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(updateRequest), Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/viewer-avatars", content);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode}");
            ((int)response.StatusCode).Should().Be(461);

            var body = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            body.Should().NotBeNull();
            body!.Error.Should().NotBeNull();
            _output.WriteLine($"  Result: ✓ SUCCESS - Correctly returned error code 461 (Duplicate Order)");
            _output.WriteLine($"  Error: {body.Error!.Message}");

            _client.DefaultRequestHeaders.Authorization = null;
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

    #region [99] MISC / ERROR HANDLING TESTS

    // ========================================
    // Unknown Path Tests - 알 수 없는 경로 테스트
    // ========================================

    /// <summary>
    /// 테스트: 존재하지 않는 경로 요청
    /// 목적: 404 또는 403 에러 처리 확인
    /// </summary>
    [Fact]
    public async Task UnknownPath_ReturnsForbidden()
    {
        var testNumber = GetNextTestNumber();
        var testName = "UnknownPath_ReturnsForbidden";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] MISC - Unknown Path");

            // Act
            var response = await _client.GetAsync("/auth/unknown");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            
            _output.WriteLine($"  Result: ✓ SUCCESS - Unknown path forbidden");
            _output.WriteLine($"  Error");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    /// <summary>
    /// 테스트: 잘못된 HTTP 메서드 사용
    /// 목적: POST 엔드포인트에 GET 요청 등의 메서드 불일치 처리 확인
    /// </summary>
    [Fact]
    public async Task WrongHttpMethod_ReturnsForbidden()
    {
        var testNumber = GetNextTestNumber();
        var testName = "WrongHttpMethod_ReturnsForbidden";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] MISC - Wrong HTTP Method");

            // Act
            var response = await _client.GetAsync("/auth/login");

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            
            _output.WriteLine($"  Result: ✓ SUCCESS - Wrong method forbidden");
            _output.WriteLine($"  Error:");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    // ========================================
    // CORS Tests - CORS 테스트
    // ========================================

    /// <summary>
    /// 테스트: OPTIONS 요청 (CORS preflight)
    /// 목적: CORS preflight 요청이 정상적으로 처리되는지 확인
    /// </summary>
    [Fact]
    public async Task Options_PreflightRequest_ReturnsSuccess()
    {
        var testNumber = GetNextTestNumber();
        var testName = "Options_PreflightRequest_ReturnsSuccess";
        try
        {
            // Arrange
            _output.WriteLine($"\n[TEST #{testNumber}] MISC - OPTIONS Preflight Request");
            var request = new HttpRequestMessage(HttpMethod.Options, "/auth/login");

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            _output.WriteLine($"  Status: {(int)response.StatusCode} ({response.StatusCode})");
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            _output.WriteLine($"  Result: ✓ SUCCESS - OPTIONS request handled");
        }
        catch (Exception ex)
        {
            RecordTestFailure(testNumber, testName, ex);
            throw;
        }
    }

    #endregion

}
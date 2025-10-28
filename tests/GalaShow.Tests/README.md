# GalaShow Integration Tests

GalaShow API의 모든 엔드포인트에 대한 통합 테스트입니다. 실제 배포된 API를 HTTP로 호출하여 테스트합니다.

## 테스트 구성

총 **23개 테스트**가 모든 API 엔드포인트를 검증합니다.

### [5-1] 로그인 (3개 테스트)
- ✅ 빈 요청 Body → BadRequest (400)
- ✅ 빈 자격증명 → BadRequest (400)
- ✅ 잘못된 자격증명 → AuthInvalidCredentials (600)

### [5-2] 토큰 재발급 (3개 테스트)
- ✅ 빈 요청 Body → Unauthorized (401)
- ✅ 빈 리프레시 토큰 → Unauthorized (401)
- ✅ 잘못된 리프레시 토큰 → AuthRefreshInvalid (604)

### [5-3] 로그아웃 (2개 테스트)
- ✅ 빈 요청 Body → Success (200)
- ✅ 빈 리프레시 토큰 → Success (200)

### [5-4] 토큰 검증 (3개 테스트)
- ✅ 토큰 누락 → AuthTokenMissing (601)
- ✅ 잘못된 토큰 → AuthTokenInvalid (602)
- ✅ Bearer 프리픽스 없음 → AuthTokenInvalid (602)

### [5-5] 기타 (3개 테스트)
- ✅ 존재하지 않는 경로 → PathNotFound (404)
- ✅ 잘못된 HTTP 메서드 → PathNotFound (404)
- ✅ OPTIONS 요청 → Success (200)

### [6-1] 배너 (2개 테스트)
- ✅ 전체 조회 → Success (200)
- ✅ 인증 없이 업데이트 → Unauthorized (401)

### [7-1] 배경 (2개 테스트)
- ✅ 전체 조회 → Success (200)
- ✅ 인증 없이 업데이트 → Unauthorized (401)

### [8-1] 정책 (2개 테스트)
- ✅ 조회 → Success (200)
- ✅ 인증 없이 업데이트 → Unauthorized (401)

### [9-1] SNS 링크 (2개 테스트)
- ✅ 조회 → Success (200)
- ✅ 인증 없이 업데이트 → Unauthorized (401)

### [10-1] Chzzk 프록시 (1개 테스트)
- ✅ API 호출 → Response 확인

## API Base URL 설정

테스트할 API의 Base URL을 다음 세 가지 방법으로 설정할 수 있습니다:

### 방법 1: appsettings.json 수정 (권장)

`tests/GalaShow.Token.Tests/appsettings.json` 파일을 수정합니다:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://127.0.0.1:3000"
  }
}
```

### 방법 2: 환경 변수 설정

환경 변수는 appsettings.json 설정을 덮어씁니다.

**Windows (PowerShell)**
```powershell
$env:API_BASE_URL="https://your-api-url.execute-api.ap-northeast-2.amazonaws.com/dev"
dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj
```

**Windows (CMD)**
```cmd
set API_BASE_URL=https://your-api-url.execute-api.ap-northeast-2.amazonaws.com/dev
dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj
```

**Linux / macOS**
```bash
export API_BASE_URL="https://your-api-url.execute-api.ap-northeast-2.amazonaws.com/dev"
dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj
```

### 우선순위

설정 값의 우선순위는 다음과 같습니다:
1. **환경 변수** `API_BASE_URL` (최우선)
2. **appsettings.json** `ApiSettings:BaseUrl`
3. 기본값: `http://127.0.0.1:3000` (로컬 SAM)

## 로컬 개발 환경

### AWS SAM Local 사용 (권장)

```bash
# SAM Local API 시작
sam local start-api

# 다른 터미널에서 테스트 실행
dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj
```

기본적으로 `http://127.0.0.1:3000`에서 실행됩니다.

## 테스트 실행

### 전체 테스트 실행
```bash
dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj
```

### 상세 출력으로 실행 (로그 확인)
```bash
dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj -v n
```

테스트 로그에서 각 테스트의 성공/실패 상태를 확인할 수 있습니다:
```
[성공] 로그인 - 빈 요청 Body
[성공] 로그인 - 빈 자격증명
[성공] 배너 - 전체 조회
[실패] 기타 - 존재하지 않는 경로
```

### 특정 테스트만 실행
```bash
# 로그인 테스트만
dotnet test --filter "FullyQualifiedName~Login"

# 배너 테스트만
dotnet test --filter "FullyQualifiedName~Banner"

# 토큰 관련 테스트만
dotnet test --filter "FullyQualifiedName~Verify|FullyQualifiedName~Refresh"
```

## 사용 기술

- **xUnit**: 테스트 프레임워크
- **FluentAssertions**: 읽기 쉬운 assertion
- **HttpClient**: 실제 HTTP API 호출
- **Microsoft.Extensions.Configuration**: 설정 파일 및 환경 변수 관리
- **ITestOutputHelper**: 테스트 로그 출력

## 주의사항

⚠️ **실제 API 호출**: 이 테스트는 실제 배포된 API 또는 로컬 SAM을 호출합니다. 테스트 실행 전에 다음을 확인하세요:

1. **로컬 테스트**: SAM Local이 실행 중인지 확인
   ```bash
   sam local start-api
   ```

2. **배포 API 테스트**: API가 배포되어 있고 접근 가능한 상태인지 확인

3. 네트워크 연결이 정상인지 확인

4. API Gateway URL이 올바르게 설정되어 있는지 확인

## CI/CD 통합

### GitHub Actions 예시

```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Setup AWS SAM
        uses: aws-actions/setup-sam@v2

      - name: Start SAM Local
        run: |
          sam local start-api &
          sleep 10

      - name: Run Integration Tests
        run: dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj -v n
```

### 배포된 API 테스트

```yaml
- name: Run integration tests against deployed API
  env:
    API_BASE_URL: ${{ secrets.DEV_API_URL }}
  run: dotnet test tests/GalaShow.Token.Tests/GalaShow.Token.Tests.csproj -v n
```

## 테스트 결과 예시

```
통과!  - 실패:     0, 통과:    23, 건너뜀:     0, 전체:    23, 기간: 5 s

표준 출력:
[테스트 시작] Base URL: http://127.0.0.1:3000
[성공] 로그인 - 빈 요청 Body
[성공] 로그인 - 빈 자격증명
[성공] 로그인 - 잘못된 자격증명
[성공] 토큰 재발급 - 빈 요청 Body
[성공] 토큰 재발급 - 빈 리프레시 토큰
[성공] 토큰 재발급 - 잘못된 리프레시 토큰
[성공] 로그아웃 - 빈 요청 Body
[성공] 로그아웃 - 빈 리프레시 토큰
[성공] 토큰 검증 - 토큰 누락
[성공] 토큰 검증 - 잘못된 토큰
[성공] 토큰 검증 - Bearer 프리픽스 없음
[성공] 배너 - 전체 조회
[성공] 배너 - 인증 없이 업데이트
[성공] 배경 - 전체 조회
[성공] 배경 - 인증 없이 업데이트
[성공] 정책 - 조회
[성공] 정책 - 인증 없이 업데이트
[성공] SNS 링크 - 조회
[성공] SNS 링크 - 인증 없이 업데이트
...
```

## 문제 해결

### 연결 거부 오류
```
System.Net.Http.HttpRequestException: Connection refused
```
→ SAM Local이 실행되지 않았습니다. `sam local start-api`를 실행하세요.

### 403 Forbidden
일부 엔드포인트에서 403이 반환될 수 있습니다. 이는 CORS 설정이나 권한 문제일 수 있으며 정상적인 동작일 수 있습니다.

### 느린 테스트 실행
SAM Local은 Lambda 함수를 Docker 컨테이너로 실행하므로 처음 실행 시 느릴 수 있습니다. Warm-up 후에는 더 빨라집니다.

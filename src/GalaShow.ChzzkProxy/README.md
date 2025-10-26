# GalaShow.ChzzkProxy

Chzzk API에 대한 프록시 Lambda 함수입니다. CORS 문제를 해결하고 클라이언트에서 Chzzk API를 안전하게 호출할 수 있도록 합니다.

## 기능

- Chzzk API (`https://openapi.chzzk.naver.com`)로의 모든 요청을 프록시
- CORS 헤더 자동 추가
- Authorization, Client-Id, Client-Secret 헤더 전달
- GET, POST, PUT, DELETE 메서드 지원
- 쿼리 파라미터 및 요청 바디 전달

## API 엔드포인트

프록시 함수는 `/chzzk` 경로에서 시작하는 모든 요청을 처리합니다:

### Dev 환경
- `https://api-dev.galashow.xyz/chzzk/{path}` → `https://openapi.chzzk.naver.com/{path}`

### Prod 환경
- `https://api.galashow.xyz/chzzk/{path}` → `https://openapi.chzzk.naver.com/{path}`

## 지원하는 Chzzk API

### Auth API
- `POST /chzzk/auth/v1/token` - 토큰 발급 및 갱신
- `POST /chzzk/auth/v1/token/revoke` - 토큰 삭제

### Channel API
- `GET /chzzk/open/v1/users/me` - 사용자 정보 조회 (Authorization 헤더 필요)
- `GET /chzzk/open/v1/channels?channelIds={channelIds}` - 채널 정보 조회 (Client-Id, Client-Secret 헤더 필요)

### Session API
- `GET /chzzk/open/v1/sessions/auth/client` - 클라이언트 세션 생성 (Client-Id, Client-Secret 헤더 필요)
- `POST /chzzk/open/v1/sessions/events/subscribe/chat` - 채팅 이벤트 구독 (Authorization 헤더 필요)
- `POST /chzzk/open/v1/sessions/events/subscribe/donation` - 도네이션 이벤트 구독 (Authorization 헤더 필요)
- `POST /chzzk/open/v1/sessions/events/subscribe/subscription` - 구독 이벤트 구독 (Authorization 헤더 필요)

## 빌드 및 배포

### 빌드
```bash
dotnet build src/GalaShow.ChzzkProxy/GalaShow.ChzzkProxy.csproj
```

### 로컬 테스트
```bash
sam build
sam local start-api --parameter-overrides StageNameParam=dev
```

### 배포 (Dev)
```bash
sam build
sam deploy --parameter-overrides StageNameParam=dev
```

### 배포 (Prod)
```bash
sam build
sam deploy --parameter-overrides StageNameParam=prod
```

또는 PowerShell 스크립트 사용:
```powershell
.\run-sam-local.ps1  # 로컬 테스트
```

## 클라이언트 코드 수정

TypeScript 클라이언트에서 `apiBaseUrl`을 프록시 URL로 변경:

```typescript
// 기존 (auth.ts, channel.ts, session.ts)
const getChzzkApiUrl = () => chzzkAuthStore.getState().apiBaseUrl;

// chzzkAuthStore 설정
const chzzkAuthStore = create<ChzzkAuthState>((set, get) => ({
    // 기존
    apiBaseUrl: 'https://openapi.chzzk.naver.com',

    // 변경 (Dev 환경)
    apiBaseUrl: 'https://api-dev.galashow.xyz/chzzk',

    // 또는 Prod 환경
    apiBaseUrl: 'https://api.galashow.xyz/chzzk',

    // ...
}));
```

이렇게 변경하면 모든 API 호출이 Lambda 프록시를 통해 이루어지며, CORS 문제가 해결됩니다.

## 사용 예시

### JavaScript/TypeScript
```typescript
// 기존 코드 그대로 사용 가능
const response = await chzzkAuthApi.getAccessToken({
    grant_type: 'authorization_code',
    code: 'auth_code',
    client_id: 'your_client_id',
    client_secret: 'your_client_secret'
});
```

### cURL
```bash
# Dev 환경
curl -X GET "https://api-dev.galashow.xyz/chzzk/open/v1/channels?channelIds=test-id" \
  -H "Client-Id: your-client-id" \
  -H "Client-Secret: your-client-secret"

# 사용자 정보 조회
curl -X GET "https://api-dev.galashow.xyz/chzzk/open/v1/users/me" \
  -H "Authorization: Bearer your-access-token"
```

## CORS 설정

프록시 함수는 다음 CORS 헤더를 자동으로 추가합니다:

```
Access-Control-Allow-Origin: *
Access-Control-Allow-Headers: Content-Type,Authorization,Client-Id,Client-Secret,Accept
Access-Control-Allow-Methods: GET,POST,PUT,DELETE,OPTIONS
```

## 로깅

CloudWatch Logs에서 다음 정보를 확인할 수 있습니다:
- 프록시된 요청 URL
- 응답 상태 코드
- 에러 메시지

로그 그룹: `/aws/lambda/DevChzzkProxyFunction` 또는 `/aws/lambda/ProdChzzkProxyFunction`

## 주의사항

- Lambda 타임아웃: 30초
- API Gateway 요청 크기 제한: 10MB
- 헤더는 대소문자 구분 없이 전달됩니다
- CORS preflight 요청(OPTIONS)은 자동으로 처리됩니다

## 트러블슈팅

### 502 Bad Gateway
- Chzzk API 서버가 응답하지 않는 경우
- 네트워크 연결 문제

### 500 Internal Server Error
- Lambda 함수 내부 오류
- CloudWatch Logs에서 자세한 에러 메시지 확인

### 403 Forbidden / 401 Unauthorized
- Authorization, Client-Id, Client-Secret 헤더가 올바르지 않은 경우
- Chzzk API 자체에서 거부한 경우

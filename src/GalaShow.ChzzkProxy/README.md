# GalaShow 치지직 연동 API

기존 `/chzzk` API Gateway와 Lambda를 사용한다. `GalaShow.ChzzkProxy`라는 프로젝트/리소스 이름은 배포 호환을 위해 유지하지만, 구현은 아래 기능만 제공하는 백엔드 연동 API다.

브라우저는 GalaShow API로 REST 요청을 보내고, 서버가 치지직 공식 API를 호출한다. OAuth 로그인 페이지는 브라우저에서 열며, 채팅 Socket.IO는 발급받은 치지직 세션 URL에 직접 연결한다.

## 경로

개발 base URL은 `https://api-dev.galashow.cloud/chzzk`, 운영은 `https://api.galashow.cloud/chzzk`다.

| Method | Base 뒤 경로 | 요청 |
| --- | --- | --- |
| GET | `/config` | 공개 `clientId`만 반환 |
| POST | `/auth/token` | JSON `{code,state}` |
| POST | `/auth/refresh` | JSON `{refreshToken}` |
| POST | `/auth/revoke` | JSON `{token,tokenTypeHint?}`, hint는 `access_token` 또는 `refresh_token` |
| GET | `/users/me` | 치지직 사용자 Bearer 토큰 |
| GET | `/channels?channelIds=id1,id2` | 치지직 Bearer 토큰, 채널 ID 1~20개 |
| POST | `/sessions` | 치지직 Bearer 토큰, 본문 없음 |
| POST | `/sessions/events/subscribe/{event}` | 치지직 Bearer 토큰, JSON `{sessionKey}` |
| OPTIONS | `/chzzk` 및 모든 하위 경로 | AWS/DB/치지직 접근 없이 응답 |

`event`는 `chat`, `donation`, `subscription`만 허용한다. 이 Bearer 토큰은 GalaShow 관리자 JWT와 다르다. 채널 조회는 `/open/v1/users/me`로 사용자를 확인한 다음 서버 앱 키를 사용한다. 세션은 공식 `/open/v1/sessions/auth`의 사용자 세션이다.

토큰 요청은 서버가 공식 camelCase 필드와 `grantType`, 앱 키를 구성한다. 기존 `/auth/v1/*`, `/open/v1/*` 경로와 임의 경로 전달은 지원하지 않으므로 PolyChat과 Client 변경을 함께 반영해야 한다. 브라우저의 `Client-Id`/`Client-Secret` 헤더와 요청 스키마에 없는 JSON 필드는 400으로 거부한다.

## 서버 설정과 배포

AWS Secrets Manager에 환경별 앱 설정을 다음 JSON 형태로 저장한다. 비밀 값은 저장소나 웹 환경변수에 넣지 않는다.

```json
{"clientId":"CHZZK_APP_CLIENT_ID","clientSecret":"CHZZK_APP_CLIENT_SECRET"}
```

SAM의 `ChzzkSecretArn` 파라미터는 Lambda의 `CHZZK_SECRET_ARN`에 연결되며, 해당 ARN에만 `secretsmanager:GetSecretValue`를 허용한다. 기본 Secrets Manager 암호화 키를 사용하는 같은 계정/리전의 secret을 기준으로 한다. 별도 고객 관리 KMS 키를 사용하면 해당 키의 복호화 권한도 별도로 구성해야 한다.

```powershell
./infra/deploy-api.ps1 -Stage dev -HostedZoneId 'Z0263745GATMIS12FEIH' -ChzzkSecretArn '<dev-secret-arn>' -Profile galashow
```

GitHub Actions에서는 `dev`/`prod` Environment의 Variable `CHZZK_SECRET_ARN`을 사용한다. 배포 인자를 생략하면 기존 스택 설정을 유지한다. 처음부터 설정이 없으면 치지직 앱 키가 필요한 기능은 503 `CHZZK_NOT_CONFIGURED`를 반환한다. 다른 API와 OPTIONS는 이 설정에 의존하지 않는다.

서버 로컬 실행은 ARN 대신 `CHZZK_CLIENT_ID`, `CHZZK_CLIENT_SECRET` 환경변수를 지원한다. ARN이 있으면 항상 Secrets Manager가 우선하며 조회 실패 시 다른 키로 대체하지 않는다. 읽은 secret은 실행 환경에서 5분간 캐시한다.

치지직 개발자 콘솔의 Redirect URI에는 사용할 웹 주소의 `/callback`을 등록한다. 예: `https://dev.galashow.cloud/callback`, `https://galashow.cloud/callback`, 로컬 개발 주소. 프론트엔드는 OAuth state와 callback origin/path를 확인한다. 사용자 access/refresh token은 현재 프론트엔드 메모리에 보관하며, 앱 Client Secret만 백엔드가 보관한다.

이 Lambda는 인터넷 접속을 위해 VPC 밖에서 실행한다. 현재 DB Lambda의 private subnet에는 NAT가 없으므로 외부 API 호출을 DB 초기화 경로에 합치지 않는다.

## 응답과 CORS

응답은 치지직의 `{code,message,content}` 형식을 유지한다. 서버가 생성하는 오류는 `{code: HTTP상태, message: 오류명}`이다. 모든 응답에 `Cache-Control: no-store`와 공통 CORS 정책을 적용하며 치지직 응답 헤더는 전달하지 않는다.

| 상태 | 의미 |
| --- | --- |
| 400 | 잘못된 JSON/필수 값/앱 키 전달 |
| 401 | 치지직 Bearer 누락 또는 형식 오류 |
| 404 / 405 | 미지원 경로 / 메서드 |
| 502 | 치지직 연결 실패 또는 잘못된 응답 |
| 503 | 서버 앱 설정 누락/조회 실패 |
| 504 | 치지직 요청 시간 초과 |

치지직이 보낸 정상 JSON 오류 상태와 본문은 유지한다. HTTP 호출은 10초 후 시간 초과이며 리디렉션을 따라가지 않는다. 토큰, 비밀 키, 요청 본문, 쿼리, 세션 URL은 로그에 기록하지 않는다.

`STAGE=dev`에서는 공통 허용 목록에 더해 HTTP/HTTPS의 정확한 `localhost` 호스트를 모든 포트에서 허용한다. 운영에서는 loopback origin을 허용하지 않는다. `CORS_ALLOWED_ORIGINS`와 기본 도메인 정책은 [공통 CORS 코드](../GalaShow.Common/Cors/CorsHandler.cs)를 따른다.

## 검증

```powershell
dotnet test GalaShow_API.sln --configuration Release --filter 'Category!=Integration'
```

가짜 HTTP 서버 경계로 공식 요청 형식, 앱 키 주입, 잘못된 요청 차단, 사용자 세션, 오류/CORS 및 secret 로딩을 검증한다. 실제 OAuth·채팅 연결은 앱 설정과 사용자 로그인 후 별도 확인해야 한다.

전체 계약은 [Swagger](../../../docs/swagger.yaml), 배포는 [인프라 안내](../../infra/README.md)를 따른다. 공식 근거: [인증](https://chzzk.gitbook.io/chzzk/chzzk-api/authorization), [세션](https://chzzk.gitbook.io/chzzk/chzzk-api/session).

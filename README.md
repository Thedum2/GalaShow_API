# GalaShow API

이 문서는 GalaShow API에 대한 개요를 제공하며, 인증 방법 및 각 엔드포인트에 대한 자세한 정보를 포함합니다.

## 인증 (Authentication)

GalaShow API는 JWT(JSON Web Token)를 사용하여 인증합니다. 대부분의 엔드포인트는 `Authorization` 헤더에 Bearer 토큰으로 액세스 토큰을 포함해야 합니다.

### 토큰 획득

`AccessToken`과 `RefreshToken`을 얻으려면 `/auth/login` 엔드포인트를 사용하십시오. `AccessToken`은 후속 요청을 인증하는 데 사용되며, `RefreshToken`은 현재 액세스 토큰이 만료되었을 때 사용자가 다시 로그인할 필요 없이 새 `AccessToken`을 얻는 데 사용될 수 있습니다.

### 토큰 갱신

`AccessToken`이 만료되면 `/auth/refresh` 엔드포인트를 사용하여 `RefreshToken`으로 새로운 `AccessToken`과 `RefreshToken` 쌍을 얻을 수 있습니다.

### 토큰 검증

`/auth/verify` 엔드포인트를 사용하여 `AccessToken`의 유효성을 검증할 수 있습니다.

## API 엔드포인트

### 1. 토큰 관리

| 엔드포인트 | 설명 | 인증 필요 | 요청 본문 | 쿼리 파라미터 | 경로 파라미터 |
|---|---|---|---|---|---|
| `POST /auth/login` | 사용자를 인증하고 JWT `AccessToken` 및 `RefreshToken`을 발급합니다. | 필요 없음 | `id` (문자열, 필수): 사용자 ID<br>`password` (문자열, 필수): 사용자 비밀번호 | 없음 | 없음 |
| `POST /auth/refresh` | `RefreshToken`을 사용하여 만료된 `AccessToken`을 갱신합니다. | 필요 없음 | `refreshToken` (문자열, 필수): 갱신 토큰 | 없음 | 없음 |
| `POST /auth/logout` | `RefreshToken`을 폐기하여 사용자를 로그아웃합니다. | 필요 없음 | `refreshToken` (문자열, 선택): 폐기할 갱신 토큰. 제공되지 않아도 요청은 성공을 반환합니다. | 없음 | 없음 |
| `GET /auth/verify` | `AccessToken`의 유효성을 검증합니다. | 필요 (Bearer 토큰) | 없음 | 없음 | 없음 |

**성공 응답 예시 (`/auth/login`, `/auth/refresh`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": {
    "accessToken": "eyJ...",
    "expiresIn": 3600,
    "accessExpiresAt": "2025-09-08T12:00:00Z",
    "refreshToken": "raw_refresh_token",
    "refreshExpiresIn": 604800,
    "refreshExpiresAt": "2025-09-15T12:00:00Z",
    "user": {
      "id": "admin",
      "role": "admin"
    }
  }
}
```

**성공 응답 예시 (`/auth/verify`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": {
    "sub": "admin",
    "role": "admin",
    "exp": "1757337600",
    "valid": true
  }
}
```

### 2. 배너 관리

| 엔드포인트 | 설명 | 인증 필요 | 요청 본문 | 쿼리 파라미터 | 경로 파라미터 |
|---|---|---|---|---|---|
| `GET /banners` | 모든 배너 정보를 조회합니다. | 필요 없음 | 없음 | 없음 | 없음 |
| `PUT /banners/{bannerId}` | 특정 배너의 메시지를 업데이트합니다. | 필요 (Bearer 토큰) | `message` (문자열, 필수): 배너의 새 메시지. | 없음 | `bannerId` (정수, 필수): 업데이트할 배너의 ID. |

**성공 응답 예시 (`GET /banners`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "message": "Welcome to GalaShow!",
      "imageUrl": "https://example.com/banner1.jpg"
    }
  ]
}
```

**성공 응답 예시 (`PUT /banners/{bannerId}`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": null
}
```

### 3. 배경 비디오 관리

| 엔드포인트 | 설명 | 인증 필요 | 요청 본문 | 쿼리 파라미터 | 경로 파라미터 |
|---|---|---|---|---|---|
| `GET /background` | 모든 배경 비디오 정보를 조회합니다. | 필요 없음 | 없음 | 없음 | 없음 |
| `PUT /background/{backId}` | 특정 배경 비디오의 메시지를 업데이트합니다. | 필요 (Bearer 토큰) | `message` (문자열, 필수): 배경 비디오의 새 메시지. | 없음 | `backId` (정수, 필수): 업데이트할 배경 비디오의 ID. |

**성공 응답 예시 (`GET /background`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "message": "Opening video",
      "videoUrl": "https://example.com/bg_video1.mp4"
    }
  ]
}
```

**성공 응답 예시 (`PUT /background/{backId}`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": null
}
```

### 4. 정책 관리

| 엔드포인트 | 설명 | 인증 필요 | 요청 본문 | 쿼리 파라미터 | 경로 파라미터 |
|---|---|---|---|---|---|
| `GET /policies` | 현재 서비스 약관 및 개인 정보 보호 정책을 조회합니다. | 필요 없음 | 없음 | 없음 | 없음 |
| `PUT /policies` | 서비스 약관 및 개인 정보 보호 정책을 업데이트합니다. | 필요 (Bearer 토큰) | `termsOfService` (문자열, 필수): 새 서비스 약관 내용.<br>`privacyPolicy` (문자열, 필수): 새 개인 정보 보호 정책 내용. | 없음 | 없음 |

**성공 응답 예시 (`GET /policies`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": {
    "termsOfService": "These are the updated terms of service...",
    "privacyPolicy": "This is the updated privacy policy..."
  }
}
```

**성공 응답 예시 (`PUT /policies`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": null
}
```

### 5. SNS 링크 관리

| 엔드포인트 | 설명 | 인증 필요 | 요청 본문 | 쿼리 파라미터 | 경로 파라미터 |
|---|---|---|---|---|---|
| `GET /sns-links` | 설정된 모든 SNS 링크를 조회합니다. | 필요 없음 | 없음 | 없음 | 없음 |
| `PUT /sns-links` | SNS 링크 목록을 업데이트합니다. 기존 링크를 제공된 목록으로 대체합니다. | 필요 (Bearer 토큰) | `data` (객체 배열, 필수): SNS 링크 객체 목록.<br>&nbsp;&nbsp;&nbsp;&nbsp;`id` (정수, 필수): SNS 링크의 ID.<br>&nbsp;&nbsp;&nbsp;&nbsp;`title` (문자열, 필수): SNS 링크의 제목.<br>&nbsp;&nbsp;&nbsp;&nbsp;`url` (문자열, 필수): SNS 링크의 URL.<br>&nbsp;&nbsp;&nbsp;&nbsp;`icon_url` (문자열, 필수): SNS 아이콘의 URL.<br>&nbsp;&nbsp;&nbsp;&nbsp;`order` (정수, 필수): SNS 링크의 표시 순서. | 없음 | 없음 |

**성공 응답 예시 (`GET /sns-links`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "title": "Facebook",
      "url": "https://facebook.com/galashow",
      "iconUrl": "https://example.com/fb_icon.png",
      "order": 1
    }
  ]
}
```

**성공 응답 예시 (`PUT /sns-links`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": null
}
```

### 6. 질문 및 카테고리 관리

| 엔드포인트 | 설명 | 인증 필요 | 요청 본문 | 쿼리 파라미터 | 경로 파라미터 |
|---|---|---|---|---|---|
| `GET /questions/random` | 무작위 질문 또는 지정된 수의 무작위 질문을 조회합니다. | 필요 (Bearer 토큰) | 없음 | `count` (정수, 선택): 조회할 무작위 질문 수 (기본값: 1). | 없음 |
| `GET /questions/{questionId}` | 특정 질문의 세부 정보를 조회합니다. | 필요 (Bearer 토큰) | 없음 | 없음 | `questionId` (정수, 필수): 질문의 ID. |
| `POST /questions` | 새 질문을 생성합니다. | 필요 (Bearer 토큰) | `categoryId` (정수, 필수): 질문이 속할 카테고리 ID.<br>`title` (문자열, 필수): 질문 텍스트.<br>`choices` (객체 배열, 필수): 질문에 대한 선택지 목록.<br>&nbsp;&nbsp;&nbsp;&nbsp;`text` (문자열, 필수): 선택지 텍스트.<br>&nbsp;&nbsp;&nbsp;&nbsp;`imageUrl` (문자열, 선택): 선택지에 대한 이미지 URL. | 없음 | 없음 |
| `PUT /questions/{questionId}` | 기존 질문을 업데이트합니다. | 필요 (Bearer 토큰) | `categoryId` (정수, 필수): 질문이 속할 카테고리 ID.<br>`title` (문자열, 필수): 질문 텍스트.<br>`choices` (객체 배열, 필수): 질문에 대한 선택지 목록.<br>&nbsp;&nbsp;&nbsp;&nbsp;`text` (문자열, 필수): 선택지 텍스트.<br>&nbsp;&nbsp;&nbsp;&nbsp;`imageUrl` (문자열, 선택): 선택지에 대한 이미지 URL. | 없음 | `questionId` (정수, 필수): 업데이트할 질문의 ID. |
| `DELETE /questions/{questionId}` | 특정 질문을 삭제합니다. | 필요 (Bearer 토큰) | 없음 | 없음 | `questionId` (정수, 필수): 삭제할 질문의 ID. |
| `GET /question-categories` | 모든 질문 카테고리를 조회합니다. | 필요 (Bearer 토큰) | 없음 | 없음 | 없음 |
| `POST /question-categories` | 새 질문 카테고리를 생성합니다. | 필요 (Bearer 토큰) | `name` (문자열, 필수): 새 카테고리의 이름. | 없음 | 없음 |
| `PUT /question-categories/{categoryId}` | 기존 질문 카테고리를 업데이트합니다. | 필요 (Bearer 토큰) | `name` (문자열, 필수): 카테고리의 새 이름. | 없음 | `categoryId` (정수, 필수): 업데이트할 카테고리의 ID. |
| `DELETE /question-categories/{categoryId}` | 특정 질문 카테고리를 삭제합니다. | 필요 (Bearer 토큰) | 없음 | 없음 | `categoryId` (정수, 필수): 삭제할 카테고리의 ID. |
| `GET /question-categories/{categoryId}` | 특정 카테고리에 속한 질문을 조회합니다. | 필요 (Bearer 토큰) | 없음 | `limit` (정수, 선택): 조회할 최대 질문 수 (기본값: 10).<br>`shuffle` (부울, 선택): true인 경우, 질문이 무작위 순서로 반환됩니다 (기본값: false). | `categoryId` (정수, 필수): 카테고리의 ID. |

**성공 응답 예시 (`GET /questions/random`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": [
    {
      "id": 1,
      "categoryId": 101,
      "title": "What is your favorite color?",
      "choices": [
        { "id": 1, "text": "Red", "imageUrl": null },
        { "id": 2, "text": "Blue", "imageUrl": null }
      ]
    }
  ]
}
```

**성공 응답 예시 (`POST /questions`):**
```json
{
  "ok": true,
  "statusCode": 201,
  "data": {
    "id": 123,
    "categoryId": 101,
    "title": "New Question",
    "choices": []
  }
}
```

**성공 응답 예시 (`GET /question-categories`):**
```json
{
  "ok": true,
  "statusCode": 200,
  "data": [
    {
      "id": 101,
      "name": "General Knowledge"
    }
  ]
}
```
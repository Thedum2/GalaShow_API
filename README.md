# GalaShow API 규격 명세

이 문서는 GalaShow 프로젝트의 API 규격을 정의합니다.

## 1. 인증 (Token)

### 1.1. 로그인

- `POST /token/login`
- 사용자의 ID와 비밀번호로 로그인을 시도하고, 성공 시 Access/Refresh 토큰을 발급합니다.

**Request Body**
```json
{
  "id": "admin",
  "password": "your_password"
}
```

**Success Response Body**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "accessExpiresAt": "2025-09-08T11:00:00Z",
  "refreshToken": "a1b2c3d4e5f6g7h8...",
  "refreshExpiresAt": "2025-09-15T10:00:00Z",
  "user": {
    "id": "admin",
    "role": "Administrator"
  }
}
```

### 1.2. 로그아웃

- `POST /token/logout`
- 현재 사용자의 Refresh 토큰을 무효화하여 로그아웃 처리합니다.

**Request Body**
```json
{
  "refreshToken": "a1b2c3d4e5f6g7h8..."
}
```

**Success Response Body**
```json
{
  "ok": true
}
```

### 1.3. 토큰 갱신

- `POST /token/refresh`
- 만료되지 않은 Refresh 토큰을 사용하여 새로운 Access/Refresh 토큰 쌍을 발급받습니다.

**Request Body**
```json
{
  "refreshToken": "a1b2c3d4e5f6g7h8..."
}
```

**Success Response Body**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "accessExpiresAt": "2025-09-08T12:00:00Z",
  "refreshToken": "b2c3d4e5f6g7h8i9...",
  "refreshExpiresAt": "2025-09-16T11:00:00Z",
  "user": {
    "id": "admin",
    "role": "Administrator"
  }
}
```

### 1.4. 토큰 유효성 검사

- `GET /token/verify`
- (Header에 `Authorization: Bearer {accessToken}` 포함)
- 현재 Access 토큰의 유효성을 검사하고 토큰에 포함된 정보를 반환합니다.

**Success Response Body**
```json
{
  "sub": "admin",
  "role": "Administrator",
  "exp": "1662631200",
  "valid": true
}
```

## 2. 배경 (Background)

### 2.1. 배경 목록 조회

- `GET /backgrounds`
- 모든 배경 목록을 조회합니다.

**Success Response Body**
```json
[
  {
    "id": 1,
    "title": "메인 페이지 배경",
    "type": "image",
    "url": "https://example.com/background.jpg"
  }
]
```

### 2.2. 배경 수정

- `PUT /backgrounds/{id}`
- 특정 배경의 정보를 수정합니다.

**Request Body**
```json
{
  "title": "수정된 배경",
  "type": "video",
  "url": "https://example.com/new_background.mp4"
}
```

**Success Response Body**
```json
{
  "id": 1,
  "title": "수정된 배경",
  "type": "video",
  "url": "https://example.com/new_background.mp4"
}
```

## 3. 배너 (Banner)

### 3.1. 배너 목록 조회

- `GET /banners`
- 모든 배너 목록을 조회합니다.

**Success Response Body**
```json
[
  {
    "id": 1,
    "message": "갈라쇼에 오신 것을 환영합니다!",
    "order": 1
  }
]
```

### 3.2. 배너 수정

- `PUT /banners/{id}`
- 특정 배너의 메시지를 수정합니다.

**Request Body**
```json
{
  "message": "새로운 이벤트가 시작되었습니다!"
}
```

**Success Response Body**
```json
{
  "id": 1,
  "message": "새로운 이벤트가 시작되었습니다!",
  "order": 1
}
```

## 4. 정책 (Policy)

### 4.1. 정책 조회

- `GET /policy`
- 서비스 이용약관과 개인정보 처리방침을 조회합니다.

**Success Response Body**
```json
{
  "termsOfService": "서비스 이용약관 내용...",
  "privacyPolicy": "개인정보 처리방침 내용..."
}
```

### 4.2. 정책 수정

- `PUT /policy`
- 서비스 이용약관과 개인정보 처리방침을 수정합니다.

**Request Body**
```json
{
  "termsOfService": "새로운 서비스 이용약관 내용...",
  "privacyPolicy": "새로운 개인정보 처리방침 내용..."
}
```

**Success Response Body**
```json
{
  "termsOfService": "새로운 서비스 이용약관 내용...",
  "privacyPolicy": "새로운 개인정보 처리방침 내용..."
}
```

## 5. SNS 링크 (SnsLink)

### 5.1. SNS 링크 목록 조회

- `GET /sns-links`
- 모든 SNS 링크 목록을 순서대로 조회합니다.

**Success Response Body**
```json
[
  {
    "title": "Instagram",
    "url": "https://instagram.com/galashow",
    "icon_url": "https://example.com/icon/instagram.png",
    "order": 1
  },
  {
    "title": "Youtube",
    "url": "https://youtube.com/galashow",
    "icon_url": "https://example.com/icon/youtube.png",
    "order": 2
  }
]
```

### 5.2. SNS 링크 일괄 수정

- `PUT /sns-links`
- 전체 SNS 링크 목록을 새로 전달한 목록으로 교체합니다.

**Request Body**
```json
{
  "data": [
    {
      "id": 1,
      "title": "New Instagram",
      "url": "https://instagram.com/new_galashow",
      "icon_url": "https://example.com/icon/new_instagram.png",
      "order": 1
    },
    {
      "id": 2,
      "title": "New Youtube",
      "url": "https://youtube.com/new_galashow",
      "icon_url": "https://example.com/icon/new_youtube.png",
      "order": 2
    }
  ]
}
```

**Success Response Body**
```json
[
  {
    "title": "New Instagram",
    "url": "https://instagram.com/new_galashow",
    "icon_url": "https://example.com/icon/new_instagram.png",
    "order": 1
  },
  {
    "title": "New Youtube",
    "url": "https://youtube.com/new_galashow",
    "icon_url": "https://example.com/icon/new_youtube.png",
    "order": 2
  }
]
```

## 6. 질문/투표 (Question)

### 6.1. 질문 카테고리 목록 조회

- `GET /question-categories`
- 모든 질문 카테고리 목록을 조회합니다.

**Success Response Body**
```json
[
  {
    "id": 1,
    "name": "재미로 보는 심리테스트"
  },
  {
    "id": 2,
    "name": "오늘의 인기 투표"
  }
]
```

### 6.2. 질문 카테고리 생성

- `POST /question-categories`
- 새로운 질문 카테고리를 생성합니다.

**Request Body**
```json
{
  "name": "새로운 카테고리"
}
```

**Success Response Body**
```json
{
  "id": 3,
  "name": "새로운 카테고리"
}
```

### 6.3. 질문 카테고리 수정

- `PUT /question-categories/{id}`
- 특정 질문 카테고리의 이름을 수정합니다.

**Request Body**
```json
{
  "name": "수정된 카테고리"
}
```

**Success Response Body**
```json
{
  "id": 3,
  "name": "수정된 카테고리"
}
```

### 6.4. 특정 카테고리의 질문 목록 조회

- `GET /questions?categoryId={id}`
- 특정 카테고리에 속한 질문 목록을 간략하게 조회합니다.

**Success Response Body**
```json
[
  {
    "id": 101,
    "title": "더 선호하는 계절은?"
  },
  {
    "id": 102,
    "title": "가장 좋아하는 색깔은?"
  }
]
```

### 6.5. 특정 질문 상세 조회

- `GET /questions/{id}`
- 특정 질문의 상세 정보(선택지 포함)를 조회합니다.

**Success Response Body**
```json
{
  "id": 101,
  "category_id": 2,
  "title": "더 선호하는 계절은?",
  "choices": [
    {
      "text": "봄",
      "image_url": "https://example.com/spring.jpg"
    },
    {
      "text": "여름",
      "image_url": "https://example.com/summer.jpg"
    },
    {
      "text": "가을",
      "image_url": "https://example.com/autumn.jpg"
    },
    {
      "text": "겨울",
      "image_url": null
    }
  ]
}
```

### 6.6. 질문 생성

- `POST /questions`
- 새로운 질문과 선택지를 생성합니다.

**Request Body**
```json
{
  "category_id": 1,
  "title": "무인도에 가져갈 3가지",
  "choices": [
    {
      "text": "물",
      "image_url": null
    },
    {
      "text": "불",
      "image_url": null
    },
    {
      "text": "칼",
      "image_url": null
    }
  ]
}
```

**Success Response Body**
```json
{
  "id": 103,
  "category_id": 1,
  "title": "무인도에 가져갈 3가지",
  "choices": [
    {
      "text": "물",
      "image_url": null
    },
    {
      "text": "불",
      "image_url": null
    },
    {
      "text": "칼",
      "image_url": null
    }
  ]
}
```

### 6.7. 질문 수정

- `PUT /questions/{id}`
- 특정 질문의 내용과 선택지를 수정합니다.

**Request Body**
```json
{
  "category_id": 1,
  "title": "수정된 질문",
  "choices": [
    {
      "text": "선택지 1",
      "image_url": null
    },
    {
      "text": "선택지 2",
      "image_url": "https://example.com/choice2.jpg"n    }
  ]
}
```

**Success Response Body**
```json
{
  "id": 103,
  "category_id": 1,
  "title": "수정된 질문",
  "choices": [
    {
      "text": "선택지 1",
      "image_url": null
    },
    {
      "text": "선택지 2",
      "image_url": "https://example.com/choice2.jpg"
    }
  ]
}
```
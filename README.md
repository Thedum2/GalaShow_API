# Galashow_API

이 문서는 GalaShow API 엔드포인트에 대한 개요를 제공합니다.

## API 엔드포인트

### 인증 (Authentication)

| 설명 | 메서드 | 엔드포인트 | 인증 필요 |
| --- | --- | --- | :---: |
| 액세스 및 리프레시 토큰을 발급받습니다. | POST | `/auth/login` | 아니요 |
| 만료된 액세스 토큰을 갱신합니다. | POST | `/auth/refresh` | 아니요 |
| 로그아웃하고 리프레시 토큰을 폐기합니다. | POST | `/auth/logout` | 아니요 |
| 액세스 토큰의 유효성을 검증합니다. | GET | `/auth/verify` | 예 |

### 질문 & 카테고리 (Questions & Categories)

| 설명 | 메서드 | 엔드포인트 | 인증 필요 |
| --- | --- | --- | :---: |
| 모든 질문 카테고리를 조회합니다. | GET | `/question-categories` | 예 |
| 새로운 질문 카테고리를 생성합니다. | POST | `/question-categories` | 예 |
| 질문 카테고리를 수정합니다. | PUT | `/question-categories/{categoryId}` | 예 |
| 질문 카테고리를 삭제합니다. | DELETE | `/question-categories/{categoryId}` | 예 |
| 카테고리별 질문 목록을 조회합니다. | GET | `/question-categories/{categoryId}` | 예 |
| ID로 단일 질문을 조회합니다. | GET | `/questions/{questionId}` | 예 |
| 새로운 질문을 생성합니다. | POST | `/questions` | 예 |
| 질문을 수정합니다. | PUT | `/questions/{questionId}` | 예 |
| 질문을 삭제합니다. | DELETE | `/questions/{questionId}` | 예 |
| 무작위 질문을 조회합니다. | GET | `/questions/random` | 예 |

### 배너 (Banners)

| 설명 | 메서드 | 엔드포인트 | 인증 필요 |
| --- | --- | --- | :---: |
| 모든 배너를 조회합니다. | GET | `/banners` | 아니요 |
| 배너를 수정합니다. | PUT | `/banners/{bannerId}` | 예 |

### 배경화면 (Backgrounds)

| 설명 | 메서드 | 엔드포인트 | 인증 필요 |
| --- | --- | --- | :---: |
| 모든 배경화면을 조회합니다. | GET | `/background` | 아니요 |
| 배경화면을 수정합니다. | PUT | `/background/{backId}` | 예 |

### 정책 (Policies)

| 설명 | 메서드 | 엔드포인트 | 인증 필요 |
| --- | --- | --- | :---: |
| 서비스 이용약관 및 개인정보 처리방침을 조회합니다. | GET | `/policies` | 아니요 |
| 서비스 이용약관 및 개인정보 처리방침을 수정합니다. | PUT | `/policies` | 예 |

### SNS 링크 (SNS Links)

| 설명 | 메서드 | 엔드포인트 | 인증 필요 |
| --- | --- | --- | :---: |
| 모든 SNS 링크를 조회합니다. | GET | `/sns-links` | 아니요 |
| 모든 SNS 링크를 수정합니다. | PUT | `/sns-links` | 예 |

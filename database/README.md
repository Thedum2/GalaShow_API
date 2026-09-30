# GalaShow DB 초기화

`schema.sql`은 현재 API의 7개 Repository와 11개 Entity를 기준으로 작성한 **빈 MySQL 8.0 DB용 v001 초기 스키마**다. 기존 서버의 DDL을 추출한 덤프가 아니며, 기존 데이터의 이전이나 기존 테이블의 변경을 수행하지 않는다. AWS/도메인 구성은 워크스페이스의 [도메인 전환 계획](../../docs/domain-migration.md)을 참고한다.

v002 [배너 슬롯 마이그레이션](migrations/v002_banner_slots.sql)은 기존 관리자 화면이 수정하는 ID 1~10의 슬롯을 만든다. 문구는 빈 문자열이며, 이미 있는 행의 문구·정렬·수정일은 그대로 둔다. 신규 초기화 Lambda는 v001 다음에 v002를 적용한다. SQL 클라이언트로 초기화하는 경우에도 아래 스키마 검증 후 v002 파일을 실행해야 배너 관리가 동작한다.

## 포함한 테이블

이름은 Repository SQL과 동일한 소문자다. DB 이름은 환경별 `DB_NAME`과 정확히 일치시킨다. 스크립트에는 `CREATE DATABASE`나 `USE`가 없으므로 실행자가 대상 DB를 선택해야 한다.

| 테이블 | Repository | 주요 계약 |
| --- | --- | --- |
| `background` | `BackGroundRepository` | 단수형 테이블 이름, `file_url`, 수정/조회만 구현 |
| `banners` | `BannerRepository` | `message`, 예약어인 `order`, 수정/조회만 구현 |
| `policies` | `PolicyRepository` | 두 정책 URL, 자동 증가 ID, 최초 수정 시 INSERT 가능 |
| `sns_links` | `SnsLinkRepository` | 요청이 ID와 표시 순서를 제공, 전체 교체 |
| `refresh_tokens` | `TokenRepository` | BIGINT ID, SHA-256 64자리 hex 해시, nullable 폐기 시각/접속 정보 |
| `minigames` | `MinigameRepository` | 자동 증가 ID와 `LAST_INSERT_ID()`, 이름 중복 방지, JSON 두 필드 |
| `minigame_tags` | `MinigameRepository` | 게임 ID와 태그 종류/값으로 조회 |
| `minigame_tutorials` | `MinigameRepository` | 게임별 `step` 순서로 조회 |
| `minigame_controls` | `MinigameRepository` | `key_name`, 문자열 배열을 직렬화한 JSON `keys` |
| `minigame_survival_stats` | `MinigameRepository` | 게임당 한 행, 누적 INT 카운터, DECIMAL 생존율 |
| `viewer_avatars` | `ViewerAvatarRepository` | 요청이 ID 제공, 순서는 유일, 트랜잭션으로 전체 교체 |

- ID와 카운터는 C# `GetInt32`/`int` 범위에 맞춘 signed INT다. 토큰 ID만 `GetInt64`/`long`에 맞춘 signed BIGINT다. SNS/아바타 ID는 `AUTO_INCREMENT`를 사용하지 않아 입력된 0도 그대로 보존한다.
- 미니게임 삭제는 부모 테이블만 DELETE하므로 네 하위 테이블의 FK에 `ON DELETE CASCADE`를 둔다. 초기화 스크립트 자체는 행을 삭제하지 않는다.
- `phase_data`/`game_data`는 읽기 코드의 NULL 분기를 허용한다. API 생성/수정은 생략된 값을 `{}`로 저장한다. `keys`는 필수 JSON이며 API는 문자열 배열로 직렬화/역직렬화한다.
- 읽기 코드가 NULL을 처리하지 않는 문자열/시각은 NOT NULL이다. INSERT에서 생략되는 생성/수정 시각에는 기본값을 둔다. SQL의 `NOW()`와 토큰의 `DateTime.UtcNow`를 맞추도록 **DB 서버/애플리케이션 연결의 시간대는 UTC**로 운영한다. 파일의 세션 설정은 초기화 연결에만 적용된다.
- charset/collation은 `utf8mb4`/`utf8mb4_0900_ai_ci`다. 이름 비교와 UNIQUE는 대소문자/악센트를 구분하지 않는다. 토큰 해시는 `ascii_bin`으로 비교한다.
- 문자열 길이는 기존 DDL에서 복원한 값이 아닌 이번 초기 스키마의 선택이다. 이름/제목/태그값/조작명/사용자 ID는 255자, 타입은 64자, IP는 IPv6를 포함해 45자다. URL/본문/사용자 에이전트는 TEXT(최대 65,535바이트), 생존율은 `DECIMAL(18,6)`로 소수점 6자리까지 보관한다. API에는 이에 대응하는 모든 길이/정밀도 검증이 없으므로 운영 콘텐츠와 맞는지 확인해야 한다.

## 안전한 초기화 순서

1. 환경별 전용 DB를 인프라 단계에서 생성하고 `DB_NAME`을 확정한다. private RDS에 접근 가능한 승인된 관리 실행 환경에서 MySQL 8 클라이언트를 사용한다. 이 디렉터리는 네트워크, 계정, 권한을 생성하지 않는다.
2. 대상 endpoint/DB를 확인하고 초기화용 계정으로 연결한다. 비밀번호는 프롬프트로 입력한다. RDS라면 공식 CA 번들을 지정해 서버 이름을 검증한다. 아래 환경변수는 운영자가 해당 환경 값으로 설정해야 한다.

   ```powershell
   mysql --no-defaults --host=$env:DB_HOST --port=$env:DB_PORT --user=$env:DB_BOOTSTRAP_USER --password --database=$env:DB_NAME --ssl-mode=VERIFY_IDENTITY --ssl-ca=$env:RDS_CA_BUNDLE --default-character-set=utf8mb4
   ```

3. MySQL 프롬프트에서 연결 대상, 버전, 시간대, 테이블 목록을 확인한다. **목록이 비어 있을 때만 신규 초기화를 진행한다.** 기존 DB라면 먼저 스키마/데이터를 별도 조사하고 백업 및 마이그레이션 계획을 세운다.

   ```sql
   SELECT @@hostname, @@port, DATABASE(), VERSION(), @@global.time_zone, @@session.time_zone;
   SHOW FULL TABLES;
   ```

4. MySQL 프롬프트를 `exit`로 종료하고 PowerShell에서 `schema.sql`을 읽어 실행한다. 아래는 이 워크스페이스의 Windows 경로이며 다른 실행 환경에서는 실제 로컬 경로를 사용한다. `--execute`는 전달한 SQL을 실행하고 종료한다. [MySQL 8 클라이언트 옵션](https://dev.mysql.com/doc/refman/8.0/en/mysql-command-options.html)

   ```powershell
   $schemaSql = Get-Content -LiteralPath C:/Workspace/Personal/GalaShow/API/database/schema.sql -Raw -Encoding utf8
   mysql --no-defaults --host=$env:DB_HOST --port=$env:DB_PORT --user=$env:DB_BOOTSTRAP_USER --password --database=$env:DB_NAME --ssl-mode=VERIFY_IDENTITY --ssl-ca=$env:RDS_CA_BUNDLE --default-character-set=utf8mb4 --execute=$schemaSql
   if ($LASTEXITCODE -ne 0) { throw 'DB 초기화 실패: 적용된 테이블을 조사하세요.' }
   ```

   `--force`를 사용하지 않는다. MySQL DDL은 전체 파일을 하나의 트랜잭션으로 롤백하지 않으므로 오류가 나면 즉시 중단하고 생성된 테이블을 조사한다. 검토 없이 삭제하거나 덮어쓰지 않는다. `CREATE TABLE IF NOT EXISTS`는 재실행 시 기존 테이블을 보존하지만, 기존 구조가 맞는지 검사하거나 수정하지는 않는다.

5. 2번 명령으로 다시 연결해 11개 테이블과 FK 네 개, 인덱스/기본값을 확인한다. 신규 DB에는 모든 테이블의 행 수가 0이어야 한다. 예시는 주요 구조 확인용이며 `SHOW CREATE TABLE`은 11개 모두 확인한다.

   ```sql
   SHOW TABLES;
   SHOW CREATE TABLE minigames;
   SHOW CREATE TABLE minigame_controls;
   SHOW CREATE TABLE minigame_survival_stats;
   SHOW CREATE TABLE refresh_tokens;
   SELECT TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
   FROM information_schema.KEY_COLUMN_USAGE
   WHERE TABLE_SCHEMA = DATABASE() AND REFERENCED_TABLE_NAME IS NOT NULL;
   SELECT @@session.sql_mode;
   ```

   strict SQL mode를 유지해 길이 초과 등이 조용히 잘리지 않도록 한다. 실제 엔진에서 API의 생성→조회→수정→삭제, JSON 읽기, 토큰 저장/폐기, 아바타 전체 교체를 별도 테스트 DB로 검증한 뒤 서비스에 연결한다.

6. DB 이름/서버 식별자, `v001`, API commit, 파일 SHA-256, 적용 시각, 검증 결과를 배포 기록에 남긴다. PowerShell의 `Get-FileHash ./database/schema.sql -Algorithm SHA256`으로 체크섬을 구할 수 있다. 자동 버전 테이블이나 자동 마이그레이션 실행기는 아직 없으며, API 시작 시 이 파일을 자동 적용하지 않는다. 이후 변경은 검토 가능한 별도 번호의 migration으로 관리하고, 이미 적용된 v001 파일을 수정해 기존 DB를 갱신했다고 간주하지 않는다. 이어서 `migrations/v002_banner_slots.sql`을 같은 방식으로 실행하고 `SELECT id, message FROM banners ORDER BY id`로 ID 1~10의 슬롯을 확인한다.

## Private RDS 수동 초기화 Lambda

관리 PC에서 private RDS에 직접 연결할 수 없으면 [Initializer](Initializer/Function.cs)를 RDS에 접근 가능한 private subnet/security group 안에 배포해 수동 호출한다. HTTP/API Gateway 이벤트는 연결하지 않는다. 함수의 실행 역할은 해당 DB secret 하나의 `secretsmanager:GetSecretValue`, VPC 연결 및 로그 권한이 필요하며, VPC에는 Secrets Manager까지 접근할 NAT 또는 VPC endpoint 경로가 필요하다. 함수 제한 시간은 180초 이상으로 설정한다.

| 설정 | 값 |
| --- | --- |
| 프로젝트/CodeUri | `database/Initializer/GalaShow.DatabaseInitializer.csproj` / `database/Initializer/` |
| 런타임 | `dotnet8` |
| Handler | `GalaShow.DatabaseInitializer::GalaShow.DatabaseInitializer.Function::FunctionHandler` |
| 필수 환경변수 | `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_SECRET_ARN` |
| Secret 형식 | JSON 객체의 `username`, `password` 문자열 |
| 허용 요청 | `{"action":"initialize"}`, `{"action":"migrate-banner-slots"}`, 개발 전용 `{"action":"seed-dev-samples"}` |
| 초기화 성공 응답 | `{"schemaVersion":"v002","result":"initialized","tableCount":11,"bannerSlotCount":10}` |

함수는 `schema.sql`과 `v002_banner_slots.sql`을 빌드/배포 출력에 복사하고, TLS를 요구하는 MySql.Data 8.3 연결로 패키징된 SQL을 실행한다. 요청의 SQL이나 접속 정보를 받지 않는다. 다른 action/누락된 action은 환경설정·AWS 접근 전에 거부한다. `seed-dev-samples`는 정확히 `STAGE=dev`일 때만 허용하며, 운영/누락/대문자 stage는 AWS 접근 전에 거부한다. `initialize`는 DB 세션 잠금을 잡고 **테이블/뷰가 하나라도 있으면 거부**하므로 재호출이나 부분 실패 후의 복구는 별도 검토가 필요하다. `migrate-banner-slots`는 기존 11개 테이블이 있는 DB에서 v002만 트랜잭션으로 실행한다. 두 action은 같은 세션 잠금을 사용한다. 자격 증명과 연결 문자열은 응답/로그에 남기지 않는다. MySQL 오류는 코드 번호만 반환한다.

API 디렉터리에서 로컬 빌드/테스트:

```powershell
dotnet build database/Initializer/GalaShow.DatabaseInitializer.csproj -c Release --tl:off
dotnet test database/Initializer.Tests/GalaShow.DatabaseInitializer.Tests.csproj --tl:off
```

배포 후에는 대상 stage와 함수 이름을 먼저 확인하고 한 번 호출한다. 다음 예시의 `DB_INITIALIZER_FUNCTION`은 실제 배포된 함수 이름이다.

```powershell
$initializerRequestPath = Join-Path $env:TEMP 'galashow-db-initialize-request.json'
$initializerResultPath = Join-Path $env:TEMP 'galashow-db-initialize-result.json'
Set-Content -LiteralPath $initializerRequestPath -Value '{"action":"initialize"}' -Encoding utf8NoBOM
aws lambda invoke --region ap-northeast-2 --function-name $env:DB_INITIALIZER_FUNCTION --invocation-type RequestResponse --payload "fileb://$initializerRequestPath" $initializerResultPath
if ($LASTEXITCODE -ne 0) { throw 'Lambda 호출 실패' }
Get-Content -LiteralPath $initializerResultPath
```

CLI 종료 코드 0 또는 HTTP 200만으로 성공을 판단하지 않는다. invoke 메타데이터에 `FunctionError`가 없고 응답 본문의 `result`/`tableCount`가 각각 `initialized`/`11`인지 확인한다. `schemaVersion=v002` 및 `bannerSlotCount=10`도 확인한다. 초기화 성공은 전체 DDL과 실제 MySQL 드라이버 실행을 확인하지만, API의 모든 CRUD/JSON 읽기 동작까지 검증한 것은 아니다.

## 이미 초기화된 v001 DB의 배너 복구

API를 재배포해 최신 초기화 함수를 올린 후 위 호출 예시의 payload를 `{"action":"migrate-banner-slots"}`로 바꿔 환경별로 호출한다. **기존 DB에 `initialize`를 다시 실행하지 않는다.** 성공 조건은 `FunctionError` 없음, `schemaVersion=v002`, `result=migrated`, `tableCount=11`, `bannerSlotCount=10`이다. 재호출해도 기존 문구와 정렬을 덮어쓰지 않는다. 요청으로 임의 SQL을 받지 않으며 HTTP 경로도 만들지 않는다.

배포 후 `GET /banners`가 200과 ID 1~10을 반환하는지, 개발 관리자에서 문구 저장 후 새로고침해 유지되는지 확인한다. 공개 Client는 빈 문구의 슬롯을 표시하지 않는다.

## 개발 DB 샘플 데이터

`seeds/dev-samples-v1.sql`은 관리자 화면·조회/편집 검증용 샘플이다. 신규 초기화나 일반 API 배포 때 자동 실행하지 않는다. 운영 환경에는 실행할 수 없다. 공개 API 계약은 바꾸지 않으며 초기화 Lambda를 수동 호출하는 방식이다.

| 데이터 | 준비한 수량 |
| --- | ---: |
| 배너 문구 | 10 |
| 배경 이미지 | 3 |
| 정책 링크 묶음 | 1 (샘플 PDF 2개) |
| SNS 링크 | 3 (플랫폼 홈 링크) |
| 미니게임 | 4 |
| 미니게임 태그/설명 단계/조작법 | 24 / 12 / 9 |
| 가상 생존 통계 | 4 |
| 아바타 | 4 |

각 이름·문구에 `[샘플]`을 표시한다. `[샘플]` 미니게임 4종은 카탈로그/폼 검증용이며 Unity로 실행되지 않는다. 파일 끝의 **트롤리 딜레마**(Unity 플러그인 `galashow.trolley`, `game_data.pluginId`·딜레마 22개·단계 시간·튜토리얼·조작 키, docs/minigame-trolley.md 4·8절)만 실제로 실행되는 게임이며 이름으로 중복을 막는다. 영상 URL은 각 게임의 선택지를 보여주는 6초 무음 H.264 MP4 샘플을 가리킨다. 영상에도 실제 게임 플레이가 아닌 개발용 샘플임을 표시한다. 아바타 `gif_url`은 현재 관리자 `<img>`가 지원하는 SVG 샘플을 가리킨다. PDF는 실제 약관/개인정보 정책이 아닌 화면 확인용 문서라고 본문에 표시한다. 계정과 refresh token 샘플은 생성하지 않는다.

기존 데이터는 보존한다. 빈 배너 문구와 ID·이름이 모두 일치하는 샘플 게임의 빈 영상 URL만 채우며, 배경·SNS·게임·아바타는 충돌한 ID를 덮어쓰지 않는다. 이미 등록된 영상 URL은 유지한다. 정책 테이블에 행이 있으면 샘플 정책을 추가하지 않는다. 관련 데이터는 ID와 이름이 일치하는 샘플 게임에만 추가하고, 같은 태그 유형·설명 단계·조작명·통계가 있으면 건너뛴다. 기존 ID가 사용 중이면 실제 추가 수량은 표보다 적을 수 있다. 입력 전체를 트랜잭션으로 실행하며 반환하는 `tableRowCounts`는 기존 데이터를 포함한 테이블 전체 행 수다.

워크스페이스 루트에서 에셋을 준비한다:

```powershell
python API/database/seeds/build-sample-assets.py
# 영상 재생성에만 Python playwright 패키지와 Microsoft Edge가 필요하다.
python API/database/seeds/build-sample-videos.py            # 전체. 특정 게임만: build-sample-videos.py minority
aws s3 sync Client/public/sample-data/v1 s3://galashow-251113431583-dev-client/sample-data/v1 --profile galashow --region ap-northeast-2
```

`Client/public/sample-data/v1`에 생성된 SVG 14개, PDF 2개, MP4 4개는 Git으로 관리하고 이후 Client 배포에도 포함한다. SQL의 에셋 기준 주소는 `https://dev.galashow.cloud/sample-data/v1/`다. 영상 파일은 `game-choice.mp4`, `game-vote.mp4`, `game-survival.mp4`, `game-minority.mp4`이며 S3에서는 `Content-Type: video/mp4`로 제공한다.

최신 초기화 Lambda를 개발 API 스택에 배포한 뒤 `{"action":"seed-dev-samples"}`를 `galashow-cloud-dev-database-initializer`에 호출한다. invoke 결과의 `FunctionError` 부재와 본문 `result=samples-seeded`, `schemaVersion=v002`, `tableRowCounts`를 확인한다. 스키마 초기화를 다시 실행할 필요는 없다. 동일 샘플을 재호출해도 중복 행을 만들지 않는다.

## 데이터와 현재 구현의 제한

- 기본 초기화 v002는 배너 편집에 필요한 빈 슬롯 10개만 만든다. 위 개발 샘플은 별도 수동 action으로 등록한다. 관리자 계정, 비밀번호, 미디어 URL, 가상의 게임/정책/배너 문구는 넣지 않는다. 배너 문구는 관리자 화면에서 수정한다. 배경은 현재 생성 API가 없어 실제 콘텐츠를 별도 등록해야 한다. 정책/링크/미니게임/아바타도 실제 운영 콘텐츠와 적절한 권한으로 등록해야 한다.
- 생산 환경 관리자 자격 증명 검증은 `TokenService.ValidateCredentialAsync`에 미구현으로 남아 있다. `refresh_tokens`를 만드는 것으로 로그인 기능이나 계정 저장소가 완성되지는 않는다.
- PRD의 게임방·참가자·라운드·결과 기록은 현재 Repository에 없으므로 이 스키마의 구현 범위에 포함하지 않는다. 해당 제품 요구사항이 제거된 것은 아니다.
- 기존 SQL 제약: 여러 태그 종류를 함께 필터링하면 같은 JOIN 행의 `tag_type`에 AND 조건을 걸어 결과가 사라질 수 있다. 정책은 최신 ID를 조회하지만 가장 오래된 ID를 수정하므로 여러 정책 행을 넣으면 동작이 어긋날 수 있다. SNS 전체 교체는 트랜잭션이 없어 중간 실패 시 일부만 저장될 수 있다. 생존 통계 누적 조회에는 행 잠금이 없어 동시 업데이트를 잃을 수 있고, 통계 직접 수정의 생존율 범위/카운터 관계 검증도 불완전하다. 스키마 추가로 이러한 동작이 해결되지는 않는다.

## 작성 시 검증 범위

2026-09-29에 모든 Repository SQL, Entity 및 관련 요청 모델을 대조했다. 임시 Python 환경의 SQLGlot MySQL 파서로 7개 Repository에서 추출한 SQL 문 40개와 스키마를 파싱해 테이블 11개/컬럼 65개의 참조, INSERT에서 생략된 필수 컬럼의 기본값, JSON 타입 3개, FK 4개 및 초기화 DML 부재를 확인했다. 동적 목록 ID와 태그 조건은 대표 값으로 치환했으며 모든 런타임 입력을 검증한 것은 아니다. 정수 타입, NULL 처리, ID 생성 및 삭제 관계도 코드와 대조했다. 로컬 Docker CLI는 있으나 Docker Desktop Linux 엔진에 연결할 수 없어 MySQL 서버 적용/드라이버 왕복 테스트는 수행하지 않았다. 외부 DB에는 연결하거나 적용하지 않았다. 정적 대조는 엔진 통합 테스트를 대신하지 않는다.

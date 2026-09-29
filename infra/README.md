# galashow.cloud 배포

서울(`ap-northeast-2`)의 Lambda/API Gateway + RDS MySQL + S3와 CloudFront 구성을 사용한다. 웹용 ACM 인증서만 `us-east-1`에 둔다. EC2/NAT 게이트웨이는 생성하지 않는다.

| 서비스 | 개발 | 운영 |
| --- | --- | --- |
| Client | https://dev.galashow.cloud | https://galashow.cloud |
| Admin | https://admin-dev.galashow.cloud | https://admin.galashow.cloud |
| API | https://api-dev.galashow.cloud | https://api.galashow.cloud |

## 파일과 스택

| 템플릿 | 스택 | 역할 |
| --- | --- | --- |
| certificate.yaml | galashow-cloud-certificate | 루트/와일드카드 인증서, 버지니아 |
| artifacts.yaml | galashow-cloud-artifacts | 비공개 Lambda 배포 패키지 저장소 |
| database.yaml | galashow-cloud-database-dev / prod | 환경별 VPC, 비공개 MySQL, Secrets Manager endpoint와 비밀 값 |
| web.json | galashow-cloud-web-dev / prod | 환경별 Client/Admin S3, CloudFront, DNS |
| ../template.yaml | galashow-cloud-api-dev / prod | API, DNS, 서울 인증서, Lambda, 수동 DB 초기화 함수 |
| github-actions.json | galashow-cloud-github-actions | GitHub OIDC 공급자와 저장소/환경별 배포 역할 6개 |

S3 웹 버킷은 OAC로 해당 CloudFront에서만 읽는다. API 패키지는 별도 버킷에 저장하여 웹에 노출되지 않는다. React 경로는 CloudFront Function으로 `/index.html`에 연결하며 `/assets/`, `/build/`와 확장자가 있는 파일의 오류는 HTML로 바꾸지 않는다.

DB는 Lambda 보안 그룹의 TCP 3306 연결만 허용한다. Lambda의 비밀 조회는 HTTPS VPC endpoint를 사용한다. CHZZK 프록시는 DB/JWT 초기화를 하지 않고 VPC 밖에서 외부 API를 호출한다. 사용자 요청에 따라 개발/운영 모두 자동 백업 보존을 0일로 설정하고 삭제·교체 시 스냅샷을 생성하지 않는다. 자동 백업도 인스턴스 삭제 시 제거한다. 운영 인스턴스의 삭제 방지는 유지한다. 기본 DB 크기는 `db.t4g.micro`, 20 GiB(gp3), 자동 확장 상한 100 GiB, Single-AZ이며 `MultiAZ=true`로 변경할 수 있다. 백업을 꺼도 DB 본체·저장공간과 endpoint 등의 AWS 사용 요금은 발생한다.

## 준비

AWS CLI v2, .NET 8 이상 SDK, Node 24가 필요하다. 프로필 로그인은 `aws login --profile galashow --region ap-northeast-2`로 진행한다. GitHub Actions는 제공된 자격증명을 사용하므로 아래 스크립트의 `-Profile ''`로 로컬 프로필 사용을 끈다.

Route 53의 `galashow.cloud` 공개 호스팅 영역 ID를 확인한다. 다른 계정/도메인의 호스팅 영역이나 이전 인증서 ARN을 재사용하지 않는다.

```powershell
aws route53 list-hosted-zones --profile galashow
$zoneId = 'Z0263745GATMIS12FEIH' # 현재 galashow.cloud 영역, 다른 계정은 확인 후 변경
```

아래 명령은 API 저장소 루트에서 실행한다. `deploy`는 같은 스택을 갱신한다. 기존 DB를 바꿀 때는 변경 집합과 데이터 이전 계획을 먼저 확인한다. 새 DB에 기존 계정의 게임/미디어/토큰 데이터가 자동 이전되지는 않는다.

```powershell
aws cloudformation deploy --stack-name galashow-cloud-certificate --template-file infra/certificate.yaml --parameter-overrides "HostedZoneId=$zoneId" --profile galashow --region us-east-1
aws cloudformation deploy --stack-name galashow-cloud-artifacts --template-file infra/artifacts.yaml --profile galashow --region ap-northeast-2
$certificateArn = aws cloudformation describe-stacks --stack-name galashow-cloud-certificate --query 'Stacks[0].Outputs[?OutputKey==`CertificateArn`].OutputValue | [0]' --output text --profile galashow --region us-east-1

foreach ($stage in 'dev', 'prod') {
    aws cloudformation deploy --stack-name "galashow-cloud-database-$stage" --template-file infra/database.yaml --parameter-overrides "StageName=$stage" --profile galashow --region ap-northeast-2
    aws cloudformation deploy --stack-name "galashow-cloud-web-$stage" --template-file infra/web.json --parameter-overrides "StageName=$stage" "HostedZoneId=$zoneId" "CertificateArn=$certificateArn" --profile galashow --region ap-northeast-2
    ./infra/deploy-api.ps1 -Stage $stage -HostedZoneId $zoneId
}
```

루트 도메인과 와일드카드의 ACM DNS 검증 레코드는 같으므로 `certificate.yaml`의 `DomainValidationOptions`에 동일 레코드를 두 번 만들지 않는다.

## API 배포와 DB 초기화

`deploy-api.ps1`은 해당 환경 DB 스택의 출력에서 연결 정보를 읽고, 각 .NET 함수를 Linux x64용으로 publish한 뒤 AWS CLI로 패키징/배포한다. 비밀번호와 JWT 키는 소스·배포 인자에 포함하지 않는다. 빌드 결과는 Git에서 제외된 `.aws-sam/`에 저장한다. 두 번째 환경 배포에서만 `-SkipBuild`로 같은 코드 빌드를 재사용할 수 있다. 코드를 수정했다면 다시 빌드한다.

필수 Lambda 환경변수는 `STAGE`, `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_SECRET_ARN`, `JWT_SECRET_ARN`, `CORS_ALLOWED_ORIGINS`다. 이전 계정의 DB/ARN으로 돌아가는 기본값은 없다. 개발/운영 CORS를 분리하고, 개발에서만 목록 설정과 관계없이 `http://localhost`, `https://localhost`의 모든 포트를 허용한다. 운영에서는 loopback origin을 허용하지 않는다.

새 DB 생성 후 [DB 초기화 안내](../database/README.md)의 수동 Lambda 호출을 **각 빈 DB에 한 번** 실행한다. 초기화 함수는 API Gateway에 연결하지 않는다. `{"action":"initialize"}`는 빈 DB에 v001 테이블과 v002 배너 슬롯을 생성하며, 성공 시 `schemaVersion=v002`, `result=initialized`, `tableCount=11`, `bannerSlotCount=10`을 확인한다. 기존 테이블이 있으면 초기화를 거부한다. 이미 v001을 적용한 DB에는 `{"action":"migrate-banner-slots"}`를 호출해 누락된 배너 슬롯만 추가한다. 기존 문구와 정렬은 보존한다.

DB 비밀번호와 JWT 키는 Secrets Manager에서 생성한다. 현재 애플리케이션은 값을 실행 환경에 캐시하므로 자동 회전을 구성하지 않는다. 수동 회전 시 RDS 비밀번호/Secret을 함께 변경하고 Lambda 실행 환경도 갱신해야 한다.

## API CI/CD

[`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml)의 `GalaShow API CI/CD`가 다음 순서로 실행된다.

| 실행 조건 | 검증 | 배포 대상 |
| --- | --- | --- |
| `develop` 대상 Pull Request | 템플릿 검사, API 단위 테스트, DB 초기화 요청 테스트, 배포 점검 스크립트 테스트 | 배포하지 않음 |
| `develop`에 push 또는 PR 병합 | 같은 검증을 모두 통과해야 진행 | 개발 API 자동 배포 |
| Actions에서 `Run workflow`, `stage=dev` | 선택한 브랜치의 코드를 검증 | 개발 API |
| Actions에서 `Run workflow`, `stage=prod` | 선택한 브랜치의 코드를 검증 | 운영 API |

배포 job만 AWS 자격증명과 `dev`/`prod` GitHub Environment를 사용한다. 환경별 배포를 직렬화하고 진행 중인 CloudFormation 배포는 새 push로 취소하지 않는다. 테스트 결과는 Actions의 `test-results-*` 아티팩트로 14일간 보관한다.

배포 후에는 API만 읽기 요청으로 점검한다. Client/Admin 배포 상태에 의존하지 않으며, 배너 슬롯 1~10과 SNS/배경 조회, 환경별 CORS를 확인한다. 개발의 localhost 임의 포트 허용과 운영의 localhost 차단도 검사한다. 배경 콘텐츠 미등록 시 451만 허용하며, 배너 오류나 누락된 슬롯은 실패로 처리한다. 이 점검이 실패하면 워크플로도 실패하지만, 이미 완료된 CloudFormation 배포를 자동으로 되돌리지는 않는다.

### GitHub 최초 설정

[저장소 환경 설정](https://github.com/Thedum2/GalaShow_API/settings/environments)의 `dev`/`prod` Environment에 아래 값을 등록한다.

| 종류 | 이름 | 값 |
| --- | --- | --- |
| Variable | `HOSTED_ZONE_ID` | `Z0263745GATMIS12FEIH` — 현재 `galashow.cloud` 공개 호스팅 영역 |
| Variable | `AWS_ACCOUNT_ID` | `251113431583` — 현재 배포 스택이 있는 계정 |
| Variable | `AWS_ROLE_ARN` | `arn:aws:iam::251113431583:role/galashow-github-api-dev` 또는 `galashow-github-api-prod` |
| Variable | `CHZZK_SECRET_ARN` | 치지직 앱 JSON(`clientId`, `clientSecret`)을 저장한 환경별 Secrets Manager ARN. 치지직 연동 시 필요 |

워크플로는 GitHub OIDC의 단기 자격증명을 사용한다. 배포 job만 `id-token: write`를 가지며 IAM 신뢰 정책은 `repo:Thedum2/GalaShow_API:environment:dev` 또는 `prod`로 제한한다. 계정이 `AWS_ACCOUNT_ID`와 다르면 배포 전에 실패한다. 로컬 `galashow` 로그인 세션은 GitHub Actions로 전달되지 않는다. 역할과 권한의 재현 방법은 [GitHub 배포 권한 설정](github-actions-setup.md)을 따른다.

기존 `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` Secrets는 새 워크플로에서 사용하지 않는다. 값을 읽거나 새 키를 발급하지 않으며 이 작업에서는 기존 키를 삭제하지 않는다.

인프라 스택 `galashow-cloud-artifacts`, `galashow-cloud-database-dev`/`prod`와 DNS 영역은 먼저 준비되어 있어야 한다. CI/CD는 API 스택을 배포하며 DB 생성·초기화·데이터 마이그레이션은 자동 실행하지 않는다.

### 실제 실행

1. 워크플로와 참조하는 `infra/`, `database/`, `template.yaml`, 소스·테스트 변경사항을 함께 GitHub에 반영한다. 기존 `.github/workflows/deloy.yml` 삭제도 반영한다.
2. `develop`에 push하면 개발 API 배포가 자동 시작된다. [Actions](https://github.com/Thedum2/GalaShow_API/actions)에서 `GalaShow API CI/CD` 실행 결과를 확인한다.
3. 운영 배포는 같은 워크플로의 `Run workflow`에서 배포할 브랜치와 `stage=prod`를 선택한다. 현재 기본 브랜치는 `develop`이다.
4. 성공 시 실행 요약에 배포 환경, API 주소, 커밋 SHA가 표시된다. 배포 후 점검 실패는 `Check deployed API and CORS` 로그에서 확인한다.

수동 실행 버튼은 워크플로 파일이 기본 브랜치에 반영된 뒤 사용할 수 있다. 환경은 `develop` 브랜치와 `v*` 태그의 실행을 허용한다. 태그의 코드를 배포하려면 GitHub CLI 로그인 후 `gh workflow run deploy.yml --ref <v태그> -f stage=prod` 또는 REST API를 사용한다. 다른 ref는 환경의 배포 정책에 먼저 추가해야 한다. 실행 조건은 [GitHub 공식 문서](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#workflow_dispatch), AWS 인증과 계정 검증은 [공식 AWS Actions 문서](https://github.com/aws-actions/configure-aws-credentials)를 참고한다.

로컬에서 개발 API를 바로 배포할 때는 저장소 루트에서 다음 명령을 실행한다.

```powershell
./infra/deploy-api.ps1 -Stage dev -HostedZoneId 'Z0263745GATMIS12FEIH' -Profile galashow
python infra/smoke-test.py --stage dev --api-only --allow-empty
```

## 치지직 앱 설정

치지직 연동 Lambda는 기존 `/chzzk` API와 배포 리소스를 재사용한다. 앱의 `clientId`와 `clientSecret`은 서버의 Secrets Manager JSON에 저장하고, 클라이언트에는 `/chzzk/config`의 공개 ID만 전달한다. `VITE_*` 변수나 브라우저 입력으로 앱 비밀 키를 넘기지 않는다.

첫 설정 또는 ARN 변경 시 `./infra/deploy-api.ps1 -Stage dev -HostedZoneId $zoneId -ChzzkSecretArn '<dev-secret-arn>'`을 실행한다. CI는 Environment Variable `CHZZK_SECRET_ARN`을 같은 인자로 전달한다. 생략하면 기존 스택 값이 유지되고, 신규 스택의 기본값은 비어 있다. 앱 설정이 없으면 치지직 앱 키가 필요한 기능은 503을 반환한다. ARN을 전달했다고 secret이 생성되지는 않는다.

SAM은 해당 Lambda에 지정한 secret의 `GetSecretValue`만 허용한다. CHZZK는 DB/JWT를 초기화하지 않고 VPC 밖에서 Secrets Manager와 치지직 API에 접속한다. 키는 5분간 캐시한다. JSON 예시, Redirect URI, 로컬 서버 환경변수는 [치지직 연동 안내](../src/GalaShow.ChzzkProxy/README.md)를 따른다.

API와 PolyChat/Client의 HTTP 계약이 함께 변경되므로 해당 변경을 함께 릴리스한다. PolyChat 코드를 먼저 원격에 반영하고 Client CI에서 참조하는 revision을 맞춘 뒤 API와 Client를 배포한다. 기존 프록시를 쓰는 구버전 Client는 새 API와 호환되지 않는다.

## 웹 배포

Client/Admin 저장소의 각각 `npm run build:dev` 또는 `npm run build:prod`로 빌드한다. 배포 대상은 해당 환경 웹 스택의 `ClientBucket`, `AdminBucket`, `ClientDistributionId`, `AdminDistributionId` 출력이다. 개발 번들을 운영 버킷에 복사하지 않는다. Unity WebGL 파일은 Client에 포함된다.

Client/Admin의 자동 배포는 각 웹 저장소의 워크플로에서 관리한다. 위 API 워크플로는 웹을 배포하지 않는다.

OAuth 공급자 콘솔에는 `https://dev.galashow.cloud/callback`과 `https://galashow.cloud/callback`을 등록한다. YouTube 승인 원본도 두 웹 origin으로 설정한다. 브라우저 팝업 인증은 콜백 URL의 code/hash를 읽는 기존 PolyChat 방식이다. 공급자 콘솔 설정 및 실제 계정 인증은 AWS 배포와 별개이며 아직 검증하지 않았다.

## 검증

```powershell
dotnet test GalaShow_API.sln --configuration Release --filter 'Category!=Integration'
dotnet test database/Initializer.Tests/GalaShow.DatabaseInitializer.Tests.csproj --configuration Release
python -m unittest discover -s infra/tests -v
cfn-lint template.yaml infra/certificate.yaml infra/database.yaml infra/artifacts.yaml infra/web.json
python infra/smoke-test.py --stage dev --api-only --allow-empty
python infra/smoke-test.py --stage all --allow-empty
```

기존 `Category=Integration` 테스트는 실행 중인 localhost API/DB를 필요로 하며 데이터를 변경한다. 운영을 대상으로 실행하지 않는다. 실제 배포 후에는 HTTPS, React 경로 새로고침, WebGL 파일 MIME, API 읽기 응답, 허용/비허용 origin의 OPTIONS 응답을 확인한다. `--allow-empty`는 미등록 배경의 451만 허용한다. 배너는 초기화 이후 반드시 200과 ID 1~10의 수정 가능한 슬롯을 반환해야 한다. 배경 콘텐츠를 등록한 뒤에는 옵션을 뺀다. 현재 응답 포맷은 `{"status":"200","data":...}`이며 API 루트 README의 기존 `ok/statusCode` 예제와 차이가 있다.

현재 운영 관리자 인증(`TokenService.ValidateCredentialAsync`)은 원래 미구현이다. 개발 인증만 있는 상태이며 이 배포에서 운영 인증을 우회하지 않는다. 빈 DB 초기화는 실제 게임 데이터/관리자 계정을 만들지 않는다.

참고: [CloudFront 인증서 리전](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html), [S3 OAC](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/private-content-restricting-access-to-s3.html), [Secrets Manager VPC endpoint](https://docs.aws.amazon.com/secretsmanager/latest/userguide/vpc-endpoint-overview.html).

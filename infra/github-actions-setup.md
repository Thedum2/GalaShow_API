# GitHub Actions 배포 권한과 환경 설정

이 설정은 AWS 계정 `251113431583`, 리전 `ap-northeast-2`의 현재 `galashow.cloud` 스택과 GitHub 소유자 `Thedum2`를 기준으로 한다.

## 인증과 배포 경계

`github-actions.json`은 GitHub OIDC 공급자 1개와 Admin/Client/API의 dev/prod 배포 역할 6개를 만든다. 저장소와 GitHub Environment 이름이 모두 일치하는 job만 역할을 사용할 수 있다. 현재 저장소는 이름 기반 OIDC subject를 사용한다. 저장소를 이전·개명하거나 immutable subject를 활성화한 경우 `*SubjectPrefix` 파라미터를 GitHub가 반환하는 실제 prefix로 바꾼다.

웹 역할은 해당 환경의 해당 사이트 S3 버킷·CloudFront 배포와 웹 스택 출력 조회에 한정한다. API 역할은 해당 환경의 API 스택·함수·실행 역할·도메인·인증서·DNS와 아티팩트 경로 `api/<stage>/`를 사용한다. DB 초기화 호출이나 DB 비밀 값 조회 권한을 주지 않는다. 실제 REST API와 CloudFront를 교체한 경우 템플릿의 리소스 ID 파라미터도 갱신해야 한다.

로컬 AWS 로그인은 부트스트랩에만 사용한다. Actions는 `id-token: write`와 `role-to-assume`으로 단기 자격증명을 받으며 기존 AWS 액세스 키 Secrets를 사용하지 않는다.

## AWS 적용

API 저장소 루트에서 실행한다. 먼저 `aws sts get-caller-identity --profile galashow --no-cli-pager`의 계정과 웹/API 스택 출력이 위 대상과 일치하는지 확인한다.

```powershell
aws cloudformation deploy --stack-name galashow-cloud-github-actions --template-file infra/github-actions.json --capabilities CAPABILITY_NAMED_IAM --profile galashow --region ap-northeast-2 --no-cli-pager --no-fail-on-empty-changeset
aws cloudformation describe-stacks --stack-name galashow-cloud-github-actions --query 'Stacks[0].Outputs' --profile galashow --region ap-northeast-2 --no-cli-pager
```

현재 값은 템플릿 파라미터 기본값에 있다. 다른 계정에 적용할 때는 배포 전에 파라미터를 해당 계정의 출력으로 바꾼다. 같은 OIDC 공급자가 다른 스택에서 이미 관리 중인 새 계정에서는 중복 생성하지 않도록 공급자 리소스를 통합한다.

## GitHub Environment 변수

세 저장소에 `dev`, `prod` Environment를 각각 만들고 다음 변수를 등록한다. `AWS_ACCOUNT_ID`와 `AWS_ROLE_ARN`은 모두 필요하며 API에는 `HOSTED_ZONE_ID=Z0263745GATMIS12FEIH`도 필요하다.

| 저장소 | Environment | `AWS_ROLE_ARN`의 역할 이름 |
| --- | --- | --- |
| GalaShow_Admin | dev | galashow-github-admin-dev |
| GalaShow_Admin | prod | galashow-github-admin-prod |
| GalaShow_Client | dev | galashow-github-client-dev |
| GalaShow_Client | prod | galashow-github-client-prod |
| GalaShow_API | dev | galashow-github-api-dev |
| GalaShow_API | prod | galashow-github-api-prod |

각 역할 ARN은 `arn:aws:iam::251113431583:role/<역할 이름>`이며 `AWS_ACCOUNT_ID=251113431583`이다. 배포 허용 ref는 `develop` 브랜치와 `v*` 태그다. 다른 ref의 수동 배포는 Environment의 deployment branch/tag 정책에 먼저 추가한다. 별도 수동 승인자는 추가하지 않는다.

Unity는 AWS 역할을 사용하지 않는다. 기존 `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD`, `GALASHOW_TOKEN` Secrets를 사용한다. 빌드 게시 토큰은 `GalaShow_UnityBuild` contents 쓰기와 선택적인 `GalaShow_Client` Actions 실행 권한이 필요하다. Client는 공개 빌드 저장소를 읽으므로 별도 개인 토큰이 필요하지 않다. Unity 라이선스와 이 토큰의 실제 유효성은 원격 빌드 실행으로 확인해야 한다.

PolyChat도 AWS 역할을 사용하지 않는다. npm 배포는 `NPM_TOKEN` Secret이 필요하며 이미 게시된 버전을 덮어쓰지 않는다. 기존 `polychat-bridge@1.0.2`는 이미 게시돼 있으므로 다음 릴리스 전에 버전을 올려야 한다. 이 CI/CD 설정 작업 자체는 패키지를 게시하지 않는다.

## 검증

```powershell
python -m unittest discover -s infra/tests -v
cfn-lint template.yaml infra/certificate.yaml infra/database.yaml infra/artifacts.yaml infra/web.json infra/github-actions.json
```

워크플로 구문은 actionlint로 검사한다. IAM 정책은 AWS Access Analyzer의 `validate-policy`로, 실제 역할의 허용·거부 범위는 IAM `simulate-principal-policy`로 확인한다. 이 검사는 GitHub에서 발급된 실제 OIDC 토큰의 교환이나 원격 배포 실행을 대신하지 않는다. 코드 커밋을 GitHub에 push한 뒤 각 워크플로 실행 결과를 확인한다. Client가 참조하는 새 UnityBuild 커밋과 `POLYCHAT_REF`에 고정한 PolyChat 커밋은 Client보다 먼저 push해야 한다.

## 2026-09-29 적용·검증 기록

- `galashow-cloud-github-actions` 스택 생성 완료. OIDC 공급자 1개와 역할 6개가 실제 AWS 계정에 존재한다.
- Admin/API/Client의 GitHub Environment 6개에 위 변수와 `develop`/`v*` 배포 정책을 등록하고 다시 조회해 확인했다.
- 실제 IAM 역할 6개에서 자기 환경의 S3 쓰기 또는 Lambda 코드 갱신은 `allowed`, 다른 환경은 `implicitDeny`임을 AWS 시뮬레이터로 확인했다. Access Analyzer의 identity policy 검증은 6개 모두 findings 0개다.
- API 오프라인 테스트 113개, 초기화 함수 테스트 13개, IAM·배포 검사 테스트 16개, CloudFormation 6개 템플릿 검사와 워크플로 구문 검사가 통과했다.
- 운영의 읽기 전용 웹/API/CORS 점검은 통과했다. 개발 서버의 localhost CORS는 현재 소스와 달라 엄격한 스모크 테스트가 실패했다. 기존 소스 수정이 배포되기 전 상태이며 검사 조건을 완화하지 않았다. 첫 개발 API 배포 후 다시 확인해야 한다.
- 애플리케이션 코드는 이 작업에서 push하거나 Actions로 실행하지 않았다. Unity 라이선스·게시 토큰·npm 토큰은 이름의 존재만 확인했고 실제 사용 성공으로 보고하지 않는다.

공식 기준: [GitHub OIDC와 AWS](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws), [AWS 인증 Action](https://github.com/aws-actions/configure-aws-credentials).

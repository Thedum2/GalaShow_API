<#
.SYNOPSIS
  Build SAM project and start local API.
.PARAMETER Stage
  배포 스테이지(dev 또는 prod). 기본값은 dev입니다.
#>
param(
    [ValidateSet("dev", "prod")]
    [string]$Stage = "dev"
)

# 1. 이전 빌드 아티팩트 삭제
Write-Host "`n[1/3] Removing previous build artifacts (.aws-sam)..."
if (Test-Path ".aws-sam") {
    Remove-Item -Recurse -Force ".aws-sam"
    Write-Host "  → .aws-sam folder removed."
} else {
    Write-Host "  → No existing .aws-sam folder."
}

# 2. SAM 빌드
Write-Host "`n[2/3] Building SAM project (host 빌드)..."
sam build --parallel
if ($LASTEXITCODE -ne 0) {
    Write-Error "SAM build failed. 스크립트를 종료합니다."
    exit $LASTEXITCODE
}

# 3. SAM 로컬 API 시작
Write-Host "`n[3/3] Starting SAM local API (StageNameParam=$Stage)..."
sam local start-api --parameter-overrides "StageNameParam=$Stage"

# (끝)

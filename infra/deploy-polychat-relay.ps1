param(
    [Parameter(Mandatory)][ValidateSet('dev', 'prod')][string]$Stage,
    # polychat-bridge npm version; defaults to the one locked in the Client repository.
    [string]$Version = '',
    [string]$EnvFile = '',
    [string]$Profile = 'galashow',
    [string]$Region = 'ap-northeast-2',
    [string]$CodeBucket = 'galashow-251113431583-ap-northeast-2-artifacts'
)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$workspace = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$profileArgs = if ($Profile) { @('--profile', $Profile) } else { @() }

if (-not $Version) {
    $lock = Get-Content (Join-Path $workspace 'Client/package-lock.json') -Raw | ConvertFrom-Json -AsHashtable
    $Version = $lock['packages']['node_modules/polychat-bridge']['version']
    if (-not $Version) { throw 'Pass -Version: polychat-bridge is not locked in Client/package-lock.json.' }
}

# Install the published package (never a local checkout or the PolyChat demo) with production dependencies only.
$work = Join-Path ([IO.Path]::GetTempPath()) "galashow-polychat-relay-$Stage"
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Path $work | Out-Null
Push-Location $work
try {
    '{"private":true}' | Set-Content package.json
    npm install "polychat-bridge@$Version" --omit=dev --no-audit --no-fund --silent
    "#!/bin/sh`nexec node node_modules/polychat-bridge/dist/server/cli.js`n" | Set-Content run.sh -NoNewline
} finally { Pop-Location }
$zip = "$work.zip"
python (Join-Path $PSScriptRoot 'zip-relay.py') $work $zip
$key = "polychat-relay/client-$Stage-$((Get-FileHash $zip -Algorithm SHA256).Hash.ToLower()).zip"
aws s3 cp $zip "s3://$CodeBucket/$key" --region $Region @profileArgs --only-show-errors

$overrides = @("StageName=$Stage", "CodeBucket=$CodeBucket", "CodeKey=$key", "PolyChatVersion=$Version")
# Credentials: Client/.env.local, then ../PolyChat/.env.local. Missing values keep the stack's previous ones.
$names = @{ CHZZK_CLIENT_ID = 'ChzzkClientId'; CHZZK_CLIENT_SECRET = 'ChzzkClientSecret'; SOOP_CLIENT_ID = 'SoopClientId';
    SOOP_CLIENT_SECRET = 'SoopClientSecret'; YOUTUBE_CLIENT_ID = 'YouTubeClientId' }
$files = if ($EnvFile) { @($EnvFile) } else { @((Join-Path $workspace 'Client/.env.local'), (Join-Path $workspace 'PolyChat/.env.local')) }
$values = @{}
foreach ($file in $files | Where-Object { Test-Path $_ }) {
    foreach ($line in Get-Content $file) {
        if ($line -match '^\s*([A-Z_]+)\s*=\s*(.*?)\s*$' -and $names.ContainsKey($Matches[1]) -and $Matches[2] -and -not $values.ContainsKey($Matches[1])) {
            $values[$Matches[1]] = $Matches[2].Trim('"')
        }
    }
}
foreach ($name in $values.Keys) { $overrides += "$($names[$name])=$($values[$name])" }

$stack = "galashow-cloud-polychat-relay-$Stage"
aws cloudformation deploy --stack-name $stack --template-file (Join-Path $PSScriptRoot 'polychat-relay.json') --region $Region @profileArgs `
    --capabilities CAPABILITY_IAM --parameter-overrides @overrides --no-fail-on-empty-changeset
$relayHost = aws cloudformation describe-stacks --stack-name $stack --region $Region @profileArgs `
    --query 'Stacks[0].Outputs[?OutputKey==`FunctionUrlHost`].OutputValue | [0]' --output text

# Route https://<client>/polychat/* to the relay. Other web stack parameters keep their current values.
aws cloudformation deploy --stack-name "galashow-cloud-web-$Stage" --template-file (Join-Path $PSScriptRoot 'web.json') --region $Region @profileArgs `
    --parameter-overrides "PolyChatRelayHost=$relayHost" --no-fail-on-empty-changeset
"polychat-bridge $Version relay: $relayHost"

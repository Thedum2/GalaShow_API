param(
    [Parameter(Mandatory)][ValidateSet('dev', 'prod')][string]$Stage,
    [Parameter(Mandatory)][string]$HostedZoneId,
    [string]$Profile = 'galashow',
    [string]$Region = 'ap-northeast-2',
    [string]$ChzzkSecretArn = '',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$apiRoot = Split-Path $PSScriptRoot -Parent
$awsCommand = Get-Command aws -ErrorAction SilentlyContinue
$awsExe = if ($awsCommand) { $awsCommand.Source } else {
    Join-Path $env:LOCALAPPDATA 'Programs/AWSCLI/Amazon/AWSCLIV2/aws.exe'
}
if (-not (Test-Path $awsExe)) { throw 'AWS CLI v2 is required.' }

function Invoke-Aws {
    param([string[]]$Arguments)
    $cliArguments = $Arguments + @('--region', $Region, '--no-cli-pager')
    if ($Profile) { $cliArguments += @('--profile', $Profile) }
    $result = & $awsExe @cliArguments
    if ($LASTEXITCODE -ne 0) { throw "AWS command failed: $($Arguments[0..1] -join ' ')" }
    return $result
}

function Get-StackOutputs {
    param([string]$StackName)
    $stack = (Invoke-Aws -Arguments @('cloudformation', 'describe-stacks', '--stack-name', $StackName, '--output', 'json') | ConvertFrom-Json).Stacks[0]
    if ($stack.StackStatus -notin @('CREATE_COMPLETE', 'UPDATE_COMPLETE')) {
        throw "Stack $StackName is not ready: $($stack.StackStatus)"
    }
    $outputs = @{}
    foreach ($item in $stack.Outputs) { $outputs[$item.OutputKey] = $item.OutputValue }
    return $outputs
}

Push-Location $apiRoot
try {
    $database = Get-StackOutputs "galashow-cloud-database-$Stage"
    $artifacts = Get-StackOutputs 'galashow-cloud-artifacts'
    $template = Get-Content -LiteralPath 'template.yaml' -Raw
    $codeDirectories = [regex]::Matches($template, '(?m)^\s+CodeUri: (.+?)/?\r?$') |
        ForEach-Object { $_.Groups[1].Value.Trim().TrimEnd('/') } | Sort-Object -Unique
    foreach ($directory in $codeDirectories) {
        $project = @(Get-ChildItem -LiteralPath $directory -Filter '*.csproj')
        if ($project.Count -ne 1) { throw "Expected one project in $directory" }
        $output = Join-Path $apiRoot ".aws-sam/local/$($project[0].BaseName)"
        if (-not $SkipBuild) {
            & dotnet publish $project[0].FullName --configuration Release --runtime linux-x64 --self-contained false `
                -p:GenerateRuntimeConfigurationFiles=true -p:PublishReadyToRun=false --output $output --nologo --verbosity quiet
            if ($LASTEXITCODE -ne 0) { throw "Build failed: $directory" }
        }
        if (-not (Test-Path (Join-Path $output "$($project[0].BaseName).runtimeconfig.json"))) {
            throw "Missing Lambda runtime configuration: $output"
        }
        $escapedDirectory = [regex]::Escape($directory)
        $outputPath = $output.Replace('\', '/')
        $template = [regex]::Replace($template, "(?m)(\s+CodeUri: )$escapedDirectory/?\r?`$", "`${1}$outputPath")
    }
    $stageDirectory = Join-Path $apiRoot ".aws-sam/$Stage"
    New-Item -ItemType Directory -Path $stageDirectory -Force > $null
    $localTemplate = Join-Path $stageDirectory 'local-template.yaml'
    $packagedTemplate = Join-Path $stageDirectory 'packaged-template.yaml'
    Set-Content -LiteralPath $localTemplate -Value $template -Encoding utf8NoBOM
    Invoke-Aws -Arguments @('cloudformation', 'package', '--template-file', $localTemplate,
        '--s3-bucket', $artifacts.BucketName, '--s3-prefix', "api/$Stage", '--output-template-file', $packagedTemplate)
    $parameters = @("StageNameParam=$Stage", "HostedZoneId=$HostedZoneId")
    # Omitted overrides preserve an existing stack parameter; a new stack uses its default.
    if (-not [string]::IsNullOrWhiteSpace($ChzzkSecretArn)) {
        $parameters += "ChzzkSecretArn=$ChzzkSecretArn"
    }
    foreach ($key in @('DatabaseHost', 'DatabasePort', 'DatabaseName', 'DatabaseSecretArn', 'JwtSecretArn', 'LambdaSecurityGroupId', 'PrivateSubnetIds')) {
        if (-not $database[$key]) { throw "Missing database stack output: $key" }
        $parameters += "$key=$($database[$key])"
    }
    Invoke-Aws -Arguments (@('cloudformation', 'deploy', '--stack-name', "galashow-cloud-api-$Stage",
        '--template-file', $packagedTemplate, '--s3-bucket', $artifacts.BucketName, '--s3-prefix', "api/$Stage",
        '--capabilities', 'CAPABILITY_IAM', '--no-fail-on-empty-changeset', '--parameter-overrides') + $parameters)
}
finally { Pop-Location }

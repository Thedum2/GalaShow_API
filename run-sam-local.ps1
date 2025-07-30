# stop existing api
sam local stop-api

# remove SAM build containers
$ids = docker ps -a --filter "ancestor=public.ecr.aws/sam/build-dotnet6" -q
if ($ids) { docker rm -f $ids }

# delete previous build artifacts
Remove-Item -Recurse -Force .aws-sam

# build & start
sam build --use-container --parallel
sam local start-api --parameter-overrides "StageNameParam=dev" --warm-containers Lazy

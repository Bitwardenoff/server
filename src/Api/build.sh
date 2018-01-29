#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJRROOT="$( cd "$( dirname "$DIR/../../.." )" && pwd )"
alias dotnet='docker run --rm -v $PROJRROOT:$PROJRROOT -w $DIR microsoft/dotnet dotnet'
shopt -s expand_aliases

echo -e "\n## Building API"

echo -e "\nBuilding app"
echo ".NET Core version $(dotnet --version)"
echo "Clean"
dotnet clean $DIR/Api.csproj -f netcoreapp2.0 -c "Release" -o $DIR/obj/Docker/publish/Api
dotnet clean $DIR/../Jobs/Jobs.csproj -f netcoreapp2.0 -c "Release" -o $DIR/obj/Docker/publish/Jobs
echo "Publish"
dotnet publish $DIR/Api.csproj -f netcoreapp2.0 -c "Release" -o $DIR/obj/Docker/publish/Api
dotnet publish $DIR/../Jobs/Jobs.csproj -f netcoreapp2.0 -c "Release" -o $DIR/obj/Docker/publish/Jobs

echo -e "\nBuilding docker image"
docker --version
docker build -t bitwarden/api $DIR/.

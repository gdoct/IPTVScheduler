#!/bin/bash
# Exit on any error
set -e

# Keep track of the last executed command
trap 'last_command=$current_command; current_command=$BASH_COMMAND' DEBUG

# Echo an error message before exiting
trap 'echo "The command \"${last_command}\" failed with exit code $?."' EXIT
dotnet clean
dotnet build /p:Configuration=Release
dotnet publish -c Release -o bin
# Build the Docker image
docker build -t ipvcr-web:latest -f Dockerfile .

scp bin/ipvcr-web.img guido@nuc-guido:~ 
ssh guido@nuc-guido << 'ENDSSH' || true

# remove the running container
docker rm -f ipvcr-web

# remove the existing image
docker rmi ipvcr-web

# load the new image
docker load -i ipvcr-web.img

# deploy the new image to a new container
docker run --name ipvcr-web --network host -d -v /media/series:/media -v /var/lib/iptvscheduler:/data ipvcr-web:latest

# remove the image from the remote server
rm ipvcr-web.img

ENDSSH

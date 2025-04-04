#!/bin/bash
scp bin/ipvcr-web.img guido@nuc-guido:~

# send these command over ssh
ssh guido@nuc-guido << 'ENDSSH'

# remove the running container
docker rm -f ipvcr-web

# remove the existing image
docker rmi ipvcr-web

# load the new image
docker load -i ipvcr-web.img

# deploy the new image to a new container
docker run --name ipvcr-web --network host -d -v /media/series:/media ipvcr-web

# remove the image from the remote server
rm ipvcr-web.img

ENDSSH

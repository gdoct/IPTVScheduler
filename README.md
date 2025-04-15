[![Docker Image CI](https://github.com/gdoct/ipvcr/actions/workflows/docker-image.yml/badge.svg)](https://github.com/gdoct/ipvcr/actions/workflows/docker-image.yml)
# IPVCR: A Docker-based IPTV Recorder / Scheduler
![image](https://github.com/user-attachments/assets/7c23f585-7103-4eda-aa75-ba20adfd9c4b)

A simple web VCR in a docker image

## Features

- React-based web interface for managing IPTV recordings
- Schedule recordings by time and channel.
- Allows timezone offsets between user and server.
- Records IPTV streams (http) through ```ffmpeg```
- Schedules recordings through ```at```
- Easy configuration through the web interface
- Support for large channel lists ( > 100 mb)

## Screenshot

![image](https://github.com/user-attachments/assets/2714a442-5914-46c2-9d52-3755116f6478)

## Requirements
The docker image requires write access to two mounted folders.
 - media : where the recordings are stored
 - data : where it stores its data and settings

The application requires a m3u file with iptv channels. This file can be copied to the mounted data volume, or uploaded through the web interface. The application supports very large m3u files.

## Docker Deployment

To run the docker image on port 5000 using host networking, you can use this command:
```
docker run --name ipvcr --network host -d -v /path/to/media:/media -v /path/to/data:/data ghcr.io/gdoct/ipvcr:latest
```

To run the docker using bridged networking and redirect the default ports, use this command:
```
docker run --name ipvcr -p 5000:5000 -d -v /path/to/media:/media -v /path/to/data:/data ghcr.io/gdoct/ipvcr:latest
```

At the first run, make sure to configure the m3u file.

## Development

This is a .NET project targeting Linux or WSL. To clone and build the solution:

```
git clone https://github.com/gdoct/ipvcr.git
cd ipvcr
./build-frontend.sh
dotnet build
dotnet run --project ipvcr.Web
```

## Docker Compose

You can also use Docker Compose to run this application:

```
version: '3'
services:
  ipvcr:
    image: ghcr.io/gdoct/ipvcr:latest
    container_name: ipvcr
    ports:
      - "5000:5000"
    volumes:
      - /path/to/media:/media
      - /path/to/data:/data
    restart: unless-stopped
```

Save this to a docker-compose.yml file and run with `docker-compose up -d`.


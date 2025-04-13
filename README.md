[![Docker Image CI](https://github.com/gdoct/ipvcr/actions/workflows/docker-image.yml/badge.svg)](https://github.com/gdoct/ipvcr/actions/workflows/docker-image.yml)
# IPVCR: A Docker-based IPTV Recorder / Scheduler
![image](https://github.com/user-attachments/assets/7c23f585-7103-4eda-aa75-ba20adfd9c4b)

A simple web VCR in a docker image

## Features

- Web-based interface for managing IPTV recordings
- Schedule recordings by time or program
- Support for IPTV streams
- Automatic recording management
- Easy configuration through the web interface

## Screenshot

![image](https://github.com/user-attachments/assets/2714a442-5914-46c2-9d52-3755116f6478)

## Requirements
The docker image requires write access to two mounted folders.
 - media : where the recordings are stored
 - data : where it stores its data and settings
The application requires a m3u file with iptv channels. This file can be copied to the mounted data volume, or uploaded through the web interface. Be aware that large m3u files (>10 mb) have not been tested and currently slow down the application because all channels are rendered into the page's 'channels' dropdown.

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

This is a .NET project. To build the solution:

```
dotnet build ipvcr.sln
```

To run the web interface locally:

```
dotnet run --project ipvcr.Web
```

## Docker Compose

You can also use Docker Compose to run the application:

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


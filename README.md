![image](https://github.com/user-attachments/assets/7c23f585-7103-4eda-aa75-ba20adfd9c4b)
# IPTV Scheduler

A simple web VCR in a docker image

## Requirements

The docker requires write access to two mounted folders.
 - media : where the recordings are stored
 - data : where it stores its data and settings

## Docker Deployment

To run the docker image on port 5000 using host networking, you can use this command:
```
docker run --name ipvcr --network host -d -v /path/to/media:/media -v /path/to/data:/data ghcr.io/gdoct/ipvcr:latest
```

To run the docker using bridged networking and redirect the default ports, use this command:
```
docker run --name ipvcr -p 5000:5000 -d -v /path/to/media:/media -v /path/to/data:/data ghcr.io/gdoct/ipvcr:latest
```

## Features

- Web-based interface for managing IPTV recordings
- Schedule recordings by time or program
- Support for IPTV streams
- Automatic recording management
- Easy configuration through the web interface

## Configuration

A sample configuration file is provided in `setting.json.example`. Copy this to your data directory as `setting.json` and modify as needed.

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


# ipvcr-base image
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Install required packages
RUN apt-get update && \
    apt-get install -y at ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create required directories
RUN mkdir -p /var/spool/cron/atjobs && \
    mkdir -p /media && \
    mkdir -p /data && \
    mkdir -p /etc/iptvscheduler && \
    chmod -R 755 /var/spool/cron/atjobs /media /var/log/iptvscheduler /etc/iptvscheduler

# Verify SDK installation
RUN dotnet --list-sdks && \
    dotnet --list-runtimes

# Create a test project to verify SDK
RUN dotnet new console --no-restore && \
    dotnet restore && \
    rm -rf *


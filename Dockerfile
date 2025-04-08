FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install required packages
RUN apt-get update && \
    apt-get install -y at ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /source

# Copy the solution file and restore dependencies
COPY ["ipvcr.sln", "."]
COPY ["ipvcr.Scheduling/ipvcr.Scheduling.csproj", "ipvcr.Scheduling/"]
COPY ["ipvcr.Scheduling.Linux/ipvcr.Scheduling.Linux.csproj", "ipvcr.Scheduling.Linux/"]
COPY ["ipvcr.Scheduling.Linux.Tests/ipvcr.Scheduling.Linux.Tests.csproj", "ipvcr.Scheduling.Linux.Tests/"]
COPY ["ipvcr.Scheduling.Shared/ipvcr.Scheduling.Shared.csproj", "ipvcr.Scheduling.Shared/"]
COPY ["ipvcr.Tests/ipvcr.Tests.csproj", "ipvcr.Tests/"]
COPY ["ipvcr.Web/ipvcr.Web.csproj", "ipvcr.Web/"]

RUN dotnet restore "ipvcr.sln"

# Copy all source code
COPY . .

# Build the solution
RUN dotnet build "ipvcr.sln" -c Release -o /app/build
RUN dotnet publish "ipvcr.Web/ipvcr.Web.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Set timezone
ENV TZ=Europe/Amsterdam
RUN apt-get update && \
    apt-get install -y tzdata && \
    ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && \
    echo $TZ > /etc/timezone

# Install required packages in final image
RUN apt-get install -y at ffmpeg && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create required directories
RUN mkdir -p /var/spool/cron/atjobs && \
    mkdir -p /media && \
    mkdir -p /data && \
    mkdir -p /etc/iptvscheduler && \
    chmod -R 755 /var/spool/cron/atjobs /media /data /etc/iptvscheduler

COPY --from=build /app/publish .

CMD ["bash", "-c", "/usr/sbin/atd && dotnet ipvcr.Web.dll"]


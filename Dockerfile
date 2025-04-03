FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the solution file and restore dependencies for the entire solution
COPY ["ipvcr.sln", "."]
COPY ["ipvcr.Scheduling/ipvcr.Scheduling.csproj", "ipvcr.Scheduling/"]
COPY ["ipvcr.Scheduling.Shared/ipvcr.Scheduling.Shared.csproj", "ipvcr.ipvcr.Scheduling.Shared/"]
COPY ["ipvcr.Scheduling.Linux/ipvcr.Scheduling.Linux.csproj", "ipvcr.Scheduling.Linux/"]
COPY ["ipvcr.Scheduling.Windows/ipvcr.Scheduling.Windows.csproj", "ipvcr.Scheduling.Windows/"]
COPY ["ipvcr.Tests/ipvcr.Tests.csproj", "ipvcr.Tests/"]
COPY ["ipvcr.Scheduling.Linux.Tests/ipvcr.Scheduling.Linux.Tests.csproj", "ipvcr.Scheduling.Linux.Tests/"]
COPY ["ipvcr.Web/ipvcr.Web.csproj", "ipvcr.Web/"]
RUN dotnet restore "ipvcr.sln"

# Copy all source code
COPY . .

# Build the solution
RUN dotnet build "ipvcr.sln" -c Release -o /app/build

# Publish only the web project
FROM build AS publish
RUN dotnet publish "ipvcr.Web/ipvcr.Web.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "ipvcr.Web.dll"]

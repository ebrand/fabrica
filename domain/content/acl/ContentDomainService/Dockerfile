FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 3460

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["content/acl/ContentDomainService/ContentDomainService.csproj", "content/acl/ContentDomainService/"]
COPY ["common/Fabrica.Domain.Esb/Fabrica.Domain.Esb.csproj", "common/Fabrica.Domain.Esb/"]

# Restore dependencies
RUN dotnet restore "content/acl/ContentDomainService/ContentDomainService.csproj"

# Copy all source files
COPY ["content/acl/ContentDomainService/", "content/acl/ContentDomainService/"]
COPY ["common/Fabrica.Domain.Esb/", "common/Fabrica.Domain.Esb/"]

# Build the application
WORKDIR /src/content/acl/ContentDomainService
RUN dotnet build "ContentDomainService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ContentDomainService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ContentDomainService.dll"]

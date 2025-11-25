FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 3420

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["product/acl/ProductDomainService/ProductDomainService.csproj", "product/acl/ProductDomainService/"]
COPY ["common/Fabrica.Domain.Esb/Fabrica.Domain.Esb.csproj", "common/Fabrica.Domain.Esb/"]

# Restore dependencies
RUN dotnet restore "product/acl/ProductDomainService/ProductDomainService.csproj"

# Copy all source files
COPY ["product/acl/ProductDomainService/", "product/acl/ProductDomainService/"]
COPY ["common/Fabrica.Domain.Esb/", "common/Fabrica.Domain.Esb/"]

# Build the application
WORKDIR /src/product/acl/ProductDomainService
RUN dotnet build "ProductDomainService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductDomainService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductDomainService.dll"]

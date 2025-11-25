# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files for restore
COPY ["admin/acl/AdminDomainService/AdminDomainService.csproj", "admin/acl/AdminDomainService/"]
COPY ["common/Fabrica.Domain.Esb/Fabrica.Domain.Esb.csproj", "common/Fabrica.Domain.Esb/"]

# Restore dependencies
RUN dotnet restore "admin/acl/AdminDomainService/AdminDomainService.csproj"

# Copy all source files
COPY ["admin/acl/AdminDomainService/", "admin/acl/AdminDomainService/"]
COPY ["common/Fabrica.Domain.Esb/", "common/Fabrica.Domain.Esb/"]

# Publish the application
WORKDIR /src/admin/acl/AdminDomainService
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port 3600 (Admin/IAM service port per PORTS.md)
EXPOSE 3600

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:3600

# Run the application directly (config fetched from Vault in Program.cs)
ENTRYPOINT ["dotnet", "AdminDomainService.dll"]

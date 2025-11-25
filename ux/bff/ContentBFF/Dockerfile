# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ContentBFF.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port 3240 (Content BFF port per PORTS.md)
EXPOSE 3240

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:3240

# Run the application
ENTRYPOINT ["dotnet", "ContentBFF.dll"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY src/MmProxy/MmProxy.csproj src/MmProxy/
RUN dotnet restore src/MmProxy/MmProxy.csproj

# Copy source code and build
COPY src/MmProxy/ src/MmProxy/
RUN dotnet publish src/MmProxy/MmProxy.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app from build stage
COPY --from=build /app/publish .

# Expose default port
EXPOSE 8080

# Configure healthcheck
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables with defaults
ENV ASPNETCORE_URLS=http://+:8080 \
    SERVICE_PORT=8080

ENTRYPOINT ["dotnet", "MmProxy.dll"]

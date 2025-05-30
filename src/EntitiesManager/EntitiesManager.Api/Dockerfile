# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/EntitiesManager/EntitiesManager.Api/EntitiesManager.Api.csproj", "EntitiesManager.Api/"]
COPY ["src/EntitiesManager/EntitiesManager.Core/EntitiesManager.Core.csproj", "EntitiesManager.Core/"]
COPY ["src/EntitiesManager/EntitiesManager.Infrastructure/EntitiesManager.Infrastructure.csproj", "EntitiesManager.Infrastructure/"]
COPY ["src/EntitiesManager/EntitiesManager.Application/EntitiesManager.Application.csproj", "EntitiesManager.Application/"]

RUN dotnet restore "EntitiesManager.Api/EntitiesManager.Api.csproj"

# Copy source code
COPY src/EntitiesManager/ .

# Build the application
RUN dotnet build "EntitiesManager.Api/EntitiesManager.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EntitiesManager.Api/EntitiesManager.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Create logs directory
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Expose ports
EXPOSE 5000 5001

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001

ENTRYPOINT ["dotnet", "EntitiesManager.Api.dll"]

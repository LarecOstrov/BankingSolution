# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
RUN apt-get update && apt-get install -y curl

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project files and restore the dependencies
COPY ["Banking.API/Banking.API.csproj", "Banking.API/"]
COPY ["Banking.Application/Banking.Application.csproj", "Banking.Application/"]
COPY ["Banking.Domain/Banking.Domain.csproj", "Banking.Domain/"]
COPY ["Banking.Infrastructure/Banking.Infrastructure.csproj", "Banking.Infrastructure/"]

# Restore the dependencies
RUN dotnet restore "./Banking.API/Banking.API.csproj"

# Copy the rest of the files
COPY . .
WORKDIR "/src/Banking.API"
RUN dotnet build "./Banking.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Banking.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish --runtime linux-x64 --self-contained false /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
# Copy built files from the previous stage
COPY --from=publish /app/publish .

# Verify contents
RUN ls -lah /app

# Health check to detect crashes
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1
# Run the application
ENTRYPOINT ["dotnet", "Banking.API.dll"]
# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["TicketsHex.API/TicketsHex.API.csproj", "TicketsHex.API/"]
COPY ["TicketsHex.Application/TicketsHex.Application.csproj", "TicketsHex.Application/"]
COPY ["TicketsHex.Domain/TicketsHex.Domain.csproj", "TicketsHex.Domain/"]
COPY ["TicketsHex.infrastructure/TicketsHex.infrastructure.csproj", "TicketsHex.infrastructure/"]
RUN dotnet restore "./TicketsHex.API/TicketsHex.API.csproj"

COPY . .
WORKDIR "/src/TicketsHex.API"
RUN dotnet publish "./TicketsHex.API.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS final
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

RUN mkdir -p /app/Logs && chown "$APP_UID:$APP_UID" /app/Logs
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "TicketsHex.API.dll"]

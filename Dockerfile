# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG VERSION=1.0.0
WORKDIR /src

COPY . .
RUN dotnet restore ParallelYou.Web/ParallelYou.Web.csproj
RUN dotnet publish ParallelYou.Web/ParallelYou.Web.csproj \
    --configuration ${BUILD_CONFIGURATION} \
    --no-restore \
    --output /app/publish \
    --property:UseAppHost=false \
    --property:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
ARG VERSION=1.0.0
WORKDIR /app

LABEL org.opencontainers.image.title="Parallel You" \
      org.opencontainers.image.description="A private, revisable record of observation, reflection, and direction." \
      org.opencontainers.image.source="https://github.com/aetheric-forge/parallel-you" \
      org.opencontainers.image.version="${VERSION}"

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ParallelYou.Web.dll"]

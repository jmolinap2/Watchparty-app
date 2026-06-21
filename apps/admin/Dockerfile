# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first for better layer caching.
COPY backend/Directory.Build.props ./backend/Directory.Build.props
COPY backend/src/WatchParty.Observability ./backend/src/WatchParty.Observability
COPY backend/src/WatchParty.Contracts ./backend/src/WatchParty.Contracts
COPY apps/admin ./apps/admin
RUN dotnet restore apps/admin/WatchParty.Admin.csproj

RUN dotnet publish apps/admin/WatchParty.Admin.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8090 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8090

ENTRYPOINT ["dotnet", "WatchParty.Admin.dll"]

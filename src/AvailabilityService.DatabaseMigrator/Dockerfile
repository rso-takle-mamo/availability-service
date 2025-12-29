FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AvailabilityService.DatabaseMigrator/AvailabilityService.DatabaseMigrator.csproj", "AvailabilityService.DatabaseMigrator/"]
RUN dotnet restore "AvailabilityService.DatabaseMigrator/AvailabilityService.DatabaseMigrator.csproj"
COPY . .
WORKDIR "/src/AvailabilityService.DatabaseMigrator"
RUN dotnet build "./AvailabilityService.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AvailabilityService.DatabaseMigrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AvailabilityService.DatabaseMigrator.dll"]

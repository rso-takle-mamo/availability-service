FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8000
ENV ASPNETCORE_URLS=http://+:8000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["AvailabilityService.Api/AvailabilityService.Api.csproj", "AvailabilityService.Api/"]
RUN dotnet restore "AvailabilityService.Api/AvailabilityService.Api.csproj"
COPY . .
WORKDIR "/src/AvailabilityService.Api"
RUN dotnet build "./AvailabilityService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AvailabilityService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AvailabilityService.Api.dll"]
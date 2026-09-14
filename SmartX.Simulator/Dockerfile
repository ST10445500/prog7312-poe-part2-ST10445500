# Builds the simulator into a container image. Its a console app, so it needs the
# plain runtime rather than the aspnet one.

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SmartX.Simulator/SmartX.Simulator.csproj", "SmartX.Simulator/"]
COPY ["SmartX.Shared/SmartX.Shared.csproj", "SmartX.Shared/"]
RUN dotnet restore "./SmartX.Simulator/SmartX.Simulator.csproj"
COPY . .
WORKDIR "/src/SmartX.Simulator"
RUN dotnet publish "./SmartX.Simulator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartX.Simulator.dll"]

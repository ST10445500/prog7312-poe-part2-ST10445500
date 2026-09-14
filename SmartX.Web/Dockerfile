# Builds the blazor client into a container image. WebAssembly compiles down to
# static files, so the image serves them with nginx rather than with .NET.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SmartX.Web/SmartX.Web.csproj", "SmartX.Web/"]
COPY ["SmartX.Shared/SmartX.Shared.csproj", "SmartX.Shared/"]
RUN dotnet restore "./SmartX.Web/SmartX.Web.csproj"
COPY . .
WORKDIR "/src/SmartX.Web"
RUN dotnet publish "./SmartX.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY SmartX.Web/nginx.conf /etc/nginx/conf.d/default.conf
# The client runs in the browser, which cannot resolve a compose service name,
# so this copy of the settings points at the gateways published port instead.
COPY SmartX.Web/docker-appsettings.json /usr/share/nginx/html/appsettings.json
EXPOSE 80

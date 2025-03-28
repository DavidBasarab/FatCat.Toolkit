FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OneOffBlazor/OneOffBlazor.csproj", "OneOffBlazor/"]
COPY ["ToolKit/ToolKit.csproj", "ToolKit/"]
RUN dotnet restore "OneOffBlazor/OneOffBlazor.csproj"
COPY . .
WORKDIR "/src/OneOffBlazor"
RUN dotnet build "OneOffBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OneOffBlazor.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY /OneOffBlazor/nginx.conf /etc/nginx/nginx.conf


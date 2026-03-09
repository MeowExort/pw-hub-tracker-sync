# Stage 1: Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["Pw.Hub.Tracker.Sync.Web/Pw.Hub.Tracker.Sync.Web.csproj", "Pw.Hub.Tracker.Sync.Web/"]
RUN dotnet restore "Pw.Hub.Tracker.Sync.Web/Pw.Hub.Tracker.Sync.Web.csproj"

# Копируем все остальные файлы и собираем проект
COPY . .
WORKDIR "/src/Pw.Hub.Tracker.Sync.Web"
RUN dotnet build "Pw.Hub.Tracker.Sync.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Pw.Hub.Tracker.Sync.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pw.Hub.Tracker.Sync.Web.dll"]

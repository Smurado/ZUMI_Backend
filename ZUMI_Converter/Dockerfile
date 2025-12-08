# Basis-Image: ASP.NET Core Runtime (Hier läuft die App später)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# WICHTIG: FFmpeg installieren
# Wir aktualisieren die Paketquellen und installieren ffmpeg
RUN apt-get update && \
    apt-get install -y ffmpeg && \
    rm -rf /var/lib/apt/lists/*

# Build-Image: SDK (Hier wird kompiliert)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Kopiere die Projektdatei und restore Abhängigkeiten
COPY ["ZUMI_Converter/ZUMI_Converter.csproj", "ZUMI_Converter/"]
RUN dotnet restore "ZUMI_Converter/ZUMI_Converter.csproj"

# Kopiere den Rest des Codes
COPY . .
WORKDIR "/src/ZUMI_Converter"
RUN dotnet build "ZUMI_Converter.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish (Erstellt die ausführbaren Dateien)
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ZUMI_Converter.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Finales Image zusammenbauen
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZUMI_Converter.dll"]
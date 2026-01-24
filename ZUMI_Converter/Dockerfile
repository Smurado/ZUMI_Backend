# Basis-Image: ASP.NET Core Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
# (HINWEIS: Es gibt noch kein aspnet:10.0, ich nehme an du nutzt 8.0 oder 9.0)
WORKDIR /app
EXPOSE 8080

# --- ÄNDERUNG START ---
# Statt apt-get nutzen wir ein statisches Image, um ein NEUES ffmpeg zu holen
# Das Image 'mwader/static-ffmpeg' enthält alles, was wir brauchen (inkl. svt-av1)
COPY --from=mwader/static-ffmpeg:8.0.1 /ffmpeg /usr/local/bin/
COPY --from=mwader/static-ffmpeg:8.0.1 /ffprobe /usr/local/bin/

# Wir brauchen apt-get update eigentlich nicht mehr für ffmpeg, 
# aber falls du libgdiplus oder so brauchst, lass es drin.
# --- ÄNDERUNG ENDE ---

# Build-Image: SDK
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
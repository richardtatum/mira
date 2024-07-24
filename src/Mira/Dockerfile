FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Find and copy all modules .csproj files to their respective directories inside /app/modules
COPY ["Modules/", "/temp/Modules"]
RUN find /temp/Modules -name "*.csproj" -exec sh -c 'd=$(dirname "{}"); mkdir -p /src/Mira/Modules/${d#/temp/Modules/} && cp "{}" /src/Mira/Modules/${d#/temp/Modules/}/$(basename "{}")' \;
RUN find /src/Mira/Modules -name "*.csproj" -print # Print out the .csproj files to verify they are copied correctly

# Copy the main project for restore
COPY ["Bot/Bot.csproj", "./Mira/Bot/"]
RUN dotnet restore "./Mira/Bot/Bot.csproj"

COPY . ./Mira
WORKDIR /src/Mira/Bot
RUN dotnet build Bot.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p db # the db gets stored here at /app/db
ENTRYPOINT ["dotnet", "Bot.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bot/Bot.csproj", "Bot/"]
COPY ["Modules", "Modules/"]

# Find and copy all .csproj files to their respective directories inside /src/Modules
RUN find /src/Modules -name "*.csproj" -exec sh -c 'mkdir -p /app/$(dirname "{}") && cp "{}" /app/{}' \;
RUN find /src/Modules -name "*.csproj" -print # Print the names of the copied files to the console

RUN dotnet restore "Bot/Bot.csproj"
COPY . .
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p db # the db gets stored here at /app/db
ENTRYPOINT ["dotnet", "Bot.dll"]

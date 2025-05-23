FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG TELEGRAM_BOT_TOKEN
ARG BASE_API_URL

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TimeTrackerBot.csproj", "."]
RUN dotnet restore "./TimeTrackerBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./TimeTrackerBot.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TimeTrackerBot.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final

ENV TELEGRAM_BOT_TOKEN=${TELEGRAM_BOT_TOKEN}
ENV BASE_API_URL=${BASE_API_URL}

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TimeTrackerBot.dll"]
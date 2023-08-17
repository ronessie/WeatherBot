FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY "telegramBot/telegramBot.csproj" .
RUN dotnet restore
COPY . .
RUN dotnet build telegramBot -c Release -o /app/build
FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app/build
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "telegramBot.dll"]
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

COPY . .
RUN dotnet publish -c Release -o out
CMD ["dotnet", "telegramBot.dll"]

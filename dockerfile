#FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
#WORKDIR /app
#COPY *.csproj .
#RUN dotnet restore
#COPY . .
#RUN dotnet publish -c release -o /app --no-restore
#FROM mcr.microsoft.com/dotnet/runtime:7.0
#WORKDIR /app
#COPY --from=build /app .
#ENTRYPOINT ["dotnet", "telegramBot.dll"]

# Использовать образ SDK для сборки приложения
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Копировать csproj и восстановить любые зависимости (через NuGet)
COPY *.csproj ./
RUN dotnet restore

# Копировать всё остальное и построить
COPY . ./
RUN dotnet publish -c Release -o out

# Создать образ на основе времени выполнения
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "telegramBot.dll"]




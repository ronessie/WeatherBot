# Weather Telegram Bot

This project is a telegram bot to retrieve weather information. The bot is written in C# and uses MongoDB for data storage, Docker for containerization and OpenWeather API to get weather data.

## Getting started

### Pre-requisites

- .NET 6.0 SDK or higher
- Docker
- MongoDB
- OpenWeather API account

### Installation

1. Clone the repository:

    ```bash
    git clone https://github.com/ronessie/WeatherBot.git
    cd WeatherBot
    ```

2. Set up environment variables:

    Create an `.env` file in the root of the project and add the following variables:

    ``env
    TELEGRAM_BOT_TOKEN=your_telegram_bot_token
    OPENWEATHER_API_KEY=your_openweather_api_key
    MONGO_CONNECTION_STRING=your_mongo_connection_string
    ```

3. Build and start the Docker container:

    ```bash
    docker-compose up --build
    ```

### Usage

Once the bot is started, you can interact with it via Telegram. Send the `/start` command to get started.

### Project structure

- `telegramBot/` - Bot source code
- `Dockerfile` - Docker configuration
- `docker-compose.yml` - Docker Compose configuration
- `README.md` - This file

### Technologies

- C#
- MongoDB
- Docker
- OpenWeather API
- Telegram.Bot library

### Contribution

If you would like to contribute to the project, please create a pull request or open an issue.

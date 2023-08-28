namespace telegramBot.Config
{
    public record MainConfig(
        string Token, string ApiToken, string DbName, string Host, int Port, string AuthorizationName, string AuthorizationPassword, string AuthDb
    );
}
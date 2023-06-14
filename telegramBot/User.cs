using MongoDB.Bson;

namespace telegramBot;

public class User
{
    public ObjectId Id { get; set; }
    public long TelegramId { get; set; }
    public string Name { get; set; }
    public string NickName { get; set; }
    public string City { get; set; }
            
    public string Status { get; set; }
}
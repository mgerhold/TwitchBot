using Newtonsoft.Json;

namespace TwitchBot.Models
{
    public class UserInfo
    {
        [JsonProperty] public int Points;
        [JsonProperty] public string Username;

    }
}

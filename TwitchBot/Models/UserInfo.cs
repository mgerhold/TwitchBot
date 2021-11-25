using Newtonsoft.Json;
using System;

namespace TwitchBot.Models
{
    public class UserInfo
    {
        [JsonProperty] public int Points;
        [JsonProperty] public string UserId;
        [JsonProperty] public DateTime LastPointSet;

    }
}

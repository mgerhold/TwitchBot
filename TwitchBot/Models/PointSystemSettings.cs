using System;

namespace TwitchBot.Models {
    public class PointSystemSettings {
        public int UserJoinAmount { get; set; } = 1;
        public int UserTimedAmount { get; set; } = 5;
        public TimeSpan PointGivingDelay { get; set; } = TimeSpan.FromMinutes(10);

    }
}

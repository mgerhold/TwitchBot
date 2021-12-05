// https://raw.githubusercontent.com/swiftyspiffy/Twitch-Auth-Example/main/TwitchAuthExample/Models/Authorization.cs

using System;
using System.Collections.Generic;
using System.Text;

namespace TwitchBot.Models {
    public class Authorization {
        public string Code { get; }

        public Authorization(string code) {
            Code = code;
        }
    }
}
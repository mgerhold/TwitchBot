﻿using System;
using System.IO;
using TwitchBot.Models;

namespace TwitchBot {

    class Program {
        static void Main(string[] args) {
            Console.WriteLine(Directory.GetCurrentDirectory());
            try {
                var bot = new Bot(Config.LoadOrDefault());
            } catch (Exception e) {
                Console.Error.WriteLine($"Unable to connect: {e.Message}");
                Console.WriteLine("Press Enter to Quit");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBot {

    class Color {
        public ConsoleColor Foreground { get; set; }
        public ConsoleColor Background { get; set; }

        public static readonly Color Default = new Color { Foreground = ConsoleColor.Gray, Background = ConsoleColor.Black };
        public static readonly Color BotHighlighted = new Color { Foreground = ConsoleColor.White, Background = ConsoleColor.Red };
        public static readonly Color Highlighted = new Color { Foreground = ConsoleColor.Black, Background = ConsoleColor.Yellow };
        public static readonly Color CommandHighlighted = new Color { Foreground = ConsoleColor.Black, Background = ConsoleColor.Blue };

        public void Apply() {
            Console.ForegroundColor = Foreground;
            Console.BackgroundColor = Background;
        }
    }

}

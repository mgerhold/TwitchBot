using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchBot.Commands;
using TwitchBot.Models;

namespace TwitchBot {
    class Bot {
        private TwitchClient client = null;
        private bool logging = true; // output logging messages? gets automatically disabled on successful connect
        private Config config = null;
        private CommandList nativeCommands = new(); // commands that execute C# code
        private CommandList customCommands = PersistentCommandList.LoadOrCreate("commands.json"); // commands that can be set through the Twitch chat        
        private Mutex commandListsMutex = new Mutex();
        private Thread timedCommandsThread = null;

        public Bot(Config config) {
            SetupNativeCommands();
            this.config = config;
            var credentials = new ConnectionCredentials(config.Username, config.OAuthToken);
            var clientOptions = new ClientOptions {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            var customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, config.Channel);

            client.OnLog += OnClientLog;
            client.OnJoinedChannel += OnClientJoinedChannel;
            client.OnMessageReceived += OnClientMessageReceived;
            client.OnConnected += OnClientConnected;

            client.Connect();

            timedCommandsThread = new Thread(new ThreadStart(HandleTimers));
            timedCommandsThread.Start();
        }

        private void HandleTimers() {
            Console.WriteLine("Started thread to handle timers...");
            while (true) { // approved by Andrei Alexandrescu
                var sleepDuration = config.TimedMessagesInterval;
                try {
                    commandListsMutex.WaitOne();
                    var allTimerCommands = nativeCommands.Commands
                        .Concat(customCommands.Commands)
                        .Where(command => command.IsTimer);
                    if (allTimerCommands.All(command => command.LastInvoked + config.TimedMessagesInterval > DateTime.Now)) {
                        var oldestCommand = allTimerCommands
                            .OrderBy(command => command.LastInvoked)
                            .FirstOrDefault();
                        if (oldestCommand is not null) {
                            sleepDuration = oldestCommand.LastInvoked + config.TimedMessagesInterval - DateTime.Now;
                        }
                    } else {
                        var nextToInvoke = allTimerCommands
                            .OrderBy(command => command.LastInvoked)
                            .FirstOrDefault();
                        if (nextToInvoke is not null) {
                            Console.WriteLine("Invoking timed command!");
                            nextToInvoke.Invoke(this);
                        }
                    }
                } finally {
                    commandListsMutex.ReleaseMutex();
                }
                Console.WriteLine($"Timed command thread sleeping...");
                Thread.Sleep(sleepDuration);
            }
        }

        private void SetupNativeCommands() {
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!add"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 3);
                    if (parts.Length != 3) {
                        bot.SendMessage($"@{message.Username} Syntax: !add <trigger> <message>");
                        return;
                    }
                    if (nativeCommands.Commands
                            .Concat(customCommands.Commands)
                            .Any(command => command.Trigger.ShouldTrigger(parts[2]))) {
                        bot.SendMessage($"@{message.Username} This command would clash with another command!");
                        return;
                    }
                    customCommands.Add(new EchoCommand {
                        Trigger = Triggers.StartsWithWord(parts[1]),
                        Response = parts[2],
                        Cooldown = TimeSpan.FromSeconds(29)
                    });
                    bot.SendMessage($"@{message.Username} Kommando {parts[1]} hinzugefügt!");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!interval"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    bot.SendMessage($"@{message.Username} The current timer interval is {config.TimedMessagesInterval} minutes.");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!setinterval"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 2);
                    if (parts.Length != 2) {
                        bot.SendMessage($"@{message.Username} Syntax: !setinterval <duration in minutes>");
                        return;
                    }
                    if (!int.TryParse(parts[1], out var minutes)) {
                        bot.SendMessage($"@{message.Username} \"{parts[1]}\" is not a valid integer!");
                        return;
                    }
                    config.TimedMessagesInterval = TimeSpan.FromMinutes(minutes);
                    bot.SendMessage($"@{message.Username} Timer interval set to {minutes}.");
                    config.Save();
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!enabletimer"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 2);
                    if (parts.Length != 2) {
                        bot.SendMessage($"@{message.Username} Syntax: !enabletimer <trigger>");
                        return;
                    }
                    var triggerstring = parts[1];
                    foreach (var command in customCommands.Commands) {
                        if (command.Trigger.ShouldTrigger(triggerstring)) {
                            command.IsTimer = true;
                            bot.SendMessage($"@{message.Username} Activated timer for {triggerstring}");
                            (customCommands as PersistentCommandList).Save();
                            return;
                        }
                    }
                    bot.SendMessage($"@{message.Username} No command found with trigger \"{triggerstring}\"");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!disabletimer"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 2);
                    if (parts.Length != 2) {
                        bot.SendMessage($"@{message.Username} Syntax: !disabletimer <trigger>");
                        return;
                    }
                    var triggerstring = parts[1];
                    foreach (var command in customCommands.Commands) {
                        if (command.Trigger.ShouldTrigger(triggerstring)) {
                            command.IsTimer = false;
                            bot.SendMessage($"@{message.Username} Deactivated timer for {triggerstring}");
                            (customCommands as PersistentCommandList).Save();
                            return;
                        }
                    }
                    bot.SendMessage($"@{message.Username} No command found with trigger \"{triggerstring}\"");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!remove"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 2);
                    if (parts.Length != 2) {
                        bot.SendMessage($"@{message.Username} Syntax: !remove <trigger>");
                        return;
                    }
                    var trigger = parts[1];
                    var findResult = customCommands.Commands.FirstOrDefault(
                        command => command.Trigger.ShouldTrigger(trigger));
                    if (findResult is null) {
                        bot.SendMessage($"@{message.Username} Unbekannter Trigger: \"{trigger}\"");
                        return;
                    }
                    customCommands.Remove(findResult);
                    bot.SendMessage($"@{message.Username} Kommando {trigger} entfernt!");
                },
                Cooldown = null
            });
            nativeCommands.Commands.Add(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!list"),
                Handler = (bot, message) => {
                    var liststring = nativeCommands.Commands
                            .Concat(customCommands.Commands)
                            .Where(command => command.Trigger.HasListRepresentation())
                            .Where(command => command.Authenticator.Authenticate(message))
                            .Select(command => command.Trigger.GetListRepresentation())
                            .OrderBy(representationstring => representationstring)
                            .Aggregate((previous, toAppend) => $"{previous} {toAppend}");
                    bot.SendMessage($"@{message.Username} Verfügbare Kommandos: {liststring}");
                },
                Cooldown = TimeSpan.FromSeconds(30)
            });
        }

        private void OnClientLog(object sender, OnLogArgs args) {
            if (!logging) {
                return;
            }
            Console.WriteLine($"{args.DateTime}: {args.BotUsername} - {args.Data}");
        }

        private void OnClientJoinedChannel(object sender, OnJoinedChannelArgs args) {
            logging = false;
            SendMessage("Achtung, Achtung, der persönliche Bedienstete von Lord coder2k ist eingetroffen!");
        }

        private bool HandleCommands(ChatMessage message) {
            try {
                commandListsMutex.WaitOne();
                foreach (var command in nativeCommands.Commands.Concat(customCommands.Commands)) {
                    if (command.Trigger.ShouldTrigger(message)) {
                        command.Invoke(this, message);
                        return true;
                    }
                }
            } finally {
                commandListsMutex.ReleaseMutex();
            }
            return false;
        }

        private void OnClientMessageReceived(object sender, OnMessageReceivedArgs args) {
            PrintUserMessage(args.ChatMessage.Username,
                             args.ChatMessage.Message,
                             args.ChatMessage.IsMe,
                             args.ChatMessage.IsHighlighted ? Color.Highlighted : null);
            if (HandleCommands(args.ChatMessage)) {
                Console.WriteLine("Handled command");
            }
        }

        private void OnClientConnected(object sender, OnConnectedArgs args) {
            Console.WriteLine($"Connected to {args.AutoJoinChannel}");
        }

        public void SendMessage(string message) {
            PrintUserMessage(config.Username, message,
                             message.StartsWith("/me"),
                             Color.BotHighlighted);
            if (!client.IsConnected || client.JoinedChannels.Count == 0) {
                Console.WriteLine($"Unable to send message since bot is not connected (message was {message})");
                return;
            }
            client.SendMessage(config.Channel, message);
        }

        private void PrintUserMessage(string username, string message, bool isMe = false,
                                      Color color = null) {
            color ??= Color.Default;
            color.Apply();
            if (isMe) {
                Console.Write($"{DateTime.Now.ToString("HH:mm:ss")}: {username} {message}");
            } else {
                Console.Write($"{DateTime.Now.ToString("HH:mm:ss")} {username}: {message}");
            }
            Color.Default.Apply();
            Console.WriteLine();
        }
    }
}

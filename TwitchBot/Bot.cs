using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitchBot.Commands;
using TwitchBot.Models;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchBot {
    class Bot {
        private TwitchClient client = null;
        private bool logging = true; // output logging messages? gets automatically disabled on successful connect
        public static Config Config { get; private set; } = null;
        private CommandList nativeCommands = new(); // commands that execute C# code
        private CommandList customCommands = PersistentCommandList.LoadOrCreate("commands.json"); // commands that can be set through the Twitch chat        
        private Mutex commandListsMutex = new Mutex();
        private Thread timedCommandsThread = null;
        private PointSystem pointSystemManager = new PointSystem();
        private System.Timers.Timer refreshTimer = null;
        private bool timedCommandsEnabled = true; // TODO: maybe change default value later?
        public static TwitchAPI Api { get; private set; }


        public Bot(Config config) {
            Config = config;
            Authenticate().GetAwaiter().GetResult();            
            SetupNativeCommands();            
            var credentials = new ConnectionCredentials(config.Username, config.AccessToken);
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
            client.OnConnected += OnConnected;
            client.OnUserJoined += OnUserJoined;

            client.Connect();

            timedCommandsThread = new Thread(new ThreadStart(HandleTimers));
            timedCommandsThread.Start();

            // disabled point system for now
            // pointSystemManager.Init(client, this);
        }

        private void SetupRefreshTimer(int expiresIn) {
            refreshTimer = new System.Timers.Timer(expiresIn * 0.8 * 1000.0);
            refreshTimer.Elapsed += async (sender, e) => await Authenticate();
            refreshTimer.Start();
        }

        private async Task Authenticate() {
            Api = new TwitchAPI();
            Api.Settings.ClientId = Config.ClientID;
            Api.Settings.Secret = Config.ClientSecret;
            Api.Settings.Scopes = new();
            if (Config.AccessToken is not null) {
                // the bot had access before
                Console.WriteLine("\t\t|================================|");
                Console.WriteLine("\t\t|   Trying to refresh token...   |");
                Console.WriteLine("\t\t|================================|");
                var refreshResponse = (await Api.Auth.RefreshAuthTokenAsync(Config.RefreshToken, Config.ClientSecret, Config.ClientID));
                Api.Settings.AccessToken = refreshResponse.AccessToken;
                SetupRefreshTimer(refreshResponse.ExpiresIn);
                Config.AccessToken = refreshResponse.AccessToken;
                Config.RefreshToken = refreshResponse.RefreshToken;
            } else {
                var server = new WebServer(Config.TwitchRedirectURL);
                var url = Api.Auth.GetAuthorizationCodeUrl(Config.TwitchRedirectURL, Api.Settings.Scopes);
                url = url.Replace("scope=", "scope=" + string.Join("+", new string[]{
                    "channel:moderate",
                    "chat:edit",
                    "chat:read",
                    "whispers:read",
                    "whispers:edit",
                    "channel:manage:broadcast",
                    "channel:manage:polls",
                    "channel:manage:predictions",
                    "channel:read:hype_train",
                    "channel:read:polls",
                    "channel:read:predictions",
                    "channel:read:redemptions",
                    "channel:read:subscriptions",
                    "moderation:read",
                    "moderator:manage:banned_users",
                    "user:read:broadcast",
                    "user:read:subscriptions",
                }));
                Console.WriteLine($"Please authorize here:\n{url}");
                // listen for incoming events
                var auth = await server.Listen();
                // exchange auth code for oauth access/refresh
                var response = await Api.Auth.GetAccessTokenFromCodeAsync(auth.Code,
                                                                          Config.ClientSecret,
                                                                          Config.TwitchRedirectURL);
                // update TwitchLib's api with the recently acquired access token
                Api.Settings.AccessToken = response.AccessToken;
                SetupRefreshTimer(response.ExpiresIn);
                Config.AccessToken = response.AccessToken;
                Config.RefreshToken = response.RefreshToken;                
            }
            // get the auth'd user
            var user = (await Api.Helix.Users.GetUsersAsync()).Users[0];
          
            Console.WriteLine($"Authorization success!\nUser: {user.DisplayName} (id: {user.Id})");

            var broadcaster = (await Api.Helix.Users.GetUsersAsync(logins: new() { Config.Username })).Users[0];
            Config.BroadcasterId = broadcaster.Id;
            var channelInformation = (await Api.Helix.Channels.GetChannelInformationAsync(broadcaster.Id)).Data;
            foreach (var info in channelInformation) {
                Console.WriteLine($"Stream title: {info.Title}");
            }
            Console.WriteLine($"Is broadcaster live? {await IsBroadcasterLive()}");
            Config.Save();
        }

        private async Task<bool> IsBroadcasterLive() {
            var streams = (await Api.Helix.Streams.GetStreamsAsync(userIds: new() { Config.BroadcasterId })).Streams;
            return streams.Length > 0;
        }

        private void HandleTimers() {
            Console.WriteLine("Started thread to handle timers...");
            while (true) { // approved by Andrei Alexandrescu
                var sleepDuration = (timedCommandsEnabled ? Config.TimedMessagesInterval : TimeSpan.FromSeconds(10));
                if (timedCommandsEnabled) {
                    try {
                        commandListsMutex.WaitOne();
                        var allTimerCommands = nativeCommands.Commands
                            .Concat(customCommands.Commands)
                            .Where(command => command.IsTimer);
                        if (allTimerCommands.All(command => command.LastInvoked + Config.TimedMessagesInterval > DateTime.Now)) {
                            var oldestCommand = allTimerCommands
                                .OrderBy(command => command.LastInvoked)
                                .FirstOrDefault();
                            if (oldestCommand is not null) {
                                sleepDuration = oldestCommand.LastInvoked + Config.TimedMessagesInterval - DateTime.Now;
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
                }
                Console.WriteLine($"Timed command thread sleeping for {sleepDuration}...");
                Thread.Sleep(sleepDuration);
            }
        }

        private void SetupNativeCommands() {
            AddNativeCommand(new NativeCommand {
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
            AddNativeCommand(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!interval"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    bot.SendMessage($"@{message.Username} The current timer interval is {Config.TimedMessagesInterval} minutes.");
                },
                Cooldown = null
            });
            AddNativeCommand(new NativeCommand {
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
                    Config.TimedMessagesInterval = TimeSpan.FromMinutes(minutes);
                    bot.SendMessage($"@{message.Username} Timer interval set to {minutes}.");
                    Config.Save();
                },
                Cooldown = null
            });
            AddNativeCommand(new NativeCommand {
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
            AddNativeCommand(new NativeCommand {
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
            AddNativeCommand(new NativeCommand {
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
            AddNativeCommand(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!edit"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 3);
                    if (parts.Length != 3) {
                        bot.SendMessage($"@{message.Username} Syntax: !edit <trigger> <new_message>");
                        return;
                    }
                    var trigger = parts[1];
                    var newMessage = parts[2];
                    var findResult = customCommands.Commands.FirstOrDefault(
                        command => command.Trigger.ShouldTrigger(trigger));
                    if (findResult is null) {
                        bot.SendMessage($"@{message.Username} Unbekannter Trigger: \"{trigger}\"");
                        return;
                    }
                    var newCommand = new EchoCommand {
                        Trigger = findResult.Trigger,
                        Response = newMessage,
                        Cooldown = findResult.Cooldown,
                    };
                    customCommands.Remove(findResult);
                    customCommands.Add(newCommand);
                    bot.SendMessage($"@{message.Username} Kommando \"{trigger}\" erfolgreich geändert.");
                }
            });
            AddNativeCommand(new NativeCommand {
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
            AddNativeCommand(new NativeCommand {
                Trigger = Triggers.StartsWithWord("!setUserLevel"),
                Authenticator = Authenticators.ModOrBroadcaster,
                Handler = (bot, message) => {
                    var parts = message.Message.Split(" ", 3);
                    if (parts.Length != 3) {
                        bot.SendMessage($"@{message.Username} Syntax: !setUserLevel <trigger> <pleb|modOrBroadcaster|broadcaster>");
                        return;
                    }
                    var trigger = parts[1];
                    var userLevel = parts[2];
                    var findResult = customCommands.Commands.FirstOrDefault(
                        command => command.Trigger.ShouldTrigger(trigger));
                    if (findResult is null) {
                        bot.SendMessage($"@{message.Username} Unbekannter Trigger: \"{trigger}\"");
                        return;
                    }
                    if (userLevel != "pleb" && userLevel != "modOrBroadcaster" && userLevel != "broadcaster") {
                        
                    }
                    IAuthenticator authenticator = null;
                    switch (userLevel) {
                        case "pleb":
                            authenticator = Authenticators.Pleb;
                            break;
                        case "modOrBroadcaster":
                            authenticator = Authenticators.ModOrBroadcaster;
                            break;
                        case "broadcaster":
                            authenticator = Authenticators.Broadcaster;
                            break;
                        default:
                            bot.SendMessage($"@{message.Username} \"{userLevel}\" is no valid user level. Must be pleb, modOrBroadcaster or broadcaster.");
                            return;
                    }                    
                    var newCommand = new EchoCommand() {
                        Cooldown = findResult.Cooldown,
                        IsTimer = findResult.IsTimer,
                        Authenticator = authenticator,
                        Trigger = findResult.Trigger,
                        ModOverridesCooldown = findResult.ModOverridesCooldown,
                        LastInvoked = findResult.LastInvoked,
                        Response = ((EchoCommand)findResult).Response,
                    };
                    customCommands.Remove(findResult);
                    customCommands.Add(newCommand);
                    bot.SendMessage($"@{message.Username} Changed user level of command \"{trigger}\" to \"{userLevel}\"");
                },
                Cooldown = null
            });
            AddNativeCommand(new NativeCommand() {
                Trigger = Triggers.StartsWithWord("!enable"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    timedCommandsEnabled = true;
                    bot.SendMessage($"@{message.Username} Timed commands are now enabled.");
                },
                Cooldown = null,
            });
            AddNativeCommand(new NativeCommand() {
                Trigger = Triggers.StartsWithWord("!disable"),
                Authenticator = Authenticators.Broadcaster,
                Handler = (bot, message) => {
                    timedCommandsEnabled = true;
                    bot.SendMessage($"@{message.Username} Timed commands are now disabled.");
                },
                Cooldown = null,
            });
        }

        public void AddNativeCommand(Command command) {
            nativeCommands.Commands.Add(command);
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

            pointSystemManager.HandleFirstMessage(args.ChatMessage);
        }

        private void OnConnected(object sender, OnConnectedArgs args) {
            Console.WriteLine($"Connected to {args.AutoJoinChannel}");
        }

        private void OnUserJoined(object sender, OnUserJoinedArgs args) {
            // TODO: find user id by username and add to persistent user info storage
        }

        public void SendMessage(string message) {
            PrintUserMessage(Config.Username, message,
                             message.StartsWith("/me"),
                             Color.BotHighlighted);
            if (!client.IsConnected || client.JoinedChannels.Count == 0) {
                Console.WriteLine($"Unable to send message since bot is not connected (message was {message})");
                return;
            }
            client.SendMessage(Config.Channel, message);
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

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace LunarCalendar
{


    class Program
    {
        // Program entry point
        static void Main(string[] args)
        {
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private readonly DiscordSocketClient client;

        private readonly CommandService commands;
        private IServiceProvider services;

        private Program()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 50
            });

            commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Info,

                CaseSensitiveCommands = false,
            });

            client.Log += Log;
            client.Connected += Client_Connected;
            commands.Log += Log;


        }

        private Task Client_Connected()
        {
            //do this here currently cause client needs to be connected before calendarmodule inits
            //TODO: just init calendarmodule later somehow
            services = BuildServiceProvider();
            return Task.CompletedTask;
        }

        public IServiceProvider BuildServiceProvider() => new ServiceCollection()
            .AddSingleton(client).AddSingleton(commands).AddSingleton<CommandHandler>().BuildServiceProvider();

        private static Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            Console.WriteLine($"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        private async Task MainAsync()
        {
            await InitCommands();

            await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken"));
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private async Task InitCommands()
        {
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
            client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var msg = arg as SocketUserMessage;
            if (msg == null) return;

            if (msg.Author.Id == client.CurrentUser.Id || msg.Author.IsBot) return;

            int pos = 0;
            if (msg.HasCharPrefix('!', ref pos) || msg.HasMentionPrefix(client.CurrentUser, ref pos))
            {
                var context = new SocketCommandContext(client, msg);
                var result = await commands.ExecuteAsync(context, pos, services);
            }
        }
    }
}
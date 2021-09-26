using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace LunarCalendar
{
    public class CalendarModule : ModuleBase<SocketCommandContext>
    {
        private DiscordSocketClient client { get; set; }
        private List<ulong> channelIDs = new List<ulong>();
        private List<ISocketMessageChannel> channels = new List<ISocketMessageChannel>();
        private List<CalendarEvent> events = new List<CalendarEvent>();

        public CalendarModule(DiscordSocketClient Client)
        {
            client = Client;
            init().GetAwaiter().GetResult();
        }

        private async Task init()
        {
            string channelsJson = "";
            if (File.Exists("channels.json"))
                channelsJson = await File.ReadAllTextAsync("channels.json");
            else
                File.Create("channels.json");

            if(channelsJson != "")
            {
                var channelList = JsonSerializer.Deserialize<List<ulong>>(channelsJson);
                foreach (var ch in channelList)
                {
                    _ = setBaseMessage(ch);
                }
            }
        }

        private async Task setBaseMessage(ulong ch)
        {
            var channel = client.GetChannel(ch) as ISocketMessageChannel;
            channels.Add(channel);
            List<IMessage> pinned = (List<IMessage>)await channel.GetPinnedMessagesAsync();

            var myMessages = pinned.Where(m => m.Author.Id == client.CurrentUser.Id);
            if (myMessages.Any())
            {
                await channel.ModifyMessageAsync(myMessages.First().Id, msg => msg.Content = "butts2");
            }
            else
            {
                await channel.SendMessageAsync("butts");
            }

            
            //draw calendar n shit
        }

        // ~say hello world -> hello world
        [Command("say")]
		[Summary("Echoes a message.")]
		public Task SayAsync([Remainder][Summary("The text to echo")] string echo)
			=> ReplyAsync(echo);

        // ~say hello world -> hello world
        [Command("register")]
        [Summary("Sets channel as calendar channel.")]
        public async Task RegisterChannel([Remainder] int _ = 0)
        {
            channelIDs.Add(Context.Channel.Id);
            await writeToFiles();
            await Context.Message.DeleteAsync();
            return;
        }

        private async Task writeToFiles()
        {
            string json = JsonSerializer.Serialize(channelIDs);
                await File.WriteAllTextAsync("channels.json", json);


            json = JsonSerializer.Serialize(events);
            await File.WriteAllTextAsync("events.json", json);
        }

    }
}
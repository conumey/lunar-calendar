using Discord.Commands;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Linq;
using System;
using System.Text;

namespace LunarCalendar
{
    [Group("event")]
    public partial class CalendarModule : ModuleBase<SocketCommandContext>
    {
        private DiscordSocketClient client { get; set; }
        private List<ulong> channelIDs = new();
        private List<ISocketMessageChannel> channels = new();
        private Dictionary<ulong, List<CalendarEvent>> events = new();

        public CalendarModule(DiscordSocketClient Client)
        {
            client = Client;
            init().GetAwaiter().GetResult();
        }


        [Command("create")]
        [Summary("Creates event.")]
        public async Task CreateEvent(string name, DateTime date, string description)
        {
            ulong channelID = Context.Channel.Id;
            if (!events.ContainsKey(channelID))
                events.Add(channelID, new());

            CalendarEvent e = new CalendarEvent()
            {
                Name = name,
                Date = date,
                Description = description
            };

            events[channelID].Add(e);
            await writeToFiles();
            await setBaseMessage(Context.Channel.Id);
            await Context.Message.DeleteAsync();
        }

        [Command("register")]
        [Summary("Sets channel as calendar channel.")]
        public async Task RegisterChannel([Remainder] int _ = 0)
        {
            channelIDs.Add(Context.Channel.Id);
            await writeToFiles();
            await setBaseMessage(Context.Channel.Id);
            await Context.Message.DeleteAsync();
        }

        private async Task init()
        {
            while (client.ConnectionState != ConnectionState.Connected)
                await Task.Delay(10); //TODO: this better


            await Task.Delay(2000);

            using(var fs = new FileStream("events.json", FileMode.OpenOrCreate))
            {
                events = await JsonSerializer.DeserializeAsync<Dictionary<ulong, List<CalendarEvent>>>(fs);
            }

            using (var fs = new FileStream("channels.json", FileMode.OpenOrCreate))
            {
                var channelList = await JsonSerializer.DeserializeAsync<List<ulong>>(fs);
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

            var pinned = await channel.GetPinnedMessagesAsync();
            var myMessages = pinned.Where(m => m.Author.Id == client.CurrentUser.Id);

            string content = generateCalendar(ch);

            if (myMessages.Any())
                await channel.ModifyMessageAsync(myMessages.First().Id, msg => msg.Content = content);
            else
                await (await channel.SendMessageAsync(content)).PinAsync();
        }

        private string generateCalendar(ulong ch)
        {
            DateTime startDate = DateTime.Today;
            DateTime endDate = startDate.AddDays(14);
            StringBuilder sb = new();

            string dividingLine = "|--------------------------------------------------------------------|";

            sb.AppendLine("```/--------------------------------------------------------------------\\");

            DateTime date = startDate;
            while (true)
            {
                string line = "";
                line += $"| {date:ddd dd MMM} | ";

                if (events.ContainsKey(ch))
                    foreach (CalendarEvent ev in events[ch].Where(e => e.Date == date))
                    {
                        line += $"[- {ev.Name} -]";
                    }

                sb.AppendLine(line);
                date = date.AddDays(1);

                if (date.AddDays(1) == endDate)
                    break;

                sb.AppendLine(dividingLine);
            }

            sb.AppendLine("\\--------------------------------------------------------------------/```");

            return sb.ToString();
        }

        private async Task writeToFiles()
        {
            try
            {
                string json = JsonSerializer.Serialize(channelIDs);
                await File.WriteAllTextAsync("channels.json", json);

                json = JsonSerializer.Serialize(events);
                await File.WriteAllTextAsync("events.json", json);
            }
            catch(Exception e)
            {
                var test = e;
            }

        }

    }
}
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelChan.Bot.Discord
{
    public class ChainPathCommand : ModuleBase<SocketCommandContext>
    {
        DiscordChatBot discordChatBot;
        public ChainPathCommand(DiscordChatBot discordChatBot)
        {
            this.discordChatBot = discordChatBot;
        }
        [Command("chainpath")]
        [Summary("Show the path, in terms of Tripwire signature IDs & system names, to the given character name in the chain")]
        public Task ChainPathAsync([Remainder][Summary("The character name")] string characterName)
        {

            return Task.CompletedTask;
        }
    }
}

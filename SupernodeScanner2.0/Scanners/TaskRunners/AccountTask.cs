using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharp2nem;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Utils;

namespace SupernodeScanner2._0.Scanners.TaskRunners
{
    internal class AccountTask
    {
        private static TelegramBot Bot { get; set; }
        internal void RegisterAccounts(Message message)
        {
            Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

            if (UserUtils.GetUser(message.Chat.Id)?.UserName == null)
            {
                // add user based on their chat ID
                UserUtils.AddUser(message.Chat.Username, message.Chat.Id);

                // declare message
                var msg1 = "You have been automatically registered, one moment please";

                // send message notifying they have been registered
                var reqAction1 = new SendMessage(message.Chat.Id, msg1);

                // send message
                Bot.MakeRequestAsync(reqAction1);
            }

            // set up regex sequence to verify address validity
            var address = new Regex(
                @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            // extract any sequence matching addresses
            var result = address.Matches(message.Text).Cast<Match>().Select(m => m.Value).ToList();

            // register any valid addresses for monitoring
            AccountUtils.AddAccount(message.Chat.Id, result);

            // notify user the account(s) was registered
            var reqAction = new SendMessage(message.Chat.Id, result.Aggregate("Addresses registered: ", (current, n) => current + n + "\n"));
            Bot.MakeRequestAsync(reqAction);
        }

        internal void UnregisterAccount(Message message, string text)
        {
            // set up regex sequence matcher
            var address = new Regex(
                @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            // extract any valid addresses
            var result = address.Matches(text.GetResultsWithoutHyphen()).Cast<Match>().Select(m => m.Value).ToList();

            var userNodes = NodeUtils.GetNodeByUser(message.Chat.Id);

            foreach (var acc in result)
            {
                SummaryUtils.DeleteHbSummaryForUser(acc, message.Chat.Id);
                SummaryUtils.DeleteTxSummaryForUser(acc, message.Chat.Id);
            }

            // delete any valid addresses
            AccountUtils.DeleteAccount(message.Chat.Id,
                result.Where(x => userNodes.All(y => y.IP != x))
                .ToList());

            // notify user
            var reqAction = new SendMessage(message.Chat.Id, result.Aggregate("Addresses unregistered: ", (current, n) => current + n + "\n"));
            Bot.MakeRequestAsync(reqAction);
        }
    }
}

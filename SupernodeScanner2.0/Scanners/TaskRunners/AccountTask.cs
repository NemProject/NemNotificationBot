using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharp2nem;
using CSharp2nem.Utils;
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
        private TelegramBot Bot { get; set; }
        internal async void RegisterAccounts(Message message)
        {
            Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

            if (UserUtils.GetUser(chatId: message.Chat.Id)?.UserName == null)
            {
                // add user based on their chat ID
                UserUtils.AddUser(userName: message.Chat.Username, chatId: message.Chat.Id);

                // declare message
                var msg1 = "You have been automatically registered, one moment please";

                // send message notifying they have been registered
                var reqAction1 = new SendMessage(chatId: message.Chat.Id, text: msg1);

                // send message
                Bot.MakeRequestAsync(request: reqAction1);
            }

            // set up regex sequence to verify address validity
            var address = new Regex(
                pattern: @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            // extract any sequence matching addresses
            var result = address.Matches(input: StringUtils.GetResultsWithoutHyphen(message.Text)).Cast<Match>().Select(selector: m => m.Value).ToList();

            // register any valid addresses for monitoring
            AccountUtils.AddAccount(chatId: message.Chat.Id, accounts: result);

            // notify user the account(s) was registered
            var reqAction = new SendMessage(chatId: message.Chat.Id, text: result.Aggregate(seed: "Addresses registered: \n", func: (current, n) => current + StringUtils.GetResultsWithHyphen(n) + "\n"));
            await Bot.MakeRequestAsync(request: reqAction);
        }

        internal void UnregisterAccount(Message message, string text)
        {
            Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

            // set up regex sequence matcher
            var address = new Regex(
                pattern: @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            // extract any valid addresses
            var result = address.Matches(input: StringUtils.GetResultsWithoutHyphen(text)).Cast<Match>().Select(selector: m => m.Value).ToList();

            var userNodes = NodeUtils.GetNodeByUser(chatId: message.Chat.Id);

            foreach (var acc in result)
            {
                SummaryUtils.DeleteHbSummaryForUser(account: acc, chatId: message.Chat.Id);
                SummaryUtils.DeleteTxSummaryForUser(account: acc, chatId: message.Chat.Id);
            }

            // delete any valid addresses
            AccountUtils.DeleteAccount(chatId: message.Chat.Id,
                accounts: result.Where(predicate: x => userNodes.All(predicate: y => y.IP != x))
                .ToList());

            // notify user
            var reqAction = new SendMessage(chatId: message.Chat.Id, text: result.Aggregate(seed: "Addresses unregistered: \n", func: (current, n) => current + StringUtils.GetResultsWithHyphen(n) + "\n"));
            Bot.MakeRequestAsync(request: reqAction);
        }
    }
}

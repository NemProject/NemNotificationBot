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

namespace SupernodeScanner2._0.Scanners.TaskRunners
{
    internal static class OptIOAccountUtils
    {
        internal static TelegramBot bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);
        internal static void OptInTx(Message message)
        {
            var address = new Regex(
                pattern: @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            var result = address.Matches(input: message.Text.GetResultsWithoutHyphen()).Cast<Match>().Select(selector: m => m.Value).ToList();

            var userAccounts = AccountUtils.GetAccountByUser(chatId: message.Chat.Id)
                .Where(predicate: e => result.Any(predicate: i => i == e.EncodedAddress)).ToList();
            
            foreach (var acc in userAccounts)
            {
                acc.CheckTxs = true;

                AccountUtils.UpdateAccount(usrAcc: acc);
            }

            var reqAction = new SendMessage(chatId: message.Chat.Id,
                text: userAccounts.Aggregate(seed: "Opted into tx notification for accounts: \n \n", func: (current, n) => current + n.EncodedAddress.GetResultsWithHyphen() + "\n"));

            bot.MakeRequestAsync(request: reqAction);
        }

        internal static void OptOutTx(Message message)
        {
            var address = new Regex(
                pattern: @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            var result = address.Matches(input: message.Text.GetResultsWithoutHyphen()).Cast<Match>().Select(selector: m => m.Value).ToList();

            var userAccounts = AccountUtils.GetAccountByUser(chatId: message.Chat.Id)
                .Where(predicate: e => result.Any(predicate: i => i == e.EncodedAddress)).ToList();

            foreach (var acc in userAccounts)
            {
                acc.CheckTxs = false;

                AccountUtils.UpdateAccount(usrAcc: acc);
            }

            var reqAction = new SendMessage(chatId: message.Chat.Id,
                text: userAccounts.Aggregate(seed: "Opted out of tx notification for accounts: \n \n", func: (current, n) => current + n.EncodedAddress.GetResultsWithHyphen() + "\n"));

            bot.MakeRequestAsync(request: reqAction);
        }

        internal static void OptOutHarvesting(Message message)
        {
            var address = new Regex(
                pattern: @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            var result = address.Matches(input: message.Text.GetResultsWithoutHyphen()).Cast<Match>().Select(selector: m => m.Value).ToList();

            var userAccounts = AccountUtils.GetAccountByUser(chatId: message.Chat.Id)
                .Where(predicate: e => result.Any(predicate: i => i == e.EncodedAddress)).ToList();

            foreach (var acc in userAccounts)
            {
                acc.CheckBlocks = false;

                AccountUtils.UpdateAccount(usrAcc: acc);
            }

            var reqAction = new SendMessage(chatId: message.Chat.Id,
                text: userAccounts.Aggregate(seed: "Opted out of harvesting notification for accounts: \n \n", func: (current, n) => current + n.EncodedAddress.GetResultsWithHyphen() + "\n"));

            bot.MakeRequestAsync(request: reqAction);
        }
        
        internal static void OptInHarvesting(Message message)
        {
            var address = new Regex(
                pattern: @"[Nn]{1,1}[a-zA-Z0-9]{39,39}");

            var result = address.Matches(input: message.Text.GetResultsWithoutHyphen()).Cast<Match>().Select(selector: m => m.Value).ToList();

            var userAccounts = AccountUtils.GetAccountByUser(chatId: message.Chat.Id)
                .Where(predicate: e => result.Any(predicate: i => i == e.EncodedAddress)).ToList();

            foreach (var acc in userAccounts)
            {
                acc.CheckBlocks = true;

                AccountUtils.UpdateAccount(usrAcc: acc);
            }

            var reqAction = new SendMessage(chatId: message.Chat.Id,
                text: userAccounts.Aggregate(seed: "Opted in to harvesting notification for accounts: \n \n", func: (current, n) => current + n.EncodedAddress.GetResultsWithHyphen() + "\n"));

            bot.MakeRequestAsync(request: reqAction);
        }
    }
}

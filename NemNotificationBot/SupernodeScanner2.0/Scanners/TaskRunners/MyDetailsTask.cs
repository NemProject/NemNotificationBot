using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CSharp2nem;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Utils;

namespace SupernodeScanner2._0.Scanners.TaskRunners
{
    internal class MyDetailsTask
    {
        internal async void ReturnMyDetails(Message message)
        {
            var Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

            var u = UserUtils.GetUser(chatId: message.From.Id);

            if (u.ChatId != message.Chat.Id)
            {
               
                var req = new SendMessage(chatId: message.Chat.Id, text: "You are not registered");
                await Bot.MakeRequestAsync(request: req);
                return;
            }

            var nodes = NodeUtils.GetNodeByUser(chatId: message.From.Id);

            var accounts = AccountUtils.GetAccountByUser(chatId: message.From.Id);

            List<string> accountString;
            try
            {
                
                var factory = new AccountFactory();
               
                   var ips = nodes.Select(selector: n => ("Alias: " + n.Alias +
                    "\nIP: " + n.IP +
                    "\nDeposit address: \n" + (accounts.All(predicate: e => e.EncodedAddress != n.DepositAddress) ? "[ACCOUNT UNREGISTERED] " : "") + n.DepositAddress.GetResultsWithHyphen() +
                    "\nBalance: " + factory.FromEncodedAddress(encodedAddress: n.DepositAddress).GetAccountInfoAsync().Result.Account.Balance / 1000000 +
                    "\nTransactions check: " + AccountUtils.GetAccount(add: n.DepositAddress, user: message.Chat.Id).CheckTxs  +
                    "\nHarvesting check: " + AccountUtils.GetAccount(add: n.DepositAddress, user: message.Chat.Id).CheckBlocks +
                    "\nhttps://supernodes.nem.io/details/" + n.SNodeID +
                    "\nhttp://explorer.ournem.com/#/s_account?account=" + n.DepositAddress + "\n\n")).ToList();

                var req = new SendMessage(chatId: message.Chat.Id, text: "**Your registered nodes with associated accounts**");

                await Bot.MakeRequestAsync(request: req);

                foreach (var s in ips)
                {
                    req = new SendMessage(chatId: message.Chat.Id, text: s);

                    await Bot.MakeRequestAsync(request: req);
                }
               

                var a = accounts.Where(predicate: y => nodes.All(predicate: x => y.EncodedAddress != x.DepositAddress))
                                .Select(selector: node => node.EncodedAddress).ToList();

                req = new SendMessage(chatId: message.Chat.Id, text: "**Your registered accounts**");

                if(a.Count > 0) await Bot.MakeRequestAsync(request: req);

                accountString = a.Select(selector: n  => 
                "\nDeposit address: \n" + n.GetResultsWithHyphen() + 
                "\nBalance: " + factory.FromEncodedAddress(encodedAddress: n).GetAccountInfoAsync().Result.Account.Balance / 1000000 +
                "\nTransactions check: " +  AccountUtils.GetAccount(add: n, user: message.Chat.Id).CheckTxs +
                "\nHarvesting check: " +  AccountUtils.GetAccount(add: n, user: message.Chat.Id).CheckBlocks +
                "\nhttp://explorer.ournem.com/#/s_account?account=" + n + "\n\n").ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
                accountString = new List<string> {"Sorry something went wrong, please try again. Possibly your node could be offline."};
            }

            foreach (var s in accountString)
            {
                var reqAction = new SendMessage(chatId: message.Chat.Id, text: s);

                Bot.MakeRequestAsync(request: reqAction);
            }
           
        }
    }
}

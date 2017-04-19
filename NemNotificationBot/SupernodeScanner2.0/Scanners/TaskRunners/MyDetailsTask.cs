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
            var Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

            var u = UserUtils.GetUser(message.From.Id);

            if (u == null || u.UserName != message.From.Username)
            {
               
                var req = new SendMessage(message.Chat.Id, "You are not registered");
                await Bot.MakeRequestAsync(req);
                return;
            }

            var nodes = NodeUtils.GetNodeByUser(message.From.Id);

            var accounts = AccountUtils.GetAccountByUser(message.From.Id);

            List<string> accountString;
            try
            {
                
                var factory = new AccountFactory();
               
                   var ips = nodes.Select(n => ("Alias: " + n.Alias +
                    "\nIP: " + n.IP +
                    "\nDeposit address: \n" + (accounts.All(e => e.EncodedAddress != n.DepositAddress) ? "[ACCOUNT UNREGISTERED] " : "") + n.DepositAddress.GetResultsWithHyphen() +
                    "\nBalance: " + factory.FromEncodedAddress(n.DepositAddress).GetAccountInfoAsync().Result.Account.Balance / 1000000 +
                    "\nTransactions check: " + AccountUtils.GetAccount(n.DepositAddress, message.Chat.Id).CheckTxs  +
                    "\nHarvesting check: " + AccountUtils.GetAccount(n.DepositAddress, message.Chat.Id).CheckBlocks +
                    "\nhttps://supernodes.nem.io/details/" + n.SNodeID +
                    "\nhttp://explorer.ournem.com/#/s_account?account=" + n.DepositAddress + "\n\n")).ToList();

                var req = new SendMessage(message.Chat.Id, "**Your registered nodes with associated accounts**");

                await Bot.MakeRequestAsync(req);

                foreach (var s in ips)
                {
                    req = new SendMessage(message.Chat.Id, s);

                    await Bot.MakeRequestAsync(req);
                }
               

                var a = accounts.Where(y => nodes.All(x => y.EncodedAddress != x.DepositAddress))
                                .Select(node => node.EncodedAddress).ToList();

                req = new SendMessage(message.Chat.Id, "**Your registered accounts**");

                if(a.Count > 0) await Bot.MakeRequestAsync(req);

                accountString = a.Select(n  => 
                "\nDeposit address: \n" + n.GetResultsWithHyphen() + 
                "\nBalance: " + factory.FromEncodedAddress(n).GetAccountInfoAsync().Result.Account.Balance / 1000000 +
                "\nTransactions check: " +  AccountUtils.GetAccount(n, message.Chat.Id).CheckTxs +
                "\nHarvesting check: " +  AccountUtils.GetAccount(n, message.Chat.Id).CheckBlocks +
                "\nhttp://explorer.ournem.com/#/s_account?account=" + n + "\n\n").ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                accountString = new List<string> {"Sorry something went wrong, please try again. Possibly your node could be offline."};
            }

            foreach (var s in accountString)
            {
                var reqAction = new SendMessage(message.Chat.Id, s);

                Bot.MakeRequestAsync(reqAction);
            }
           
        }
    }
}

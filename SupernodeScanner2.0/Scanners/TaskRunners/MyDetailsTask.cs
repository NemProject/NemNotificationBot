using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CSharp2nem;
using CSharp2nem.Connectivity;
using CSharp2nem.Model.AccountSetup;
using CSharp2nem.RequestClients;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Utils;
using CSharp2nem.Utils;

namespace SupernodeScanner2._0.Scanners.TaskRunners
{
    internal class MyDetailsTask
    {
        private Connection Con = new Connection();
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
            List<string> ips = new List<string>();
            try
            {
              
                var client = new AccountClient(Con);

                if (nodes.Count > 0)
                {
                    ips = nodes.Select(selector: n => ("Alias: " + n.Alias +
                   "\nIP: " + n.IP +   
                   "\nDeposit address: \n" + (accounts.All(predicate: e => e.EncodedAddress != n.DepositAddress) ? "[ACCOUNT UNREGISTERED] " : "") + StringUtils.GetResultsWithHyphen(n.DepositAddress) +
                   "\nBalance: " + client.EndGetAccountInfo(client.BeginGetAccountInfoFromAddress(n.DepositAddress)).Account.Balance / 1000000 +
                   "\nTransactions check: " + AccountUtils.GetAccount(add: n.DepositAddress, user: message.Chat.Id).CheckTxs +
                   "\nHarvesting check: " + AccountUtils.GetAccount(add: n.DepositAddress, user: message.Chat.Id).CheckBlocks +
                   "\nhttps://supernodes.nem.io/details/" + n.SNodeID +
                   "\nhttp://explorer.ournem.com/#/s_account?account=" + n.DepositAddress + "\n\n")).ToList();

                    
                }

                var req = new SendMessage(chatId: message.Chat.Id, text: "**Your registered nodes with associated accounts**");

                await Bot.MakeRequestAsync(request: req);

                foreach (var s in ips)
                {
                    req = new SendMessage(chatId: message.Chat.Id, text: s);

                    await Bot.MakeRequestAsync(request: req);
                }
               

                var a = accounts.Select(selector: acc => acc.EncodedAddress).ToList();

                req = new SendMessage(chatId: message.Chat.Id, text: "**Your registered accounts**");

                if(a.Count > 0) await Bot.MakeRequestAsync(request: req);

                accountString = a.Select(selector: n  => 
                "\nAccount address: \n" + StringUtils.GetResultsWithHyphen(n) + 
                "\nBalance: " + client.EndGetAccountInfo(client.BeginGetAccountInfoFromAddress(n)).Account.Balance / 1000000 +
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

                await Bot.MakeRequestAsync(request: reqAction);
            }
           
        }
    }
}

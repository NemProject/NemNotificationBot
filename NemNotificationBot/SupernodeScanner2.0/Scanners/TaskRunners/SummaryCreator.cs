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
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.Utils;
using NetTelegramBotApi.Types;

namespace SupernodeScanner2._0.Scanners.TaskRunners
{
    internal class SummaryCreator
    {
        internal void GetSummary( string text, Chat chat)
        {
            int days;

            switch (text)
            {
                case "/dailySummary":
                    days = 1;
                    break;
                case "/sevenDaySummary":
                    days = 7;
                    break;
                case "/thirtyOneDaySummary":
                    days = 31;
                    break;
                default:
                    try
                    {
                        days = int.Parse(s: Regex.Replace(input: text.Substring(startIndex: text.LastIndexOf(value: ':') + 1), pattern: @"[\s]+", replacement: string.Empty));

                        break;
                    }
                    catch (Exception)
                    {
                        var reqAction = new SendMessage(chatId: chat.Id, text: "Please insert the number of days for which you want a summary. eg. \"/customSummary: 4\"");

                        var Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

                        Bot.MakeRequestAsync(request: reqAction);

                        return;
                    }             
            }

            var acc = AccountUtils.GetAccountByUser(chatId: chat.Id);

            var nodes = NodeUtils.GetAllNodes();

            foreach (var address in acc)
            {
                SuperNode nodeAlias = null;

                try
                {
                    nodeAlias = nodes.Single(predicate: x => x.DepositAddress == address.EncodedAddress);            
                }
                catch(Exception)
                {
                    
                }


                var txsummaries = SummaryUtils.GetTxSummary(account: address.EncodedAddress, days: days, chatId: chat.Id);

                var txIn = txsummaries.Count(predicate: e => address.EncodedAddress == e.Recipient);

                var txOut = txsummaries.Count(predicate: e => address.EncodedAddress != e.Recipient);

                var txValueIn = txsummaries.Where(predicate: x => txsummaries.Any(predicate: y => y.AmountIn > 0)).ToList()
                                    .Select(selector: e => e.AmountIn)
                                    .Sum() / 1000000;

                var txValueOut = txsummaries.Where(predicate: x => txsummaries.Any(predicate: y => y.AmountIn > 0)).ToList()
                                     .Select(selector: e => e.AmountOut)
                                     .Sum() / 1000000;

                var snPayout = txsummaries.Where(predicate: x => txsummaries.Any(predicate: y => y.AmountIn > 0)).ToList()
                                   .Select(selector: e => e.AmountIn)
                                   .Where(
                                       predicate: x =>
                                           txsummaries.Any(
                                               predicate: y =>
                                                   y.Sender ==
                                                   AddressEncoding.ToEncoded(network: 0x68,
                                                       publicKey: new PublicKey(
                                                           key: "d96366cdd47325e816ff86039a6477ef42772a455023ccddae4a0bd5d27b8d23"))))
                                   .Sum() / 1000000;

                var accountBalanceDifference = txValueIn - txValueOut;
                var totalTxs = txIn + txOut;

                var hbsummaries = SummaryUtils.GetHBSummary(account: address.EncodedAddress, days: days, chatId: chat.Id);

                var totalFees = hbsummaries.Where(predicate: x => hbsummaries.Any(predicate: y => y.FeesEarned > 0)).ToList()
                                    .Select(selector: e => e.FeesEarned)
                                    .Sum() / 1000000;

                var totalBlocks = hbsummaries.Count(predicate: e => address.EncodedAddress == e.MonitoredAccount);

                var reqAction = new SendMessage(chatId: chat.Id,
                    text: "Summary for account: \n" + address.EncodedAddress.GetResultsWithHyphen() + "\n" +
                    (nodeAlias != null ? ("Deposit address for node: " + nodeAlias.Alias + "\n") : "") + 
                    "Transactions in: " + txIn + "\n" +
                    "Transactions out: " + txOut + "\n" +
                    "Total transactions: " + totalTxs + "\n" +
                    "Transactions value in: " + txValueIn + " XEM\n" +
                    "Transactions value out: " + txValueOut + " XEM\n" +
                    "Transactions value total: " + (txValueIn - txValueOut) + " XEM\n" +
                    "Total supernode payout: " + snPayout + " XEM\n" +
                    "Harvested blocks: " + totalBlocks + "\n" +
                    "Total harvested fees: " + totalFees + " XEM\n" +
                    "Change in balance: " + (accountBalanceDifference + totalFees) + " XEM\n" +
                    "Final balance: " + new AccountFactory()
                        .FromEncodedAddress(encodedAddress: address.EncodedAddress)
                        .GetAccountInfoAsync()
                        .Result.Account.Balance / 1000000 + "\n" +
                    "http://explorer.ournem.com/#/s_account?account=" + address.EncodedAddress
                );
                var Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);
                Bot.MakeRequestAsync(request: reqAction);
            }

        }
    }
}

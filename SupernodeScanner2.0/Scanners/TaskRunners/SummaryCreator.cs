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
                        days = int.Parse(Regex.Replace(text.Substring(text.LastIndexOf(':') + 1), @"[\s]+", string.Empty));

                        break;
                    }
                    catch (Exception)
                    {
                        var reqAction = new SendMessage(chat.Id, "Please insert the number of days for which you want a summary. eg. \"/customSummary: 4\"");

                        var Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

                        Bot.MakeRequestAsync(reqAction);

                        return;
                    }             
            }

            var acc = AccountUtils.GetAccountByUser(chat.Id);

            var nodes = NodeUtils.GetAllNodes();

            foreach (var address in acc)
            {
                SuperNode nodeAlias = null;

                try
                {
                    nodeAlias = nodes.Single(x => x.DepositAddress == address.EncodedAddress);            
                }
                catch(Exception)
                {
                    
                }


                var txsummaries = SummaryUtils.GetTxSummary(address.EncodedAddress, days, chat.Id);

                var txIn = txsummaries.Count(e => address.EncodedAddress == e.Recipient);

                var txOut = txsummaries.Count(e => address.EncodedAddress != e.Recipient);

                var txValueIn = txsummaries.Where(x => txsummaries.Any(y => y.AmountIn > 0)).ToList()
                                    .Select(e => e.AmountIn)
                                    .Sum() / 1000000;

                var txValueOut = txsummaries.Where(x => txsummaries.Any(y => y.AmountIn > 0)).ToList()
                                     .Select(e => e.AmountOut)
                                     .Sum() / 1000000;

                var snPayout = txsummaries.Where(x => txsummaries.Any(y => y.AmountIn > 0)).ToList()
                                   .Select(e => e.AmountIn)
                                   .Where(
                                       x =>
                                           txsummaries.Any(
                                               y =>
                                                   y.Sender ==
                                                   AddressEncoding.ToEncoded(0x68,
                                                       new PublicKey(
                                                           "d96366cdd47325e816ff86039a6477ef42772a455023ccddae4a0bd5d27b8d23"))))
                                   .Sum() / 1000000;

                var accountBalanceDifference = txValueIn - txValueOut;
                var totalTxs = txIn + txOut;

                var hbsummaries = SummaryUtils.GetHBSummary(address.EncodedAddress, days, chat.Id);

                var totalFees = hbsummaries.Where(x => hbsummaries.Any(y => y.FeesEarned > 0)).ToList()
                                    .Select(e => e.FeesEarned)
                                    .Sum() / 1000000;

                var totalBlocks = hbsummaries.Count(e => address.EncodedAddress == e.MonitoredAccount);

                var reqAction = new SendMessage(chat.Id,
                    "Summary for account: \n" + address.EncodedAddress.GetResultsWithHyphen() + "\n" +
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
                        .FromEncodedAddress(address.EncodedAddress)
                        .GetAccountInfoAsync()
                        .Result.Account.Balance / 1000000 + "\n" +
                    "http://explorer.ournem.com/#/s_account?account=" + address.EncodedAddress
                );
                var Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);
                Bot.MakeRequestAsync(reqAction);
            }

        }
    }
}

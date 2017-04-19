using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharp2nem;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Scanners.TaskRunners;
using SupernodeScanner2._0.Utils;
using Account = SupernodeScanner2._0.DataContextModel.Account;

namespace SupernodeScanner2._0.Scanners
{
    internal class HarvestedBlockScanner
    {
        internal async void ScanBlocks()
        {
            while (true)
            {
                var accounts = AccountUtils.GetAccounts();

                foreach (var a in accounts)
                {                 
                     await ScanBlocks(a);
                }
            }
        }

        private static async Task ScanBlocks(Account userAccount)
        {
            try
            {
                var nemAcc = new AccountFactory().FromEncodedAddress(userAccount.EncodedAddress);

                var blocks = await nemAcc.GetHarvestingInfoAsync();

                if (nemAcc.GetAccountInfoAsync().Result.Meta.RemoteStatus != "ACTIVE" && nemAcc.GetAccountInfoAsync().Result.Meta.Status != "UNLOCKED" && userAccount.CheckBlocks)
                {
                    var bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

                    var reqAction = new SendMessage(userAccount.OwnedByUser, 
                        "The account: \n" + userAccount.EncodedAddress.GetResultsWithHyphen() + 
                        " \nhas stopped harvesting. " +
                        "Harvesting notifications for this account have been turned off. " +
                        "Restart harvesting and then opt in to harvesting notifications for this account.");

                    userAccount.CheckBlocks = false;

                    AccountUtils.UpdateAccount(userAccount);

                    await bot.MakeRequestAsync(reqAction);

                    return;
                }
                foreach (var t in blocks.data)
                {
                    if (blocks.data.Count <= 0 || userAccount.LastBlockHarvestedHeight >= t?.height) continue;

                    userAccount.LastBlockHarvestedHeight = t?.height;

                    AccountUtils.UpdateAccount(userAccount);

                    var hb = new AccountHarvestedSummary()
                    {
                        BlockHeight = t.height,
                        FeesEarned = t.totalFee,
                        MonitoredAccount = userAccount.EncodedAddress,
                        DateOfInput = DateTime.Now,
                        OwnedByUser = userAccount.OwnedByUser
                    };

                    SummaryUtils.AddHBSummary(hb);

                  
                    if (!userAccount.CheckBlocks) continue;

                    await Notify(userAccount, t);
                }
            }
            catch (Exception)
            {
                await ScanBlocks(userAccount);
            }
           
        }

        private static async Task Notify(Account a, HarvestingData.Datum b)
        {
            var bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

            var reqAction = new SendMessage(a.OwnedByUser, "The account: \n" + a.EncodedAddress.GetResultsWithHyphen() + " harvested a new block. \n" + "http://explorer.ournem.com/#/s_block?height=" + b.height + "\nFees included: " + (b.totalFee / 1000000));

            await bot.MakeRequestAsync(reqAction);
        }
    }
}

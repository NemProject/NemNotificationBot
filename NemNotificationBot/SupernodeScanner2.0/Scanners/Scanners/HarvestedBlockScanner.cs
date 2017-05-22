using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
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
                     await ScanBlocks(userAccount: a);
                }
            }
        }

        private static async Task ScanBlocks(
            Account userAccount)
        {
            try
            {
               
                var nemAcc = new AccountFactory().FromEncodedAddress(encodedAddress: userAccount.EncodedAddress);
                
                var blocks = await nemAcc.GetHarvestingInfoAsync();

                foreach (var t in blocks.data)
                {
                    if (blocks.data.Count <= 0 || userAccount.LastBlockHarvestedHeight >= t?.height) continue;
                   
                    userAccount.LastBlockHarvestedHeight = t?.height;

                    AccountUtils.UpdateAccount(
                        usrAcc: userAccount);

                    var hb = new AccountHarvestedSummary()
                    {
                        BlockHeight = t.height,
                        FeesEarned = t.totalFee,
                        MonitoredAccount = userAccount.EncodedAddress,
                        DateOfInput = DateTime.Now,
                        OwnedByUser = userAccount.OwnedByUser
                    };

                    SummaryUtils.AddHBSummary(s: hb);

                  
                    if (!userAccount.CheckBlocks) continue;

                    await Notify(usrAcc: userAccount, hrvData: t);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
                
            }
           
        }

        private static async Task Notify(Account usrAcc, HarvestingData.Datum hrvData)
        {
            var bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

            var reqAction = new SendMessage(chatId: usrAcc.OwnedByUser, text: "The account: \n" + usrAcc.EncodedAddress.GetResultsWithHyphen() + " harvested a new block. \n" + "http://explorer.ournem.com/#/s_block?height=" + hrvData.height + "\nFees included: " + (hrvData.totalFee / 1000000));

            await bot.MakeRequestAsync(request: reqAction);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharp2nem;
using CSharp2nem.Connectivity;
using CSharp2nem.RequestClients;
using CSharp2nem.ResponseObjects.Account;
using CSharp2nem.Utils;
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
        static Connection Con = new Connection();
        internal void ScanBlocks()
        {
            Con.SetMainnet();

            while (true)
            {
                var accounts = AccountUtils.GetAccounts();
                
                foreach (var a in accounts)
                {
                    
                    Thread.Sleep(500);
                    ScanBlocks(userAccount: a);
                }
            }
        }

        private static void ScanBlocks(Account userAccount)
        {
            try
            {
                var aClient = new AccountClient(Con);
               
                aClient.BeginGetHarvestingInfo(ar =>
                {
                    try
                    {                
                        if (ar.Content.data != null)
                        {
                            foreach (var t in ar.Content.data)
                            {
                                if (ar.Content.data.Count <= 0 || userAccount.LastBlockHarvestedHeight >= t?.height)
                                    continue;

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

                                try
                                {
                                    if (userAccount.CheckBlocks)
                                    {
                                        SummaryUtils.AddHBSummary(s: hb);
                                        Notify(usrAcc: userAccount, hrvData: t);
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (e.Message.Contains("blocked"))
                                    {
                                        AccountUtils.DeleteAccountsByUser(userAccount.OwnedByUser);

                                        NodeUtils.DeleteUserNodes(userAccount.OwnedByUser);

                                        UserUtils.DeleteUser(userAccount.OwnedByUser);
                                    }

                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(value: e);   
                    }
                }, userAccount.EncodedAddress);     
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
            }
           
        }

        private static async void Notify(Account usrAcc, HarvestingData.Datum hrvData)
        {
            try
            {
                var bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

                var reqAction = new SendMessage(chatId: usrAcc.OwnedByUser, text: "The account: \n" + StringUtils.GetResultsWithHyphen(usrAcc.EncodedAddress) + " harvested a new block. \n" + "http://explorer.ournem.com/#/s_block?height=" + hrvData.height + "\nFees included: " + (hrvData.totalFee / 1000000));

                await bot.MakeRequestAsync(request: reqAction);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("blocked"))
                {
                    AccountUtils.DeleteAccountsByUser(usrAcc.OwnedByUser);

                    NodeUtils.DeleteUserNodes(usrAcc.OwnedByUser);

                    UserUtils.DeleteUser(usrAcc.OwnedByUser);
                }
            }      
        }
    }
}

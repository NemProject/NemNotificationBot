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
using SupernodeScanner2._0.Utils;
using Account = SupernodeScanner2._0.DataContextModel.Account;

namespace SupernodeScanner2._0.Scanners
{
    internal class TransactionScanner
    {
        internal async void ScanAccounts()
        {
            while (true)
            {
                var accounts = AccountUtils.GetAccounts();
             
                foreach (var a in accounts)
                { 
                    await ScanTransactions(a);
                }
            }
        }

        private static async Task ScanTransactions(Account userAccount)
        {
            try
            {
                var acc = new AccountFactory().FromEncodedAddress(userAccount.EncodedAddress);

                var transactions = await acc.GetAllTransactionsAsync();                

                foreach (var t in transactions.data)
                {
                    if (transactions.data.Count <= 0 || userAccount.LastTransactionHash == transactions.data?[0]?.meta?.hash?.data)
                        break;

                    if (userAccount.LastTransactionHash == t.meta?.hash?.data)
                    {
                        
                        userAccount.LastTransactionHash = transactions.data?[0]?.meta?.hash?.data;
                        AccountUtils.UpdateAccount(userAccount);

                        break;
                    }

                    var summary = new AccountTxSummary
                    {                       
                        AmountIn = t.transaction.recipient == userAccount.EncodedAddress ? t.transaction.amount : 0,
                        AmountOut = t.transaction.recipient != userAccount.EncodedAddress ? t.transaction.amount : 0,
                        MonitoredAccount = userAccount.EncodedAddress,
                        Recipient = t.transaction.recipient,
                        Sender = AddressEncoding.ToEncoded(0x68, new PublicKey(t.transaction.signer)),
                        DateOfInput = DateTime.Now,
                        OwnedByUser = userAccount.OwnedByUser
                        
                    };

                    SummaryUtils.AddTxSummary(summary);

                    

                    if (userAccount.CheckTxs == false) continue;
                    
                    await Notify(userAccount, t);

                }
            }
            catch (Exception)
            {
               await ScanTransactions(userAccount);
            }      
        }

        private static async Task Notify(Account a, Transactions.TransactionData t)
        {
            var bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

            var reqAction = new SendMessage(a.OwnedByUser, "There was a new transaction on account: " + a.EncodedAddress.GetResultsWithHyphen() + "\n \n" + "http://explorer.ournem.com/#/s_account?account=" + a.EncodedAddress + "\n\nRecipient: "+ t.transaction.recipient.GetResultsWithHyphen() + "\nAmount: " + (t.transaction.amount / 1000000) + " XEM");

            await bot.MakeRequestAsync(reqAction);           
        }
    }
}

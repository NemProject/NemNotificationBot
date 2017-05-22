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
                    await ScanTransactions(userAccount: a);
                   // await ScanPendingTransactions(a);
                }
            }
        }

      // private static async Task ScanPendingTransactions(Account userAccount)
      // {
      //     var acc = new AccountFactory().FromEncodedAddress(userAccount.EncodedAddress);
      // 
      //     var transactions = await acc.GetUnconfirmedTransactionsAsync();
      // 
      //     try
      //     {
      //         foreach (var t in transactions.data)
      //         {
      //             if (t.transaction.type != 4100 && t.transaction.signatures.All(e=> e.otherAccount != userAccount.EncodedAddress)) continue;
      // 
      //             var bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);
      // 
      //             var reqAction = new SendMessage(userAccount.OwnedByUser,
      //                 "There is a new pending multisig transaction that requires your signature on account: \n" + userAccount.EncodedAddress.GetResultsWithHyphen() +                        
      //                 "\nTo recipient: \n" +  t.transaction.otherTrans.recipient.GetResultsWithHyphen() +
      //                 "\nAmount: " + (t.transaction.otherTrans.amount / 1000000) + " XEM");
      // 
      //             await bot.MakeRequestAsync(reqAction);
      //         }
      //     }
      //     catch (Exception e)
      //     {
      //         Console.WriteLine(e);
      //     }
      // }
        private static async Task ScanTransactions(Account userAccount)
        {
            try
            {
                var acc = new AccountFactory().FromEncodedAddress(encodedAddress: userAccount.EncodedAddress);

                

                var transactions = await acc.GetAllTransactionsAsync();                

                foreach (var t in transactions.data)
                {
                   if (t.transaction.type != 257 && t.transaction?.otherTrans?.type != 257) continue;

                    if (transactions.data.Count <= 0 || userAccount.LastTransactionHash == transactions.data?[index: 0]?.meta?.hash?.data)
                        break;

                        if (userAccount.LastTransactionHash == t.meta?.hash?.data)
                        {
                        
                            userAccount.LastTransactionHash = transactions.data?[index: 0]?.meta?.hash?.data;
                            AccountUtils.UpdateAccount(usrAcc: userAccount);

                            break;
                        }

                    var summary = new AccountTxSummary
                    {                       
                        AmountIn = t.transaction.recipient == userAccount.EncodedAddress ? t.transaction.amount : 0,
                        AmountOut = t.transaction.recipient != userAccount.EncodedAddress ? t.transaction.amount : 0,
                        MonitoredAccount = userAccount.EncodedAddress,
                        Recipient = t.transaction.type == 257 ?  t.transaction.recipient : t.transaction.otherTrans.recipient,
                        Sender =  AddressEncoding.ToEncoded(network: 0x68, publicKey: new PublicKey(key: t.transaction.type == 257 ?  t.transaction.signer : t.transaction.otherTrans.signer)),
                        DateOfInput = DateTime.Now,
                        OwnedByUser = userAccount.OwnedByUser
                        
                    };

                    SummaryUtils.AddTxSummary(s: summary);

                    if (userAccount.CheckTxs == false) continue;
                    
                    await Notify(a: userAccount, t: t);

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);                
            }      
        }

        private static async Task Notify(Account a, Transactions.TransactionData t)
        {
            var bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

            var reqAction = new SendMessage(chatId: a.OwnedByUser, 
                text: "There is a new " + ( t.transaction.type == 257 ? "" : "multisig ") + "transaction on account: \n" + a.EncodedAddress.GetResultsWithHyphen() + 
                "\nhttp://explorer.ournem.com/#/s_account?account=" + a.EncodedAddress + 
                "\nRecipient: \n"+ (t.transaction.type == 257 ? t.transaction.recipient.GetResultsWithHyphen() : t.transaction.otherTrans.recipient.GetResultsWithHyphen()) + 
                "\nAmount: " + (t.transaction.amount / 1000000) + " XEM");

            await bot.MakeRequestAsync(request: reqAction);           
        }
    }
}

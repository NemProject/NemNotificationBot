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
                }
            }
        }

        private static async Task ScanTransactions(Account userAccount)
        {
            try
            {
                var acc = new AccountFactory().FromEncodedAddress(encodedAddress: userAccount.EncodedAddress);

                var transactions = await acc.GetAllTransactionsAsync();

                var txs = transactions.data.Where(e => e.transaction?.otherTrans?.type == 257).ToList();
                txs.AddRange(transactions.data.Where(e => e.transaction.type == 257));

                foreach (var t in txs)
                {

                    if (userAccount.LastTransactionHash == txs?[0]?.meta.innerHash?.data 
                     || userAccount.LastTransactionHash == txs?[0]?.meta?.hash?.data){
                        break;
                    }
                        

                    if (userAccount.LastTransactionHash == t.meta?.hash?.data || userAccount.LastTransactionHash == t.meta.innerHash.data)
                    {                       
                        userAccount.LastTransactionHash = txs?[0].transaction.type == 4100 ? txs?[0]?.meta.innerHash?.data : txs?[0]?.meta?.hash?.data;
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

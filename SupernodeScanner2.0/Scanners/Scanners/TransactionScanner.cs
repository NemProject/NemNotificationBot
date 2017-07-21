using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharp2nem;
using CSharp2nem.Connectivity;
using CSharp2nem.CryptographicFunctions;
using CSharp2nem.Model.AccountSetup;
using CSharp2nem.RequestClients;
using CSharp2nem.ResponseObjects.Transaction;
using CSharp2nem.Utils;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Utils;


namespace SupernodeScanner2._0.Scanners
{
    internal class TransactionScanner
    {
        static Connection Con = new Connection();

        public TransactionScanner()
        {
            Con = new Connection();
            Con.SetMainnet();
            
        }
        internal void ScanAccounts()
        {
            while (true)
            {
                
                var accounts = AccountUtils.GetAccounts();
             
                foreach (var a in accounts)
                {
                    Thread.Sleep(500);
                   
                    ScanTransactions(userAccount: a);     
                }
            }
        }

        private static void ScanTransactions(Account userAccount)
        {
            try
            {
                var tClient = new TransactionDataClient(Con);
               
                tClient.BeginGetAllTransactions(ar =>
                {
                   
                    try
                    {
                        var txs = ar.Content.data.Where(e => e.transaction?.otherTrans?.type == 257 || e.transaction?.type == 257).ToList();
                       

                        foreach (var t in txs)
                        {
                           
                            if (userAccount.LastTransactionHash == txs?[0]?.meta.innerHash?.data || userAccount.LastTransactionHash == txs?[0]?.meta?.hash?.data)
                            {
                                userAccount.LastTransactionHash = txs?[0].transaction.type == 4100 ? txs?[0]?.meta.innerHash?.data : txs?[0]?.meta?.hash?.data;
                                                         
                                break;
                            }


                            if (userAccount.LastTransactionHash == t.meta?.hash?.data || userAccount.LastTransactionHash == t.meta.innerHash.data)
                            {
                                userAccount.LastTransactionHash = txs?[0].transaction.type == 4100 ? txs?[0]?.meta.innerHash?.data : txs?[0]?.meta?.hash?.data;
                                AccountUtils.UpdateAccount(usrAcc: userAccount);
                                
                                break;
                            }

                            if (!userAccount.CheckTxs) continue;

                            try
                            {                               
                                var summary = new AccountTxSummary
                                {
                                    AmountIn = t.transaction.recipient == userAccount.EncodedAddress ? t.transaction.amount : 0,
                                    AmountOut = t.transaction.recipient != userAccount.EncodedAddress ? t.transaction.amount : 0,
                                    MonitoredAccount = userAccount.EncodedAddress,
                                    Recipient = t.transaction.type == 257 ? t.transaction.recipient : t.transaction.otherTrans.recipient,
                                    Sender = AddressEncoding.ToEncoded(network: 0x68, publicKey: new PublicKey(key: t.transaction.type == 257 ? t.transaction.signer : t.transaction.otherTrans.signer)),
                                    DateOfInput = DateTime.Now,
                                    OwnedByUser = userAccount.OwnedByUser

                                };
                               
                                Notify(a: userAccount, t: t);

                                SummaryUtils.AddTxSummary(s: summary);
                            }
                            catch (Exception e)
                            {

                                if (e.Message.Contains("blocked"))
                                {
                                    AccountUtils.DeleteAccountsByUser(userAccount.OwnedByUser);

                                    NodeUtils.DeleteUserNodes(userAccount.OwnedByUser);

                                    UserUtils.DeleteUser(userAccount.OwnedByUser);
                                }
                                else Console.WriteLine("Trans scanner line 112: " + e.StackTrace);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(value: e);

                        if (e.Message.Contains("blocked"))
                        {
                            AccountUtils.DeleteAccountsByUser(userAccount.OwnedByUser);

                            NodeUtils.DeleteUserNodes(userAccount.OwnedByUser);

                            UserUtils.DeleteUser(userAccount.OwnedByUser);
                        }
                    }
                    
                }, userAccount.EncodedAddress);

                
            }
            catch (Exception e)
            {   
                Console.WriteLine("Transaction scanner line 136: " + e.StackTrace);
            }      
        }

        private static async void Notify(Account a, Transactions.TransactionData t)
        {
            try
            {
                var bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);
                var msg = "There is a new " + (t.transaction.type == 257 ? "" : "multisig ") +
                          "transaction on account: \n" +
                          StringUtils.GetResultsWithHyphen(a.EncodedAddress) +
                          "\nhttp://explorer.ournem.com/#/s_account?account=" + a.EncodedAddress +
                          "\nRecipient: \n" +
                          (t.transaction.type == 257
                              ? StringUtils.GetResultsWithHyphen(t.transaction.recipient)
                              : StringUtils.GetResultsWithHyphen(t.transaction.otherTrans.recipient)) +
                          "\nAmount: " + (t.transaction.amount / 1000000) + " XEM";

                var reqAction = new SendMessage(chatId: a.OwnedByUser,
                    text: msg);
                Console.WriteLine(msg);
                await bot.MakeRequestAsync(request: reqAction);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("blocked"))
                {
                    AccountUtils.DeleteAccountsByUser(a.OwnedByUser);

                    NodeUtils.DeleteUserNodes(a.OwnedByUser);

                    UserUtils.DeleteUser(a.OwnedByUser);
                }
            }
                    
        }
    }
}

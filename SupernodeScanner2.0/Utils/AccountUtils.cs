using CSharp2nem;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharp2nem.RequestClients;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using SupernodeScanner2._0.DBControllers;
using SuperNodeScanner;

namespace SupernodeScanner2._0.DataContextModel
{
    internal class AccountUtils
    {
        
        internal static void AddAccount(long chatId, List<string> accounts)
        {
            var context = new AccountDataContext();

            foreach (var acc in accounts)
            {
                switch (acc)
                {
                    case "NBZMQO7ZPBYNBDUR7F75MAKA2S3DHDCIFG775N3D":
                    case "NCPAYOUTH2BGEGT3Q7K75PV27QKMVNN2IZRVZWMD":
                    case null:
                        continue;
                }

                if (GetAccount(add: acc, user: chatId)?.EncodedAddress != null) return;

                var a = new AccountClient(); //(encodedAddress: acc);
                
                var blocks = a.EndGetHarvestingInfo(a.BeginGetHarvestingInfo(acc)).data; // check hash

                var tClient = new TransactionDataClient();

                tClient.BeginGetAllTransactions(ar =>
                {
                    
                    var txs = ar.Content.data.Where(e => e.transaction?.otherTrans?.type == 257).ToList();

                    txs.AddRange(ar.Content.data.Where(e => e.transaction.type == 257));

                    var account = new Account()
                    {

                        OwnedByUser = chatId,
                        EncodedAddress = acc.ToUpper(),
                        LastTransactionHash = txs.Count > 0 ? txs?[0].transaction.type == 4100 ? txs?[0]?.meta.innerHash?.data : txs?[0]?.meta?.hash?.data : "none", //
                        LastBlockHarvestedHeight = blocks.Count > 0 ? blocks[index: 0]?.height : 0,
                        CheckBlocks = true,
                        CheckTxs = true

                    };

                    context.Accounts.InsertOnSubmit(entity: account);

                    try
                    {
                        context.SubmitChanges();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("account utils: " + e.Message);
                    }
                }, acc);                  
            }        
        }

        internal static void DeleteAccount(long chatId, List<string> accounts)
        {
            var context = new AccountDataContext();

            foreach (var acc in accounts)
            { 
                Account account;

                try
                {
                    account = context.Accounts.Where(predicate: e=>e.OwnedByUser == chatId).Single(predicate: e => e.EncodedAddress == acc);
                }
                catch (Exception)
                {
                    continue;
                }

                context.Accounts.DeleteOnSubmit(entity: account);

                context.SubmitChanges();
            }   
        }

        internal static void DeleteAccountsByUser(long chatId)
        {
            var context = new AccountDataContext();

            var accounts = context.Accounts.Where(r => r.OwnedByUser == chatId);

            foreach (var acc in accounts)
            {
                Account account;
                try
                {
                    account = context.Accounts.Where(predicate: e => e.OwnedByUser == chatId).Single(predicate: e => e.EncodedAddress == acc.EncodedAddress);
                }
                catch (Exception)
                {
                    continue;
                }

                context.Accounts.DeleteOnSubmit(entity: account);

                context.SubmitChanges();
            }
        }


        internal static List<Account> GetAccountByUser(long chatId)
        {

            if (UserUtils.GetUser(chatId: chatId)?.ChatId == null) return null;

            var context = new AccountDataContext();

            return context.Accounts.Where(predicate: node => node.OwnedByUser == chatId).ToList();
        }

        internal static void UpdateAccount(Account usrAcc)
        {
            var context = new AccountDataContext();

            var acc = context.Accounts.Where(predicate: e => e.EncodedAddress == usrAcc.EncodedAddress).Single(predicate: i => i.OwnedByUser == usrAcc.OwnedByUser);

            acc.LastTransactionHash = usrAcc.LastTransactionHash;
            acc.LastBlockHarvestedHeight = usrAcc.LastBlockHarvestedHeight;
            acc.CheckBlocks = usrAcc.CheckBlocks;
            acc.CheckTxs = usrAcc.CheckTxs;
            context.SubmitChanges();
        }
        internal static void UpdateAccount(List<Account> accs, long user)
        {
            var context = new AccountDataContext();
            
            foreach(var acc in accs)
            {
                var a = context.Accounts.Where(predicate: e => e.EncodedAddress == acc.EncodedAddress).Single( predicate: i => i.OwnedByUser == user);


                a.LastTransactionHash = acc.LastTransactionHash;
                a.LastBlockHarvestedHeight = acc.LastBlockHarvestedHeight;
                a.CheckBlocks = acc.CheckBlocks;
                a.CheckTxs = acc.CheckTxs;

                context.SubmitChanges();
            }    
        }

        internal static List<Account> GetAccounts()
        {
            var context = new AccountDataContext();

            try
            {
                var acc = context.Accounts.ToList();
                return acc;
            }
            catch (Exception e)
            {
                Console.WriteLine(value: e);
                return null;
            }

        }
        internal static Account GetAccount(string add, long user)
        {
            var context = new AccountDataContext();

            try
            {
              var acc = context.Accounts.Where(predicate: e => e.OwnedByUser == user).Single(predicate: e => e.EncodedAddress == add);
             
              return acc;
            }
            catch (Exception)
            {
                return null;
            }
            
        }
    }
}


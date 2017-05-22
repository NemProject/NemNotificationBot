using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SupernodeScanner2._0.DataContextModel;

namespace SupernodeScanner2._0.Utils
{
    internal class SummaryUtils
    {
        internal static void AddTxSummary(AccountTxSummary s)
        { 
            var context = new AccountTxSummaryDataContext();
                
            context.AccountTxSummaries.InsertOnSubmit(entity: s);

            context.SubmitChanges();
        }

        internal static List<AccountTxSummary> GetTxSummary(string account, int days, long chatId)
        {

            var context = new AccountTxSummaryDataContext();

            DateTime startDate = DateTime.Today.AddDays(value: -days);
            
              
            return context.AccountTxSummaries
                .Where(predicate: e => e.MonitoredAccount == account)
                .Where(predicate: e => e.OwnedByUser == chatId)
                .Where(predicate: e => e.DateOfInput > startDate.Date)
                .ToList();
        }

        internal static void DeleteTxSummaryForUser(string account, long chatId)
        {
            var context = new AccountTxSummaryDataContext();

            var summaries = GetTxSummary(account: account, days: 1000, chatId: chatId);

            foreach (var sum in summaries)
            {
                context.AccountTxSummaries.Attach(entity: sum);
                context.AccountTxSummaries.DeleteOnSubmit(entity: sum);
            }

            context.SubmitChanges();
        }

        internal static void DeleteHbSummaryForUser(string account, long chatId)
        {
            var context = new AccountHarvestedSummaryDataContext();

            var summaries = GetHBSummary(account: account, days: 1000, chatId: chatId);

            foreach (var sum in summaries)
            {
                context.AccountHarvestedSummaries.Attach(entity: sum);
                context.AccountHarvestedSummaries.DeleteOnSubmit(entity: sum);
            }

            context.SubmitChanges();
        }

        internal static List<AccountHarvestedSummary> GetHBSummary(string account, int days, long chatId)
        {

            var context = new AccountHarvestedSummaryDataContext();

            DateTime startDate = DateTime.Today.AddDays(value: -days);


            return context.AccountHarvestedSummaries
                .Where(predicate: e => e.MonitoredAccount == account)
                .Where(predicate: e => e.OwnedByUser == chatId)
                .Where(predicate: e => e.DateOfInput > startDate.Date)
                .ToList();
        }


        internal static void AddHBSummary(AccountHarvestedSummary s)
        {
            var context = new AccountHarvestedSummaryDataContext();

            context.AccountHarvestedSummaries.InsertOnSubmit(entity: s);

            context.SubmitChanges();
        }
    }
    
}

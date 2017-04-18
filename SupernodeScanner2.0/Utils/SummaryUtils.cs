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
                
            context.AccountTxSummaries.InsertOnSubmit(s);

            context.SubmitChanges();
        }

        internal static List<AccountTxSummary> GetTxSummary(string account, int days, long chatId)
        {

            var context = new AccountTxSummaryDataContext();

            DateTime startDate = DateTime.Today.AddDays(-days);
            
              
            return context.AccountTxSummaries
                .Where(e => e.MonitoredAccount == account)
                .Where(e => e.OwnedByUser == chatId)
                .Where(e => e.DateOfInput > startDate.Date)
                .ToList();
        }

        internal static void DeleteTxSummaryForUser(string account, long chatId)
        {
            var context = new AccountTxSummaryDataContext();

            var summaries = GetTxSummary(account, 1000, chatId);

            foreach (var sum in summaries)
            {
                context.AccountTxSummaries.Attach(sum);
                context.AccountTxSummaries.DeleteOnSubmit(sum);
            }

            context.SubmitChanges();
        }

        internal static void DeleteHbSummaryForUser(string account, long chatId)
        {
            var context = new AccountHarvestedSummaryDataContext();

            var summaries = GetHBSummary(account, 1000, chatId);

            foreach (var sum in summaries)
            {
                context.AccountHarvestedSummaries.Attach(sum);
                context.AccountHarvestedSummaries.DeleteOnSubmit(sum);
            }

            context.SubmitChanges();
        }

        internal static List<AccountHarvestedSummary> GetHBSummary(string account, int days, long chatId)
        {

            var context = new AccountHarvestedSummaryDataContext();

            DateTime startDate = DateTime.Today.AddDays(-days);


            return context.AccountHarvestedSummaries
                .Where(e => e.MonitoredAccount == account)
                .Where(e => e.OwnedByUser == chatId)
                .Where(e => e.DateOfInput > startDate.Date)
                .ToList();
        }


        internal static void AddHBSummary(AccountHarvestedSummary s)
        {
            var context = new AccountHarvestedSummaryDataContext();

            context.AccountHarvestedSummaries.InsertOnSubmit(s);

            context.SubmitChanges();
        }
    }
    
}

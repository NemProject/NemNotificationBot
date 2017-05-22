using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using SupernodeScanner2._0.DataContextModel;

namespace SupernodeScanner2._0.DBControllers
{
    internal class UserUtils
    {
        internal static void AddUser(string userName, long chatId)
        {
            try
            {
                if (GetUser(chatId: chatId)?.ChatId == chatId) return;

                var user = new User()
                {
                    ChatId = chatId,
                    UserName = userName,
                };

                var context = new UserDataContext();

                context.Users.InsertOnSubmit(entity: user);

                context.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }

        internal static void DeleteUser(string userName, long chatId)
        {
            try
            {
                if (GetUser(chatId: chatId)?.ChatId == null) return;

                AccountUtils.DeleteAccount(chatId: chatId, accounts: AccountUtils.GetAccountByUser(chatId: chatId).Select(selector: e => e.EncodedAddress).ToList());

                var context = new UserDataContext();

                var user = context.Users.Single(predicate: e => e.ChatId == chatId);

                context.Users.DeleteOnSubmit(entity: user);

                context.SubmitChanges();
            }   
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
           
        }
       
        internal static User GetUser(long chatId)
        {
            var context = new UserDataContext();
            try
            {
                var acc = context.Users.Single(predicate: e => e.ChatId == chatId);
                return acc;
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}

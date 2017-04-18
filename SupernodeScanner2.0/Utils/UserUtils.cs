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
            if (GetUser(chatId)?.UserName != null) return;

            var user = new User()
            {
                ChatId = chatId,
                UserName = userName,    
            };
            var context = new UserDataContext();

            context.Users.InsertOnSubmit(user);

            context.SubmitChanges();
        }

        internal static void DeleteUser(string userName, long chatId)
        {
            if (GetUser(chatId)?.UserName == null) return;

            AccountUtils.DeleteAccount(chatId, AccountUtils.GetAccountByUser(chatId).Select(e => e.EncodedAddress).ToList());

            var context = new UserDataContext();

            var user = context.Users.Single(e => e.ChatId == chatId);

            context.Users.DeleteOnSubmit(user);

            context.SubmitChanges();
           
        }
       
        internal static User GetUser(long chatId)
        {
            var context = new UserDataContext();
            try
            {
                var acc = context.Users.Single(e => e.ChatId == chatId);
                return acc;
            }
            catch (Exception)
            {
                return null;
            }

        }
    }
}

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
using SuperNodeScanner;

namespace SupernodeScanner2._0.Utils
{
    internal class NodeUtils
    {
        internal static List<SuperNodes.Node> AddNode(long chatId, List<SuperNodes.Node> nodes)
        {
            var list = new List<SuperNodes.Node>();
            var context = new SuperNodeDataContext();

            foreach (var node in nodes)
            {    

                var n = GetNode(ip: node.Ip, user: chatId);

                if (n?.IP != null && n?.OwnedByUser == chatId)
                {
                    
                    continue;
                };
                
                var snode = new SuperNode()
                {
                    OwnedByUser = chatId,
                    IP = node.Ip,
                    LastTest = 0,
                    DepositAddress = node.PayoutAddress,
                    SNodeID = int.Parse(s: node.Id),
                    Alias = node.Alias                 
                };
                
                context.SuperNodes.InsertOnSubmit(entity: snode);
                list.Add(item: node);
            }
            
            context.SubmitChanges();
            return list;
        }

        internal static void DeleteNode(long chatId, List<string> nodes)
        {
            var context = new SuperNodeDataContext();

            foreach (var ip in nodes)
            {
                var n = GetNode(ip: ip, user: chatId);

                if (n?.OwnedByUser != chatId) continue;

                context.SuperNodes.Attach(entity: n);
                context.SuperNodes.DeleteOnSubmit(entity: n);    
            }

            context.SubmitChanges();
        }

        internal static void UpdateNode(SuperNode snode, long chatId)
        {
            if (GetNode(ip: snode.IP, user: chatId)?.IP == null) return;

            var context = new SuperNodeDataContext();

            var node = context.SuperNodes
                .Where(predicate: e => e.OwnedByUser == snode.OwnedByUser)
                .Single(predicate: e => e.IP == snode.IP);

            node.LastTest = snode.LastTest;
            node.WentOffLine = snode.WentOffLine;

            context.SubmitChanges();
        }
        internal static List<SuperNode> GetAllNodes()
        {
            var context = new SuperNodeDataContext();

            return context.SuperNodes.ToList();
        }

        internal static SuperNode GetNode(string ip, long user)
        {
            var context = new SuperNodeDataContext();
            try
            {
                var acc = context.SuperNodes.Where(predicate: e => e.OwnedByUser == user).Single(predicate: e => e.IP == ip);
                return acc;
            }
            catch (Exception)
            {
                return null;
            }

        }

        internal static List<SuperNode> GetNodeByUser(long chatId)
        {
            if (UserUtils.GetUser(chatId: chatId)?.ChatId == null) return null;

            var context = new SuperNodeDataContext();

            return context.SuperNodes.Where(predicate: node => node.OwnedByUser == chatId).ToList();  
        }
    }
}

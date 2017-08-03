using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharp2nem;
using CSharp2nem.ResponseObjects;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SuperNodeScanner;

namespace SupernodeScanner2._0.Utils
{
    internal class NodeUtils
    {
        internal static SupernodeResponseData.Supernodes AddNode(long chatId, SupernodeResponseData.Supernodes nodes)
        {
            var list = new SupernodeResponseData.Supernodes()
            {
                data = new List<SupernodeResponseData.Nodes>()
            };
           

            var context = new SuperNodeDataContext();

            try
            {
                foreach (var node in nodes.data)
                {

                    var n = GetNode(ip: node.ip, user: chatId);

                    if (n?.IP != null && n?.OwnedByUser == chatId)
                    {

                        continue;
                    };

                    var snode = new SuperNode()
                    {
                        OwnedByUser = chatId,
                        IP = node.ip,
                        LastTest = 0,
                        DepositAddress = node.payoutAddress,
                        SNodeID = node.id,
                        Alias = node.alias
                    };

                    context.SuperNodes.InsertOnSubmit(entity: snode);
                    list.data.Add(item: node);
                }

                context.SubmitChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine("add node " +  e.StackTrace);
            }

            return list;
        }

        internal static void DeleteNode(long chatId, List<string> nodes)
        {
            var context = new SuperNodeDataContext();

            try
            {
                foreach (var ip in nodes)
                {
                    var n = GetNode(ip: ip, user: chatId);

                    if (n?.OwnedByUser != chatId) continue;

                    context.SuperNodes.Attach(entity: n);
                    context.SuperNodes.DeleteOnSubmit(entity: n);
                }

                context.SubmitChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("delete node " + ex.Message);
            }
            
        }

        internal static void DeleteUserNodes(long chatId)
        {

            var context = new SuperNodeDataContext();

            try
            {
                var nodes = context.SuperNodes.Where(r => r.OwnedByUser == chatId).ToList();

                foreach (var node in nodes)
                {
                    var n = GetNode(ip: node.IP, user: chatId);

                    if (n?.OwnedByUser != chatId) continue;

                    context.SuperNodes.Attach(n);
                    context.SuperNodes.DeleteOnSubmit(entity: n);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("delete user node " + ex.Message);
            }
            

            context.SubmitChanges();
        }

        internal static void UpdateNode(SuperNode snode, long chatId)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine("update node " + ex.Message);    
            }
            
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

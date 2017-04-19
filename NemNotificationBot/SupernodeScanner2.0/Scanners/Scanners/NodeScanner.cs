using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharp2nem;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using SupernodeScanner2._0.Utils;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;

namespace SuperNodeScanner
{
    internal class NodeScanner
    {
        private TelegramBot bot { get; set; }

        internal NodeClient NodeClient = new NodeClient();

        internal async void TestNodes()
        {
           
            while (true)
            {
                
                bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

                var nodes = NodeUtils.GetAllNodes();

                foreach (var n in nodes)
                {                   
                    try
                    {
                        var client = new NodeClient();

                        client.Connection.SetHost(n.IP);

                        var info = client.ExtendedNodeInfo().Result;

                        if (info.Node.Endpoint.Host == n.IP)
                        {
                            await ScanTests(n);
                        }
                        else
                        {
                            await Nofity(n, "Node: " + n.IP + " is offline or otherwise unreachable. It will be removed from your list of registered nodes.");

                            if (n.OwnedByUser == null) continue;

                            NodeUtils.DeleteNode(
                                chatId: (long)n.OwnedByUser, 
                                nodes: new List<string> { n.IP });
                            AccountUtils.DeleteAccount(
                                chatId: (long)n.OwnedByUser,
                                accounts: new List<string> { AccountUtils.GetAccount(n.DepositAddress, (long)n.OwnedByUser).EncodedAddress }
                            );
                        }
                    }
                    catch (Exception)
                    {                    
                        await Nofity(n, "Node: " + n.IP + " is offline or otherwise unreachable. It will be removed from your list of registered nodes.");

                        if (n.OwnedByUser == null) continue;

                        NodeUtils.DeleteNode(
                            chatId: (long)n.OwnedByUser, 
                            nodes: new List<string> { n.IP});
                        AccountUtils.DeleteAccount(
                            chatId: (long)n.OwnedByUser,
                            accounts: new List<string> { AccountUtils.GetAccount(n.DepositAddress, (long)n.OwnedByUser).EncodedAddress }
                        );
                    }           
                }              
            }           
        }

        internal async Task<List<string>> ScanTests(SuperNode n)
        {
            try
            {
                var lastTest = NodeClient.SuperNodeByIp(n.IP).Result;

                var result = new List<string>();

                var tests = lastTest.TestResults.nodeDetails.OrderBy(e => e.id).ToList();

                for (var x = 0; x < 4; x++)
                {
                    if (!(n.LastTest < long.Parse(tests[x].id))) continue;

                    n.LastTest = int.Parse(tests[x].id);

                    result = ValidateTests(tests[x]);

                    if (result.Count == 0) continue;

                    var msg = "Node: " + n.Alias + 
                            "\nWith IP: " + n.IP + 
                           " \nfailed tests on " + "\nDate: " + tests[x].dateAndTime.Substring(0, 10) + 
                                                   "\nTime: " + tests[x].dateAndTime.Substring(11, 8) + "\n";

                    msg = result.Aggregate(msg, (current, t) => current + (t + ": FAILED\n") + "https://supernodes.nem.io/details/" + n.SNodeID);

                    if (n.OwnedByUser != null)
                    {
                        NodeUtils.UpdateNode(n, (long)n.OwnedByUser);                        
                    }

                    await Nofity(n, msg);                   
                }

                return result;
            }
            catch (Exception)
            {
               return await ScanTests(n);
            }
            
        }

        private static List<string> ValidateTests(SuperNodes.NodeDetail data)
        {
            return (from propertyInfo
                    in data.GetType().GetProperties()
                    where propertyInfo.PropertyType == typeof(bool)
                    where !(bool)propertyInfo.GetValue(data, null)
                    select propertyInfo.Name
                    ).ToList();
        }

        private async Task Nofity(SuperNode n, string msg)
        {
            if (n.OwnedByUser != null)
            {
                var reqAction = new SendMessage((long)n.OwnedByUser, msg);
                await bot.MakeRequestAsync(reqAction);
            }
        }     
    }
}

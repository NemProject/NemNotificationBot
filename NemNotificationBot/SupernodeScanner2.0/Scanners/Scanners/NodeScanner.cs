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
                
                bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

                var nodes = NodeUtils.GetAllNodes();

                foreach (var n in nodes)
                {
                    try
                    {
                        var con = new Connection();

                        con.SetHost(n.IP);

                        var client = new NodeClient(con);

                        var info = client.ExtendedNodeInfo().Result;
                        

                        if (n.WentOffLine != null)
                        {

                            var nis = new NisClient(con);
                            
                            if (nis.Status().Result.Code != 6) continue;

                            await Nofity(
                                node: n,
                                msg: "Node: " + n.Alias + "\n" + " With IP: " + n.IP +
                                "\nis back online.");

                            n.WentOffLine = null;

                            NodeUtils.UpdateNode(snode: n, chatId: n.OwnedByUser);
                        }
                        if (info.Node.Endpoint.Host == n.IP)
                        {
                            
                            await ScanTests(n: n);
                        }   
                    }
                    catch (Exception e)
                    {
                        if (n.WentOffLine == null)
                        {
                            await Nofity(node: n,
                                msg: "Node: " + n.Alias + "\n" + "With IP: " + n.IP +
                                " \nis offline or otherwise unreachable. It will be removed from your list of registered nodes in 48 hours if it is not reachable in that time.");

                            n.WentOffLine = DateTime.Now;

                            NodeUtils.UpdateNode(snode: n, chatId: n.OwnedByUser);
                        }
                        else if (n.WentOffLine < DateTime.Now.AddDays(value: -2))
                        {
                            await Nofity(node: n,
                                msg: "Node: " + n.IP +
                                " has been offline or otherwise unreachable for 48 hours. It will be removed from your list of registered nodes.");

                            NodeUtils.DeleteNode(
                                chatId: (long)n.OwnedByUser,
                                nodes: new List<string> { n.IP });
                            
                            AccountUtils.DeleteAccount(
                                chatId: (long)n.OwnedByUser,
                                accounts: new List<string> { AccountUtils.GetAccount(add: n.DepositAddress, user: (long)n.OwnedByUser).EncodedAddress }
                            );
                        }
                    }
                }
            }           
        }

        internal async Task ScanTests(SuperNode n)
        {
            try
            {            
                var lastTest = NodeClient.SuperNodeByIp(ip: n.IP).Result;

                var result = new List<string>();

                if (lastTest.TestResults== null) return;

                var tests = lastTest.TestResults.nodeDetails.OrderBy(keySelector: e => e.id).ToList();
               
                for (var x = 0; x < 4; x++)
                {
                    if (!(n.LastTest < long.Parse(s: tests[index: x].id))) continue;

                    n.LastTest = int.Parse(s: tests[index: x].id);

                    result = ValidateTests(data: tests[index: x]);

                    if (result == null) continue;

                    if (result.Count == 0) continue;

                    var msg = "Node: " + n.Alias + 
                            "\nWith IP: " + n.IP + 
                           " \nfailed tests on " + "\nDate: " + tests[index: x].dateAndTime.Substring(startIndex: 0, length: 10) + 
                                                   "\nTime: " + tests[index: x].dateAndTime.Substring(startIndex: 11, length: 8) + "\n";

                    msg = result.Aggregate(seed: msg, func: (current, t) => current + (t + ": FAILED\n") + "https://supernodes.nem.io/details/" + n.SNodeID + "\n");

     
                    NodeUtils.UpdateNode(snode: n, chatId: (long)n.OwnedByUser);                        
                    

                    await Nofity(node: n, msg: msg);                   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
        }

        private static List<string> ValidateTests(SuperNodes.NodeDetail data)
        {
            return (from propertyInfo
                    in data.GetType().GetProperties()
                    where propertyInfo.PropertyType == typeof(bool)
                    where !(bool)propertyInfo.GetValue(obj: data, index: null)
                    select propertyInfo.Name
                    ).ToList();
        }

        private async Task Nofity(SuperNode node, string msg)
        {

            var reqAction = new SendMessage(chatId: (long)node.OwnedByUser, text: msg);
            await bot.MakeRequestAsync(request: reqAction);
            
        }     
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharp2nem;
using CSharp2nem.Connectivity;
using CSharp2nem.Model.DataModels;
using CSharp2nem.RequestClients;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using SupernodeScanner2._0;
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
                    Thread.Sleep(500);
                    
                    try
                    {
                        var con = new Connection();

                        con.SetHost(n.IP);
                       
                        con.AutoHost = false;

                        var client = new NodeClient(con);
                        
                        var info = client.EndGetExtendedNodeInfo(client.BeginGetExtendedNodeInfo());

                        if (n.WentOffLine != null)
                        {
                            var nis = new NisClient(con);
                            
                            if (nis.EndGetStatus(nis.BeginGetStatus()).Code != 6) continue;

                            await Nofity(
                                node: n,
                                msg: "Node: " + n.Alias + "\n" + " With IP: " + n.IP +
                                "\nis back online.");

                            n.WentOffLine = null;

                            NodeUtils.UpdateNode(snode: n, chatId: n.OwnedByUser);
                        }
                        if (info.Node.endpoint.Host == n.IP)
                        {  
                            ScanTests(n: n);
                        }   
                    }
                    catch (Exception e)
                    {    
                        if (e.Message.Contains("blocked"))
                        {
                            AccountUtils.DeleteAccountsByUser(n.OwnedByUser);

                            NodeUtils.DeleteUserNodes(n.OwnedByUser);

                            UserUtils.DeleteUser(n.OwnedByUser);

                            break;
                        }

                        if (n.WentOffLine == null)
                        {
                            try
                            {
                                await Nofity(node: n,
                                    msg: "Node: " + n.Alias + "\n" + "With IP: " + n.IP +
                                         " \nis offline or otherwise unreachable. It will be removed from your list of registered nodes in 48 hours if it is not reachable in that time.");

                            }
                            catch (Exception ex)
                            {
                                
                                await Nofity(node: n,
                                msg: "Node: " + n.Alias + "\n" + "With IP: " + n.IP +
                                " \nis offline or otherwise unreachable. It will be removed from your list of registered nodes in 48 hours if it is not reachable in that time.");       
                            }

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
                var testTypes = new List<string>()
                {
                    "node version test",
                    "chain height test",
                    "chain part test",
                    "responsiveness test",
                    "bandwidth test",
                    "computing power test",
                    "ping test",
                    "node balance test"
                };

                var superClient = new SupernodeClient();

                superClient.BeginGetTestResults(ar =>
                {
                    try
                    {
                        
                        if (ar.Content.data[0].round != n.LastTest)
                        {
                            var bitArray = new BitArray(new[] { ar.Content.data[0].testResult });

                            var passed = new bool[32];

                            bitArray.CopyTo(passed, 0);

                            var passedBits = ToBitInts(bitArray);
                          
                            if (passedBits.Contains(0))
                            {
                                var msg = "Node: " + n.Alias +
                                          "\nWith IP: " + n.IP +
                                          " \nfailed tests on " + "\nDate: " +
                                          ar.Content.data[0].dateAndTime.Substring(startIndex: 0, length: 10) +
                                          "\nTime: " + ar.Content.data[0].dateAndTime.Substring(startIndex: 11, length: 8) +
                                          "\n";

                                for (var index = 0; index < 8; index++)
                                {
                                    var pass = passedBits[index];

                                    if (pass == 0)
                                    {
                                        msg += pass == 0 ? testTypes[index] + ": failed\n" : testTypes[index] + ": passed\n";
                                    }
                                }

                                try
                                {
                                    msg += "https://supernodes.nem.io/details/" + n.SNodeID + "\n";
                                    n.LastTest = ar.Content.data[0].round;
                                    NodeUtils.UpdateNode(snode: n, chatId: n.OwnedByUser);

                                    Console.WriteLine(msg);

                                    Nofity(node: n, msg: msg);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("node scanner line 199: " + e.StackTrace);

                                    if (e.Message.Contains("blocked"))
                                    {
                                        UserUtils.DeleteUser(n.OwnedByUser);
                                    }
                                }
                            }
                        }
                       
                        
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("node scanner line 214: " + ex.Message);
                    }
                }, new SupernodeStructures.TestResultRequestData
                {
                    alias = n.Alias,
                    numRounds = 1,
                    roundFrom = -1
                });                
            }
            catch (Exception e)
            {
                Console.WriteLine("node scanner line 225"  + e.StackTrace);
            }
            
        }

        public static int[] ToBitInts(BitArray bits)
        {
            int[] ints = new int[8];

            for (int i = 0; i < 8; i++)
            {
                ints[i] = bits[i] ? 1 : 0;
               
            }

            return ints;
        }

        private async Task Nofity(SuperNode node, string msg)
        {
            try
            {
                var reqAction = new SendMessage(chatId: (long) node.OwnedByUser, text: msg);
                await bot.MakeRequestAsync(request: reqAction);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("blocked"))
                {
                    AccountUtils.DeleteAccountsByUser(node.OwnedByUser);

                    NodeUtils.DeleteUserNodes(node.OwnedByUser);

                    UserUtils.DeleteUser(node.OwnedByUser);
                }
            }
        }     
    }
}

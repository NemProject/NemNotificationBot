using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharp2nem;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Utils;

namespace SupernodeScanner2._0.Scanners.TaskRunners
{
    internal class NodeManagement
    {
        internal void ManageNodes(Chat chat, string text)
        {
            var Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);

            // if the user is not known, add the user to the database
            if (UserUtils.GetUser(chat.Id)?.UserName == null)
            {
                // add user based on their chat ID
                UserUtils.AddUser(chat.Username, chat.Id);

                // declare message
                var msg1 = "You have been automatically registered, one moment please";

                // send message notifying they have been registered
                var reqAction1 = new SendMessage(chat.Id, msg1);

                // send message
                Bot.MakeRequestAsync(reqAction1);
            }

            // set up regex pattern matching sequences.
            var ip = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            var ip2 = new Regex(@"[a-zA-Z]{1,15}\.[a-zA-Z]{1,15}\.[a-zA-Z]{1,15}");
            var ip3 = new Regex(@"[a-zA-Z]{1,15}\.[a-zA-Z]{1,15}");

            // scan list of submitted ip's for any valid sequences
            var result = ip.Matches(text).Cast<Match>().Select(m => m.Value)
                .Concat(ip2.Matches(text).Cast<Match>().Select(m => m.Value))
                .Concat(ip3.Matches(text).Cast<Match>().Select(m => m.Value)).ToArray();

            // declare a nodeClient to retrieve node data.
            var nodeClient = new NodeClient();

            // get a list of all supernodes
            var nodes = nodeClient.SuperNodeList().Result.Nodes;

            // check submitted list against the list of all supernodes
            var validNodes = new List<SuperNodes.Node>();
            foreach (string userIp in result)
                foreach (var node in nodes)
                {
                    if (userIp != node.Ip) continue;

                    if (node.PayoutAddress == null)
                    {
                        var bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);
                        var req = new SendMessage(chat.Id, "One of the nodes you have submitted is invalid, or has not been accepted into the supernode program yet, or it has not recieved its first payment. The invalid node was node registered. Please check your nodes and try again");
                        bot.MakeRequestAsync(req);
                        continue;
                    }
                    validNodes.Add(node);
                }

            // if the user wants to register a node
            if (text.StartsWith("/registerNode:") && text != "/registerNode:")
            {
                // automatically add the deposit account of each registered node as a monitored account
                // nodes must be cross referenced with total supernode list to acquire the deposit address
                // as the supernode API doesnt contain this information
                string msg1;
                try
                {
                    AccountUtils.AddAccount(
                         chat.Id,
                         nodes.Where(x => validNodes.Any(y => y.Ip == x.Ip)).ToList()
                         .Select(node => node.PayoutAddress).ToList());

                    var nodesAdded = NodeUtils.AddNode(chat.Id, validNodes);


                    // return a message showing which accounts were registered
                    msg1 = nodes.Count > 0
                        ? nodesAdded.Aggregate("Nodes registered: \n \n", (current, n) => current + n.Ip + "\n")
                        : "No nodes were added. It/they may be offline or have an invalid IP. Check your node ip's and try again";

                    // send message
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    msg1 = "Something went wrong, please try again.";
                }

                var reqAction1 = new SendMessage(chat.Id, msg1);
                Bot.MakeRequestAsync(reqAction1);

            }

            // if a user wants to unregister an account
            if (text.StartsWith("/unregisterNode:") && text != "/unregisterNode:")
            {
                string msg2;
                try
                {
                    // declare message assuming nothing goes wrong
                    msg2 = string.Concat("Your nodes were removed");

                    // make sure the user is registered
                    if (UserUtils.GetUser(chat.Id)?.UserName != chat.Username)
                    {
                        // if not, tell them
                        var reqAction3 = new SendMessage(chat.Id, "You are not registered");
                        Bot.MakeRequestAsync(reqAction3);
                        return;
                    }

                    // get all user nodes
                    var userNodes = NodeUtils.GetNodeByUser(chat.Id);

                    // delete any nodes submitted
                    NodeUtils.DeleteNode(chat.Id, result.ToList());

                    // delete any associated deposit accounts that would have been automatically registered
                    AccountUtils.DeleteAccount(chat.Id, nodes.Where(x => userNodes.Any(y => y.IP == x.Ip)).ToList()
                             .Select(node => node.PayoutAddress).ToList());
                }
                catch (Exception)
                {
                    msg2 = "Something went wrong. please try again";
                }
                
                // send a message to notify user of any changes
                var reqAction2 = new SendMessage(chat.Id, msg2);
                Bot.MakeRequestAsync(reqAction2);
            }
        }
    }
}

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
            var Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);

            // if the user is not known, add the user to the database
            if (UserUtils.GetUser(chatId: chat.Id)?.ChatId == null)
            {
                // add user based on their chat ID
                UserUtils.AddUser(userName: chat.Username, chatId: chat.Id);

                // declare message
                var msg1 = "You have been automatically registered, one moment please";

                // send message notifying they have been registered
                var reqAction1 = new SendMessage(chatId: chat.Id, text: msg1);

                // send message
                Bot.MakeRequestAsync(request: reqAction1);
            }

            // set up regex pattern matching sequences.
            var ip = new Regex(pattern: @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            var ip2 = new Regex(pattern: @"[a-zA-Z0-9]{1,20}\.[a-zA-Z0-9]{1,20}\.[a-zA-Z0-9]{1,20}");
            var ip3 = new Regex(pattern: @"[a-zA-Z0-9]{1,20}\.[a-zA-Z0-9]{1,20}");

            // scan list of submitted ip's for any valid sequences
            var result = ip.Matches(input: text).Cast<Match>().Select(selector: m => m.Value)
                .Concat(second: ip2.Matches(input: text).Cast<Match>().Select(selector: m => m.Value))
                .Concat(second: ip3.Matches(input: text).Cast<Match>().Select(selector: m => m.Value)).ToArray();

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
                        var bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);
                        var req = new SendMessage(chatId: chat.Id, text: "One of the nodes you have submitted is invalid, or has not been accepted into the supernode program yet, or it has not recieved its first payment. The invalid node was node registered. Please check your nodes and try again");
                        bot.MakeRequestAsync(request: req);
                        continue;
                    }
                    validNodes.Add(item: node);
                }

            // if the user wants to register a node
            if (text.StartsWith(value: "/registerNode:") && text != "/registerNode:")
            {
                // automatically add the deposit account of each registered node as a monitored account
                // nodes must be cross referenced with total supernode list to acquire the deposit address
                // as the supernode API doesnt contain this information
                string msg1;
                try
                {
                    AccountUtils.AddAccount(
                         chatId: chat.Id,
                         accounts: nodes.Where(predicate: x => validNodes.Any(predicate: y => y.Ip == x.Ip)).ToList()
                         .Select(selector: node => node.PayoutAddress).ToList());

                    var nodesAdded = NodeUtils.AddNode(chatId: chat.Id, nodes: validNodes);


                    // return a message showing which accounts were registered
                    msg1 = nodes.Count > 0
                        ? nodesAdded.Aggregate(seed: "Nodes registered: \n \n", func: (current, n) => current + n.Ip + "\n")
                        : "No nodes were added. It/they may be offline or have an invalid IP. Check your node ip's and try again";

                    // send message
                   
                }
                catch (Exception e)
                {
                    Console.WriteLine(value: e);
                    msg1 = "Something went wrong, please try again.";
                }

                var reqAction1 = new SendMessage(chatId: chat.Id, text: msg1);
                Bot.MakeRequestAsync(request: reqAction1);

            }

            // if a user wants to unregister an account
            if (text.StartsWith(value: "/unregisterNode:") && text != "/unregisterNode:")
            {
                string msg2;
                try
                {
                    // declare message assuming nothing goes wrong
                    msg2 = result.Length > 1 ? "Your nodes were removed" : "Your node was removed";

                    // make sure the user is registered
                    if (UserUtils.GetUser(chatId: chat.Id)?.ChatId != chat.Id)
                    {
                        // if not, tell them
                        var reqAction3 = new SendMessage(chatId: chat.Id, text: "You are not registered");
                        Bot.MakeRequestAsync(request: reqAction3);
                        return;
                    }

                    // get all user nodes
                    var userNodes = NodeUtils.GetNodeByUser(chatId: chat.Id);

                    // delete any nodes submitted
                    NodeUtils.DeleteNode(chatId: chat.Id, nodes: result.ToList());

                    // delete any associated deposit accounts that would have been automatically registered
                    
                    AccountUtils.DeleteAccount(chatId: chat.Id,
                        accounts: userNodes.Where(predicate: y => AccountUtils.GetAccountByUser(chatId: chat.Id)
                                    .Any(predicate: x => x.EncodedAddress == y.DepositAddress))
                                    .Where(predicate: y => result.Any(predicate: x => x == y.IP))
                                    .Select(selector: acc => acc.DepositAddress).ToList());
                }
                catch (Exception)
                {
                    msg2 = "Something went wrong. Please try again. If the problem persists, please notify kodtycoon";
                }
                
                // send a message to notify user of any changes
                var reqAction2 = new SendMessage(chatId: chat.Id, text: msg2);
                Bot.MakeRequestAsync(request: reqAction2);
            }
        }
    }
}

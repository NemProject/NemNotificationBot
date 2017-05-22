using System;
using System.Collections.Generic;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CSharp2nem;
using SupernodeScanner2._0.DataContextModel;
using SupernodeScanner2._0.DBControllers;
using SupernodeScanner2._0.Utils;
using SupernodeScanner2._0.Scanners.TaskRunners;
using SupernodeScanner2._0;

namespace SuperNodeScanner
{
    internal class TelegramScanner
    {

        private bool stopMe = false;

        internal async void RunBot()
        {
            
            // get access key from congif
            var Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]); // insert access token

            // get "me"
            var me = Bot.MakeRequestAsync(request: new GetMe()).Result;

            // if me is null, connection failed, maybe incorrect access token
            if (me == null)
            {
                Console.WriteLine(value: "GetMe() FAILED. Do you forget to add your AccessToken to App.config?");
                Console.WriteLine(value: "(Press ENTER to quit)");
                Console.ReadLine();
                return;
            }

            // print out that connection worked
            Console.WriteLine(format: "{0} (@{1}) connected!", arg0: me.FirstName, arg1: me.Username);

            Console.WriteLine();
            Console.WriteLine(format: "Find @{0} in Telegram and send him a message - it will be displayed here", arg0: me.Username);
            Console.WriteLine(value: "(Press ENTER to stop listening and quit)");

            // set message update offset
            long offset = 0;

            // run continuously
            while (!stopMe)
            {
                Update[] updates;

                try
                {
                    updates = Bot.MakeRequestAsync(request: new GetUpdates() {Offset = offset}).Result;
                }
                catch (Exception)
                {
                    Bot = new TelegramBot(accessToken: ConfigurationManager.AppSettings[name: "accessKey"]);
                    continue;
                }

                // if none, start next iteration of the loop
                if (updates == null) continue;

                // repeat following for each update
                foreach (var update in updates)
                {
                    // declare the first update as checked
                    offset = update.UpdateId + 1;
                    
                    // go to next iteration of loop if message is null
                    if (update.Message == null)
                    {
                        continue;
                    }
                   
                    // get the message text
                    var text = update.Message.Text;

                    // if empty or null, reiterate loop
                    if (string.IsNullOrEmpty(value: text))
                    {
                        continue;
                    }
                    
                    // if message is to register or unregister a node do the following
                    if (text.StartsWith(value: "/registerNode:") && text != "/registerNode:" ||
                        text.StartsWith(value: "/unregisterNode:") && text != "/unregisterNode:")
                    {
                        // send message to let the user know the bot is working
                        var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "One moment please..");

                        await Bot.MakeRequestAsync(request: reqAction);

                        var t = Task.Run(action: () => new NodeManagement().ManageNodes(chat: update.Message.Chat, text: text));
                        continue;
                    }
                    
                    if (text == "/dailySummary" || text == "/sevenDaySummary" || text == "/thirtyOneDaySummary" || text.StartsWith(value: "/customSummary:"))
                    {
                        var t = Task.Run(action: () => new SummaryCreator().GetSummary(text: text, chat: update.Message.Chat));

                        continue;
                    }
                    
                    // if a user wants to register an account, not linked to a supernode
                    if (text.StartsWith(value: "/registerAccount:") && text != "/registerAccount:")
                    {
                        var t = Task.Run(action: () => new AccountTask().RegisterAccounts(message: update.Message));
                        continue;
                    }

                    // if a user wants to unregister an account
                    if (text.StartsWith(value: "/unregisterAccount:") && text != "/unregisterAccount:") 
                    {
                        var t = Task.Run(action: () => new AccountTask().UnregisterAccount(message: update.Message, text: text));
                        continue;
                    }

                    if (text.StartsWith(value: "/optInTxsAcc:") && text != "/optInTxsAcc:")
                    {
                        OptIOAccountUtils.OptInTx(message: update.Message);
                       
                        continue;
                    }
                    if (text.StartsWith(value: "/optOutTxsAcc:") && text != "/optOutTxsAcc:")
                    {
                        OptIOAccountUtils.OptOutTx(message: update.Message);

                        continue;
                    }

                    if (text.StartsWith(value: "/optInHarvestingAcc:") && text != "/optInHarvestingAcc:")
                    {
                        OptIOAccountUtils.OptInHarvesting(message: update.Message);

                        continue;
                    }
                    if (text.StartsWith(value: "/optOutHarvestingAcc:") && text != "/optOutHarvestingAcc:")
                    {
                        OptIOAccountUtils.OptOutHarvesting(message: update.Message);

                        continue;
                    }

                    switch (text)
                    {
                        case "/registerAccount:":
                            {
                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "To register an account, use the command \"/registerAccount:\" followed by a comma delimited list of accounts");

                                Bot.MakeRequestAsync(request: reqAction);

                                continue;
                            } 
                        case "/unregisterAccount:":
                            {
                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "To unregister an account, use the commmand \"/unregisterAccount:\" followed by a comma delimited list of accounts");

                                Bot.MakeRequestAsync(request: reqAction);

                                continue;
                            }
                        case "/registerNode:":
                            {
                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "To register a node, use the command \"/registerNode:\" followed by a comma delimited list of IP addresses. Addresses consisting of characters are also supported. eg. bob.nem.ninja");

                                Bot.MakeRequestAsync(request: reqAction);

                                continue;
                            }
                        case "/unregisterNode:":
                            {
                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "To unregister a node, use the commmand \"/unregisterNode:\" followed by a comma delimited list of IP addresses");

                                Bot.MakeRequestAsync(request: reqAction);

                                continue;
                            }
                        case "/deleteAccount":
                            {
                                UserUtils.DeleteUser(userName: update.Message.From.Username, chatId: update.Message.Chat.Id);

                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "You Account has been removed");

                                Bot.MakeRequestAsync(request: reqAction);

                                continue;
                            }
                        case "/start":
                            {
                                UserUtils.AddUser(userName: update.Message.From.Username, chatId: update.Message.Chat.Id);

                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id,
                                    text: "Hello. \n\n" +
                                    "Please start by registering a supernode or NEM acccount. \n" +
                                    "When you register a supernode, the deposit account of the supernode is" +
                                    " automatically registered under your username and you will start to " +
                                    "recieve notifications about failed supernode tests, any transactions " +
                                    "associated with the deposit account as well as any blocks the deposit account of each node harvests\n\n" +
                                    "If you dont want to get notifications about the supernode depsoit account, simply unregister the " +
                                    "account by using the \"/unregisterAccount:\" command followed by the account address you wish to unregister." +
                                    "This does not unregister your supernode, rather, only the deposit account associated with it\n\n" +
                                    "You can also in opt out of specific notification types for each node or NEM account you have registered. " +
                                    "Check out the \"/optIO\" command for more details. You are automatically opted in for all notifications when you register a node or nem account.\n\n" +
                                    "Use the \"/myDetails\" command to see the nodes and accounts you have registered, what notifications they are signed up for and some additional information. \n\n" 
                                    )
                                { ReplyMarkup = KeyBoards.MainMenu };

                                Bot.MakeRequestAsync(request: reqAction);

                                continue;
                            }
                        
                        case "/myDetails":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id, text: "One moment please..");

                                Bot.MakeRequestAsync(request: req);

                                var t = Task.Run(action: () => new MyDetailsTask().ReturnMyDetails(message: update.Message));

                                continue;
                            }
                        case "/summary":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id, 
                                    text: "Use the commands below to generate a summary for your accounts. " +
                                    "Include a number after custom summary to get a summary of any given" +
                                    " days up to the current day")
                                { ReplyMarkup = KeyBoards.SummaryMenu };

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/help":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id,
                                    text: "Hello.\n\n" +
                                    "Check out the commands below to see what you can do, or check out the blog at <link> for more information \n\n" +
                                    "When you register a supernode, the deposit account of the supernode is" +
                                    " automatically registered under your username and you will start to " +
                                    "recieve notifications about failed supernode tests, any transactions " +
                                    "associated with the deposit account as well as any blocks the deposit account of each node harvests\n\n" +
                                    "If you dont want to get notifications about the supernode depsoit account, simply unregister the " +
                                    "associated deposit account by using the \"/unregisterAccount:\" command followed by the account address you wish to unregister." +
                                    "This does not unregister your supernode, rather, only the deposit account associated with it\n\n" +
                                    "You can also in opt out of specific notification types for each node or NEM account you have registered. " +
                                    "Check out the \"/optIO\" command for more details. You are automatically opted in for all notifications when you register a node or nem account.\n\n" +
                                    "Use the \"/myDetails\" command to see the nodes and accounts you have registered, what notifications they are signed up for and some additional information")
                                { ReplyMarkup = KeyBoards.MainMenu };

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/back":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id, text: "Main menu") { ReplyMarkup = KeyBoards.MainMenu };

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optIO":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id,
                                    text: "Use any of the commands below to opt in or out of any particular notification types. " +
                                    "You can either opt in or out of notification types globally for all accounts registered to you" +
                                    " or selectively per account.")
                                { ReplyMarkup = KeyBoards.OptMenu };

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optInTxsAcc:":
                        {
                                var req = new SendMessage(chatId: update.Message.Chat.Id, 
                                    text: "To opt into transaction notifications for a given account or accounts, use the \"/optInTxsAcc:\" command, " +
                                    "followed by a list of comma delimeted addresses ");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                        }
                        case "/optOutTxsAcc:":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id,
                                    text: "To opt out of transaction notifications for a given account or accounts, " +
                                    "use the \"/optOutTxsAcc:\" command, followed by a list of comma delimeted addresses ");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optInHarvestingAcc:":
                            {                                
                                var req = new SendMessage(chatId: update.Message.Chat.Id, 
                                    text: "To opt into harvesting notifications for a given account or accounts, " +
                                    "use the \"/optInHarvestingAcc:\" command, followed by a list of comma delimeted addresses ");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optOutHarvestingAcc:":
                            {
                                var req = new SendMessage(chatId: update.Message.Chat.Id, 
                                    text: "To opt out of harvesting notifications for a given account or accounts, " +
                                    "use the \"/optOutHarvestingAcc:\" command, followed by a list of comma delimeted addresses ");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optInTxsGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(chatId: update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckTxs = true;
                                }

                                AccountUtils.UpdateAccount(accs: accs, user: update.Message.Chat.Id);

                                var req = new SendMessage(chatId: update.Message.Chat.Id, 
                                    text: "You have opted into transaction notifications");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optOutTxsGlobal":
                            {
                                    var accs = AccountUtils.GetAccountByUser(chatId: update.Message.Chat.Id);

                                    foreach (var acc in accs)
                                    {
                                        acc.CheckTxs = false;

                                    }

                                    AccountUtils.UpdateAccount(accs: accs, user: update.Message.Chat.Id);

                                    var req = new SendMessage(chatId: update.Message.Chat.Id,
                                        text: "You have opted out of transaction notifications");

                                    Bot.MakeRequestAsync(request: req);

                                    continue;
                                                             
                            }
                        case "/optInHarvestingGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(chatId: update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckBlocks = true;

                                }

                                AccountUtils.UpdateAccount(accs: accs, user: update.Message.Chat.Id);      

                                var req = new SendMessage(chatId: update.Message.Chat.Id, text: "You have opted into harvesting notifications");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        case "/optOutHarvestingGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(chatId: update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckBlocks = false;

                                }

                                AccountUtils.UpdateAccount(accs: accs, user: update.Message.Chat.Id);

                                var req = new SendMessage(chatId: update.Message.Chat.Id, text: "You have opted out of harvesting notifications");

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }        
                        case "/harvestingSpace":
                            {
                                var reqAction = new SendMessage(chatId: update.Message.Chat.Id, text: "One moment please..");

                                Bot.MakeRequestAsync(request: reqAction);

                                var freeNodes = new List<string>();

                                var nodeClient = new NodeClient();

                                var nodes = nodeClient.SuperNodeList().Result.Nodes;

                                var r = new Random();

                                for (var index = r.Next(minValue: 0, maxValue: 320); index < 400; index++)
                                {

                                    var node = nodes[index: index];

                                    nodeClient.Connection.SetHost(host: node.Ip);

                                    var result = await nodeClient.UnlockedInfo();

                                    if (result.NumUnlocked < result.MaxUnlocked) freeNodes.Add(item: node.Ip);
                                    
                                    if (freeNodes.Count == 3) break;
                                }

                                var message = string.Join(separator: "\n", values: freeNodes);

                                var req = new SendMessage(chatId: update.Message.Chat.Id, text: message);

                                Bot.MakeRequestAsync(request: req);

                                continue;
                            }
                        default:
                        {
                                

                                var req = new SendMessage(chatId: update.Message.Chat.Id, text: "Main menu") { ReplyMarkup = KeyBoards.MainMenu };

                                Bot.MakeRequestAsync(request: req);

                                continue;
                        }
                            
                    }
                }

            }
        }      
    }

    internal class NodeIndexPair
    {
        internal SuperNodes.Node node { get; set; }
        internal int index { get; set; }
    }

}

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

namespace SuperNodeScanner
{
    internal class TelegramScanner
    {

        private bool stopMe = false;

        internal async void RunBot()
        {
            
            // get access key from congif
            var Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]); // insert access token

            // get "me"
            var me = Bot.MakeRequestAsync(new GetMe()).Result;

            // if me is null, connection failed, maybe incorrect access token
            if (me == null)
            {
                Console.WriteLine("GetMe() FAILED. Do you forget to add your AccessToken to App.config?");
                Console.WriteLine("(Press ENTER to quit)");
                Console.ReadLine();
                return;
            }

            // print out that connection worked
            Console.WriteLine("{0} (@{1}) connected!", me.FirstName, me.Username);

            Console.WriteLine();
            Console.WriteLine("Find @{0} in Telegram and send him a message - it will be displayed here", me.Username);
            Console.WriteLine("(Press ENTER to stop listening and quit)");

            // set message update offset
            long offset = 0;

            // run continuously
            while (!stopMe)
            {
                Update[] updates;

                try
                {
                    updates = Bot.MakeRequestAsync(new GetUpdates() {Offset = offset}).Result;
                }
                catch (Exception)
                {
                    Bot = new TelegramBot(ConfigurationManager.AppSettings["accessKey"]);
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
                    if (string.IsNullOrEmpty(text))
                    {
                        continue;
                    }
                    
                    // if message is to register or unregister a node do the following
                    if (text.StartsWith("/registerNode:") && text != "/registerNode:" ||
                        text.StartsWith("/unregisterNode:") && text != "/unregisterNode:")
                    {
                        // send message to let the user know the bot is working
                        var reqAction = new SendMessage(update.Message.Chat.Id, "One moment please..");

                        await Bot.MakeRequestAsync(reqAction);

                        var t = Task.Run(() => new NodeManagement().ManageNodes(update.Message.Chat, text));
                        continue;
                    }
                    
                    if (text == "/dailySummary" || text == "/sevenDaySummary" || text == "/thirtyOneDaySummary" || text.StartsWith("/customSummary:"))
                    {
                        // send message to let the user know the bot is working
                        var reqAction = new SendMessage(update.Message.Chat.Id, "One moment please..");

                        await Bot.MakeRequestAsync(reqAction);

                        var t = Task.Run(() => new SummaryCreator().GetSummary(text, update.Message.Chat));

                        continue;
                    }
                    
                    // if a user wants to register an account, not linked to a supernode
                    if (text.StartsWith("/registerAccount:") && text != "/registerAccount:")
                    {
                        var t = Task.Run(() => new AccountTask().RegisterAccounts(update.Message));
                        continue;
                    }

                    // if a user wants to unregister an account
                    if (text.StartsWith("/unregisterAccount:") && text != "/unregisterAccount:") 
                    {
                        var t = Task.Run(() => new AccountTask().UnregisterAccount(update.Message, text));
                        continue;
                    }

                    if (text.StartsWith("/optInTxsAcc:") && text != "optInTxsAcc:")
                    {
                        OptIOAccountUtils.OptInTx(update.Message);
                       
                        continue;
                    }
                    if (text.StartsWith("/optOutTxsAcc:") && text != "optOutTxsAcc:")
                    {
                        OptIOAccountUtils.OptOutTx(update.Message);

                        continue;
                    }

                    if (text.StartsWith("/optInHarvestingAcc:") && text != "opInHarvestingAcc:")
                    {
                        OptIOAccountUtils.OptInHarvesting(update.Message);

                        continue;
                    }
                    if (text.StartsWith("/optOutHarvestingAcc:") && text != "optOutHarvestingAcc:")
                    {
                        OptIOAccountUtils.OptOutHarvesting(update.Message);

                        continue;
                    }

                    switch (text)
                    {
                        case "/registerAccount:":
                            {
                                var reqAction = new SendMessage(update.Message.Chat.Id, "To register an account, use the command \"/registerAccount:\" followed by a comma delimited list of accounts");

                                Bot.MakeRequestAsync(reqAction);

                                continue;
                            } 
                        case "/unregisterAccount:":
                            {
                                var reqAction = new SendMessage(update.Message.Chat.Id, "To unregister an account, use the commmand \"/unregisterAccount:\" followed by a comma delimited list of accounts");

                                Bot.MakeRequestAsync(reqAction);

                                continue;
                            }
                        case "/registerNode:":
                            {
                                var reqAction = new SendMessage(update.Message.Chat.Id, "To register a node, use the command \"/registerNode:\" followed by a comma delimited list of IP addresses. Addresses consisting of characters are also supported. eg. bob.nem.ninja");

                                Bot.MakeRequestAsync(reqAction);

                                continue;
                            }
                        case "/unregisterNode:":
                            {
                                var reqAction = new SendMessage(update.Message.Chat.Id, "To unregister a node, use the commmand \"/unregisterNode:\" followed by a comma delimited list of IP addresses");

                                Bot.MakeRequestAsync(reqAction);

                                continue;
                            }
                        case "/deleteAccount":
                            {
                                UserUtils.DeleteUser(update.Message.From.Username, update.Message.Chat.Id);

                                var reqAction = new SendMessage(update.Message.Chat.Id, "You Account has been removed");

                                Bot.MakeRequestAsync(reqAction);

                                continue;
                            }
                        case "/start":
                            {
                                UserUtils.AddUser(update.Message.From.Username, update.Message.Chat.Id);

                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/registerNode:"), new KeyboardButton("/unregisterNode:") },
                                    new[] { new KeyboardButton("/registerAccount:"), new KeyboardButton("/unregisterAccount:") },
                                    new[] { new KeyboardButton("/optIO"), new KeyboardButton("/harvestingSpace") },
                                    new[] { new KeyboardButton("/help") , new KeyboardButton("/myDetails") },
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var reqAction = new SendMessage(update.Message.Chat.Id,
                                    "Hello. \n\n" +
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
                                { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(reqAction);

                                continue;
                            }
                        
                        case "/myDetails":
                            {
                                var req = new SendMessage(update.Message.Chat.Id, "One moment please..");

                                Bot.MakeRequestAsync(req);

                                var t = Task.Run(() => new MyDetailsTask().ReturnMyDetails(update.Message));

                                continue;
                            }
                        case "/help":
                            {

                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/registerNode:"), new KeyboardButton("/unregisterNode:") },
                                    new[] { new KeyboardButton("/registerAccount:"), new KeyboardButton("/unregisterAccount:") },
                                    new[] { new KeyboardButton("/optIO"), new KeyboardButton("/harvestingSpace") },
                                    new[] { new KeyboardButton("/help") , new KeyboardButton("/myDetails") },
                                    
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id,
                                    "Hello.\n\n" +
                                    "You can now chose between registering a supernode or NEM acccount. Check out the commands below on how to do that \n\n" +
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
                                { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/back":
                            {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/registerNode:"), new KeyboardButton("/unregisterNode:") },
                                    new[] { new KeyboardButton("/registerAccount:"), new KeyboardButton("/unregisterAccount:") },
                                    new[] { new KeyboardButton("/optIO"), new KeyboardButton("/harvestingSpace") },
                                    new[] { new KeyboardButton("/help") , new KeyboardButton("/myDetails") },
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "Main menu") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optIO":
                            {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },                     
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },     
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id,
                                    "Use any of the commands below to opt in or out of any particular notification types. " +
                                    "You can either opt in or out of notification types globally for all accounts registered to you" +
                                    " or selectively per account.")
                                { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optInTxsAcc:":
                        {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "To opt into transaction notifications for a given account or accounts, use the \"/optInTxsAcc:\" command, followed by a list of comma delimeted addresses ") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                        }
                        case "/optOutTxsAcc:":
                            {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "To opt out of transaction notifications for a given account or accounts, use the \"/optOutTxsAcc:\" command, followed by a list of comma delimeted addresses ") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optInHarvestingAcc:":
                            {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "To opt into harvesting notifications for a given account or accounts, use the \"/optInHarvestingAcc:\" command, followed by a list of comma delimeted addresses ") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optOutHarvestingAcc:":
                            {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "To opt out of harvesting notifications for a given account or accounts, use the \"/optOutHarvestingAcc:\" command, followed by a list of comma delimeted addresses ") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optInTxsGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckTxs = true;
                                }

                                AccountUtils.UpdateAccount(accs);

                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "You have opted into transaction notifications") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optOutTxsGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckTxs = false;

                                }

                                AccountUtils.UpdateAccount(accs);

                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "You have opted out of transaction notifications") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optInHarvestingGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckBlocks = true;

                                }

                                AccountUtils.UpdateAccount(accs);

                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "You have opted into harvesting notifications") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        case "/optOutHarvestingGlobal":
                            {
                                var accs = AccountUtils.GetAccountByUser(update.Message.Chat.Id);

                                foreach (var acc in accs)
                                {
                                    acc.CheckBlocks = false;

                                }

                                AccountUtils.UpdateAccount(accs);

                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/optInTxsGlobal") ,  new KeyboardButton("/optOutTxsGlobal") },
                                    new[] { new KeyboardButton("/optInHarvestingGlobal"), new KeyboardButton("/optOutHarvestingGlobal") },
                                    new[] { new KeyboardButton("/optInTxsAcc:") ,  new KeyboardButton("/optOutTxsAcc:") },
                                    new[] { new KeyboardButton("/optOutHarvestingAcc:") ,  new KeyboardButton("/optOutHarvestingAcc:") },
                                    new[] { new KeyboardButton("/back")}
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "You have opted out of harvesting notifications") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

                                continue;
                            }

                        case "/harvestingSpace":
                            {
                                var reqAction = new SendMessage(update.Message.Chat.Id, "One moment please..");

                                Bot.MakeRequestAsync(reqAction);

                                var freeNodes = new List<string>();

                                var nodeClient = new NodeClient();

                                var nodes = nodeClient.SuperNodeList().Result.Nodes;

                                var r = new Random();

                                for (var index = r.Next(0, 320); index < 400; index++)
                                {

                                    var node = nodes[index];

                                    nodeClient.Connection.SetHost(node.Ip);

                                    var result = await nodeClient.UnlockedInfo();

                                    if (result.NumUnlocked < result.MaxUnlocked) freeNodes.Add(node.Ip);
                                    
                                    if (freeNodes.Count == 3) break;
                                }

                                var message = string.Join("\n", freeNodes);

                                var req = new SendMessage(update.Message.Chat.Id, message);

                                Bot.MakeRequestAsync(req);

                                continue;
                            }
                        default:
                        {
                                var keyb = new ReplyKeyboardMarkup()
                                {
                                    Keyboard = new[] {
                                    new[] { new KeyboardButton("/registerNode:"), new KeyboardButton("/unregisterNode:") },
                                    new[] { new KeyboardButton("/registerAccount:"), new KeyboardButton("/unregisterAccount:") },
                                    new[] { new KeyboardButton("/optIO"), new KeyboardButton("/harvestingSpace") },
                                    new[] { new KeyboardButton("/help") , new KeyboardButton("/myDetails") },
                                },
                                    OneTimeKeyboard = true,
                                    ResizeKeyboard = true
                                };

                                var req = new SendMessage(update.Message.Chat.Id, "Main menu") { ReplyMarkup = keyb };

                                Bot.MakeRequestAsync(req);

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

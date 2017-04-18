using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTelegramBotApi.Types;

namespace SupernodeScanner2._0
{
    internal static class KeyBoards
    {
        internal static ReplyKeyboardMarkup OptMenu  = new ReplyKeyboardMarkup()
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

        internal static ReplyKeyboardMarkup MainMenu = new ReplyKeyboardMarkup()
        {
            Keyboard = new[]
                            {
                                new[] { new KeyboardButton("/registerNode:"), new KeyboardButton("/unregisterNode:") },
                                new[] { new KeyboardButton("/registerAccount:"), new KeyboardButton("/unregisterAccount:") },
                                new[] { new KeyboardButton("/optIO"), new KeyboardButton("/harvestingSpace") },
                                new[] { new KeyboardButton("/summary"), new KeyboardButton("/myDetails") },
                                new[] { new KeyboardButton("/help") }
                            },
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        internal static ReplyKeyboardMarkup SummaryMenu = new ReplyKeyboardMarkup()
        {
            Keyboard = new[]
                            {
                                new[] { new KeyboardButton("/dailySummary"), new KeyboardButton("/sevenDaySummary") },
                                new[] { new KeyboardButton("/thirtyOneDaySummary"), new KeyboardButton("/customSummary:") },
                                new[] { new KeyboardButton("/back") }
                            },
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

    }
}

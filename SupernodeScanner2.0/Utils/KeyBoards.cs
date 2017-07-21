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
                                new[] { new KeyboardButton(text: "/optInTxsGlobal") ,  new KeyboardButton(text: "/optOutTxsGlobal") },
                                new[] { new KeyboardButton(text: "/optInHarvestingGlobal"), new KeyboardButton(text: "/optOutHarvestingGlobal") },
                                new[] { new KeyboardButton(text: "/optInTxsAcc:") ,  new KeyboardButton(text: "/optOutTxsAcc:") },
                                new[] { new KeyboardButton(text: "/optInHarvestingAcc:") ,  new KeyboardButton(text: "/optOutHarvestingAcc:") },
                                new[] { new KeyboardButton(text: "/back")}
                             },
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        internal static ReplyKeyboardMarkup MainMenu = new ReplyKeyboardMarkup()
        {
            Keyboard = new[]
                            {
                                new[] { new KeyboardButton(text: "/registerNode:"), new KeyboardButton(text: "/unregisterNode:") },
                                new[] { new KeyboardButton(text: "/registerAccount:"), new KeyboardButton(text: "/unregisterAccount:") },
                                new[] { new KeyboardButton(text: "/optIO"), new KeyboardButton(text: "/harvestingSpace") },
                                new[] { new KeyboardButton(text: "/summary"), new KeyboardButton(text: "/myDetails") },
                                new[] { new KeyboardButton(text: "/help") }
                            },
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        internal static ReplyKeyboardMarkup SummaryMenu = new ReplyKeyboardMarkup()
        {
            Keyboard = new[]
                            {
                                new[] { new KeyboardButton(text: "/dailySummary"), new KeyboardButton(text: "/sevenDaySummary") },
                                new[] { new KeyboardButton(text: "/thirtyOneDaySummary"), new KeyboardButton(text: "/customSummary:") },
                                new[] { new KeyboardButton(text: "/back") }
                            },
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

    }
}

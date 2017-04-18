using System;
using System.Threading.Tasks;
using SupernodeScanner2._0.Scanners;
using Timer = System.Timers.Timer;

namespace SuperNodeScanner
{
    internal class Program
    {
        private static TelegramScanner TelegramScanner { get; set; }
        private static NodeScanner NodeScanner { get; set; }
        private static HarvestedBlockScanner BlockScanner { get; set; }
        private static TransactionScanner TransactionScanner { get; set; }  
        private static Task T2 { get; set; }
        private static Task Ts { get; set; }
        private static Task Ns { get; set; }
        private static Task Bs { get; set; }
        private static void Main(string[] args)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
         
            TelegramScanner = new TelegramScanner();
            T2 = Task.Run(() => TelegramScanner.RunBot());

            NodeScanner = new NodeScanner();
            Ns = Task.Run(() => NodeScanner.TestNodes());

            TransactionScanner = new TransactionScanner();
            Ts = Task.Run(() => TransactionScanner.ScanAccounts());

            BlockScanner = new HarvestedBlockScanner();
            Bs = Task.Run(() => BlockScanner.ScanBlocks());

            Console.ReadKey();
        }
    }
}

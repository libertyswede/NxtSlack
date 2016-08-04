using System.Linq;
using System;
using NxtLib.Blocks;
using NxtLib.ServerInfo;
using System.Threading;

namespace NxtSlack
{
    public class Program
    {
        private const string ServerAddress = "http://node1.ardorcrypto.com:7876/nxt";

        public static void Main(string[] args)
        {
            var serverInfoService = new ServerInfoService(ServerAddress);
            var blockChainStatus = serverInfoService.GetBlockchainStatus().Result;
            var lastHeight = blockChainStatus.NumberOfBlocks - 1;
            Console.WriteLine($"Starting up Nxt Slack Integration Program from height {lastHeight}");

            while (true)
            {
                lastHeight = ScanBlockchain(blockChainStatus.NumberOfBlocks - 1, lastHeight);
                Thread.Sleep(10000);

                blockChainStatus = serverInfoService.GetBlockchainStatus().Result;
            }
        }

        private static int ScanBlockchain(int currentHeight, int lastHeight)
        {
            var blockService = new BlockService(ServerAddress);
            while (currentHeight > lastHeight)
            {
                var blockReply = blockService.GetBlockIncludeTransactions(BlockLocator.ByHeight(++lastHeight)).Result;
                Console.WriteLine($"New block detected @ height: {blockReply.Height} has {blockReply.Transactions.Count} transaction(s)");

                foreach (var transaction in blockReply.Transactions.Where(t => t.Message != null && t.Message.IsText))
                {
                    Console.WriteLine($"----------------------------------------");
                    Console.WriteLine($"Transaction: {transaction.TransactionId}");
                    Console.WriteLine($"TransactionType: {transaction.SubType}");
                    Console.WriteLine($"From: {transaction.SenderRs}");
                    Console.WriteLine($"To: {transaction.RecipientRs}");
                    Console.WriteLine($"Message: {transaction.Message.MessageText}");
                    Console.WriteLine($"----------------------------------------");
                }
            }
            return lastHeight;
        }
    }
}

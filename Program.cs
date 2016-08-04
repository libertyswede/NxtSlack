using System.Linq;
using System;
using NxtLib;
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
                Console.WriteLine($"New block detected @ height: {blockReply.Height}");

                foreach (var transaction in blockReply.Transactions.Where(t => t.SubType == TransactionSubType.MessagingArbitraryMessage && 
                                                                               t.Message != null && t.Message.IsText))
                {
                    Console.WriteLine($"Message from {transaction.SenderRs} to {transaction.RecipientRs}");
                    Console.WriteLine($"{transaction.Message.MessageText}");
                }
            }
            return lastHeight;
        }
    }
}

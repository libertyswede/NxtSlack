using System.Linq;
using System;
using NxtLib.Blocks;
using NxtLib.ServerInfo;
using System.Threading;
using System.Collections.Generic;
using NxtLib;

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
                foreach (var transaction in FilterTransactions(blockReply.Transactions))
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

        private const ulong superBTCDAssetId = 6918149200730574743UL;
        private const ulong superBTCAssetId = 12659653638116877017UL;
        private static readonly HashSet<ulong> IgnoredAssetIds = new HashSet<ulong>(new [] { superBTCAssetId, superBTCDAssetId });

        private const ulong dracoCurrencyId = 9340369183481620469UL;
        private static readonly HashSet<ulong> IgnoredCurrencyIds = new HashSet<ulong>(new [] {dracoCurrencyId});

        private static IEnumerable<Transaction> FilterTransactions(List<Transaction> transactions)
        {
            var query = transactions.Where(t => t.Message != null && t.Message.IsText);
            
            foreach (var assetTransferTransaction in query.Where(t => t.SubType == TransactionSubType.ColoredCoinsAssetTransfer))
            {
                var attachment = (ColoredCoinsAssetTransferAttachment)assetTransferTransaction.Attachment;
                if (IgnoredAssetIds.Contains(attachment.AssetId))
                {
                    query = query.Where(t => t.TransactionId != assetTransferTransaction.TransactionId);
                }
            }
            foreach (var msTransferTransaction in query.Where(t => t.SubType == TransactionSubType.MonetarySystemCurrencyTransfer))
            {
                var attachment = (MonetarySystemCurrencyTransferAttachment)msTransferTransaction.Attachment;
                if (IgnoredCurrencyIds.Contains(attachment.CurrencyId))
                {
                    query = query.Where(t => t.TransactionId != msTransferTransaction.TransactionId);
                }
            }

            return query;
        }
    }
}

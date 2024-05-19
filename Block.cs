using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlockchainAssignment
{
    public class Block
    {
        /* Block Variables */
        public DateTime timestamp;

        public int index,difficulty;

        public String prevHash,
            hash,
            merkleRoot,
            minerAddress;

        public List<Transaction> transactionList;

        public long nonce;

        public double reward;

        // Semaphore to control access to the nonce. This ensures that only one thread can modify at one time.
        private static SemaphoreSlim nonceSemaphore = new SemaphoreSlim(1, 1);

        // Target block time in seconds
        private int targetBlockTime = 10;

        // List to store block mining times for dynamic difficulty adjustment
        private List<double> miningTimes = new List<double>();

        /* Genesis block constructor */
        public Block(bool miningOperationTriggered = false)
        {
            timestamp = DateTime.Now;
            index = 0;
            difficulty = 4; // Set the default difficulty
            transactionList = new List<Transaction>();

            // Only mine if the operation is explicitly triggered
            if (miningOperationTriggered)
            {
                hash = Mine();
            }
            else
            {
                // Set some default values or perform other initialization if needed
                hash = "GenesisBlockHash";
                merkleRoot = "GenesisMerkleRoot";
                minerAddress = "GenesisMinerAddress";
                reward = 100.0;
            }
        }

        /* New Block constructor */
        public Block(Block lastBlock, List<Transaction> transactions, String minerAddress)
        {
            timestamp = DateTime.Now;

            index = lastBlock.index + 1;
            prevHash = lastBlock.hash;

            this.minerAddress = minerAddress;
            reward = 100.0;
            transactions.Add(createRewardTransaction(transactions));
            transactionList = new List<Transaction>(transactions);

            merkleRoot = MerkleRoot(transactionList);
            difficulty = 5;
            hash = Mine();
        }

        public String CreateHash()
        {
            String hash = String.Empty;
            SHA256 hasher = SHA256Managed.Create();

            String input = timestamp.ToString() + index + prevHash + nonce + merkleRoot;

            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            foreach (byte x in hashByte)
                hash += String.Format("{0:x2}", x);

            return hash;
        }

        public String Mine()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            bool hashFound = false;
            string validHash = null;
            long validNonce = 0;

            // 24 threads on my pc
            int numberOfThreads = Environment.ProcessorCount; //1, 2, 4, 16 etc...
            var tasks = new List<Task>();
            // Define a cancellation token
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = cancellationTokenSource.Token;
            for (int i = 0; i < numberOfThreads; i++)
            {
                var task = Task.Run(() =>
                {
                    long threadNonce = GetStartingNonce(Task.CurrentId.HasValue ? Task.CurrentId.Value : 0);
                    string threadHash;
                    while (!hashFound && !token.IsCancellationRequested)
                    {
                        threadHash = CreateHashWithNonce(threadNonce);

                        if (threadHash.StartsWith(new string('0', difficulty)))
                        {
                            lock (nonceSemaphore)
                            {
                                if (!hashFound)
                                {
                                    hashFound = true;
                                    validHash = threadHash;
                                    validNonce = threadNonce;
                                    cancellationTokenSource.Cancel();  // Stop all other threads
                                }
                            }
                        }
                        threadNonce++;
                    }
                }, token);
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();
            nonce = validNonce;  // Update the nonce with the valid value found by the threads
            Console.WriteLine($"Block mined with hash: {validHash}");
            Console.WriteLine($"Mining time: {stopwatch.Elapsed.TotalSeconds}s");

            return validHash;
        }

        private string CreateHashWithNonce(long nonce)
        {
            string input = timestamp.ToString() + index + prevHash + nonce + merkleRoot;
            using (SHA256 hasher = SHA256Managed.Create())
            {
                byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder hash = new StringBuilder();
                foreach (byte x in hashByte)
                {
                    hash.Append(x.ToString("x2"));
                }
                return hash.ToString();
            }
        }



        private void AdjustDifficulty()
        {
            // Calculate moving average
            double movingAverage = miningTimes.Take(10).Reverse().Average();

            // Adjust difficulty based on moving average
            if (movingAverage < targetBlockTime && difficulty > 1)
            {
                difficulty--;
            }
            else if (movingAverage > targetBlockTime)
            {
                difficulty++;
            }
        }

        private long GetStartingNonce(int threadId)
        {
            long startingNonce = threadId; // Simple way to ensure a unique starting nonce
            return startingNonce;
        }

        // Merkle Root Algorithm
        public static String MerkleRoot(List<Transaction> transactionList)
        {
            List<String> hashes = transactionList.Select(t => t.hash).ToList();


            if (hashes.Count == 0)
            {
                return String.Empty;
            }
            if (hashes.Count == 1)
            {
                return HashCode.HashTools.combineHash(hashes[0], hashes[0]);
            }
            while (hashes.Count != 1)
            {
                List<String> merkleLeaves = new List<String>();

                for (int i = 0; i < hashes.Count; i += 2)
                {
                    if (i == hashes.Count - 1)
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i]));
                    }
                    else
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i + 1]));
                    }
                }
                hashes = merkleLeaves;
            }
            return hashes[0];
        }

        public Transaction createRewardTransaction(List<Transaction> transactions)
        {
            double fees = transactions.Aggregate(0.0, (acc, t) => acc + t.fee);
            return new Transaction("Mine Rewards", minerAddress, (reward + fees), 0, "");
        }

        /* Concatenate all properties to output to the UI */
        public override string ToString()
        {
            string proofType = IsPoSBlock() ? "PoS" : "PoW"; 

            return "[BLOCK START]"
                + "\nIndex: " + index
                + "\tTimestamp: " + timestamp
                + "\nPrevious Hash: " + prevHash
                + "\n------------" + proofType + "------------"
                + "\nDifficulty Level: " + difficulty
                + "\nNonce: " + nonce
                + "\nHash: " + hash
                + "\n-- Rewards --"
                + "\nReward: " + reward
                + "\nMiners Address: " + minerAddress
                + "\n-- " + transactionList.Count + " Transactions --"
                + "\nMerkle Root: " + merkleRoot
                + "\n" + String.Join("\n", transactionList)
                + "\n[BLOCK END]";
        }

        private bool IsPoSBlock()
        {
            // Check if the block has a validator address and a staked amount
            return !string.IsNullOrEmpty(minerAddress) && reward > 0;
        }
    }
}


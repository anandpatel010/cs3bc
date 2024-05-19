using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace BlockchainAssignment
{
    public partial class BlockchainApp : Form
    {
        public enum TransactionSelectionStrategy
        {
            Greedy,
            Altruistic,
            Random,
            AddressPreference
        }
        private Blockchain blockchain;
        private TransactionSelectionStrategy selectionStrategy = TransactionSelectionStrategy.Random;

        public BlockchainApp()
        {
            InitializeComponent();
            blockchain = new Blockchain();
            UpdateText("New blockchain initialised!");
        }

        private void UpdateText(String text)
        {
            output.Text = text;
        }

        private void ReadAll_Click(object sender, EventArgs e)
        {
            UpdateText(blockchain.ToString());
        }

        private void PrintBlock_Click(object sender, EventArgs e)
        {
            if (Int32.TryParse(blockNo.Text, out int index))
                UpdateText(blockchain.GetBlockAsString(index));
            else
                UpdateText("Invalid Block No.");
        }

        private void PrintPendingTransactions_Click(object sender, EventArgs e)
        {
            UpdateText(String.Join("\n", blockchain.transactionPool));
        }

        private void GenerateWallet_Click(object sender, EventArgs e)
        {
            Wallet.Wallet myNewWallet = new Wallet.Wallet(out string privKey);

            publicKey.Text = myNewWallet.publicID;
            privateKey.Text = privKey;
        }

        private void ValidateKeys_Click(object sender, EventArgs e)
        {
            if (Wallet.Wallet.ValidatePrivateKey(privateKey.Text, publicKey.Text))
                UpdateText("Keys are valid");
            else
                UpdateText("Keys are invalid");
        }

        private void CheckBalance_Click(object sender, EventArgs e)
        {
            UpdateText(blockchain.GetBalance(publicKey.Text).ToString() + " Assignment Coin");
        }

        private void CreateTransaction_Click(object sender, EventArgs e)
        {
            Transaction transaction = new Transaction(publicKey.Text, reciever.Text, Double.Parse(amount.Text), Double.Parse(fee.Text), privateKey.Text);
            blockchain.transactionPool.Add(transaction);
            UpdateText(transaction.ToString());
        }

        private void SingleThreadMineButton_Click(object sender, EventArgs e)
        {
            List<Transaction> transactions = blockchain.GetPendingTransactions();

            // Sort transactions based on the selected strategy
            SortTransactions(transactions);

            Block newBlock = new Block(blockchain.GetLastBlock(), transactions, publicKey.Text);
            blockchain.blocks.Add(newBlock);
            UpdateText($"Mining completed!\n{blockchain.ToString()}");
        }

        private void Validate_Click(object sender, EventArgs e)
        {
            if (blockchain.blocks.Count == 1)
            {
                if (!Blockchain.ValidateHash(blockchain.blocks[0]))
                    UpdateText("Blockchain is invalid");
                else
                    UpdateText("Blockchain is valid");
                return;
            }

            for (int i = 1; i < blockchain.blocks.Count - 1; i++)
            {
                if (
                    blockchain.blocks[i].prevHash != blockchain.blocks[i - 1].hash ||
                    !Blockchain.ValidateHash(blockchain.blocks[i]) ||
                    !Blockchain.ValidateMerkleRoot(blockchain.blocks[i])
                )
                {
                    UpdateText("Blockchain is invalid");
                    return;
                }
            }
            UpdateText("Blockchain is valid");
        }

        private void MultiThreadMineButton_Click(object sender, EventArgs e)
        {
            List<Transaction> transactions = blockchain.GetPendingTransactions();
            int numThreads = Environment.ProcessorCount;
            List<Thread> miningThreads = new List<Thread>();

            for (int i = 0; i < numThreads; i++)
            {
                Thread thread = new Thread(() => MineBlockInBackground(transactions, publicKey.Text));
                miningThreads.Add(thread);
                thread.Start();
            }

            foreach (var thread in miningThreads)
            {
                thread.Join();
            }

            UpdateText($"Mining completed!\n{blockchain.ToString()}");
        }

        private void MineBlockInBackground(List<Transaction> transactions, string minerAddress)
        {
            Block newBlock = new Block(blockchain.GetLastBlock(), transactions, minerAddress);
            blockchain.blocks.Add(newBlock);
        }

        private void SortTransactions(List<Transaction> transactions)
        {
            switch (selectionStrategy)
            {
                case TransactionSelectionStrategy.Greedy:
                    transactions.Sort((t1, t2) => t2.fee.CompareTo(t1.fee)); // Sort by highest fee first
                    break;
                case TransactionSelectionStrategy.Altruistic:
                    transactions.Sort((t1, t2) => t1.timestamp.CompareTo(t2.timestamp)); // Sort by longest wait first
                    break;
                case TransactionSelectionStrategy.Random:
                    transactions.Shuffle(); // Shuffle the transactions randomly
                    break;
                case TransactionSelectionStrategy.AddressPreference:
                    transactions.Sort((t1, t2) => t1.senderAddress.CompareTo(t2.senderAddress)); // Sort by sender address preference
                    break;
            }
        }

        private void GreedyButton_Click(object sender, EventArgs e)
        {
            selectionStrategy = TransactionSelectionStrategy.Greedy;
            UpdateText("Transaction selection strategy set to Greedy.");
        }

        private void AltruisticButton_Click(object sender, EventArgs e)
        {
            selectionStrategy = TransactionSelectionStrategy.Altruistic;
            UpdateText("Transaction selection strategy set to Altruistic.");
        }

        private void RandomButton_Click(object sender, EventArgs e)
        {
            selectionStrategy = TransactionSelectionStrategy.Random;
            UpdateText("Transaction selection strategy set to Random.");
        }

        private void AddressPreferenceButton_Click(object sender, EventArgs e)
        {
            selectionStrategy = TransactionSelectionStrategy.AddressPreference;
            UpdateText("Transaction selection strategy set to Address Preference.");
        }

        private void PoSMineButton_Click(object sender, EventArgs e)
        {
            List<Transaction> transactions = blockchain.GetPendingTransactions();
            string validatorAddress = "Validator1"; // Change this to the actual validator address

            PoSBlock newPoSBlock = blockchain.CreatePoSBlock(transactions, validatorAddress, 100); // Adjust staked amount
            UpdateText($"PoS Mining completed!\n{blockchain.ToString()}");
        }
    }
}

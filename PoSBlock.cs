using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;

namespace BlockchainAssignment
{
    public class PoSBlock : Block
    {
        public double StakedAmount { get; set; }
        public string ValidatorAddress { get; set; }

        public PoSBlock(Block lastBlock, List<Transaction> transactions, string validatorAddress, double stakedAmount)
            : base(lastBlock, transactions, validatorAddress)
        {
            ValidatorAddress = validatorAddress;
            StakedAmount = stakedAmount;
            MinePoS();
        }
        private void MinePoS()
        {

            Random random = new Random();
            double randomNumber = random.NextDouble();

            // The validator is selected based on their staked amount.

            if (randomNumber < (StakedAmount / 10000.0)) // Assuming a total stake in the system is 10,000 for simplicity
            {
                string combinedData = $"{timestamp}-{prevHash}-{ValidatorAddress}-{StakedAmount}";
                hash = CalculateHash(combinedData);
                Console.WriteLine($"PoS Block mined by {ValidatorAddress} with stake {StakedAmount}");
            }
            else
            {
                Console.WriteLine($"Validator {ValidatorAddress} with stake {StakedAmount} was not selected to mine the next block.");
                // validator 2
            }
        }


        private string CalculateHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}




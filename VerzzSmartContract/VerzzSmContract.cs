using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace VerzzSmartContract
{

    /*
    
    MIT License
    Copyright (c) 2018 Verzz
    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.

    */

    public class VerzzSmContract : SmartContract
    {

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Verzz Smart Contract - Could be used by others aswell and for other purposes (E-SPORTS ?)                                       //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  SMARTCONTRACT INFORMATION
        /*
         * 
         * MATCH CRUD
         * 1) Match Creation (Create)
         * 2) Match Information (Read)
         * 3) Match Winner Team (Update)
         * 4) Match Cancellation (Delete)
         * 
         * PARTICIPATION CR
         * 1) Join Match (Create)
         * 2) Get Participants (Read)
         * 
         */


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  Token Settings
        public static string Name() => "Verzz";
        public static string Symbol() => "VRZ";
        public static readonly byte[] Owner = "AdjxxWCU25aHG5ed8wCUHXRS3VVtNmq2Nu".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong neo_decimals = 100000000;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  ICO Settings
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
        private const ulong total_amount = 100000000 * factor; // total token amount
        private const ulong pre_ico_cap = 30000000 * factor; // pre ico token amount
        private const ulong basic_rate = 1000 * factor;
        private const int ico_start_time = 1519476889;
        private const int ico_end_time = 1529476889;


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  ACTIONS
        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        [DisplayName("refund")]
        public static event Action<byte[], BigInteger> Refund;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  MAIN(operation, object[...])
        //  When triggered, check the owner
        //  If application, go to dispatch
        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = operation.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                return Dispatch(operation, args);
            }

            return "Exception : Something went wrong, please contact info@verzz.org and explain your situation.";
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Dispatch(string operation, params object[] args)
        // Finds the correct operation and returns an Object (different possibilities)
        // Has VERZZ-specific operations at start, followed by the NEP-5 ones
        public static Object Dispatch(string operation, params object[] args)
        {
            /*
                 * args[0] = creationPublicAddress
                 * args[1] = numberOfPlayers
                 * args[2] = signature
            */
            if (operation == "creatematch")
            {
                if (args.Length != 3) return false;                

                // Verify that the received public addr is the one owned by the person sending the request to the smartcontract
                byte[] signature = ((string)args[2]).AsByteArray();

                if (!VerifySignature(signature, ((string)args[0]).AsByteArray()))
                {
                    return false;
                }

                return CreateMatch((string)args[0], (int)args[1]);
            }

            /*
                 * args[0] = creationPublicAddress
            */
            if (operation == "ismatchregistered")
            {
                if (args.Length != 1) return false;
                return IsMatchRegistered((string)args[0]);
            }


            if (operation == "winner")
            {
                /*
                 * args[0] = creationPublicAddress
                 * args[1] = winners (string created by concatening all the winners public addresses (by our middleware))
                 * args[2] = address of sender
                 * args[3] = signature, if the organiser sets the winner, the others have to vote and accept the result by submitting the same winners string and thus this arg will be empty
                 */

                if (args.Length == 4) return Winner((string)args[0], (string)args[2], (string)args[1], VerifySignature(((string)args[3]).AsByteArray(), ((string)args[0]).AsByteArray()));
                if (args.Length == 3) return Winner((string)args[0], (string)args[2], (string)args[1], false);
                else return false;
            }

            /*
                 * args[0] = creationPublicAddress
                 * args[1] = signature
            */
            if (operation == "cancelmatch")
            {
                if (args.Length != 2) return false;

                // Verify that the received public addr is the one owned by the person sending the request to the smartcontract
                byte[] signature = ((string)args[1]).AsByteArray();

                if (!VerifySignature(signature, ((string)args[0]).AsByteArray()))
                {
                    return false;
                }

                return Cancel((string)args[0]);
            }

            /*
                 * args[0] = creationPublicAddress
                 * args[1] = joiningPerson (don't have to check signature because it would be stupid to put someone elses pub addr to receive winning pot)
            */
            if (operation == "joinmatch")
            {
                if (args.Length != 2) return false;
                return JoinMatch((string)args[0], (string)args[1]);
            }
            
            /*
                 * args[0] = creationPublicAddress
            */
            if (operation == "getparticipants")
            {
                if (args.Length != 1) return false;
                return GetParticipants((string)args[0]);
            }

            /*
                * args[0] = creationPublicAddress
           */
            if (operation == "getwinner")
            {
                if (args.Length != 1) return false;
                return GetWinner((string)args[0]);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // NEP-5 TOKEN TEMPLATE
            if (operation == "deploy")
            {
                return Deploy();
            }

            if (operation == "mintTokens")
            {
                return MintTokens();
            }

            if (operation == "totalSupply")
            {
                return TotalSupply();
            }

            if (operation == "name")
            {
                return Name();
            }

            if (operation == "symbol")
            {
                return Symbol();
            }

            if (operation == "transfer")
            {
                if (args.Length != 3) return false;
                byte[] from = (byte[])args[0];
                byte[] to = (byte[])args[1];
                BigInteger value = (BigInteger)args[2];
                return Transfer(from, to, value);
            }

            if (operation == "balanceOf")
            {
                if (args.Length != 1) return 0;
                byte[] account = (byte[])args[0];
                return BalanceOf(account);
            }

            if (operation == "decimals")
            {
                return Decimals();
            }

            return "Exception : No corresponding operations.";
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CreateMatch(string creatorPublicAddress, int terrainFee, int InscriptionFee, int numberOfParticipants)
        // Syntax of a match
        // Return boolean to tell if creating has been done
        private static bool CreateMatch(string creatorPublicAddress, int numberOfParticipants)
        {
            // Gets the match in storage
            var match = Storage.Get(Storage.CurrentContext, creatorPublicAddress);

            // Creator already has a match
            if (match.Length > 0)
            {
                return false;
            }

            // Put in the match in the storage
            Storage.Put(Storage.CurrentContext, creatorPublicAddress, "Created");

            // Put in the number of players of the match
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipant", numberOfParticipants);

            // Put in the number of registered players of the match
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered", 1);

            // Put in the first participant (match creator)
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_participant0", creatorPublicAddress);

            // Put in the basic winner (nowinner)
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_winner", "nowinner");

            // Put in the basic winnervotes (0) used to define the number of times the participants agreed with the winner set by the organizor
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_winnervotes", 0);

            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "v", 0);

            // Match successfully created
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // IsMatchRegistered(string creatorPublicAddress)
        // Returns the json of the match created in the storage (if he exists)
        // Can be seen as a IsCreated and a GetInfo
        private static string IsMatchRegistered(string creatorPublicAddress)
        {
            return Storage.Get(Storage.CurrentContext, creatorPublicAddress).AsString();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Cancel(string creatorPublicAddress)
        // Cancels the match of the creator and unlocks all the VRZ and gives them back to the respective participants
        // Return boolean to tell if the cancellation has been done 
        private static bool Cancel(string creatorPublicAddress)
        {
            // Gets the match in storage
            var match = Storage.Get(Storage.CurrentContext, creatorPublicAddress);

            // Creator has no match
            if (match.Length == 0)
            {
                return false;
            }

            // Delete in the match in the storage
            Storage.Delete(Storage.CurrentContext, creatorPublicAddress);

            // Get  the number of players of the match
            BigInteger numberOfParticipants = Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered").AsBigInteger();

            // Delete the number of players of the match
            Storage.Delete(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipants");
            Storage.Delete(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered");
            Storage.Delete(Storage.CurrentContext, creatorPublicAddress + "numberOfVotes");
            Storage.Delete(Storage.CurrentContext, creatorPublicAddress + "_winnervotes");

            // Repeat this action until we delete all the participants
            for (int i = 0; i < numberOfParticipants; i++)
            {
                // Deletes the vote of the user if it exists
                if(Storage.Get(Storage.CurrentContext, "_participant" + i).AsString().Length > 0)
                    Storage.Delete(Storage.CurrentContext, creatorPublicAddress + Storage.Get(Storage.CurrentContext, "_participant" + i).AsString());
                // Deletes the user
                Storage.Delete(Storage.CurrentContext, creatorPublicAddress + "_participant" + i);
            }

            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Winner(string creatorPublicAddress, string winnershash)
        // Sets the winner for the 
        // Return boolean to tell if winners are part of the
        private static bool Winner(string creatorPublicAddress, string self, string winnershash, bool isOrganiser)
        {
            if (isOrganiser)
            {
                // Check if the organizor has already given an answer
                if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_winner").AsString() != "nowinner")
                    return false;
                Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_winner", winnershash);
            }
            else
            {
                // Check if the organizor has already given an answer
                if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_winner").AsString() == "nowinner")
                    return false;

                // Check if the user has already voted
                if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + self).AsString().Length > 0)
                    return false;

                var points = Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_winnervotes").AsBigInteger();
                var numberOfVotes = Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfVotes").AsBigInteger();

                if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_winner").AsString() == winnershash)
                    points = points + 1;
                else
                {
                    points = points - 1;
                }

                numberOfVotes = numberOfVotes + 1;

                Storage.Put(Storage.CurrentContext, creatorPublicAddress + "numberOfVotes", numberOfVotes);
                Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_winnervotes", points);
                Storage.Put(Storage.CurrentContext, creatorPublicAddress + self, "voted");
            }
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // JoinMatch(string creatorPublicAddress, string participantPublicAddress)
        // Makes the participant join the match
        // Return boolean to tell if he has correctly been added
        private static bool JoinMatch(string creatorPublicAddress, string participantPublicAddress)
        {
            // Gets the match in storage
            var match = Storage.Get(Storage.CurrentContext, creatorPublicAddress).AsString();

            // Match doesn't exist
            if (match.Length == 0)
            {
                return false;
            }

            // Get  the number of players of the match
            BigInteger numberOfParticipants = Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered").AsBigInteger();

            // Max number of players reached
            if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipant").AsBigInteger() == numberOfParticipants)
            {
                return false;
            }

            // Check if the participant already joined the match
            for (int i = 0; i < numberOfParticipants; i++)
            {
                if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_participant" + i).AsString() == participantPublicAddress)
                {
                    return false;
                }
            }

            numberOfParticipants = numberOfParticipants + 1;

            // Put in the updated participants list
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_participant" + numberOfParticipants, participantPublicAddress);
            // Update number of participants
            Storage.Put(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered", numberOfParticipants);

            // Participant has been succesfully added
            return true;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GetParticipants(string creatorPublicAddress)
        // Returns all the participants of a match
        private static string GetParticipants(string creatorPublicAddress)
        {
            // Get  the number of players of the match
            BigInteger numberOfParticipants = Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered").AsBigInteger();

            string participants = "{[";
            // Repeat this action until we delete all the participants
            for (int i = 0; i < numberOfParticipants; i++)
            {
                participants += Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_participant" + i).AsString();

                if (i < numberOfParticipants - 1)
                {
                    participants += ",";
                }
            }

            participants += "]}";

            return participants;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // GetParticipants(string creatorPublicAddress)
        // Returns all the participants of a match
        private static string GetWinner(string creatorPublicAddress)
        {
            BigInteger numberOfParticipants = Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfParticipantsRegistered").AsBigInteger();

            // The votes aren't done yet
            if (Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_numberOfVotes").AsBigInteger() < (numberOfParticipants / 2))
            {
                return "nowinner";
            }

            // The participants aren't okay with what the organizor said
            if(Storage.Get(Storage.CurrentContext, creatorPublicAddress + "_winnervotes").AsBigInteger() < (numberOfParticipants / 2))
            {
                return "noagreement";
            }

            // winner
            return Storage.Get(Storage.CurrentContext, creatorPublicAddress+"_winner").AsString();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  End of VERZZ-SMARTCONTRACT                                                                                                      //                                                                                
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  NEP-5 TEMPLATE                                                                                                                  //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        // initialization parameters, only once
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, Owner, pre_ico_cap);
            Storage.Put(Storage.CurrentContext, "totalSupply", pre_ico_cap);
            Transferred(null, Owner, pre_ico_cap);
            return true;
        }

        // The function MintTokens is only usable by the chosen wallet
        // contract to mint a number of tokens proportional to the
        // amount of neo sent to the wallet contract. The function
        // can only be called during the tokenswap period
        // 将众筹的neo转化为等价的ico代币
        public static bool MintTokens()
        {
            byte[] sender = GetSender();
            // contribute asset is not neo
            if (sender.Length == 0)
            {
                return false;
            }
            ulong contribute_value = GetContributeValue();
            // the current exchange rate between ico tokens and neo during the token swap period
            // 获取众筹期间ico token和neo间的转化率
            ulong swap_rate = CurrentSwapRate();
            // crowdfunding failure
            // 众筹失败
            if (swap_rate == 0)
            {
                Refund(sender, contribute_value);
                return false;
            }
            // you can get current swap token amount
            ulong token = CurrentSwapToken(sender, contribute_value, swap_rate);
            if (token == 0)
            {
                return false;
            }
            // crowdfunding success
            // 众筹成功
            BigInteger balance = Storage.Get(Storage.CurrentContext, sender).AsBigInteger();
            Storage.Put(Storage.CurrentContext, sender, token + balance);
            BigInteger totalSupply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            Storage.Put(Storage.CurrentContext, "totalSupply", token + totalSupply);
            Transferred(null, sender, token);
            return true;
        }

        // get the total token supply
        // 获取已发行token总量
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }

        // function that is always called when someone wants to transfer tokens.
        // 流转token调用
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (from == to) return true;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        // get the account balance of another account with address
        // 根据地址获取token的余额
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

        // The function CurrentSwapRate() returns the current exchange rate
        // between ico tokens and neo during the token swap period
        private static ulong CurrentSwapRate()
        {
            const int ico_duration = ico_end_time - ico_start_time;
            uint now = Runtime.Time;
            int time = (int)now - ico_start_time;
            if (time < 0)
            {
                return 0;
            }
            else if (time < ico_duration)
            {
                return basic_rate;
            }
            else
            {
                return 0;
            }
        }

        //whether over contribute capacity, you can get the token amount
        private static ulong CurrentSwapToken(byte[] sender, ulong value, ulong swap_rate)
        {
            ulong token = value / neo_decimals * swap_rate;
            BigInteger total_supply = Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
            BigInteger balance_token = total_amount - total_supply;
            if (balance_token <= 0)
            {
                Refund(sender, value);
                return 0;
            }
            else if (balance_token < token)
            {
                Refund(sender, (token - balance_token) / swap_rate * neo_decimals);
                token = (ulong)balance_token;
            }
            return token;
        }

        // check whether asset is neo and get sender script hash
        private static byte[] GetSender()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == neo_asset_id) return output.ScriptHash;
            }
            return new byte[] { };
        }

        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        // get all you contribute neo amount
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == neo_asset_id)
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //  End of NEP-5 TOKEN TEMPLATE
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    }
}

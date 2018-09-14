﻿using Nethereum.Hex.HexConvertors;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.StandardTokenEIP20.Events.DTO;
using Wallet.BlockchainAPI.Model;
using Wallet.Helpers;
using Wallet.Models;
using Wallet.ViewModels;

namespace Wallet.BlockchainAPI
{
    public class BlockchainExplorer : IBlockchainExplorer
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private Web3 web3;

        public BlockchainExplorer(IServiceScopeFactory scopeFactory)
        {
            web3 = new Web3();
            _scopeFactory = scopeFactory;
        }

        public Task<string> GetCode(string address)
        {
            return web3.Eth.GetCode.SendRequestAsync(address);
        }

        public async Task<HexBigInteger> GetLastAvailableBlockNumber()
        {
            try
            {
                return await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            }
            catch (Exception e)
            {
                return new HexBigInteger(0);
            }
        }

        public Task<BlockWithTransactions> GetBlockByNumber(int blockNumber)
        {
            return web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(blockNumber));
        }

        public List<BlockChainTransaction> GetLatestTransactions(int startBlockNumber, int endBlockNumber)
        {
            var result = new List<BlockChainTransaction>();
            var temp = new List<BlockChainTransaction>();
            for (var i = startBlockNumber; i <= endBlockNumber; i++)
            {
                temp.Clear();
                try
                {
                    var block = web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
                        new HexBigInteger(i)).Result;

                    foreach (var transact in block.Transactions)
                    {
                        if (transact.Input.Equals(Constants.Strings.TransactionType.Usual,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            var status =
                                web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transact.TransactionHash)
                                    .Result;
                            bool isSuccess = true;
                            if (status != null)
                            {
                                if (status.Status.Value == 0)
                                {
                                    isSuccess = false;
                                }
                            }

                            double value = 0;
                            try
                            {
                                value = (double) Web3.Convert.FromWei(transact.Value.Value, 18);
                            }
                            catch (Exception)
                            {

                            }

                            temp.Add(new BlockChainTransaction()
                            {
                                TransactionHash = transact.TransactionHash,
                                FromAddress = transact.From,
                                ToAddress = transact.To,
                                What = "ETH",
                                IsSuccess = isSuccess,
                                ContractAddress = String.Empty,
                                DecimalValue = value,
                                Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                    (long) (block.Timestamp.Value)),
                                BlockNumber = (int) block.Number.Value
                            });
                        }
                        else if (transact.Input.StartsWith(Constants.Strings.TransactionType.Transfer))
                        {
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
                                var status =
                                    web3.Eth.Transactions.GetTransactionReceipt
                                        .SendRequestAsync(transact.TransactionHash).Result;
                                bool isSuccess = true;
                                if (status != null)
                                {
                                    if (status.Status.Value == 0)
                                    {
                                        isSuccess = false;
                                    }
                                }

                                var decodedInput = InputDecoder.DecodeTransferInput(transact.Input);
                                var token = dbContext.Erc20Tokens.FirstOrDefault(tok =>
                                    tok.Address.Equals(transact.To, StringComparison.CurrentCultureIgnoreCase));


                                double value = 0;
                                if (token != null)
                                {
                                    try
                                    {
                                        value = (double) Web3.Convert.FromWei(decodedInput.Value,
                                            token?.DecimalPlaces ?? 18);
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }

                                temp.Add(new BlockChainTransaction()
                                {
                                    TransactionHash = transact.TransactionHash,
                                    FromAddress = transact.From,
                                    ToAddress = decodedInput.To,
                                    ContractAddress = transact.To,
                                    What = token?.Symbol ?? "Unknown",
                                    IsSuccess = isSuccess,
                                    DecimalValue = value,
                                    Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                        (long) (block.Timestamp.Value)),
                                    BlockNumber = (int) block.Number.Value
                                });
                            }
                        }
                    }

                    result.AddRange(temp);
                }
                catch (RpcClientUnknownException e)
                {
                    i--;
                }
                catch (RpcClientTimeoutException)
                {
                    i--;
                }
                catch (Exception e)
                {

                }
            }
            return result;
        }

        private async Task<List<CustomTransaction>> GetTransactionByAddress(string accountAddress,
            List<Transaction> transactions, BigInteger timestamp)
        {
            var result = new List<CustomTransaction>();

            foreach (var t in transactions)
            {
                if (t.Input.Equals(Constants.Strings.TransactionType.Usual,
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    if (t.From.Equals(accountAddress, StringComparison.CurrentCultureIgnoreCase) ||
                        t.To.Equals(accountAddress, StringComparison.CurrentCultureIgnoreCase))
                    {
                        var status =
                            await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(t.TransactionHash);
                        bool isSuccess = true;
                        if (status != null)
                        {
                            if (status.Status.Value == 0)
                            {
                                isSuccess = false;
                            }
                        }

                        result.Add(new CustomTransaction()
                        {
                            TransactionHash = t.TransactionHash,
                            From = t.From,
                            To = t.To,
                            What = "ETH",
                            IsSuccess = isSuccess,
                            DecimalValue = Web3.Convert.FromWei(t.Value.Value, 18),
                            Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                (long)(timestamp))
                        });
                    }
                } //get token transfer
                else if (t.Input.StartsWith(Constants.Strings.TransactionType.Transfer))
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
                        var status =
                            await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(t.TransactionHash);
                        bool isSuccess = true;
                        if (status != null)
                        {
                            if (status.Status.Value == 0)
                            {
                                isSuccess = false;
                            }
                        }

                        var decodedInput = InputDecoder.DecodeTransferInput(t.Input);

                        if (t.From.Equals(accountAddress, StringComparison.CurrentCultureIgnoreCase))
                        {
                            var token = dbContext.Erc20Tokens.FirstOrDefault(tok =>
                                tok.Address.Equals(t.To, StringComparison.CurrentCultureIgnoreCase));

                            result.Add(new CustomTransaction()
                            {
                                TransactionHash = t.TransactionHash,
                                From = t.From,
                                To = decodedInput.To,
                                What = token?.Symbol ?? "Unknown",
                                IsSuccess = isSuccess,
                                ContractAddress = t.To,
                                DecimalValue = Web3.Convert.FromWei(decodedInput.Value, token?.DecimalPlaces ?? 18),
                                Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                    (long)(timestamp))
                            });
                        }
                        else if (decodedInput.To.Equals(accountAddress, StringComparison.CurrentCultureIgnoreCase))
                        {
                            var token = dbContext.Erc20Tokens.FirstOrDefault(tok =>
                                tok.Address.Equals(t.To, StringComparison.CurrentCultureIgnoreCase));

                            result.Add(new CustomTransaction()
                            {
                                TransactionHash = t.TransactionHash,
                                From = t.From,
                                To = decodedInput.To,
                                What = token?.Symbol ?? "Unknown",
                                IsSuccess = isSuccess,
                                ContractAddress = t.To,
                                DecimalValue = Web3.Convert.FromWei(decodedInput.Value, token?.DecimalPlaces ?? 18),
                                Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                    (long)(timestamp))
                            });
                        }
                    }
                }
            }

            return result;
        }

        private async Task<List<CustomTransaction>> GetSmartContractTransactionByAddress(string accountAddress,
            List<Transaction> transactions, BigInteger timestamp)
        {
            var result = new List<CustomTransaction>();

            foreach (var t in transactions)
            {
                if (t.Input.StartsWith(Constants.Strings.TransactionType.Transfer))
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<WalletDbContext>();

                        var decodedInput = InputDecoder.DecodeTransferInput(t.Input);

                        if (t.To.Equals(accountAddress, StringComparison.CurrentCultureIgnoreCase))
                        {
                            var status =
                                await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(t.TransactionHash);
                            bool isSuccess = true;
                            if (status != null)
                            {
                                if (status.Status.Value == 0)
                                {
                                    isSuccess = false;
                                }
                            }

                            var token = dbContext.Erc20Tokens.FirstOrDefault(tok =>
                                tok.Address.Equals(t.To, StringComparison.CurrentCultureIgnoreCase));

                            result.Add(new CustomTransaction()
                            {
                                TransactionHash = t.TransactionHash,
                                From = t.From,
                                To = decodedInput.To,
                                What = token?.Symbol ?? "Unknown",
                                IsSuccess = isSuccess,
                                ContractAddress = t.To,
                                DecimalValue = Web3.Convert.FromWei(decodedInput.Value, token?.DecimalPlaces ?? 18),
                                Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                                    (long)(timestamp))
                            });
                        }
                    }
                }
            }

            return result;
        }

        public async Task<List<CustomTransaction>> GetTransactions(string account, int blockNumber)
        {
            try
            {
                var block = (await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
                    new HexBigInteger(blockNumber)));

                return await GetTransactionByAddress(account, block.Transactions.ToList(), block.Timestamp.Value);
            }
            catch (Exception e)
            {
                return new List<CustomTransaction>();
            }
        }

        public async Task<List<CustomTransaction>> GetSmartContractTransactions(string account, int blockNumber)
        {
            try
            {
                var block = (await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
                    new HexBigInteger(blockNumber)));

                return await GetSmartContractTransactionByAddress(account, block.Transactions.ToList(), block.Timestamp.Value);
            }
            catch (Exception e)
            {
                return new List<CustomTransaction>();
            }
        }

        public Task<BigInteger> GetTokenTotalSupply(string contractAddress)
        {
            var cont = web3.Eth.GetContract(Constants.Strings.ABI.Abi, contractAddress);
            var eth = cont.GetFunction("totalSupply");
            return eth.CallAsync<BigInteger>();
        }

        public async Task<List<CustomEventLog>> GetFullEventLogs(ERC20Token contract, int lastBlockNumber, int blocknum = 1)
        {
            var cont = web3.Eth.GetContract(Constants.Strings.ABI.Abi, contract.Address);
            var transEvent = cont.GetEvent("Transfer");

            var logs = new List<EventLog<Transfer>>();
            var events = new List<CustomEventLog>();

            for (var i = blocknum; i <= lastBlockNumber; i += 100)
            {
                if ((i+99)>lastBlockNumber)
                    break;
                try
                {
                    var filter = transEvent.CreateFilterInput(new BlockParameter((ulong)i), new BlockParameter((ulong)(i + 99)));
                    var log = transEvent.GetAllChanges<Transfer>(filter).Result;
                    logs.AddRange(log);
                }
                catch (Exception e)
                {
                    i -= 100;
                }

            }

            BlockWithTransactions block = null;
            foreach (var log in logs)
            {
                if (block == null || log.Log.BlockNumber.Value != block.Number.Value)
                {
                    block = web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(
                        new HexBigInteger(log.Log.BlockNumber)).Result;
                }

                events.Add(new CustomEventLog()
                {
                    ERC20TokenId = contract.Id,
                    From = log.Event.AddressFrom,
                    To = log.Event.AddressTo,
                    AmountOfToken = Web3.Convert.FromWei(log.Event.Value, contract.DecimalPlaces),
                    WhenDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(
                        (long)(block.Timestamp.Value)),
                    BlockNumber = (int)log.Log.BlockNumber.Value
                });
            }

            return events;
        }

        public Task<BigInteger> GetTokenHolderBalance(string holderAddress, string contractAddress)
        {
            var cont = web3.Eth.GetContract(Constants.Strings.ABI.Abi, contractAddress);
            var eth = cont.GetFunction("balanceOf");
            return eth.CallAsync<BigInteger>(holderAddress);
        }

        /// <summary>
        /// get current balance eth
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public Task<HexBigInteger> BalanceETH(string account)
        {
            return web3.Eth.GetBalance.SendRequestAsync(account);
        }

        /// <summary>
        /// get token erc20
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        /// 
        public async Task<ERC20TokenViewModel> BalanceToken(ERC20Token token, string account)
        {
            try
            {
                var cont = web3.Eth.GetContract(Constants.Strings.ABI.Abi, token.Address);
                var eth = cont.GetFunction("balanceOf");
                var balance = await eth.CallAsync<BigInteger>(account);
                return new ERC20TokenViewModel()
                {
                    Address = token.Address,
                    Balance = Web3.Convert.FromWei(balance, token.DecimalPlaces),
                    DecimalPlaces = token.DecimalPlaces,
                    Symbol = token.Symbol
                };
            }
            catch (Exception e)
            {
                return new ERC20TokenViewModel()
                {
                    Address = token.Address,
                    Balance = 0,
                    DecimalPlaces = token.DecimalPlaces,
                    Symbol = token.Symbol
                };
            }
        }
    }
}
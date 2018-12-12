﻿using System.Collections.Generic;
using System.Threading;
using Smoulder;
using StockMarketAnalysis.Smoulder;

namespace StockMarketAnalysis
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var smoulderFactory = new SmoulderFactory();
            var smoulder = smoulderFactory.Build(new Loader(), new Processor(), new Distributor());

            //These parameters could come from anywhere, but I don't have a database/config file to pull them from
            var smoulderParameters = new object[] {57000,
                "OU9SMS12HKE8MPLV",
                new List<string>
                {
                    "AAPL",
                    "ABBV",
                    "ABT",
                    "ACN",
                    "AGN",
                    "AIG",
                    "ALL",
                    "AMGN",
                    "AMZN",
                    "AXP",
                    "BA",
                    "BAC",
                    "BIIB",
                    "BK",
                    "BKNG",
                    "BLK",
                    "BMY",
                    "C",
                    "CAT",
                    "CELG",
                    "CHTR",
                    "CL",
                    "CMCSA",
                    "COF",
                    "COP",
                    "COST",
                    "CSCO",
                    "CVS"
                }
            };

            smoulder.Start(smoulderParameters).Wait();

            while (!smoulder.loaderCancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(500);
            }
        }
    }
}

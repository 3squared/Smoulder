﻿using System;
using System.Threading;

namespace Smoulder.ExampleApplication.SmoulderClasses
{
    public class ExampleDistributor : DistributorBase<DistributeDataObject>
    {
        public override void Action(DistributeDataObject data, CancellationToken cancellationToken)
        {
            //Report data to a downsteam service, post it to another database, aggregate some data etc

            Random rng = new Random();
            Thread.Sleep(rng.Next(1, 250));
        }

        //No need to override Startup()
        //No need for the downtime between items on queue, so didn't override OnNoQueueItem()
        //Live dangerously by ignoring any errors by not overriding OnError()

        public override void Finalise()
        {
            Console.WriteLine("Starting Processor finalisation." + GetDistributorQueueCount() + " items left to process");

            while (Dequeue(out var data))
            {
                    Random rng = new Random();
            }

            Console.WriteLine("Finished Distributor finalisation." + GetDistributorQueueCount() + " items left to process");
        }
    }
}

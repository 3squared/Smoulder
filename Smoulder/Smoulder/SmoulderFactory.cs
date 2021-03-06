﻿using System.Collections.Concurrent;
using Smoulder.Interfaces;

namespace Smoulder
{
    public class SmoulderFactory : ISmoulderFactory
    {
        public Smoulder<TProcessData, TDistributeData> Build<TProcessData, TDistributeData>(
            ILoader<TProcessData> providedLoader = null,
            IProcessor<TProcessData, TDistributeData> providedProcessor = null,
            IDistributor<TDistributeData> providedDistributor = null, int processorQueueBound = 0,
            int distributorQueueBound = 0) where TProcessData : new() where TDistributeData : new()
        {
            var loader = providedLoader ?? new LoaderBase<TProcessData>();
            var processor = providedProcessor ?? new ProcessorBase<TProcessData,TDistributeData>();
            var distributor = providedDistributor ?? new DistributorBase<TDistributeData>();

            //Create Queues
            ConcurrentQueue<TProcessData> underlyingProcessQueue = new ConcurrentQueue<TProcessData>();
            BlockingCollection<TProcessData> processorQueue = processorQueueBound > 0
                ? new BlockingCollection<TProcessData>(underlyingProcessQueue, processorQueueBound)
                : new BlockingCollection<TProcessData>(underlyingProcessQueue);

            ConcurrentQueue<TDistributeData> underlyingDistributorQueue = new ConcurrentQueue<TDistributeData>();
            BlockingCollection<TDistributeData> distributorQueue = distributorQueueBound > 0
                ? new BlockingCollection<TDistributeData>(underlyingDistributorQueue, distributorQueueBound)
                : new BlockingCollection<TDistributeData>(underlyingDistributorQueue);

            //Hooks units up to Queues
            loader.RegisterProcessorQueue(processorQueue);
            processor.RegisterProcessorQueue(processorQueue, underlyingProcessQueue);

            processor.RegisterDistributorQueue(distributorQueue);
            distributor.RegisterDistributorQueue(distributorQueue, underlyingDistributorQueue);

            //Creates a Smoulder encapsulating the units
            var smoulder = new Smoulder<TProcessData, TDistributeData>(loader, processor, distributor, processorQueue, distributorQueue);

            //Returns the Smoulder
            return smoulder;
        }
    }
}

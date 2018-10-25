﻿using System.Collections.Concurrent;
using Smoulder.Interfaces;

namespace Smoulder
{
    public abstract class LoaderBase : ILoader
    {
        private ConcurrentQueue<IProcessDataObject> _processorQueue;

        public void RegisterProcessorQueue(ConcurrentQueue<IProcessDataObject> processorQueue)
        {
            _processorQueue = processorQueue;
        }
    }
}

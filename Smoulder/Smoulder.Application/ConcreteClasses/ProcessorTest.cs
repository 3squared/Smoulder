﻿using System.Threading.Tasks;
using Smoulder.Interfaces;

namespace Smoulder.Tests.TestConcreteClasses
{
    public class ProcessorTest : ProcessorBase
    {
        public override void Action()
        {

            if (ProcessorQueue.TryDequeue(out IProcessDataObject queueItem))
            {
                var data = (ProcessDataObjectTest) queueItem;
                var result = new DistributeDataObjectTest();
                result.DataValue1 = data.DataValue2;
                result.DataValue2 = data.DataValue1;

                DistributorQueue.Enqueue(result);
            }
            else
            {
                Task.Delay(50);
            }
        }
    }
}

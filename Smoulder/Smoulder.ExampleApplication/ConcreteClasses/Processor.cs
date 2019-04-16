﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Smoulder.Interfaces;

namespace Smoulder.ExampleApplication.ConcreteClasses
{
    public class Processor : ProcessorBase<ProcessDataObject,DistributeDataObject>
    {
        public override void Action(CancellationToken cancellationToken)
        {
                var data = Dequeue();
                var result = new DistributeDataObject
                {
                    DataValue1 = data.DataValue,
                    DataValue2 = data.DataValue / 2
                };

                Enqueue(result);

                Random rng = new Random();
                Task.Delay(rng.Next(1,1000));
        }

        public override void Finalise()
        {
            Console.WriteLine("Starting Processor finalisation." + GetProcessorQueueCount() + " items left to process");

            while (GetProcessorQueueCount() != 0)
            {
                    var data = Dequeue();
                    var result = new DistributeDataObject
                    {
                        DataValue1 = data.DataValue,
                        DataValue2 = data.DataValue / 2
                    };

                    Enqueue(result);

                    Random rng = new Random();
                    Task.Delay(rng.Next(1, 1000));
            }

            Console.WriteLine("Finished Processor finalisation." + GetProcessorQueueCount() + " items left to process");
        }
    }
}

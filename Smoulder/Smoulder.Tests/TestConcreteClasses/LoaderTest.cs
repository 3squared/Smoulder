﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Smoulder.Concrete;

namespace Smoulder.Tests.TestConcreteClasses
{
    public class LoaderTest : LoaderBase
    {
        public override void Action(CancellationToken cancellationToken)
        {
            var data = new ProcessDataObjectTest();
            ProcessorQueue.Enqueue(data);
            Task.Delay(10);
        }
    }
}

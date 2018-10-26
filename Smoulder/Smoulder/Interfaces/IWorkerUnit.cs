﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Smoulder.Interfaces
{
    public interface IWorkerUnit
    {
        Task Start();
        void Action();
        Task Stop();
    }
}

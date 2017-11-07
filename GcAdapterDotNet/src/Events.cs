using System;
using System.Collections.Generic;
using System.Text;

namespace GcAdapterDotNet
{
    public struct AdapterEvent
    {
        internal Adapter adapter;

        public Adapter Adapter { get { return adapter; } }
    }

    public struct ControllerEvent
    {
        internal Controller controller;

        public Controller  Controller { get { return controller; } }
    }
}

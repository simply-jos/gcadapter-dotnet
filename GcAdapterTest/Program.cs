using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GcAdapterDotNet;

namespace gcadapter_test
{
    class Program
    {
        static void Main(string[] args)
        {
            var daemon = new AdapterDaemon();

            daemon.AdapterPluggedIn += (e) =>
            {
                Console.WriteLine("Adapter plugged in (Address {0})", e.Adapter.address);
            };

            daemon.AdapterUnplugged += (e) =>
            {
                Console.WriteLine("Adapter unplugged (Address {0})", e.Adapter.address);
            };

            daemon.ControllerPluggedIn += (ev) =>
            {
                Console.WriteLine("Controller plugged in (Port {0})", ev.Controller.portNumber);

                ev.Controller.StateUpdate += (e) =>
                {
                    Console.WriteLine("Controller state update: {0} {1} {2} {3} {4}", e.Controller.PadState.ax, e.Controller.PadState.ay, e.Controller.PadState.cx, e.Controller.PadState.cy, e.Controller.PadState.buttons);
                };
            };

            daemon.ControllerUnplugged += (e) =>
            {
                Console.WriteLine("Controller unplugged (Port {0})", e.Controller.portNumber);
            };

            daemon.Start();
        }
    }
}

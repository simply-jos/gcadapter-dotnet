using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GcAdapter;

namespace gcadapter_test
{
    class Program
    {
        static void Main(string[] args)
        {
            var daemon = new AdapterDaemon();

            daemon.AdapterPluggedIn += (s, e) =>
            {
                Console.WriteLine("Adapter plugged in (Address {0})", e.Adapter.address);
            };

            daemon.AdapterUnplugged += (s, e) =>
            {
                Console.WriteLine("Adapter unplugged (Address {0})", e.Adapter.address);
            };

            daemon.ControllerPluggedIn += (s, e) =>
            {
                Console.WriteLine("Controller plugged in (Port {0})", e.Controller.portNumber);
            };

            daemon.ControllerUnplugged += (s, e) =>
            {
                Console.WriteLine("Controller unplugged (Port {0})", e.Controller.portNumber);
            };

            daemon.Start();
        }
    }
}

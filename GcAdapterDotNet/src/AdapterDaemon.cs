using System;
using System.Threading;
using System.Collections.Generic;

using System.Linq;

using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace GcAdapterDotNet
{
    public class Adapter
    {
        public readonly UsbDevice adapterDevice;
        public readonly object address;

        public readonly UsbEndpointReader reader;
        public readonly UsbEndpointWriter writer;

        public Controller[] controllers = new Controller[4];

        public Adapter(UsbDevice adapterDevice)
        {
            this.adapterDevice = adapterDevice;
            address = adapterDevice.UsbRegistryInfo[DevicePropertyType.Address];

            reader = adapterDevice.OpenEndpointReader(ReadEndpointID.Ep01);
            writer = adapterDevice.OpenEndpointWriter(WriteEndpointID.Ep02);

            for (uint i=0;i<4;++i)
            {
                this.controllers[i] = new Controller(i);
            }

            SendStart();
        }

        public void SendStart()
        {
            // Tell the adapter to start workin'
            Write(Protocol.Outgoing.Start);
        }

        internal void ForceDisconnect(ref Action<ControllerEvent> controllerUnplugged)
        {
            foreach (var controller in this.controllers)
            {
                controller.ForceUnplug(ref controllerUnplugged);
            }
        }

        public void Poll(ref Action<ControllerEvent> controllerPluggedIn, ref Action<ControllerEvent> controllerUnplugged, ref Action<ControllerEvent> controllerStateUpdate)
        {
            // Read from the adapter and see what's being reported
            var readBuffer = new byte[37];
            int readLength;

            reader.Read(readBuffer, 10, out readLength);

            // Read each port for any state changes
            for (uint i=0;i<4;++i)
            {
                var controller = this.controllers[i];
                controller.ReadState(ref controllerPluggedIn, ref controllerUnplugged, ref controllerStateUpdate, ref readBuffer);
            }
        }

        public void Write(byte[] data)
        {
            int transferLength;

            writer.Write(data, 10, out transferLength);
        }
    }

    public class AdapterDaemon
    {
        private const int adapterPollRate = 1000;

        private Thread daemonThread;
        private bool threadRunning;

        private List<Adapter> adapters = new List<Adapter>();
        private DateTime nextCleanup = DateTime.UtcNow;

        public event Action<AdapterEvent> AdapterPluggedIn;
        public event Action<AdapterEvent> AdapterUnplugged;

        public event Action<ControllerEvent> ControllerPluggedIn;
        public event Action<ControllerEvent> ControllerUnplugged;
        public event Action<ControllerEvent> ControllerStateUpdate;

        public AdapterDaemon()
        {
        }

        public void Start()
        {
            if (daemonThread != null || threadRunning)
                return;

            threadRunning = true;

            daemonThread = new Thread(AdapterDaemon.BackgroundRunnable);
            daemonThread.Start(this);
        }

        public void Stop()
        {
            this.threadRunning = false;
            this.daemonThread = null;
        }

        private static void BackgroundRunnable(object userdata)
        {
            var daemon = userdata as AdapterDaemon;

            while (daemon.threadRunning)
            {
                daemon.BackgroundTick();
            }
        }

        private void BackgroundTick()
        {
            // Poll adapter state at a fixed rate
            if (DateTime.UtcNow > nextCleanup) {
                FindNewAdapters();
                RemoveUnpluggedAdapters();

                nextCleanup = DateTime.UtcNow;
                nextCleanup.AddMilliseconds(AdapterDaemon.adapterPollRate);
            }

            // Poll from each adapter for new controllers
            foreach (var adapter in adapters)
            {
                adapter.Poll(ref ControllerPluggedIn, ref ControllerUnplugged, ref ControllerStateUpdate);
            }

            // Yield the CPU
            System.Threading.Thread.Sleep(4);
        }

        private void FindNewAdapters()
        {
            var adapterFinder = new UsbDeviceFinder(Constants.vendorId, Constants.deviceId);
            var adapterRegistries = UsbDevice.AllDevices.FindAll(adapterFinder);

            foreach (UsbRegistry registry in adapterRegistries)
            {
                if (!this.adapters.Any(a => a.address.Equals(registry[DevicePropertyType.Address])))
                {
                    UsbDevice device;
                    if (registry.Open(out device))
                    {
                        var a = new Adapter(device);
                        this.adapters.Add(a);

                        AdapterPluggedIn(new AdapterEvent {
                            adapter = a
                        });
                    }
                }
            }
        }

        private void RemoveUnpluggedAdapters()
        {
            this.adapters.RemoveAll(a => {
                if (!a.adapterDevice.UsbRegistryInfo.IsAlive)
                {
                    a.ForceDisconnect(ref ControllerUnplugged);

                    AdapterUnplugged(new AdapterEvent {
                        adapter = a
                    });

                    return true;
                } else
                {
                    return false;
                }
            });
        }
    }
}

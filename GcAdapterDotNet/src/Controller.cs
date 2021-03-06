using System;

namespace GcAdapterDotNet
{
    [Flags]
    public enum Button : ushort
    {
        A     = 0x0001,
        B     = 0x0002,
        X     = 0x0004,
        Y     = 0x0008,

        Left  = 0x0010,
        Right = 0x0020,
        Down  = 0x0040,
        Up    = 0x0080,

        S     = 0x0100,

        Z     = 0x0200,
        R     = 0x0400,
        L     = 0x0800
    }

    public struct PadState
    {
        public float ax;
        public float ay;

        public float cx;
        public float cy;

        public float l;
        public float r;

        public Button buttons;
    }

    public class Controller
    {
        public readonly uint portNumber;

        private PadState padState;
        public PadState PadState { get { return padState; } }

        private bool active;
        public bool Active { get { return active; } }

        public event Action<ControllerEvent> StateUpdate;

        public Controller(uint portNumber)
        {
            this.active = false;
            this.portNumber = portNumber;
        }

        internal void ForceUnplug(ref Action<ControllerEvent> controllerUnplugged)
        {
            if (this.active)
            {
                this.active = false;

                controllerUnplugged(new ControllerEvent { controller = this });
            }
        }

        public void ReadState(ref Action<ControllerEvent> controllerPluggedIn,
                              ref Action<ControllerEvent> controllerUnplugged,
                              ref byte[] data)
        {
            // Each controller's data is offset into the 37 byte chunk that the
            // adapter sends back every time its polled

            // Base address in the data array for our controller
            uint baseAddress = 1 + portNumber * 9;

            // Check if the type of this controller has updated
            byte controllerType = data[baseAddress + 0];

            if (controllerType != Constants.controllerNone)
            {
                if (!this.active)
                    controllerPluggedIn(new ControllerEvent { controller = this });

                this.active = true;
            } else
            {
                if (this.active)
                {
                    StateUpdate = null;
                    controllerUnplugged(new ControllerEvent { controller = this });

                    this.active = false;
                }

                // No sense in reading the rest of the state, early-out
                return;
            }

            padState.buttons = 0;

            // Read out the state from the slice of data
            // Two sets of button bytes, one at base + 1 and one at base + 2
            Button b = (Button)(data[1 + portNumber * 9 + 1] | (data[1 + portNumber * 9 + 2] << 8));

            padState.buttons |= b & Button.A;
            padState.buttons |= b & Button.B;
            padState.buttons |= b & Button.X;
            padState.buttons |= b & Button.Y;

            padState.buttons |= b & Button.Left;
            padState.buttons |= b & Button.Right;
            padState.buttons |= b & Button.Up;
            padState.buttons |= b & Button.Down;

            padState.buttons |= b & Button.S;
            padState.buttons |= b & Button.Z;
            padState.buttons |= b & Button.L;
            padState.buttons |= b & Button.R;

            // The axises are stored sequentially starting at the base address + 3 bytes, one byte each,
            // in the following order:
            //
            //   Analog X,  Analog Y
            //   C-Stick X, C-Stick Y
            //   L trigger, R trigger
            padState.ax = ((int)data[baseAddress + 3] - 127) / 255.0f;
            padState.ay = ((int)data[baseAddress + 4] - 127) / 255.0f;

            padState.cx = ((int)data[baseAddress + 5] - 127) / 255.0f;
            padState.cy = ((int)data[baseAddress + 6] - 127) / 255.0f;

            padState.l = ((int)data[baseAddress  + 7] - 127) / 255.0f;
            padState.r = ((int)data[baseAddress  + 8] - 127) / 255.0f;

            // Fire off the state update event
            StateUpdate(new ControllerEvent { controller = this });
        }
    }
}

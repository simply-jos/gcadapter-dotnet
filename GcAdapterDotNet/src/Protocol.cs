using System;

namespace GcAdapterDotNet
{
    namespace Protocol
    {
        public static class Outgoing
        {
            public static readonly byte[] Start = {
                0x13, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            public static readonly byte Poll = 0x11;
        }
    }
}

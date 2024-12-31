using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaspberryPiBrain
{
    public class MyHouseManagement
    {
        private byte currentLightState { get; set; } = 0x00;

        public static byte[] GetStateLight
        {
            get => [0x47, 0x45, 0x54, 0x0D, 0x0A];  // GET + 0x0D 0x0A
        }
    }
}
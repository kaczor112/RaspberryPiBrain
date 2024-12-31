using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspberryPiBrain
{
    /// <summary>
    /// Helper Functions
    /// </summary>
    public class HF
    {
        public static bool TheSameArray(byte[] a, byte[] b)
        {
            if((a == null) || (b == null)) return false;
            if(a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
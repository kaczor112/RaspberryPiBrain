using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class ApplicationSettingsModel
    {
        public bool RefreshSettings { get; set; }
        public bool Debug { get; set; }
#pragma warning disable CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.
        public string GniazdkaSerial { get; set; }
        public string OswietlenieSerial { get; set; }
        public int LoopDelay { get; set; }
        public string KeyID { get; set; }
        public string MyWebsite { get; set; }
#pragma warning restore CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.

        public static ApplicationSettingsModel Default
        {
            get
            {
                return new ApplicationSettingsModel()
                {
                    RefreshSettings = true,
                    Debug = true,
                    GniazdkaSerial = "/dev/ttyACM0",
                    OswietlenieSerial = "/dev/ttyUSB0",
                    LoopDelay = 500,
                    KeyID = "MojeID",
                    MyWebsite = @"https://example.com/"
                };
            }
        }
    }
}
using MainComponents;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaspberryPiBrain
{
    public class Program
    {
        public static string[] domNames = ["GoscinnyDuze", "GoscinnyMale", "Kuchnia", "Przedpokoj", "WC", "Sypialnia", "Aux", "ChoinkaLampka"];
        static void Main(string[] args)
        {
            try
            {
                ApplicationSettings.CheckRefresh();

                Logger.Write("Run RaspberryPiBrain v0.01");
                /* Główny program na RaspberryPi do komunikacji pomiędzy serwerem a SmartHome 

                Console.WriteLine("Running on: " + Directory.GetCurrentDirectory());
                Console.WriteLine("Dostępne porty COM:");
                var ports = SerialManagement.GetAvailablePorts();
                if (ports?.Length > 0)
                {
                    foreach (var port in ports)
                    {
                        Console.WriteLine(port);
                    }
                }*/

                using NetworkManagement networkManagement = new();
                
                byte[] czujnikZmierzchuBuffer = [];
                using SerialManagement gniazdkaSerial = new(ApplicationSettings.GniazdkaSerial,
                    data => { if (data != null) czujnikZmierzchuBuffer = data; });

                byte[] stanOswietleniaBuffer = [];
                using SerialManagement oswietlenieSerial = new(ApplicationSettings.OswietlenieSerial,
                    data => { if (data != null) stanOswietleniaBuffer = data; });

                // TODO Disable log Serial

                // Zmienne do pamiętania poprzedniego1 stanu
                bool[] networkState = new bool[8], arduinoState = new bool[8];
                byte lastDataSend = 0x00;
                byte[] stanOswietleniaStanZapamietany = [];

                try
                {
                    bool MainLoop = true, first  = true, ChoinkaLampkaState = false;

                    while (MainLoop)
                    {
                        oswietlenieSerial.SendData(MyHouseManagement.GetStateLight);

                        Thread.Sleep(ApplicationSettings.LoopDelay);

                        if (networkManagement.NetworkModel != null)
                        {
                            for (int i = 0; i < domNames.Length; i++)
                            {
                                foreach (var item in networkManagement.NetworkModel)
                                {
                                    if(domNames[i] == item.Param)
                                    {
                                        bool newState = bool.Parse(item.Value);
                                        if (newState != networkState[i] || first)
                                        {
                                            Logger.Write(domNames[i] + ": " + newState);
                                            networkState[i] = newState;
                                        }

                                        if("ChoinkaLampka" == domNames[i])
                                        {
                                            ChoinkaLampkaState = newState;
                                        }
                                    }
                                }
                            }
                        }

                        byte data = 0x30; // <- Początek licz w ASCII
                        if (ChoinkaLampkaState) data += 0b00000001;
                        if ((DateTime.Now.Hour >= 22) || (DateTime.Now.Hour < 6)) data += 0b00000010;

                        if ((lastDataSend != data) || first)
                        {
                            gniazdkaSerial.SendData([data, 0x0D, 0x0A]);
                            lastDataSend = data;
                        }


                        first = false;
                        if(!HF.TheSameArray(stanOswietleniaBuffer, stanOswietleniaStanZapamietany))
                        {
                            stanOswietleniaStanZapamietany = stanOswietleniaBuffer;
                            Logger.Write("Stan Oświetlenia: " + 
                                string.Join(", ", stanOswietleniaStanZapamietany.Select(b => b.ToString("X2"))));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionManagement.Log(ex, "Program.Main2");
                }
                finally
                {
                    gniazdkaSerial.ClosePort();
                    oswietlenieSerial.ClosePort();
                    NetworkManagement.RunLoop = false;
                }
            }
            catch (Exception ex)
            {
                ExceptionManagement.Log(ex, "Program.Main1");
            }
        }
    }
}
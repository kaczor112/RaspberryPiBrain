using MainComponents;

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
                /* Główny program na RaspberryPi do komunikacji pomiędzy serwerem a SmartHome */

                Console.WriteLine("Running on: " + Directory.GetCurrentDirectory());
                Console.WriteLine("Dostępne porty COM:");
                var ports = SerialManagement.GetAvailablePorts();
                if (ports?.Length > 0)
                {
                    foreach (var port in ports)
                    {
                        Console.WriteLine(port);
                    }
                }

                using NetworkManagement networkManagement = new();
                
                using SerialManagement serialManagement = new(ApplicationSettings.SerialPort);

                // TODO Disable log Serial

                // Zmienne do pamiętania poprzedniego stanu
                bool[] networkState = new bool[8], arduinoState = new bool[8];
                byte lastDataSend = 0x00;

                try
                {
                    bool MainLoop = true, first  = true, ChoinkaLampkaState = false;

                    while (MainLoop)
                    {
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
                        if ((DateTime.Now.Hour >= 22) || (DateTime.Now.Hour <= 6)) data += 0b00000010;

                        if ((lastDataSend != data) || first)
                        {
                            serialManagement.SendData([data, 0x0D, 0x0A]);
                            lastDataSend = data;
                        }


                        first = false;
                    }
                }
                catch (Exception ex)
                {
                    ExceptionManagement.Log(ex, "Program.Main2");
                }
                finally
                {
                    serialManagement.ClosePort();
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
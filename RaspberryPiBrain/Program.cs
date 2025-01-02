using MainComponents;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaspberryPiBrain
{
    public class Program
    {
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

                MyHouseManagement myHouse = new();
                
                byte[] czujnikZmierzchuBuffer = [];
                using SerialManagement gniazdkaSerial = new(ApplicationSettings.GniazdkaSerial,
                    data => { if (data != null) czujnikZmierzchuBuffer = data; });

                byte[] stanOswietleniaBuffer = [];
                using SerialManagement oswietlenieSerial = new(ApplicationSettings.OswietlenieSerial,
                    data => { if (data != null) stanOswietleniaBuffer = data; });

                // TODO Disable log Serial

                DateTime gniazdkaTime = DateTime.Now, oswietlenieTime = DateTime.Now;

                try
                {
                    Logger.Write("Uruchamiam inicjowanie programu");

                    oswietlenieSerial.SendData(MyHouseManagement.GetStateArduino);

                    Thread.Sleep(ApplicationSettings.LoopDelay);

                    if (stanOswietleniaBuffer?.Length > 0)
                    {
                        myHouse.SetStateArduino(stanOswietleniaBuffer);
                        myHouse.InitFromArduino();
                        stanOswietleniaBuffer = null;
                    }

                    bool MainLoop = true; byte lastDataSend = 0x00;

                    Logger.Write("Uruchamiam pętle główną");

                    while (MainLoop)
                    {
                        if (czujnikZmierzchuBuffer == null || czujnikZmierzchuBuffer?.Length == 0)
                        {
                            if(DateTime.Now.Minute != gniazdkaTime.Minute)  // Odświeźam raz na minute
                            {
                                gniazdkaSerial.SendData(MyHouseManagement.GetStateArduino);
                                gniazdkaTime = DateTime.Now;
                            }
                        }

                        if(stanOswietleniaBuffer == null || stanOswietleniaBuffer?.Length == 0)
                        {
                            if ((DateTime.Now.Second / 10) != (gniazdkaTime.Second / 10))  // Odświeźam raz na 10 sek
                            {
                                oswietlenieSerial.SendData(MyHouseManagement.GetStateArduino);
                            }
                        }

                        Thread.Sleep(ApplicationSettings.LoopDelay);

                        if (stanOswietleniaBuffer?.Length > 0)
                        {
                            myHouse.SetStateArduino(stanOswietleniaBuffer);
                            stanOswietleniaBuffer = null;
                        }

                        if (networkManagement.NetworkModel != null)
                        {
                            myHouse.SetStateHttp(networkManagement.NetworkModel);
                        }

                        myHouse.HeartBeat();

                        if(myHouse.FrameToSendArduinoLight != null)
                        {
                            oswietlenieSerial.SendData(myHouse.FrameToSendArduinoLight);
                            myHouse.FrameToSendArduinoLight = null;
                        }

                        byte data = 0x30; // <- Początek licz w ASCII
                        //if (ChoinkaLampkaState) data += 0b00000001;
                        if ((DateTime.Now.Hour >= 22) || (DateTime.Now.Hour < 6)) data += 0b00000010;

                        if ((lastDataSend != data))
                        {
                            gniazdkaSerial.SendData([data, 0x0D, 0x0A]);
                            lastDataSend = data;
                        }


                        //first = false;
                        //if(!HF.TheSameArray(stanOswietleniaBuffer, stanOswietleniaStanZapamietany))
                        //{
                        //    stanOswietleniaStanZapamietany = stanOswietleniaBuffer;
                        //    Logger.Write("Stan Oświetlenia: " + 
                        //        string.Join(", ", stanOswietleniaStanZapamietany.Select(b => b.ToString("X2"))));
                        //}
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
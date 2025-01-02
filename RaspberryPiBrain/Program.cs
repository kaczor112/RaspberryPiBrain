using MainComponents;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaspberryPiBrain
{
    public class Program
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Usuń nieużywany parametr", Justification = "<Oczekujące>")]
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
                    data => { if (data?.Length < 10) stanOswietleniaBuffer = data; });

                // TODO Disable log Serial

                DateTime gniazdkaTime = DateTime.Now, oswietlenieTime = DateTime.Now;

                try
                {
                    //Logger.Write("Uruchamiam inicjowanie programu");

                    oswietlenieSerial.SendData(MyHouseManagement.GetStateArduino);

                    Thread.Sleep(ApplicationSettings.LoopDelay * 2);

                    if (stanOswietleniaBuffer?.Length > 0)
                    {
                        myHouse.SetStateArduino(stanOswietleniaBuffer);
                        myHouse.InitFromArduino();
                        stanOswietleniaBuffer = null;
                    }

                    bool MainLoop = true; byte lastDataSend = 0x00; bool sendOswietlenie = false;

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
                            if (((DateTime.Now.Second / 10) != (oswietlenieTime.Second / 10)) || sendOswietlenie)  // Odświeźam raz na 10 sek
                            {
                                oswietlenieSerial.SendData(MyHouseManagement.GetStateArduino);
                                oswietlenieTime = DateTime.Now;
                                sendOswietlenie = false;
                            }
                        }

                        Thread.Sleep(ApplicationSettings.LoopDelay);

                        if (stanOswietleniaBuffer?.Length > 0)
                        {
                            if (stanOswietleniaBuffer.Length < 10)   // To potwierdzeni odebrania zArduino poprzedniego rozkazu
                            {
                                myHouse.SetStateArduino(stanOswietleniaBuffer);
                            }

                            stanOswietleniaBuffer = null;
                        }
                        
                        if (czujnikZmierzchuBuffer?.Length > 0)
                        {
                            string tempCzujnikZmierzchu = string.Concat(Array.ConvertAll(czujnikZmierzchuBuffer, b => (char)b));
                            tempCzujnikZmierzchu = string.Concat(tempCzujnikZmierzchu.Where(char.IsDigit));

                            Logger.Write("Czujnik zmierzchu: " + tempCzujnikZmierzchu + " Length: " + czujnikZmierzchuBuffer.Length);

                            czujnikZmierzchuBuffer = null;
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
                            sendOswietlenie = true;
                            Thread.Sleep(ApplicationSettings.LoopDelay);
                        }

                        byte data = 0x30; // <- Początek licz w ASCII
                        if (myHouse.ChoinkaLampkaState) data += 0b00000001;
                        if ((DateTime.Now.Hour >= 22) || (DateTime.Now.Hour < 6)) data += 0b00000010;

                        if ((lastDataSend != data))
                        {
                            gniazdkaSerial.SendData([data, 0x0D, 0x0A]);
                            lastDataSend = data;
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
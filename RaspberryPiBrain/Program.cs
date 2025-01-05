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
                /* Główny program na RaspberryPi do komunikacji pomiędzy serwerem a SmartHome */
                string versionOfBrainRPI = "Run RaspberryPiBrain v0.04";
                ApplicationSettings.CheckRefresh();

                Logger.Write(versionOfBrainRPI);
                if (!ApplicationSettings.Debug) Console.WriteLine(DateTime.Now + " " + versionOfBrainRPI);

                MyHouseManagement myHouse = new();
                DateTime gniazdkaTime = DateTime.Now, oswietlenieTime = DateTime.Now;

                using NetworkManagement networkManagement = new( data => { if (data != null) { myHouse.SetStateHttp(data);} });

                using SerialManagement gniazdkaSerial = new("Gniazdka", ApplicationSettings.GniazdkaSerial,
                    data => { if (data != null) { myHouse.SetCzujnikZmierzchu(data); gniazdkaTime = DateTime.Now; } });

                using SerialManagement oswietlenieSerial = new("Oswietlenie", ApplicationSettings.OswietlenieSerial,
                    data => { if (data?.Length < 10) { myHouse.SetStateArduino(data); oswietlenieTime = DateTime.Now; } });

                bool CzujnikZmierzchu = false;

                try
                {
                    oswietlenieSerial.SendData(MyHouseManagement.GetStateArduino);

                    Thread.Sleep(ApplicationSettings.LoopDelay * 2);

                    myHouse.InitFromArduino();

                    while (true)
                    {
                        if(DateTime.Now > gniazdkaTime.AddMinutes(1))  // Odświeźam raz na minute
                        {
                            gniazdkaSerial.SendData(MyHouseManagement.GetStateArduino);
                        }

                        if (DateTime.Now > oswietlenieTime.AddSeconds(1))
                        {
                            oswietlenieSerial.SendData(MyHouseManagement.GetStateArduino);
                        }

                        Thread.Sleep(ApplicationSettings.LoopDelay);

                        myHouse.HeartBeat();

                        if(myHouse.FrameToSendArduinoLight != null)
                        {
                            oswietlenieSerial.SendData(myHouse.FrameToSendArduinoLight);
                            myHouse.FrameToSendArduinoLight = null;
                            Thread.Sleep(ApplicationSettings.LoopDelay);
                        }

                        byte[] gniazdkaFrameToSend = myHouse.FrameToSendGniazdka;
                        if(gniazdkaFrameToSend != null) gniazdkaSerial.SendData(gniazdkaFrameToSend);

                        if(CzujnikZmierzchu != myHouse.CzujnikZmierzchu)
                        {
                            Logger.Write("Czujnik zmierzchu: " + myHouse.CzujnikZmierzchu);
                            CzujnikZmierzchu = myHouse.CzujnikZmierzchu;
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
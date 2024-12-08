using MainComponents;

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
                /* Główny program na RaspberryPi do komunikacji pomiędzy serwerem a SmartHome */

                using NetworkManagement networkManagement = new();
                
                using SerialManagement serialManagement = new(ApplicationSettings.SerialPort);

                // TODO Disable log Serial

                try
                {
                    bool MainLoop = true;

                    while (MainLoop)
                    {
                        if (networkManagement.NetworkModel == null)
                        {
                            Thread.Sleep(ApplicationSettings.LoopDelay);
                            continue;
                        }

                        foreach (var item in networkManagement.NetworkModel)
                        {
                            if("GoscinnyDuze" == item.Param)
                            {
                                serialManagement.SendData(bool.Parse(item.Value) ? "onx" : "offx");
                            }

                            if ("Aux" == item.Param)
                            {
                                if(bool.Parse(item.Value))
                                {
                                    MainLoop = false;
                                    break;
                                }
                            }
                        }
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
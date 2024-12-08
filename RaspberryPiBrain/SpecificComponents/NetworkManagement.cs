using MainComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaspberryPiBrain
{
    public partial class NetworkManagement : IDisposable
    {
        public static bool RunLoop {  get; set; }

        public NetworkManagement()
        {
            RunLoop = true;
            _ = Task.Run(RefreshLoop);
        }

        public async Task RefreshLoop()
        {
            try
            {
                while (RunLoop)
                {
                    await LoadValuesOfDom();
                    
                    ApplicationSettings.CheckRefresh();

                    Thread.Sleep(ApplicationSettings.LoopDelay);
                }
            }
            catch (Exception ex)
            {
                RunLoop = false;
                ExceptionManagement.Log(ex, "RefreshLoop");
            }
        }

        public void Dispose()
        {
            RunLoop = false;
        }
    }
}
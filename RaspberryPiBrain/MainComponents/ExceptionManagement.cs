using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainComponents
{
    public class ExceptionManagement
    {
        public static void Log(Exception exception, string fileNameAndDescription)
            => Log(exception, fileNameAndDescription, fileNameAndDescription);
        public static void Log(Exception exception, string fileName, string fileDescription)
        {
			try
			{
                string ExceptionText = DateTime.Now.ToString(ApplicationSettings.DateFormat) + " Exception in " + fileName + "[" + fileDescription + "]:\t" + exception.Message;
                
                if (ApplicationSettings.Debug) Console.WriteLine(ExceptionText);

                ExceptionText += "\n\nFull details: " + exception + "\n\n==============================\n";

                FileManagement.SaveLog(
                    "Exception\\Exception_" + fileName, 
                    "Exception_" + fileName + DateTime.Now.ToString("_yyyy.MM.dd"), 
                    ExceptionText);
            }
			catch (Exception ex)
			{
                MainException(ex, "MainException_Log");
            }
        }

        public static void MainException(Exception exception, string fileName)
        {
            // Funkcja na zasadnicze wyjątki z MainComponents (tylko z ExceptionManagement, FileManagement)
            // jako klas wzajemnie zależnych celem uniknięcia zapętlenia.

            Console.WriteLine("Failed to save exception" + "[" + fileName + "]: " + exception.Message);
        }
    }
}
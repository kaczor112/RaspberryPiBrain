using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace MainComponents
{
    public sealed class  SerialManagement : IDisposable
    {
        private SerialPort _serialPort { get; set; }
        private bool SerialRunning { get; set; } = true;

        private event Action<byte[]> DataReceived;

#pragma warning disable CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.
        public SerialManagement(string portName, int baudRate = 115200)
        {
            DataReceived += data => Logger.Write($"Odebrano: {Encoding.UTF8.GetString(data)}");

            SerialRunning = true;
            OpenPort(portName, baudRate);
        }

        public SerialManagement(string portName, Action<byte[]> dataReceived, int baudRate = 115200)
        {
            DataReceived += dataReceived;

            SerialRunning = true;
            OpenPort(portName, baudRate);
        }
#pragma warning restore CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.

        /// <summary>
        /// Lista dostępnych portów COM.
        /// </summary>
        /*  Console.WriteLine("Dostępne porty COM:");
            var ports = SerialManagement.GetAvailablePorts();
            if (ports.Length == 0)
            {
                Console.WriteLine("Brak dostępnych portów.");
                return;
            }

            foreach (var port in ports)
            {
                Console.WriteLine(port);
            }

            Console.WriteLine("Wybierz port:");
            string selectedPort = Console.ReadLine();*/
        public static string[] GetAvailablePorts() => SerialPort.GetPortNames();

        private void OpenPort(string portName, int baudRate = 115200)
        {

            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                    throw new InvalidOperationException("Port jest już otwarty.");

                _serialPort = new SerialPort(portName, baudRate)
                {
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Handshake = Handshake.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.Open();
                Task.Run(() => ReadData());
            }
            catch (Exception ex)
            {
                SerialRunning = false;
                ExceptionManagement.Log(ex, "SerialManagement", "OpenPort");
            }
        }

        public void SendData(string data) => SendData(Encoding.UTF8.GetBytes(data));
        public void SendData(byte[] data)
        {
            if (_serialPort != null && _serialPort.IsOpen && data != null)
            {
                if (ApplicationSettings.FrameLogs) Logger.Frame("Send", data);
                _serialPort.Write(data, 0, data.Length);
            }
        }

        private void ReadData(int timeout = 0)
        {
            try
            {
                while (SerialRunning)
                {
                    while (SerialRunning && _serialPort?.BytesToRead == 0)
                    {
                        Thread.Sleep(100);
                    }

                    if (SerialRunning && _serialPort?.IsOpen == true)
                    {
                        byte[] readByts = new byte[_serialPort.BytesToRead];

                        _serialPort.Read(readByts, 0, readByts.Length);

                        if(ApplicationSettings.FrameLogs) Logger.Frame("Receive", readByts);

                        DataReceived?.Invoke(readByts);
                    }
                }
            }
            catch (TimeoutException ex)
            {
                ExceptionManagement.Log(ex, "SerialManagement", "ReadData_TimeoutException[" + timeout + "]");
                if(timeout < 50) ReadData(++timeout);
            }
            catch (Exception ex)
            {
                SerialRunning = false;
                ClosePort();
                ExceptionManagement.Log(ex, "SerialManagement", "ReadData");
            }
        }

        public void ClosePort()
        {
            SerialRunning = false;

            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
#pragma warning disable CS8625 // Nie można przekonwertować literału o wartości null na nienullowalny typ referencyjny.
                _serialPort = null;
#pragma warning restore CS8625 // Nie można przekonwertować literału o wartości null na nienullowalny typ referencyjny.
            }
        }

        public void Dispose()
        {
            ClosePort();
        }
    }
}
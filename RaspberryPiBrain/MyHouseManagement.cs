using MainComponents;
using Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RaspberryPiBrain
{
    public class MyHouseManagement
    {
        private byte currentLightState { get; set; } = 0x00;

        public static byte[] GetStateArduino
             => [0x47, 0x45, 0x54, 0x0D, 0x0A];  // GET + 0x0D 0x0A

        /* Program (zasada działania):
         * Pobieram z ARD stan oświetlenia
         * Delay
         * Ustawiam http obecnym stanem oświetlenia
         * -> Zapamiętuje stany w ARD i http
         * 
         * Początek pętli
         * Sprawdzam czy zmienił sie stan ARD -> ustawiam http
         * Sprawdzam czy zmienił sie stan http -> ustawiam ARD
         * 
         * Pobieram stan czujnika zmierzchu
         * 
         * Ustawiam gniazdko Choinka/Lampka
         *      -> Lampka dwie zależności (6-22 oraz włączony czujnik zmierzchu)
         * 
         */

        private byte CurrentLightState { get; set; } // Obecny stan oświetlenia
        public void InitFromArduino()
        {
            // Inicjowanie Początkowe na podstawie Arduino
            StateLightHttp = StateLightArduino;
            CurrentLightState = StateLightArduino;

            SendStateToHttp(StateLightArduino);
        }

        private int counterUpdate { get; set; } = 0;
        public void HeartBeat()
        {
            // Zasadnicza funkcja sprawdzająca różnice i wymuszająca stany.

            // counterUpdate bo myślę że natychmiastowe sprawdzanie po wysłaniu może być za szybko

            if (counterUpdate++ < 10) return;

            if (StateLightArduino != CurrentLightState)
            {
                StateLightHttp = StateLightArduino;
                CurrentLightState = StateLightArduino;

                SendStateToHttp(StateLightArduino);

                counterUpdate = 0;
            } 
            else if (StateLightHttp != CurrentLightState)
            {
                StateLightArduino = StateLightHttp;
                CurrentLightState = StateLightHttp;

                SendStateToArduino(StateLightHttp);

                counterUpdate = 0;
            }

            if(counterUpdate > 100) counterUpdate = 100; // Stan oczekiwania
        }

        private byte StateLightArduino { get; set; }
        public void SetStateArduino(byte[] arduinoData)
        {
            /* Ustawiam wartości odebrane z Arduino
             * Jest to jedna liczba w formacie ASCII w tablicy byte[]
             */
            if(arduinoData?.Length > 0)
            {
                // Konwersja bajtów ASCII na string
                string numberString = string.Concat(Array.ConvertAll(arduinoData, b => (char)b));

                // Usunięcie potencjalnych znaków nieliczbowych (np. spacji lub innych symboli)
                numberString = string.Concat(numberString.Where(char.IsDigit));

                // Konwersja na int
                if (int.TryParse(numberString, out int result))
                {
                    byte tempStateLightArduino = (byte)(result & 0xFF); // 0xFF bo tylko to jest oświetleniem pozostała liczba to stan przełączników
                    if (ApplicationSettings.Debug && (tempStateLightArduino != StateLightArduino))
                    {
                        StateLightArduino = tempStateLightArduino;
                        Logger.Write("Obecny stan oświetlenia ARD: 0b" + Convert.ToString(StateLightArduino, 2));
                    }
                }
            }
        }

        public static string[] domNames = ["GoscinnyDuze", "GoscinnyMale", "Kuchnia", "Przedpokoj", "WC", "Sypialnia", "Aux", "ChoinkaLampka"];
        private byte StateLightHttp { get; set; }
        public void SetStateHttp(List<DomModel> httpData)
        {
            if (httpData?.Count > 0)
            {
                for (int i = 0; i < domNames.Length; i++)
                {
                    foreach (var itemHttp in httpData)
                    {
                        if (domNames[i] == itemHttp.Param)
                        {
                            bool newState = bool.Parse(itemHttp.Value);
                            if (newState != (((StateLightHttp >> i) & 1) == 1))
                            {
                                // Ustawienie bitu
                                if (newState)
                                {
                                    StateLightHttp = (byte)(StateLightHttp | (1 << i)); // Ustawienie bitu na 1
                                }
                                else
                                {
                                    StateLightHttp = (byte)(StateLightHttp & ~(1 << i)); // Ustawienie bitu na 0
                                }

                            }
                        }
                    }
                }
            }
        }

        private void SendStateToHttp(byte newStateHttp)
        {
            // Aktualizuje stan http na podstawie ARD

            for (int i = 0; i < domNames.Length; i++)
            {
                bool newState = ((newStateHttp >> i) & 1) == 1;
                if (newState != (((StateLightHttp >> i) & 1) == 1))
                {
                    _ = NetworkManagement.SendRequest(domNames[i], newState);
                }
            }
        }

        public byte[] FrameToSendArduinoLight {  get; set; }
        private void SendStateToArduino(byte newStateArduino)
        {
            // Aktualizuje stan ADR na podstawie http

            string numberString = newStateArduino.ToString();
            byte[] byteArray = Array.ConvertAll(numberString.ToCharArray(), c => (byte)c);


            FrameToSendArduinoLight = [..byteArray, 0x0D, 0x0A];
        }
    }
}
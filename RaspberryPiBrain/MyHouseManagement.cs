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
            CurrentLightState = StateLightArduino;

            SendStateToHttp(StateLightArduino);

            OrderUpdate = 1;
        }

        private int OrderUpdate { get; set; } = 0;  // Zmienna dzięki której czekam na ustawienie właściwego stanu
        public void HeartBeat()
        {
            // Zasadnicza funkcja sprawdzająca różnice i wymuszająca stany.

            // Blokuje aż do pełnego zaktualizowania

            if (OrderUpdate == 1)
            {
                if (StateLightHttp != CurrentLightState)
                {
                    if(ApplicationSettings.Debug) Logger.Write("Block upd http - current: 0b" + Convert.ToString(CurrentLightState, 2).PadLeft(8, '0') + " old: 0b" + Convert.ToString(StateLightHttp, 2).PadLeft(8, '0'));
                    SendStateToHttp(CurrentLightState);
                } else OrderUpdate = 0;
            }
            else if (OrderUpdate == 2)
            {
                if (StateLightArduino != CurrentLightState)
                {
                    if (ApplicationSettings.Debug) Logger.Write("Block upd Ardu - current: 0b" + Convert.ToString(CurrentLightState, 2).PadLeft(8,'0') + " old: 0b" + Convert.ToString(StateLightArduino, 2).PadLeft(8, '0'));
                    SendStateToArduino(CurrentLightState);
                }
                else OrderUpdate = 0;
            }
            else if (StateLightArduino != CurrentLightState)
            {
                if (ApplicationSettings.Debug) Logger.Write("Change Ardu - current: 0b" + Convert.ToString(CurrentLightState, 2).PadLeft(8, '0') + " new: 0b" + Convert.ToString(StateLightArduino, 2).PadLeft(8, '0'));
                CurrentLightState = StateLightArduino;

                SendStateToHttp(StateLightArduino);

                OrderUpdate = 1;    // Aktualizuje http na mocy Arduino
            } 
            else if (StateLightHttp != CurrentLightState)
            {
                if (ApplicationSettings.Debug) Logger.Write("Change http - current: 0b" + Convert.ToString(CurrentLightState, 2).PadLeft(8, '0') + " new: 0b" + Convert.ToString(StateLightHttp, 2).PadLeft(8, '0'));
                CurrentLightState = StateLightHttp;

                SendStateToArduino(StateLightHttp);

                OrderUpdate = 2;    // Aktualizuje Arduino na mocy http
            }
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
                    byte tempStateLightArduino = (byte)~(result & 0xFF); // 0xFF bo tylko to jest oświetleniem pozostała liczba to stan przełączników
                    if (ApplicationSettings.Debug && (tempStateLightArduino != StateLightArduino))
                    {
                        StateLightArduino = tempStateLightArduino;
                        //Logger.Write("Obecny stan oświetlenia ARD: 0b" + Convert.ToString(result, 2) + " state: 0b" + Convert.ToString(StateLightArduino, 2));
                    }
                }
            }
        }

        private static readonly string[] domNames = ["GoscinnyDuze", "GoscinnyMale", "Kuchnia", "Przedpokoj", "WC", "Sypialnia", "Aux", "ChoinkaLampka"];
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
                    NetworkManagement.SendRequest(domNames[i], newState).Wait();
                }
            }

            StateLightHttp = newStateHttp;
        }

#pragma warning disable CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.
        public string FrameToSendArduinoLight {  get; set; }
#pragma warning restore CS8618 // Pole niedopuszczające wartości null musi zawierać wartość inną niż null podczas kończenia działania konstruktora. Rozważ dodanie modyfikatora „required” lub zadeklarowanie go jako dopuszczającego wartość null.
        private void SendStateToArduino(byte newStateArduino)
        {
            FrameToSendArduinoLight = newStateArduino + "\r\n";
            if (ApplicationSettings.Debug) Logger.Write("SendStateToArduino: " + Convert.ToString(newStateArduino, 2).PadLeft(8, '0'));
        }

        public bool ChoinkaLampkaState
            => (CurrentLightState & 0x80) > 0;
        public bool GoscinnyDuzeLampkaState
            => (CurrentLightState & 0x01) > 0;

        public bool CzujnikZmierzchu {  get; private set; }
        public void SetCzujnikZmierzchu(byte[] czujnikZmierzchuBytes)
        {
            if (czujnikZmierzchuBytes?.Length > 0)
            {
                string tempCzujnikZmierzchu = string.Concat(Array.ConvertAll(czujnikZmierzchuBytes, b => (char)b));
                tempCzujnikZmierzchu = tempCzujnikZmierzchu.Trim();

                //Logger.Write("Czujnik zmierzchu: " + tempCzujnikZmierzchu + " Length: " + czujnikZmierzchuBuffer.Length);

                if (tempCzujnikZmierzchu == "RUN") CzujnikZmierzchu = true;
                else if(tempCzujnikZmierzchu == "OFF") CzujnikZmierzchu = false;
            }
        }
    }
}
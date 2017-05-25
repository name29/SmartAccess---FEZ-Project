using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.SocketInterfaces;
using Gadgeteer.Modules.GHIElectronics;

namespace Gadgeteer.Modules.Polito
{
    /// <summary>
    /// A ArduinoDistanceRFID module for Microsoft .NET Gadgeteer
    /// </summary>
    public class ArduinoDistanceRFID : GTM.Module
    {
        private RS232 rs232;

        /// <summary>
        /// 
        /// </summary>
        public enum ARDUINO_EVENT
        {
            /// <summary>
            /// 
            /// </summary>
            START=0,
            /// <summary>
            /// 
            /// </summary>
            DISTANZA_OK,
            /// <summary>
            /// 
            /// </summary>
            RFID,
            /// <summary>
            /// 
            /// </summary>
            NON_RICONOSCIUTO
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arduino_event"></param>
        /// <param name="payload"></param>
        public delegate void NewEventHandler(ArduinoDistanceRFID sender, ARDUINO_EVENT arduino_event, String payload);
        
        /// <summary>
        /// 
        /// </summary>
        public event NewEventHandler onNewEvent;
        private event NewEventHandler onNewEventInt;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="socketNumber"></param>
        public ArduinoDistanceRFID(int socketNumber)
        {
            rs232 = new RS232(socketNumber);
            rs232.Configure(9600, GT.SocketInterfaces.SerialParity.None, GT.SocketInterfaces.SerialStopBits.One, 8, GT.SocketInterfaces.HardwareFlowControl.NotRequired);
            rs232.Port.LineReceived += Port_LineReceived;
        }

        /// <summary>
        /// 
        /// </summary>
        public void reset()
        {
            rs232.Port.WriteLine("_START");
        }

        /// <summary>
        /// 
        /// </summary>
        public void distanceMode()
        {
            rs232.Port.WriteLine("_DISTANCE");
        }

        private void Port_LineReceived(GTI.Serial sender, string line)
        {
            String serialRead = line.Trim(new[] { '\r', '\n' });

            if (serialRead == "_START") onArduinoEvent(this, ARDUINO_EVENT.START, "");
            else if (serialRead == "_DISTANZA_OK") onArduinoEvent(this, ARDUINO_EVENT.DISTANZA_OK, "");
            else if (startWidth(serialRead, "_RFID"))
            {
                string[] split = serialRead.Split(' ');
                if (split.Length > 1)
                {
                    onArduinoEvent(this, ARDUINO_EVENT.RFID, split[1]);
                }
                else
                {
                    onArduinoEvent(this, ARDUINO_EVENT.NON_RICONOSCIUTO, serialRead);
                }
            }
            else onArduinoEvent(this, ARDUINO_EVENT.NON_RICONOSCIUTO, serialRead);
        }

        private Boolean startWidth(String str, String search)
        {
            return (str.Length >= search.Length && str.Substring(0, search.Length) == search);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arduino_event"></param>
        /// <param name="payload"></param>
        protected virtual void onArduinoEvent(ArduinoDistanceRFID sender, ARDUINO_EVENT arduino_event,String payload)
        {
            if (this.onNewEventInt == null)
            {
                this.onNewEventInt = new NewEventHandler(this.onArduinoEvent);
            }

            // Program.CheckAndInvoke helps event callers get onto the Dispatcher thread.  
            // If the event is null then it returns false.
            // If it is called while not on the Dispatcher thread, it returns false but also re-invokes this method on the Dispatcher.
            // If on the thread, it returns true so that the caller can execute the event.
            object[] param = new object[2] { arduino_event, payload };

            if (Program.CheckAndInvoke(onNewEvent, this.onNewEventInt, sender, param ))
            {
                this.onNewEvent(sender, arduino_event,payload);
            }
        }
    }
}

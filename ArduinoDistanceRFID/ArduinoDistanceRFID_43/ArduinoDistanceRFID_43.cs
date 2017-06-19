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
            DEBUG,
            /// <summary>
            /// 
            /// </summary>
            NON_RICONOSCIUTO
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public delegate void NewEventHandler(ArduinoDistanceRFID sender, ArduinoEventArgs args);
        
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
            String[] messages = line.Split("\r\n".ToCharArray());

            foreach (String message in messages)
            {
                if (message == "_START") onArduinoEvent(this, new ArduinoEventArgs(ARDUINO_EVENT.START, ""));
                else if (message == "_DISTANZA_OK") onArduinoEvent(this, new ArduinoEventArgs(ARDUINO_EVENT.DISTANZA_OK, ""));
                else if (startWidth(message, "_RFID"))
                {
                    string[] split = message.Split(' ');
                    if (split.Length > 1)
                    {
                        onArduinoEvent(this, new ArduinoEventArgs(ARDUINO_EVENT.RFID, split[1]));
                    }
                    else
                    {
                        onArduinoEvent(this, new ArduinoEventArgs(ARDUINO_EVENT.NON_RICONOSCIUTO, message));
                    }
                }
                else if (startWidth(message, "DEBUG"))
                {
                    onArduinoEvent(this, new ArduinoEventArgs(ARDUINO_EVENT.DEBUG, message));
                }
                else onArduinoEvent(this, new ArduinoEventArgs(ARDUINO_EVENT.NON_RICONOSCIUTO, message));
            }
        }

        private Boolean startWidth(String str, String search)
        {
            return (str.Length >= search.Length && str.Substring(0, search.Length) == search);
        }

        /// <summary>
        /// 
        /// </summary>
        public class ArduinoEventArgs
        {
            /// <summary>
            /// 
            /// </summary>
            public ARDUINO_EVENT arduino_event;
            /// <summary>
            /// 
            /// </summary>
            public String payload;
  
            /// <summary>
            /// 
            /// </summary>
            /// <param name="e"></param>
            /// <param name="s"></param>
            public ArduinoEventArgs(ARDUINO_EVENT e, string s)
            {
                arduino_event = e;
                payload = s;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void onArduinoEvent(ArduinoDistanceRFID sender, ArduinoEventArgs args)
        {
            if (this.onNewEventInt == null)
            {
                this.onNewEventInt = new NewEventHandler(this.onArduinoEvent);
            }

            // Program.CheckAndInvoke helps event callers get onto the Dispatcher thread.  
            // If the event is null then it returns false.
            // If it is called while not on the Dispatcher thread, it returns false but also re-invokes this method on the Dispatcher.
            // If on the thread, it returns true so that the caller can execute the event.

            if (Program.CheckAndInvoke(onNewEvent, this.onNewEventInt, sender, args ))
            {
                this.onNewEvent(sender,args);
            }
        }
    }
}

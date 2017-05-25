using Gadgeteer.Modules.Polito;
using Gadgeteer.Modules.GHIElectronics;
using System;
using System.Collections;
using Microsoft.SPOT;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Json.NETMF;
using System.Net;
using System.Text;
using System.Net.Sockets;
using GHI.Utilities;

namespace System.Diagnostics
{
    public enum DebuggerBrowsableState
    {
        Never,
        Collapsed,
        RootHidden
    }
}

namespace SmartAccess
{
    public partial class Program
    {
        String lastErrorMessage;
        GT.Timer timerCheckConnectivity;

        Boolean keepaliveReceived ;
        Object keepLock = new object();
        int connection_timeout = 3000;
        String ethernet_mac_address;

        public enum SmartAccessState
        {
            OFFLINE=0,
            READY,
            WAIT_DATA,
            TAKE_PHOTO,
            TAKEING_PHOTO,
            SENDING_PHOTO,
            ERROR,
            PROGRAM_RFID,
        }
        SmartAccessState modeP = SmartAccessState.OFFLINE;

        Object modeLock;
        public SmartAccessState mode
        {
            get
            {
                return modeP;
            }
            set
            {
                lock(modeLock)
                {
                    if (modeP == SmartAccessState.OFFLINE)
                    {
                        if (value == SmartAccessState.ERROR) return;

                        if (value == SmartAccessState.READY)
                        {
                            modeP = value;
                            return;
                        }
                        
                    }
                    else if ( modeP == SmartAccessState.READY )
                    {
                        if ( value == SmartAccessState.WAIT_DATA || value == SmartAccessState.TAKE_PHOTO || value == SmartAccessState.ERROR || value == SmartAccessState.OFFLINE || value == SmartAccessState.READY )
                        {
                            modeP = value;
                            return;
                        }
                        
                    }
                    else if (modeP == SmartAccessState.WAIT_DATA)
                    {
                        if ( value == SmartAccessState.PROGRAM_RFID ||  value == SmartAccessState.TAKE_PHOTO || value == SmartAccessState.ERROR || value == SmartAccessState.OFFLINE || value == SmartAccessState.READY)
                        {
                            modeP = value;
                            return;
                        }
                        
                    }
                    else if (modeP == SmartAccessState.TAKE_PHOTO)
                    {
                        if (value == SmartAccessState.TAKEING_PHOTO || value == SmartAccessState.ERROR || value == SmartAccessState.OFFLINE || value == SmartAccessState.READY)
                        {
                            modeP = value;
                            return;
                        }
                        
                    }
                    else if (modeP == SmartAccessState.TAKEING_PHOTO)
                    {
                        if (value == SmartAccessState.SENDING_PHOTO || value == SmartAccessState.ERROR || value == SmartAccessState.OFFLINE || value == SmartAccessState.READY)
                        {
                            modeP = value;
                            return;
                        }
                        
                    }
                    else if (modeP == SmartAccessState.SENDING_PHOTO)
                    {
                        if (value == SmartAccessState.READY || value == SmartAccessState.ERROR || value == SmartAccessState.OFFLINE )
                        {
                            modeP = value;
                            return;
                        }
                        
                    }
                    else if ( modeP == SmartAccessState.ERROR)
                    {
                        if (value == SmartAccessState.READY || value == SmartAccessState.OFFLINE )
                        {
                            modeP = value;
                            return;
                        }
                    }
                    else if (modeP == SmartAccessState.PROGRAM_RFID)
                    {
                        if (value == SmartAccessState.READY || value == SmartAccessState.OFFLINE || value == SmartAccessState.ERROR)
                        {
                            modeP = value;
                            return;
                        }
                    }

                    throw new Exception("Invalid SET");
                }
            }
        }
        WebEvent remoteLogin;

        String webserviceHost = "vps.name29.net";
        int webserviceHTTPPort = 3000;
        int webserviceSFTPort = 5000;
        String webserviceBaseURL;

        Object isNetworkUpLock;
        Boolean isNetworkUp;

        String rfidIDGLOBAL;
        String testoGLOBAL;
        GT.Picture pictureGLOBAL;

        Boolean isPhyNetworkUp;
        GUIManager guiManager;

        GT.SocketInterfaces.AnalogOutput audioOutput;
        Microsoft.SPOT.Font displayFont = Resources.GetFont(Resources.FontResources.small);
        SimpleFileTransfer sft;

        OurCamera camera;

        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            modeLock = new object();

            guiManager = new GUIManager();
            guiManager.OnErrorCancel += GuiManager_OnErrorCancel;

            webserviceBaseURL = "http://" + webserviceHost + ":" + webserviceHTTPPort + "/";

            isNetworkUp = false;
            isNetworkUpLock = new object();
            keepaliveReceived = true;

            setSystemOffline("Starting...");
            Debug.Print("Starting...");

            lastErrorMessage = "";
            rfidIDGLOBAL = "";
            testoGLOBAL = "";

            isPhyNetworkUp = false;

            timerCheckConnectivity = new GT.Timer(10000);
            timerCheckConnectivity.Tick += this.tickTimerCheckConnectivity;

            //rs232.Configure(9600, GT.SocketInterfaces.SerialParity.None, GT.SocketInterfaces.SerialStopBits.One, 8, GT.SocketInterfaces.HardwareFlowControl.NotRequired);
            //rs232.Port.LineReceived += Port_LineReceived;

            camera = new OurCamera(3);

            camera.CameraConnected += Camera_CameraConnected;
            camera.CameraDisconnected += Camera_CameraDisconnected;
            camera.BitmapStreamed += Camera_BitmapStreamed;


            audioOutput = breakout.CreateAnalogOutput(Gadgeteer.Socket.Pin.Five);

            button.Mode = GTM.GHIElectronics.Button.LedMode.OnWhilePressed;
            button.ButtonPressed += Button_ButtonPressed;

            if(!ethernetJ11D.NetworkInterface.NetworkAvailable)
            {
                isPhyNetworkUp = false;
                //WebServer.StopLocalServer();
                isNetworkUp = false;
                setSystemOffline("ERR12 Server non raggiungibile");
            }

            Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            //Microsoft.SPOT.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;

            StringBuilder sb = new StringBuilder(18);
            foreach (byte b in ethernetJ11D.NetworkSettings.PhysicalAddress)
            {
                if (sb.Length > 0)
                    sb.Append(':');
                sb.Append(b.ToString("X2"));
            }

            ethernet_mac_address = sb.ToString();

            //ethernetJ11D.NetworkUp += EthernetJ11D_NetworkUp;
            //ethernetJ11D.NetworkDown += EthernetJ11D_NetworkUp;
            //ethernetJ11D.UseDHCP();
            ethernetJ11D.NetworkSettings.EnableDynamicDns();
            ethernetJ11D.NetworkSettings.EnableDhcp();
            //ethernetJ11D.NetworkSettings.EnableStaticIP("192.168.98.100", "255.255.255.0", "192.168.98.1");
            //string[] dnsserver = { "8.8.8.8", "8.8.4.4" };
            //ethernetJ11D.NetworkSettings.EnableStaticDns(dnsserver);

            string ipAddress = ethernetJ11D.NetworkSettings.IPAddress;
            WebServer.StartLocalServer("0.0.0.0", 3000);

            ////Register to path
            remoteLogin = WebServer.SetupWebEvent("remoteLogin");
            remoteLogin.WebEventReceived += RemoteLogin_WebEventReceived;


            ethernetJ11D.UseThisNetworkInterface();

            sft = new SimpleFileTransfer(webserviceHost, webserviceSFTPort, connection_timeout + 6000);


            camera.CurrentPictureResolution = OurCamera.PictureResolution.Resolution320x240;
            camera.run(new Bitmap(320, 240));

            arduinoDistanceRFID.onNewEvent += ArduinoDistanceRFID_onNewEvent;
            arduinoDistanceRFID.reset();

            //rs232.Port.WriteLine("_START");
        }

        private void GuiManager_OnErrorCancel()
        {
            if ( mode != SmartAccessState.OFFLINE ) mode = SmartAccessState.READY;
            guiManager.showWelcome();
        }

        private void Camera_BitmapStreamed(OurCamera sender, Bitmap e)
        {
            if (mode == SmartAccessState.TAKE_PHOTO || mode == SmartAccessState.TAKEING_PHOTO)
            {
                if (mode == SmartAccessState.TAKEING_PHOTO)
                {
                    mode = SmartAccessState.SENDING_PHOTO;
                    byte[] bmpFile = Bitmaps.ConvertToFile(e);

                    pictureGLOBAL = new GT.Picture(bmpFile, GT.Picture.PictureEncoding.BMP);

                    guiManager.setStream("Foto scattata con successo! Salvataggio in corso...", e);
                    saveAndUploadPhoto();
                }
                else
                {
                    guiManager.setStream(e, true);
                }

                camera.continueStreaming();
            }
        }

        private void EthernetJ11D_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            lock (isNetworkUpLock)
            {
                if (state == GTM.Module.NetworkModule.NetworkState.Up)
                {
                    isPhyNetworkUp = true;

                    tickTimerCheckConnectivity(timerCheckConnectivity);

                    //isNetworkUp = true;
                    //Setup the webserver


                    ////programRfid = WebServer.SetupWebEvent("programRfid");
                    ////programRfid.WebEventReceived += ProgramRfid_WebEventReceived;

                    //TimeServiceSettings time = new TimeServiceSettings()
                    //{
                    //    ForceSyncAtWakeUp = true
                    //};

                    //try
                    //{
                    //    //IPAddress[] address = Dns.GetHostEntry("ntp.inrim.it").AddressList;
                    //    //time.PrimaryServer = address[0].GetAddressBytes();
                    //    time.PrimaryServer = IPAddress.Parse("193.204.114.105").GetAddressBytes();
                    //    TimeService.Settings = time;
                    //    TimeService.SetTimeZoneOffset(1);
                    //    //                TimeService.Start();
                    //}
                    //catch (System.Net.Sockets.SocketException ex)
                    //{
                    //    Debug.Print("Impossibile attivare il servizio del tempo: " + ex.Message);
                    //    //TODO 
                    //}
                    ////Restart the check connectivity timer
                    //timerCheckConnectivity.Restart();
                    //timerCheckConnectivity.Start();
                    //setSystemOnline();
                }
                else
                {
                    isNetworkUp = false;
                    setSystemOffline("ERR11 Server non raggiungibile");
                }
            }
        }

        private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
        {
            lock (isNetworkUpLock)
            {
                if (ethernetJ11D.NetworkInterface.NetworkAvailable && !isNetworkUp)
                {
                    isPhyNetworkUp = true;

                    tickTimerCheckConnectivity(timerCheckConnectivity);

                    //isNetworkUp = true;
                    //Setup the webserver
                    //string ipAddress = ethernetJ11D.NetworkSettings.IPAddress;
                    //WebServer.StartLocalServer(ipAddress, 80);

                    ////Register to path
                    //remoteLogin = WebServer.SetupWebEvent("remoteLogin");
                    //remoteLogin.WebEventReceived += RemoteLogin_WebEventReceived;

                    //programRfid = WebServer.SetupWebEvent("programRfid");
                    //programRfid.WebEventReceived += ProgramRfid_WebEventReceived;

                    //TimeServiceSettings time = new TimeServiceSettings()
                    //{
                    //    ForceSyncAtWakeUp = true
                    //};

                    //try
                    //{
                    //    //IPAddress[] address = Dns.GetHostEntry("ntp.inrim.it").AddressList;
                    //    //time.PrimaryServer = address[0].GetAddressBytes();
                    //    time.PrimaryServer = IPAddress.Parse("193.204.114.105").GetAddressBytes();
                    //    TimeService.Settings = time;
                    //    TimeService.SetTimeZoneOffset(1);
                    //    //                TimeService.Start();
                    //}
                    //catch (System.Net.Sockets.SocketException ex)
                    //{
                    //    Debug.Print("Impossibile attivare il servizio del tempo: " + ex.Message);
                    //    //TODO 
                    //}
                    //Restart the check connectivity timer
                    //timerCheckConnectivity.Restart();
                    //timerCheckConnectivity.Start();
                    //setSystemOnline();
                }
            }
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, Microsoft.SPOT.Net.NetworkInformation.NetworkAvailabilityEventArgs e)
        {
            if ( e.IsAvailable)
            {
                isPhyNetworkUp = true;
                tickTimerCheckConnectivity(timerCheckConnectivity);
            }
            else
            {
                isPhyNetworkUp = false;
                lock (isNetworkUpLock)
                {
                    //                WebServer.StopLocalServer();
                    isNetworkUp = false;
                    setSystemOffline("ERR12 Server non raggiungibile");
                }
            }
        }

        int counter = 0;
        private void Button_ButtonPressed(Button sender, Button.ButtonState state)
        {
            String line = "";
            if (mode != SmartAccessState.TAKE_PHOTO)
            {
                counter = 0;
            }

            if ( counter == 0 )
            {
                line = "_RFID 01020304";
                counter = 1;
                ArduinoDistanceRFID_onNewEvent(null, ArduinoDistanceRFID.ARDUINO_EVENT.RFID, "01020304");
            }
            else if ( counter == 1 )
            {
                line = "_DISTANZA_VICINO";
                counter = 2;
            }
            else if (counter == 2)
            {
                line = "_DISTANZA_LONTANO";
                counter = 3;
            }
            else if (counter == 3)
            {
                line = "_DISTANZA_OK";
                counter = 0;
                ArduinoDistanceRFID_onNewEvent(null, ArduinoDistanceRFID.ARDUINO_EVENT.DISTANZA_OK, "");
            }


            //Port_LineReceived(null, line);
        }

        private void Camera_CameraDisconnected(OurCamera sender)
        {
        }

        private void Camera_CameraConnected(OurCamera sender)
        {
        }


        /// <summary>
        /// Set the system as offline ( not connectivity with the webservice server)
        /// </summary>
        /// <param name="error">Error to show/log</param>
        private void setSystemOffline(String error)
        {
            //camera.StopStreamingBitmaps();
            //TODO fare meglio, se richiamata con un altro messaggio di errore, cambiare il messaggio d'errore
            if (mode != SmartAccessState.OFFLINE)
            {
                mode = SmartAccessState.OFFLINE;
                lastErrorMessage = "";
                isNetworkUp = false;
            }
            if ( lastErrorMessage != error)
            {
                guiManager.showError(error);
            }
        }

        /// <summary>
        /// Set the system as online (the FEZ can reach the webservice)
        /// </summary>
        private void setSystemOnline()
        {
            if (mode == SmartAccessState.OFFLINE )
            {
                mode = SmartAccessState.READY;
                Debug.Print("Il sistema e' di nuovo online!");
                //TODO rimuovere eventuali stati di errore (schermate,ecc...)
                clearErrorScreen();
            }
        }

        private void setWeather(String description, String icon)
        {
            if ( mode == SmartAccessState.READY)
            {
                guiManager.showWeather(description, icon);
            }
        }

        private void takePhoto()
        {
            //guiManager.showMessage("Riproduzione audio");
            //PlayFile(audioGLOBAL);
            camera.continueStreaming();
            mode = SmartAccessState.TAKE_PHOTO;

            guiManager.setStream(testoGLOBAL);
        }

        private void clearErrorScreen()
        {
            mode = SmartAccessState.READY;
            guiManager.showWelcome();
        }


        private void handleWebException(Exception e, char suffix, Boolean setOffline)
        {
            lock (isNetworkUpLock)
            {
                if (isNetworkUp)
                {
                    if (e is WebException)
                    {
                        WebException we = (WebException)e;

                        if (we.Status == WebExceptionStatus.Timeout)
                        {
                            if (setOffline) setSystemOffline("ERR07"+suffix+ " Server non raggiungibile");
                            else
                            {
                                mode = SmartAccessState.ERROR;
                                guiManager.showErrorWithCancel("ERR07" + suffix + " Ingresso/Uscita non registrata.");
                            }
                        }
                        else if (we.Status == WebExceptionStatus.ConnectFailure)
                        {
                            if (setOffline) setSystemOffline("ERR08" + suffix + " Server non raggiungibile");
                            else
                            {
                                mode = SmartAccessState.ERROR;
                                guiManager.showErrorWithCancel("ERR08" + suffix + " Ingresso/Uscita non registrata.");
                            }
                        }
                        else
                        {
                            if (setOffline) setSystemOffline("ERR08" + suffix + " Server non raggiungibile" + we.Status.ToString());
                            else
                            {
                                mode = SmartAccessState.ERROR;
                                guiManager.showErrorWithCancel("ERR08" + suffix + " Ingresso/Uscita non registrata. HTTP" + we.Status.ToString());
                            }
                        }
                    }
                    else if (e is SFTException)
                    {
                        SFTException se = (SFTException)e;

                        if (setOffline) setSystemOffline("ERR17" + suffix + " Server non raggiungibile " + se.Message);
                        else
                        {
                            mode = SmartAccessState.ERROR;
                            guiManager.showErrorWithCancel("ERR18" + suffix + " Ingresso/Uscita non registrata." + se.Message);
                        }
                    }
                    else if (e is SocketException)
                    {
                        SocketException se = (SocketException)e;

                        if (setOffline) setSystemOffline("ERR09" + suffix + " Server non raggiungibile " + se.ErrorCode.ToString());
                        else
                        {
                            mode = SmartAccessState.ERROR;
                            guiManager.showErrorWithCancel("ERR09" + suffix + " Ingresso/Uscita non registrata." + se.ErrorCode.ToString());
                        }
                    }
                    else
                    {
                        if (setOffline) setSystemOffline("ERR10" + suffix + " Server non raggiungibile" + e.Message);
                        else
                        {
                            mode = SmartAccessState.ERROR;
                            guiManager.showErrorWithCancel("ERR10" + suffix + " Ingresso/Uscita non registrata." + e.Message);
                        }
                    }
                }
            }
        }

        /* **************************************************************************************************
         *   
         *  Handle WebService (SERVER)
         * 
         * ************************************************************************************************** */

        /// <summary>
        /// Handle request for /remoteLogin. This function start the verification photo procedure and use the rfid received from the http request
        /// </summary>
        /// 
        delegate void TakePhotoHandler();

        private void RemoteLogin_WebEventReceived(string path, WebServer.HttpMethod method, Responder responder)
        {
            if ( mode != SmartAccessState.READY )
            {
                responder.Respond(System.Text.Encoding.UTF8.GetBytes("{ success: false }"), "application/json");
                return;
            }
            if (responder.HttpMethod == WebServer.HttpMethod.POST && startWidth(responder.Body.ContentType.Trim(), "application/json"))
            {
                /*Path starting zeros BUG!  https://www.ghielectronics.com/community/forum/topic?id=15039 */
                Byte[] bArray = null;
                Boolean copyFlag = false;

                int j = 0;
                for (int i = 0; i < responder.Body.RawContent.Length; i++)
                {
                    if (!copyFlag && responder.Body.RawContent[i] != 0)
                    {
                        copyFlag = true;
                        bArray = new Byte[responder.Body.RawContent.Length - i];
                    }
                    if (copyFlag)
                    {
                        bArray[j] = responder.Body.RawContent[i];
                        j++;
                    }
                }

                //String jsonData = responder.Body.Text;
                String jsonData = new String(System.Text.Encoding.UTF8.GetChars(bArray));

                Hashtable t = (Hashtable)JsonSerializer.DeserializeString(jsonData);

                if (t != null && t.Contains("rfidID") && t.Contains("message") && t.Contains("member_id"))
                {
                    rfidIDGLOBAL = (String)t["rfidID"];
                    testoGLOBAL = (String)t["message"];
                    //audioGLOBAL = new Byte[0];
                    long memberId = (long)t["member_id"];

                    responder.Respond(System.Text.Encoding.UTF8.GetBytes("{ success: true }"), "application/json");

                    arduinoDistanceRFID.distanceMode();
                    guiManager.showMessage("Login remoto in corso...");
                    takePhoto();

                    //Thread th = new Thread(() =>
                    //{
                    //    try
                    //    {

                    //        MyFTP ftp = new MyFTP(webserviceHost, webserviceFTPPort, "smartaccess", "smartaccess", connection_timeout + 6000);

                    //        audioGLOBAL = ftp.get("audio/" + memberId + "/" + (String)t["audio_file"]);
                    //        Program.BeginInvoke(new System.Threading.ThreadStart(delegate { takePhoto();  }));
                    //    }
                    //    catch (Exception e)
                    //    {
                    //        mode = SmartAccessState.ERROR;
                    //        guiManager.showErrorWithCancel("ERR20 Ingresso/Uscita non registrata");
                    //        return;
                    //    }

                    //});
                    //th.Start();
                }
                else
                {
                    mode = SmartAccessState.ERROR;
                    guiManager.showErrorWithCancel("ERR22 Ingresso/Uscita non registrata");
                    responder.Respond(System.Text.Encoding.UTF8.GetBytes("{ success: false }"), "application/json");
                    return;
                }
            }
            else
            {
                mode = SmartAccessState.ERROR;
                guiManager.showErrorWithCancel("ERR23 Ingresso/Uscita non registrata");
                responder.Respond(System.Text.Encoding.UTF8.GetBytes("{ success: false }"), "application/json");
                return;
            }
        }


        /* **************************************************************************************************
         *   
         *  Call and Handle WebService (CLIENT)
         * 
         * ************************************************************************************************** */

        Object lockCheckConnTick = new object();
        /// <summary>
        /// Periodically called by the timer in order to check if the WebService is reachable
        /// </summary>
        /// <param name="timer"></param>
        private void tickTimerCheckConnectivity(GT.Timer timer)
        {
            lock (lockCheckConnTick)
            {
                //Debug.GC(false);
                //Debug.EnableGCMessages(true);
                if (!timer.IsRunning) timer.Start();
                if (mode != SmartAccessState.ERROR && mode != SmartAccessState.OFFLINE && mode != SmartAccessState.READY) return;
                if (!keepaliveReceived) return;

                if (isPhyNetworkUp)
                {
                    String jsonSend = "{\"mac_address\":\"" + ethernet_mac_address + "\"}";
                    POSTContent postContent = POSTContent.CreateTextBasedContent(jsonSend);
                    HttpRequest lastCheckRequest = HttpHelper.CreateHttpPostRequest(webserviceBaseURL + "rfid/keep", postContent, "application/json", connection_timeout);

                    lastCheckRequest.ExceptionThrown += checkConnectivityException;
                    lastCheckRequest.ResponseReceived += checkConnectivityResponse;

                    lastCheckRequest.SendRequest();
                    keepaliveReceived = false;
                }
            }
        }

        private void checkConnectivityException(HttpRequest sender, Exception e)
        {
            handleWebException(e, 'K', true);
            keepaliveReceived = true;
        }


        /// <summary>
        /// Manage Http Response from checkConnectivity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void checkConnectivityResponse(HttpRequest sender, HttpResponse response)
        {
            lock (isNetworkUpLock)
            {
                if (response.StatusCode != "200")
                {
                    setSystemOffline(response.StatusCode);
                    isNetworkUp = false;
                }
                else
                {
                    setSystemOnline();
                    isNetworkUp = true;

                    String jsonData = response.Text;
                    Hashtable t = (Hashtable)JsonSerializer.DeserializeString(jsonData);

                    if (t != null && t.Contains("description") && t.Contains("icon"))
                    {
                        setWeather( (String)t["description"], (String) t["icon"]);
                    }

                }
                keepaliveReceived = true;
            }
        }

        /// <summary>
        /// Get information about rfidGLOBAL from the WebService
        /// </summary>
        private void getRfidDetails()
        {
            if ( isNetworkUp )
            {
                guiManager.showMessage("Interrogazione server in corso...");

                GETContent getContent = new GETContent();
                HttpRequest req = HttpHelper.CreateHttpGetRequest(webserviceBaseURL +"rfid/"+rfidIDGLOBAL,getContent,connection_timeout);

                req.ExceptionThrown += getRfidDetailsException;
                req.ResponseReceived += getRfidDetailsRespone;
                req.SendRequest();
            }
        }

        /// <summary>
        /// Handle exception inside the HttpRequest
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void getRfidDetailsException(HttpRequest sender, Exception e)
        {
            handleWebException(e, 'D', false);
        }

        /// <summary>
        /// Handle response from HTTP getDetails request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void getRfidDetailsRespone(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode == "200")
            {
                if (startWidth(response.ContentType,"application/json"))
                {
                    String jsonData = response.Text;
                    Hashtable t = (Hashtable)JsonSerializer.DeserializeString(jsonData);

                    if (t.Contains("member"))
                    {
                        Hashtable member = (Hashtable)t["member"];
                        if (member.Contains("audio_file") && member.Contains("message") && member.Contains("id") )
                        {
                            testoGLOBAL = (String)member["message"];
                            //audioGLOBAL = new Byte[0];
                            long memberId = (long)member["id"];

                            //try
                            //{

                            //    MyFTP ftp = new MyFTP(webserviceHost, webserviceFTPPort, "smartaccess", "smartaccess", connection_timeout + 6000);

                            //    audioGLOBAL = ftp.get("audio/" + memberId + "/" + (String)member["audio_file"]);

                            //    takePhoto();
                            //}
                            //catch (Exception e)
                            //{
                            //    mode = SmartAccessState.ERROR;
                            //    guiManager.showErrorWithCancel("ERR15 Ingresso/Uscita non registrata");
                            //}

                            takePhoto();

                        }
                        else
                        {
                            mode = SmartAccessState.ERROR;
                            guiManager.showErrorWithCancel("ERR02 Ingresso/Uscita non registrata");
                        }
                    }
                    else
                    {
                        mode = SmartAccessState.ERROR;
                        guiManager.showErrorWithCancel("ERR19 Ingresso/Uscita non registrata");
                    }
                }
                else
                {
                    mode = SmartAccessState.ERROR;
                    guiManager.showErrorWithCancel("ERR03 Ingresso/Uscita non registrata");
                }
            }
            else if (response.StatusCode == "404")
            {
                if (isNetworkUp)
                {
                    mode = SmartAccessState.PROGRAM_RFID;

                    guiManager.showMessage("Creazione RFID nel database...");

                    //rs232.Port.WriteLine("_START");

                    arduinoDistanceRFID.reset();

                    POSTContent postContent = POSTContent.CreateTextBasedContent("{\"code\":\"" + rfidIDGLOBAL + "\"}");
                    HttpRequest req = HttpHelper.CreateHttpPostRequest(webserviceBaseURL + "rfid/set", postContent,
                        "application/json", connection_timeout);

                    req.ExceptionThrown += createRfidException;
                    req.ResponseReceived += createRfidResponse;
                    req.SendRequest();
                }
            }
            else
            {
                mode = SmartAccessState.ERROR;
                guiManager.showErrorWithCancel("ERR04 Ingresso/Uscita non registrata HTTP:" + response.StatusCode);
            }
        }

        private void createRfidException(HttpRequest sender, Exception e)
        {
            handleWebException(e, 'C', false);
        }

        private void createRfidResponse(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode == "201")
            {
                mode = SmartAccessState.READY;

                guiManager.showThankYou();
            }
            else if (response.StatusCode == "200")
            {
                mode = SmartAccessState.READY;
                guiManager.showTemp("Rfid non associata a nessun dipendente =)", GT.Color.Orange, 10000);

            }
            else
            {
                guiManager.showErrorWithCancel("ERR05 Ingresso/Uscita non registrata HTTP:" + response.StatusCode);
                mode = SmartAccessState.ERROR;
            }
        }


        /// <summary>
        /// Send the photo to the WebService
        /// </summary>
        private void saveAndUploadPhoto()
        {
            if (isNetworkUp)
            {
                String jsonSend = "{\"rfidID\":\""+rfidIDGLOBAL+"\",\"image_ext\":\"jpg\"}";
                POSTContent postContent = POSTContent.CreateTextBasedContent(jsonSend);
                HttpRequest req = HttpHelper.CreateHttpPostRequest(webserviceBaseURL + "rfid/save",
                                postContent, "application/json",connection_timeout);

                req.ExceptionThrown += Req_ExceptionThrown;
                req.ResponseReceived += sendPhotoResponse;
                req.SendRequest();
            }
        }

        /// <summary>
        /// Handle Exception inside the HTTP Request
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Req_ExceptionThrown(HttpRequest sender, Exception e)
        {
            handleWebException(e, 'U', false);
        }

        /// <summary>
        /// Handle HTTP Response from saveAndUploadPhoto
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        private void sendPhotoResponse(HttpRequest sender, HttpResponse response)
        {
            if (response.StatusCode == "200")
            {
                if (startWidth(response.ContentType, "application/json"))
                {
                    String jsonData = response.Text;
                    Hashtable t = (Hashtable)JsonSerializer.DeserializeString(jsonData);

                    if (t.Contains("photo_path") && t.Contains("check_in_photo") && t.Contains("check_out_photo"))
                    {
                        String image_filename = (String)t["photo_path"];
                        if ( t["check_out_photo"] != null)
                        {
                            image_filename += t["check_out_photo"];
                        }
                        else
                        {
                            image_filename += t["check_in_photo"];
                        }

                        try
                        {
                            sft.upload("images/"+image_filename+".bmp", pictureGLOBAL.PictureData);

                            mode = SmartAccessState.READY;

                            guiManager.showThankYou();
                        }
                        catch (Exception e)
                        {
                            handleWebException(e, 'F', false);
                            return;
                        }
                    }
                    else
                    {
                        mode = SmartAccessState.ERROR;
                        guiManager.showErrorWithCancel("ERR13 Ingresso/Uscita non registrata");
                    }
                }
                else
                {
                    mode = SmartAccessState.ERROR;
                    guiManager.showErrorWithCancel("ERR14 Ingresso/Uscita non registrata");
                }
            }
            else
            {
                guiManager.showErrorWithCancel("ERR06 Ingresso/Uscita non registrata HTTP:" + response.StatusCode);
                mode = SmartAccessState.ERROR;
            }
        }

        /* **************************************************************************************************
         *   
         *  Serial Port and Audio Playback
         * 
         * ************************************************************************************************** */

        /*
        private void Port_LineReceived(GT.SocketInterfaces.Serial sender, string line)
        {
            String serialRead = line.Trim(new[] { '\r', '\n' });
            
            if ( serialRead == "_START")
            {
                try
                {
                    mode = SmartAccessState.READY;
                    guiManager.resetStatus();
                    guiManager.showWelcome();
                }
                catch (Exception)
                {
                }
            }
            else if (serialRead == "_DISTANZA_OK")
            {
                if (mode == SmartAccessState.TAKE_PHOTO)
                {
                    mode = SmartAccessState.TAKEING_PHOTO;

                    //camera.StopStreaming();
                    //gman.writeOnScreen("Foto in corso!", GT.Color.Green, false);
                    //camera.TakePicture();
                    guiManager.setStream("Foto in corso!");
                }
            }
            else if (serialRead == "_DISTANZA_LONTANO")
            {
                if (mode == SmartAccessState.TAKE_PHOTO)
                {
                    guiManager.setStream("Faccia troppo lontana");
                }
            }
            else if (serialRead == "_DISTANZA_VICINO")
            {
                if (mode == SmartAccessState.TAKE_PHOTO)
                {
                    guiManager.setStream("Faccia troppo vicina");
                }
            }
            else if (startWidth(serialRead,"_RFID"))
            {
                if ( mode == SmartAccessState.ERROR)
                {
                    mode = SmartAccessState.READY;
                    guiManager.resetStatus();
                }

                if ( mode == SmartAccessState.READY)
                {
                    mode = SmartAccessState.WAIT_DATA;
                    rfidIDGLOBAL = serialRead.Split(' ')[1];
                    getRfidDetails();
                }
            }
            else
            {
                Debug.Print("Ricevuto strano messaggio:" + serialRead);
                //gman.showErrorWithCancel("Strano messaggio ricevuto dalla linea seriale:" + serialRead);
            }
        }
        */

        private void ArduinoDistanceRFID_onNewEvent(ArduinoDistanceRFID sender, ArduinoDistanceRFID.ARDUINO_EVENT arduino_event, string payload)
        {
            if ( arduino_event == ArduinoDistanceRFID.ARDUINO_EVENT.START)
            {
                try
                {
                    mode = SmartAccessState.READY;
                    guiManager.resetStatus();
                    guiManager.showWelcome();
                }
                catch (Exception)
                {
                }
            }
            else if (arduino_event == ArduinoDistanceRFID.ARDUINO_EVENT.DISTANZA_OK)
            {
                if (mode == SmartAccessState.TAKE_PHOTO)
                {
                    mode = SmartAccessState.TAKEING_PHOTO;
                    guiManager.setStream("Foto in corso!");
                }
            }
            else if ( arduino_event == ArduinoDistanceRFID.ARDUINO_EVENT.RFID)
            {
                if (mode == SmartAccessState.ERROR)
                {
                    mode = SmartAccessState.READY;
                    guiManager.resetStatus();
                }

                if (mode == SmartAccessState.READY)
                {
                    mode = SmartAccessState.WAIT_DATA;
                    rfidIDGLOBAL = payload;
                    getRfidDetails();
                }
            }
            else
            {
                Debug.Print("Ricevuto strano messaggio da arduino ("+payload+")");
            }
        }

        /*
	    public void PlayFile(Byte[] audio)
        {
            if (audio.Length > 0)
            {
                GHI.IO.Audio.PlayPcm(Microsoft.SPOT.Hardware.Cpu.AnalogOutputChannel.ANALOG_OUTPUT_0, audio, 8000);
            }
        }
        */

        private Boolean startWidth(String str, String search)
        {
            return (str.Length >= search.Length && str.Substring(0, search.Length) == search);
        }
    }
}
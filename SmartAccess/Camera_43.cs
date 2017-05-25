using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using System.Threading;

using Microsoft.SPOT.Hardware;

//using GHI.Premium.USBHost;
//using GHI.Premium.System;
//using GHIElectronics.NETMF.USBHost;
//using GHIElectronics.NETMF.System;
using Gadgeteer.Modules.GHIElectronics;
using GHI.Usb.Host;
using Gadgeteer;

namespace SmartAccess
{
    public class OurCamera : GTM.Module
    {
        Webcam _camera;
        private Thread _cameraThread;
        private object cameraLock = new object();
        private Bitmap _targetBitmap;

        private bool _runThread;
        private System.Threading.AutoResetEvent are;
        private DateTime _lastTimeStreamStart;
        private int _restartStreamingTrigger = 1500;

        private enum CameraStatus
        {
            Disconnected = 0,
            Ready = 1,
            //TakePicture = 2,
            //StreamBitmap = 3
        }
        private CameraStatus _cameraStatus;

        /// <summary></summary>
        /// <param name="socketNumber">The mainboard socket that has the camera module plugged into it.</param>
        public OurCamera(int socketNumber)
        {
            GT.Socket socket = GT.Socket.GetSocket(socketNumber, true, this, null);

            if (socket.SupportsType('H') == false)
            {
                throw new GT.Socket.InvalidSocketException("Socket " + socket +
                    " does not support support Camera modules. Please plug the Camera module into a socket labeled 'H'");
            }

            are = new AutoResetEvent(false);
            _runThread = false;

            // Reserve the pins used by the USBHost interface
            // These calls will throw PinConflictExcpetion if they are already reserved
            try
            {
                socket.ReservePin(GT.Socket.Pin.Three, this);
                socket.ReservePin(GT.Socket.Pin.Four, this);
                socket.ReservePin(GT.Socket.Pin.Five, this);
            }

            catch (Exception e)
            {
                throw new GT.Socket.InvalidSocketException("There is an issue connecting the Camera module to socket " + socketNumber +
                    ". Please check that all modules are connected to the correct sockets or try connecting the Camera to a different 'H' socket", e);
            }

            CurrentPictureResolution = PictureResolution.Resolution320x240;
            _cameraStatus = CameraStatus.Disconnected;
        }

        public void run(Bitmap target)
        {
            _targetBitmap = target;

            Controller.WebcamConnected += Controller_WebcamConnected;
            foreach (BaseDevice bd in Controller.GetConnectedDevices())
            {
                if (bd.Type == BaseDevice.DeviceType.Webcam)
                {
                    Controller_WebcamConnected(null, new Webcam(bd.Id, bd.InterfaceIndex, bd.VendorId, bd.ProductId, bd.PortNumber));
                }
            }
        }

        private void Controller_WebcamConnected(object sender, Webcam e)
        {
            lock(this)
            {
                if (this._cameraStatus != CameraStatus.Disconnected) return;

                try
                {
                    //_camera = new USBH_Webcam(device);
                    _camera = e;
                    _camera.Disconnected += _camera_Disconnected;

                    PictureResolution p = new PictureResolution(_targetBitmap.Width, _targetBitmap.Height);
                    _camera.WorkerInterval = 100;
                    _camera.StartStreaming(GetImageFormat(p));

                    this._cameraThread = new Thread(CameraCommunication);
                    _runThread = true;
                    this._cameraThread.Start();

                    this._cameraStatus = CameraStatus.Ready;
                    DebugPrint("Camera connected.");
                    CameraConnectedEvent(this);
                }
                catch(Exception ex)
                {
                    ErrorPrint("Eccezione " + ex.Message);
                }
            }
        }

        private void _camera_Disconnected(BaseDevice sender, EventArgs e)
        {
            try
            {
                _cameraStatus = CameraStatus.Disconnected;
                _runThread = false;
                //_TakePictureFlag = false;
                this._camera.StopStreaming();
                _camera = null;
                DebugPrint("Camera disconnected.");
                OnCameraDisconnectedEvent(this);
            }

            catch
            { }
        }

        /// <summary>
        ///  Delegate method to handle the <see cref="CameraConnectedEvent"/>.
        /// </summary>
        /// <param name="sender"></param>
        public delegate void CameraConnectedEventHandler(OurCamera sender);

        /// <summary>
        /// Event raised when a Camera is connected to the Mainboard.
        /// </summary>
        public event CameraConnectedEventHandler CameraConnected;

        private CameraConnectedEventHandler _CameraConnected;

        /// <summary>
        /// Raises the <see cref="CameraConnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="OurCamera"/> object that raised the event.</param>
        protected virtual void CameraConnectedEvent(OurCamera sender)
        {
            if (_CameraConnected == null) _CameraConnected = new CameraConnectedEventHandler(CameraConnectedEvent);
            if (Program.CheckAndInvoke(CameraConnected, _CameraConnected, sender))
            {
                CameraConnected(sender);
            }
        }

        /// <summary>
        /// Represents the delegate that is used for the <see cref="CameraDisconnected"/>.
        /// </summary>
        /// <param name="sender">The <see cref="OurCamera"/> object that raised the event.</param>
        public delegate void CameraDisconnectedEventHandler(OurCamera sender);

        /// <summary>
        /// Event raised when a Camera is disconnected from the Mainboard.
        /// </summary>
        public event CameraDisconnectedEventHandler CameraDisconnected;

        private CameraDisconnectedEventHandler _CameraDisconnected;

        /// <summary>
        /// Raises the <see cref="CameraDisconnected"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="OurCamera"/> object that raised the event.</param>
        protected virtual void OnCameraDisconnectedEvent(OurCamera sender)
        {
            if (_CameraDisconnected == null) _CameraDisconnected = new CameraDisconnectedEventHandler(OnCameraDisconnectedEvent);
            if (Program.CheckAndInvoke(CameraDisconnected, _CameraDisconnected, sender))
            {
                CameraDisconnected(sender);
            }
        }

        /// <summary>
        /// Gets the ready status of the camera.
        /// </summary>
        /// <remarks>
        /// After you create a <see cref="Camera"/> object, the camera initializes asynchronously; 
        /// it takes approximately 400-600 milliseconds before this property returns <b>true</b>.
        /// </remarks>
        public bool CameraReady { get { return this._cameraStatus == CameraStatus.Ready; } }


        /// <summary>
        /// Class that specifies the image resolutions supported by the camera.
        /// </summary>
        public class PictureResolution
        {
            /// <summary>
            /// Gets the width of the picture resolution.
            /// </summary>
            public int Width { get; private set; }
            /// <summary>
            /// Gets the height of the picture resolution.
            /// </summary>
            public int Height { get; private set; }

            /// <summary>
            /// Picture resolution 320x240
            /// </summary>
            public static readonly PictureResolution Resolution320x240 = new PictureResolution(320, 240);

            /// <summary>
            /// Picture resolution 176x144
            /// </summary>
            public static readonly PictureResolution Resolution176x144 = new PictureResolution(176, 144);

            /// <summary>
            /// Picture resolution 160x120
            /// </summary>
            public static readonly PictureResolution Resolution160x120 = new PictureResolution(160, 120);

            /// <summary>
            /// Initializes a new <see cref="PictureResolution"/> object.
            /// </summary>
            /// <param name="width">Width supported resolution in pixels.</param>
            /// <param name="height">Height of supported resolution in pixels.</param>
            public PictureResolution(int width, int height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// Initializes a new <see cref="PictureResolution"/> object from a member of the <see cref="DefaultResolutions"/> enumeration.
            /// </summary>
            /// <param name="resolution">A member of the <see cref="DefaultResolutions"/> enumeration.</param>
            public PictureResolution(DefaultResolutions resolution)
            {
                switch (resolution)
                {
                    case DefaultResolutions._320x240:
                        Width = 320;
                        Height = 240;
                        break;
                    case DefaultResolutions._160x120:
                        Width = 160;
                        Height = 120;
                        break;
                    case DefaultResolutions._176x144:
                        Width = 176;
                        Height = 144;
                        break;
                }
            }

            /// <summary>
            /// Enumeration of supported resolutions.
            /// </summary>
            public enum DefaultResolutions
            {
                /// <summary>
                /// Width 320, height 240.
                /// </summary>
                _320x240,
                /// <summary>
                /// Width 176, height 144.
                /// </summary>
                _176x144,
                /// <summary>
                /// Width 160, height 120.
                /// </summary>
                _160x120
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="PictureResolution"/> enumeration.
        /// </summary>
        public PictureResolution CurrentPictureResolution { get; set; }

        private Webcam.ImageFormat GetImageFormat(PictureResolution pictureResolution)
        {
            Webcam.ImageFormat imageFormat = null;
            Webcam.ImageFormat[] imageFormats;

            if (_camera != null)
            {
                try
                {
                    imageFormats = _camera.SupportedFormats;
                    for (int i = 0; i < imageFormats.Length; i++)
                    {
                        if (imageFormats[i].Width == pictureResolution.Width && imageFormats[i].Height == pictureResolution.Height)
                        {
                            imageFormat = imageFormats[i];
                        }
                    }
                }
                catch
                {
                    throw new Exception("Unable to get supported image formats from camera.");
                }
            }
            else
            {
                throw new Exception("Camera must be connected to be able to get valid image formats.");
            }

            if (imageFormat != null)
            {
                return imageFormat;
            }
            else
            {
                throw new ArgumentException("No valid image formats were found for the specified PictureResolution.");
            }

        }

        private void CameraCommunication()
        {
            bool newEvent = false;
            _lastTimeStreamStart = DateTime.Now;

            while (_runThread)
            {
                lock (cameraLock)
                {
                    try
                    {
                        if (_cameraStatus == CameraStatus.Ready)
                        {
                            if (_camera.IsNewImageAvailable())
                            {
                                _camera.GetImage(_targetBitmap, 0, 0, _camera.CurrentStreamingFormat.Width, _camera.CurrentStreamingFormat.Height);

                                OnBitmapStreamedEvent(this, _targetBitmap);
                                newEvent = true;
                            }
                            else
                            {
                                TimeSpan _restartStreamingTriggerTimeSpan = new TimeSpan(1000*1000*_camera.WorkerInterval);

                                if ((DateTime.Now - _lastTimeStreamStart) > _restartStreamingTriggerTimeSpan)

                                {
                                    _camera.StopStreaming();
                                    _camera.StartStreaming(GetImageFormat(CurrentPictureResolution));

                                    _lastTimeStreamStart = DateTime.Now;
                                }
                            }
                        }
                    }
                    catch
                    {
                        ErrorPrint("Unable to get picture from camera");
                    }
                }

                if ( newEvent )
                {
                    newEvent = false;
                    are.WaitOne();
                    _lastTimeStreamStart = DateTime.Now;
                }
                else
                {
                    Thread.Sleep(_camera.WorkerInterval*2);
                }
            }
        }

        public void continueStreaming()
        {
            lock (cameraLock)
            {
                //suspended = false;
                are.Set();
            }
        }

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="BitmapStreamed"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="OurCamera"/> object that raised the event.</param>
        /// <param name="bitmap">A <see cref="Bitmap"/> containing the captured image.</param>
        public delegate void BitmapStreamedEventHandler(OurCamera sender, Bitmap bitmap);

        /// <summary>
        /// Event raised when the camera has completed streaming a bitmap.
        /// </summary>
        public event BitmapStreamedEventHandler BitmapStreamed;

        private BitmapStreamedEventHandler OnBitmapStreamed;

        /// <summary>
        /// Raises the <see cref="BitmapStreamed"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="OurCamera"/> that raised the event.</param>
        /// <param name="bitmap">The <see cref="Bitmap"/> that contains the bitmap from the camera.</param>
        protected virtual void OnBitmapStreamedEvent(OurCamera sender, Bitmap bitmap)
        {
            if (OnBitmapStreamed == null) OnBitmapStreamed = new BitmapStreamedEventHandler(OnBitmapStreamedEvent);
            if (Program.CheckAndInvoke(BitmapStreamed, OnBitmapStreamed, sender, bitmap))
            {
                BitmapStreamed(sender, bitmap);
            }
        }

    }
}

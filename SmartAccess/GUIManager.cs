using System;
using Microsoft.SPOT;
using System.Threading;
using GHI.Glide;
using GT = Gadgeteer;
using GHI.Glide.Geom;

namespace SmartAccess
{
    class GUIManager
    {
        public delegate void ErrorCancelHandler();

        public event ErrorCancelHandler OnErrorCancel;

        Thread guiThread;
        bool stopTimer = false;
        GT.Timer timerResetThankYou;
        enum mode
        {
            NORMAL,
            ERROR_CANCEL,
            ERROR_BLOCK
        }
        Boolean isStreaming;
        mode guiMode;
        Bitmap error_gif;
        Bitmap _01d;
        Bitmap _02d;
        Bitmap _03d;
        Bitmap _04d;
        Bitmap _09d;
        Bitmap _10d;
        Bitmap _11d;
        Bitmap _13d;
        Bitmap _50d;

        public GUIManager()
        {
            GlideTouch.Initialize();

            error_gif = new GT.Picture(Resources.GetBytes(Resources.BinaryResources.error_mesage), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _01d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._01d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _02d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._02d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _03d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._03d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _04d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._04d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _09d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._09d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _10d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._10d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _11d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._11d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _13d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._13d), GT.Picture.PictureEncoding.GIF).MakeBitmap();
            _50d = new GT.Picture(Resources.GetBytes(Resources.BinaryResources._50d), GT.Picture.PictureEncoding.GIF).MakeBitmap();

            guiMode = mode.NORMAL;
            isStreaming = false;
            timerResetThankYou = new GT.Timer(10000);
            timerResetThankYou.Tick += TimerResetThankYou_Tick;
        }

        public void resetStatus()
        {
            stopTimer = true;
            isStreaming = false;
            if (guiMode == mode.ERROR_CANCEL)
            {
                guiMode = mode.NORMAL;
            }
        }

        public void showWelcome()
        {
            stopTimer = true;
            isStreaming = false;
            guiMode = mode.NORMAL;
            messageImageWindow("Sistema attivo",null, GT.Color.Green);
        }

        public void showWeather(String message, String icon)
        {
            if (guiMode != mode.NORMAL) return;

            stopTimer = true;
            isStreaming = false;
            guiMode = mode.NORMAL;
            Bitmap gif=null;

            switch(icon)
            {
                case "01d":
                    gif = _01d;
                    break;
                case "02d":
                    gif = _02d;
                    break;
                case "03d":
                    gif = _03d;
                    break;
                case "04d":
                    gif = _04d;
                    break;
                case "09d":
                    gif = _09d;
                    break;
                case "10d":
                    gif = _10d;
                    break;
                case "11d":
                    gif = _11d;
                    break;
                case "13d":
                    gif = _13d;
                    break;
                case "50d":
                    gif = _50d;
                    break;

            }

            messageImageWindow(message, gif, GT.Color.Green);
        }

        public void showMessage(String message)
        {
            if (guiMode != mode.NORMAL) return;

            stopTimer = true;
            isStreaming = false;
            guiMode = mode.NORMAL;
            messageImageWindow(message,null, GT.Color.Green);
        }

        private void TimerResetThankYou_Tick(GT.Timer timer)
        {
            timer.Stop();
            if (stopTimer) return;
            if (guiMode != mode.NORMAL) return;
            isStreaming = false;
            showWelcome();
        }

        public void showThankYou()
        {
            if (guiMode != mode.NORMAL) return;

            isStreaming = false;
            showTemp("Grazie! Dati salvati correttamente =)",GT.Color.Green,10000);
        }

        public void showTemp(String msg,GT.Color color,int hideInterval)
        {
            if (guiMode != mode.NORMAL) return;

            stopTimer = false;
            isStreaming = false;
            messageImageWindow(msg, null,color);
            timerResetThankYou.Interval = new TimeSpan(0,0,0,0,hideInterval);
            timerResetThankYou.Restart();
            //timerResetThankYou.Start();
        }

        public void setStream(String msg)
        {
            if (guiMode != mode.NORMAL) return;
            stopTimer = true;
            isStreaming = true;
            streamingWindow(msg, null, GT.Color.Green);
        }

        public void setStream(String msg, Bitmap e)
        {
            if (guiMode != mode.NORMAL) return;

            stopTimer = true;
            isStreaming = true;
            streamingWindow(msg, e, GT.Color.Green);
        }

        public void setStream(Bitmap e,Boolean onlyIfStreaming)
        {
            if (guiMode != mode.NORMAL) return;

            if (onlyIfStreaming && !isStreaming) return;

            stopTimer = true;
            isStreaming = true;
            streamingWindow(null,e, GT.Color.Green);
        }

        public void showError(String message)
        {
            stopTimer = true;
            isStreaming = false;
            guiMode = mode.ERROR_BLOCK;
            messageImageWindow(message,error_gif, GT.Color.Red);
        }

        public void showErrorWithCancel(String message)
        {
            stopTimer = true;
            isStreaming = false;
            if (guiMode == mode.NORMAL)
            {
                guiMode = mode.ERROR_CANCEL;
                messageImageButtonWindow(message, GT.Color.Red, error_gif, true);
            }
        }

        private void messageImageButtonWindow(String message,GT.Color color,Bitmap image, Boolean showButton)
        {
            lock (this)
            {
                Boolean loadedNew = false;
                GHI.Glide.Display.Window window = Glide.MainWindow;
                if (window == null || window.Name != "messageImageButtonWindow")
                {
                    window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.messageImageButtonWindow));
                    loadedNew = true;
                }

                string message1 = message;
                string message2 = "";

                GHI.Glide.UI.TextBlock textBlock1 = (GHI.Glide.UI.TextBlock)window.GetChildByName("textBlock1");
                GHI.Glide.UI.TextBlock textBlock2 = (GHI.Glide.UI.TextBlock)window.GetChildByName("textBlock2");

                Boolean fit = false;
                while (!fit)
                {
                    Rectangle r = GHI.Glide.FontManager.GetRect(textBlock1.Font,message1);

                    if ( r.Width > textBlock1.Width)
                    {
                        fit = false;
     
                        message2 = message1.Substring(message1.Length - 1, 1) + message2;
                        message1 = message1.Substring(0, message1.Length - 1);
                    }
                    else
                    {
                        fit = true;
                    }
                }

                textBlock1.Text = message1;
                textBlock1.FontColor = color;

                textBlock2.Text = message2;
                textBlock2.FontColor = color;

                GHI.Glide.UI.Image img = (GHI.Glide.UI.Image)window.GetChildByName("imageBox");

                if (image != null)
                {
                    img.Bitmap = image;
                    img.Visible = true;
                }
                else
                {
                    img.Visible = false;
                }

                GHI.Glide.UI.Button btn = (GHI.Glide.UI.Button)window.GetChildByName("btnClear");
                btn.Text = "Clear";

                if (showButton)
                {
                    window.TapEvent += Btn_TapEvent;
                    btn.TapEvent += Btn_TapEvent;
                    btn.Visible = true;
                }
                else
                {
                    window.TapEvent -= Btn_TapEvent;
                    btn.TapEvent -= Btn_TapEvent;
                    btn.Visible = false;
                }

                if (loadedNew)
                {
                    Glide.MainWindow = window;
                }
                else
                {
                    window.Invalidate();
                }
            }
        }

        private void Btn_TapEvent(object sender)
        {
            OnErrorCancel?.Invoke();

            showWelcome();
        }

        private void messageImageWindow(String message,Bitmap img, GT.Color color)
        {
            messageImageButtonWindow(message, color, img, false);
        }

        private void streamingWindow(String text, Bitmap img, GT.Color color)
        {
            lock (this)
            {
                if (text != null) Debug.Print(text);
                Boolean loadedNew = false;
                GHI.Glide.Display.Window window = Glide.MainWindow;
                if (window == null || window.Name != "streamingWindow")
                {
                    window = GlideLoader.LoadWindow(Resources.GetString(Resources.StringResources.streamingWindow));
                    loadedNew = true;
                }

                if (text != null)
                {
                    GHI.Glide.UI.TextBlock textBlock = (GHI.Glide.UI.TextBlock)window.GetChildByName("textBlock");

                    textBlock.Text = text;
                    textBlock.FontColor = color;
                    window.FillRect(textBlock.Rect);
                    textBlock.Invalidate();
                }
                if (img != null)
                {
                    GHI.Glide.UI.Image image = (GHI.Glide.UI.Image)window.GetChildByName("imageBox");
                    image.Bitmap = img;
                    image.Invalidate();
                }

                if (loadedNew)
                {
                    Glide.MainWindow = window;
                }
                else
                {
                    //window.Invalidate();
                }
            }
        }
    }
}

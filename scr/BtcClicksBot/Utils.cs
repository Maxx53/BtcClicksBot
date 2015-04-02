using System;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;

namespace BtcClicksBot
{
    public delegate void eventDelegate(object data, int index, flag myflag);

    [Flags]
    public enum flag
    {
        ReportProgress,
        SendText,
        UpdStatus,
        LoginInfo,
        StripImg,
        AdsLoaded
    }

    [Flags]
    public enum loginRes
    {
        AlreadyLogged,
        LoginSuccess,
        AccountBlocked,
        NetworkProblem,
        UnknownError,
        CodeException,
        BadPassword
    }

    class Utils
    {
        private const string logPath = "log.txt";
        public const string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:31.0) Gecko/20100101 Firefox/31.0";


        public static void AddtoLog(string logstr)
        {
            try
            {
                using (FileStream fs = new FileStream(logPath, FileMode.OpenOrCreate, FileSystemRights.AppendData,
                FileShare.Write, 4096, FileOptions.None))
                {
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(DateTime.Now);
                        writer.WriteLine(logstr);
                        writer.WriteLine();
                        writer.Close();
                    }
                    fs.Close();
                }
            }
            catch (Exception)
            {
                //dummy
            }

        }



        public static Bitmap AdjustBrightness(Bitmap Image, int Value)
        {
            Bitmap TempBitmap = Image;
            float FinalValue = (float)Value / 255.0f;
            Bitmap NewBitmap = new Bitmap(TempBitmap.Width, TempBitmap.Height);
            Graphics NewGraphics = Graphics.FromImage(NewBitmap);
            float[][] FloatColorMatrix ={
                      new float[] {1, 0, 0, 0, 0},
                      new float[] {0, 1, 0, 0, 0},
                      new float[] {0, 0, 1, 0, 0},
                      new float[] {0, 0, 0, 1, 0},
                      new float[] {FinalValue, FinalValue, FinalValue, 1, 1}
                 };

            ColorMatrix NewColorMatrix = new ColorMatrix(FloatColorMatrix);
            ImageAttributes Attributes = new ImageAttributes();
            Attributes.SetColorMatrix(NewColorMatrix);
            NewGraphics.DrawImage(TempBitmap, new Rectangle(0, 0, TempBitmap.Width, TempBitmap.Height), 0, 0, TempBitmap.Width, TempBitmap.Height, GraphicsUnit.Pixel, Attributes);
            Attributes.Dispose();
            NewGraphics.Dispose();
            return NewBitmap;
        }

        public static Bitmap MakeGrayscale(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][] 
      {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
      });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        public static Bitmap cropImage(Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }
  


        //========== Networking =====================================================================================================


        public static string SendPostRequest(string req, string url, string refer, CookieContainer cookie, WebProxy proxy)
        {
            var requestData = Encoding.UTF8.GetBytes(req);
            string content = string.Empty;

            try
            {
                var request = (HttpWebRequest)
                    WebRequest.Create(url);

                request.CookieContainer = cookie;
                request.Method = "POST";

                //New
                request.Proxy = proxy;

                request.Timeout = 120000;
                //KeepAlive is True by default
                //request.KeepAlive = true;
                request.UserAgent = userAgent;

                request.Referer = refer;
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                request.ContentLength = requestData.Length;

                using (var s = request.GetRequestStream())
                {
                    s.Write(requestData, 0, requestData.Length);
                }

                HttpWebResponse resp = (HttpWebResponse)request.GetResponse();

                var stream = new StreamReader(resp.GetResponseStream());
                content = stream.ReadToEnd();

                cookie = request.CookieContainer;
                resp.Close();
                stream.Close();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse resp = e.Response;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();
                    }
                }

            }

            return content;
        }



        public static string GetRequest(string url, CookieContainer cookie, WebProxy proxy)
        {
            string content = string.Empty;

            try
            {

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                request.Proxy = proxy;

                request.Timeout = 120000;

                //KeepAlive is True by default
                //request.KeepAlive = keepAlive;

                request.UserAgent = userAgent;

                request.Accept = "application/json";
                request.CookieContainer = cookie;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var stream = new StreamReader(response.GetResponseStream());
                content = stream.ReadToEnd();

                response.Close();
                stream.Close();

            }

            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {

                    HttpWebResponse resp = (HttpWebResponse)e.Response;
                    int statCode = (int)resp.StatusCode;

                    if (statCode == 403)
                    {
                        content = "403";
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
                }

            }

            return content;

        }
    }
}

using System;
using System.Windows.Forms;
using Awesomium.Windows.Forms;
using Awesomium.Core;
using System.Text.RegularExpressions;
using System.Net;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace BtcClicksBot
{
    public partial class RegForm : Form
    {
        public RegForm()
        {
            InitializeComponent();
        }

        public WebControl webBrowser = new WebControl();
        private const string regReq = "username={0}&password={1}&pconfirm={1}&email={2}&timezone=Europe%2FLondon&adcopy_response={3}&adcopy_challenge={4}&terms=on";

        //username=mrpickles321&password=demon8891&pconfirm=demon8891&email=oduvanpolevoj%40mail.ru&timezone=Europe%2FLondon&adcopy_response=abelian+grape&adcopy_challenge=2%40K2I613-R5myEA75I4wvGOiR3VG372-zW%40USmQdNeoZagENU8bgrS7M39APTC4lJjo212aYQBOudlvotGS6b05p9rEcXfIVcPvjpXQv.lYI77XPgLCU1BwBfiPHe1yksmVU2mCpqsvhW7zdfOkldP7jnEFTmqLcJ3b5z8YF0.tlyctQFN7CSQcWlF7X2BCUVAAskKwN-WFF6lYiHBMeIE0MV4LZuAuLG.wpaugnPwpS41bQwklbgpafBDgCFL6asE.MYBt-Z6rnX4iX59zgMnRFTnOVzVs-2JfNz-kDMfruYDnUKLOaTvetnpUtbnYzK6dJkhFXIK0uoA&terms=on
        //{"result":"success","message":"You have successfully signed up. A confirmation email has been sent to your email address with a link to verify your email address.<br\/>\n        If you do not verify your email address within 7 days, your account will be deleted automatically."}
        //Referer	https://btcclicks.com/signup
        //https://btcclicks.com/ajax/signup

        private const string regRef = "https://btcclicks.com/signup";
        private const string regUrl = "https://btcclicks.com/ajax/signup";

        private void RegForm_Load(object sender, EventArgs e)
        {
            webBrowser.ViewType = WebViewType.Offscreen;
            this.Controls.Add(webBrowser);
        }

        public static void StartLoadImgTread(string imgUrl, PictureBox picbox)
        {
            if (imgUrl.Contains("http://"))
            {
                ThreadStart threadStart = delegate() { loadImg(imgUrl, picbox, true, false); };
                Thread pTh = new Thread(threadStart);
                pTh.IsBackground = true;
                pTh.Start();
            }
        }

        static public void loadImg(string imgurl, PictureBox picbox, bool drawtext, bool doWhite)
        {
            try
            {

                if (imgurl == string.Empty)
                    return;

                if (drawtext)
                {
                    picbox.Image = Properties.Resources.working;
                }


                WebClient wClient = new WebClient();
                byte[] imageByte = wClient.DownloadData(imgurl);
                using (MemoryStream ms = new MemoryStream(imageByte, 0, imageByte.Length))
                {
                    ms.Write(imageByte, 0, imageByte.Length);
                    var resimg = Image.FromStream(ms, true);
                    picbox.Image = resimg;
                }
            }
            catch (Exception exc)
            {
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            //webBrowser.Source = "ya.ru".ToUri();
           // var js = Utils.GetRequest("https://api-secure.solvemedia.com/papi/challenge.script?k=K2I613-R5myEA75I4wvGOiR3VG372-zW", null, null);
           // MessageBox.Show(js);
          //  MessageBox.Show(webBrowser.ExecuteJavascriptWithResult(js));


            webControl1.Source = regRef.ToUri();


            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dynamic document = (JSObject)webControl1.ExecuteJavascriptWithResult("document");

            using (document)
            {
                var textbox = document.getElementsByTagName("input");

                if (textbox == null)
                    return;

                for (int i = 0; i < textbox.length; i++)
                {
                    string idStr = textbox[i].getAttribute("id");

                    switch (idStr)
                    {
                        case "inputUsername": 
                            textbox[i].value = "govno";
                            break;
                        case "inputEmail": 
                            textbox[i].value = "email@govno.ru";
                            break;
                        case "inputPassword":
                        case "inputConfirm":
                            textbox[i].value = "pass";
                            break;
                        case "adcopy_response":
                            textbox[i].value = "answer";
                            break;
                       // case "inputTerms":
                       //     textbox[i].value = true;
                       //     break;
                    }


                 }

                string page = webControl1.ExecuteJavascriptWithResult("document.getElementsByTagName('html')[0].innerHTML");
                if (!page.Contains("application/x-shockwave"))
                {
                    MessageBox.Show("Img");
                    var imgLink = "http:"+ Regex.Match(page, "(?<=<iframe src=\")(.*)(?=\" height=\"150\" width=\"300\")", RegexOptions.Singleline).ToString();
                    MessageBox.Show(imgLink);
                    StartLoadImgTread(imgLink, pictureBox1);
                }
                else
                {
                    MessageBox.Show("flash");
                    var imgLink = Regex.Match(page, "(?<=flash\" data=\")(.*)(?=\" style=\"width:100%)", RegexOptions.Singleline).ToString();
                    MessageBox.Show(imgLink);
                    axShockwaveFlash1.Movie = imgLink;
                }

               
               webControl1.ExecuteJavascriptWithResult("document.getElementById(\"inputTerms\").checked = true;");
                

               var btn = document.getElementsByTagName("button");

                for (int i = 0; i < btn.length; i++)
                {
                    if (btn[i].getAttribute("type") == "submit")
                    {
                        btn[i].Invoke("click");
                        break;
                    }
                }
                  
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        private void axShockwaveFlash1_OnReadyStateChange(object sender, AxShockwaveFlashObjects._IShockwaveFlashEvents_OnReadyStateChangeEvent e)
        {

            MessageBox.Show( e.newState.ToString());
            return;
           
            Graphics g = axShockwaveFlash1.CreateGraphics();
            Bitmap bmp = new Bitmap(axShockwaveFlash1.Size.Width, axShockwaveFlash1.Size.Height, g);
            Graphics memoryGraphics = Graphics.FromImage(bmp);
            IntPtr dc = memoryGraphics.GetHdc();
            bool success = PrintWindow(axShockwaveFlash1.Handle, dc, 0);
            memoryGraphics.ReleaseHdc(dc);
            pictureBox1.Image = bmp;
        }

     
    }
}

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
using AxShockwaveFlashObjects;
using tessnet2;
using System.Text;
using System.Collections.Generic;

namespace BtcClicksBot
{
    public partial class RegForm : Form
    {
        public RegForm()
        {
            InitializeComponent();
        }

        public WebControl webBrowser = new WebControl();
        private List<Account> accList = new List<Account>();

        private const string regReq = "username={0}&password={1}&pconfirm={1}&email={2}&timezone=Europe%2FLondon&adcopy_response={3}&adcopy_challenge={4}&terms=on";

        //username=user&password=pass&pconfirm=pass&email=mail&timezone=Europe%2FLondon&adcopy_response=abelian+grape&adcopy_challenge=2%40K2I613-R5myEA75I4wvGOiR3VG372-zW%40USmQdNeoZagENU8bgrS7M39APTC4lJjo212aYQBOudlvotGS6b05p9rEcXfIVcPvjpXQv.lYI77XPgLCU1BwBfiPHe1yksmVU2mCpqsvhW7zdfOkldP7jnEFTmqLcJ3b5z8YF0.tlyctQFN7CSQcWlF7X2BCUVAAskKwN-WFF6lYiHBMeIE0MV4LZuAuLG.wpaugnPwpS41bQwklbgpafBDgCFL6asE.MYBt-Z6rnX4iX59zgMnRFTnOVzVs-2JfNz-kDMfruYDnUKLOaTvetnpUtbnYzK6dJkhFXIK0uoA&terms=on
        //{"result":"success","message":"You have successfully signed up. A confirmation email has been sent to your email address with a link to verify your email address.<br\/>\n        If you do not verify your email address within 7 days, your account will be deleted automatically."}
        //Referer	https://btcclicks.com/signup
        //https://btcclicks.com/ajax/signup

        private const string regRef = "https://btcclicks.com/signup";
        private const string regUrl = "https://btcclicks.com/ajax/signup";

        private void RegForm_Load(object sender, EventArgs e)
        {
            webBrowser.Width = 800;
            webBrowser.Height = 1300;
            webBrowser.ViewType = WebViewType.Offscreen;
            panel1.Controls.Add(webBrowser);

            for (int i = 0; i < 10 ; i++)
            {
                accList.Add(new Account(RandomStr(), CreatePassword(8), "email@email.com"));
            }
        }

        private class Account
        {
            public Account(string username, string password, string email)
            {
                this.Username = username;
                this.Password = password;
                this.Email = email;
            }

            public string Username { set; get; }
            public string Password { set; get; }
            public string Email { set; get; }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //webBrowser.Source = "ya.ru".ToUri();
           // var js = Utils.GetRequest("https://api-secure.solvemedia.com/papi/challenge.script?k=K2I613-R5myEA75I4wvGOiR3VG372-zW", null, null);
           // MessageBox.Show(js);
          //  MessageBox.Show(webBrowser.ExecuteJavascriptWithResult(js));


            webBrowser.Source = regRef.ToUri();

            while (webBrowser.IsLoading)
            {
                Application.DoEvents();
                Thread.Sleep(20);
            };

            string page = webBrowser.ExecuteJavascriptWithResult("document.getElementsByTagName('html')[0].innerHTML");

            if (!page.Contains("application/x-shockwave"))
            {
                var surface = webBrowser.Surface as ImageSurface;
                var captcha = Utils.cropImage(surface.Image, new Rectangle(44, 898, 300, 132));
                pictureBox1.Image = captcha;
               // surface.Image.Save("result.png");
                textBox1.Text = DoTesseract(captcha);
            }
            else
            {
                MessageBox.Show("flash");
                var imgLink = Regex.Match(page, "(?<=flash\" data=\")(.*)(?=\" style=\"width:100%)", RegexOptions.Singleline).ToString();
                MessageBox.Show(imgLink);

                WebClient wClient = new WebClient();
                byte[] flashByte = wClient.DownloadData(imgLink);
                InitFlashMovie(axShockwaveFlash1, flashByte);

                while (axShockwaveFlash1.ReadyState != 4)
                {
                    Thread.Sleep(50);
                    Application.DoEvents();
                }


                Application.DoEvents();
                MessageBox.Show(axShockwaveFlash1.ReadyState.ToString());
                Graphics g = axShockwaveFlash1.CreateGraphics();
                Bitmap bmp = new Bitmap(axShockwaveFlash1.Size.Width, axShockwaveFlash1.Size.Height, g);
                Graphics memoryGraphics = Graphics.FromImage(bmp);
                IntPtr dc = memoryGraphics.GetHdc();
                bool success = PrintWindow(axShockwaveFlash1.Handle, dc, 0);
                memoryGraphics.ReleaseHdc(dc);
                pictureBox1.Image = bmp;

            }

            
        }

        private static string DoTesseract(Image input)
        {
            var bmp = new Bitmap(input, new Size(100, 44));
            var ocr = new Tesseract();
            //ocr.SetVariable("tessedit_char_blacklist", "0123456789+-");
            ocr.Init(null, "eng", false);
            
            var result = ocr.DoOCR(bmp, Rectangle.Empty);
            string ret = string.Empty;
 
            foreach (var item in result)
            {
                ret += item.Text + " ";
            }

            return ret.Trim();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            dynamic document = (JSObject)webBrowser.ExecuteJavascriptWithResult("document");

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
                            textbox[i].value = "randomName609884325";
                            break;
                        case "inputEmail":
                            textbox[i].value = "randomName609884325@gmail.com";
                            break;
                        case "inputPassword":
                        case "inputConfirm":
                            textbox[i].value = "dfkgdfgdefgrer";
                            break;
                        case "adcopy_response":
                            textbox[i].value = textBox1.Text;
                            break;
                       // case "inputTerms":
                       //     textbox[i].value = true;
                       //     break;
                    }


                 }


                webBrowser.ExecuteJavascriptWithResult("document.getElementById(\"inputTerms\").checked = true;");
                

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



        private void InitFlashMovie(AxShockwaveFlash flashObj, byte[] swfFile)
        {
            using (MemoryStream stm = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stm))
                {
                    /* Write length of stream for AxHost.State */
                    writer.Write(8 + swfFile.Length);
                    /* Write Flash magic 'fUfU' */
                    writer.Write(0x55665566);
                    /* Length of swf file */
                    writer.Write(swfFile.Length);
                    writer.Write(swfFile);
                    stm.Seek(0, SeekOrigin.Begin);
                    /* 1 == IPeristStreamInit */
                    flashObj.OcxState = new AxHost.State(stm, 1, false, null);
                }
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);


        private void button3_Click(object sender, EventArgs e)
        {
            //var surface = webBrowser.Surface as ImageSurface;
          //  surface.Image.Save("result2.png");
        }

        public static string RandomStr()
        {
            string rStr = Path.GetRandomFileName();
            rStr = rStr.Replace(".", ""); // For Removing the .
            return rStr;
        }

        public string CreatePassword(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!#%&*+";
            StringBuilder res = new StringBuilder();
            Random rnd = new Random();
            while (0 < length--)
            {
                res.Append(valid[rnd.Next(valid.Length)]);
            }
            return res.ToString();
        }
    }
}

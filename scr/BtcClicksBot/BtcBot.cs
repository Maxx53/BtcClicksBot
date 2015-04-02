﻿// <copyright file="BtcBot.cs" company="Maxx53">
// Copyright (c) 2015 All Rights Reserved
// </copyright>
// <author>Maxx53</author>
// <date>08/03/2015</date>
// <summary>Program for automatic ads viewing at http://btcclicks.com</summary>

using System.ComponentModel;
using System.Net;
using System;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using tessnet2;
using System.Data;
using Awesomium.Core;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Awesomium.Windows.Forms;
using System.Diagnostics;

namespace BtcClicksBot
{

    public class BtcBot
    {

        //------------ Properties ------------------------------------

        public string Username { set; get; }
        public string Password { set; get; }
        public string Address { set; get; }
        public int Pos { set; get; }
        public int waitTimeout { set; get; }
        public int Interval { set; get; }
        public int Index { set; get; }
        public string Balance = "0 mBTC";

        public CookieContainer Cookie
        {
            get
            {
                return cookie;
            }
            set
            {
                if (value == null)
                {
                    cookie = new CookieContainer();
                }
                else
                    cookie = value;
            }
        }

        public string Proxy
        {
            get
            {
                if (proxy == null)
                    return "None";
                else
                    return string.Format("{0}:{1}", proxy.Address.Host, proxy.Address.Port);
            }
            set
            {
                if (value != "None")
                {
                    try
                    {
                        proxy = new WebProxy(value);
                    }
                    catch (Exception)
                    {
                        proxy = null;
                    }
                }
                else
                {
                    proxy = null;
                }
            }
        }

        public bool isWorking
        {
            get
            {
                return (mainThread.IsBusy | adsThread.IsBusy);
            }
        }


        //------------ Constants ------------------------------------

        public const string host = "btcclicks.com";
        public const string solveHost = "api-secure.solvemedia.com";
        const string siteUrl = "http://" + host;
        const string siteUrlS = "https://" + host;

        const string loginRefUrl = siteUrlS + "/loginform";
        const string checkAcc = siteUrlS + "/ajax/login";
        const string loginUrl = siteUrlS + "/login";
        const string loginReq = "token={0}&username={1}&password={2}";

        const string adsUrl = siteUrl + "/ads";
        const string vEndUrl = siteUrl + "/ajax/vend";
        const string vEndReq = "ad={0}&c={1}&b={2}";
        const string jsKey = "V8sb9t6pQy";
        const string tokenUrl = siteUrl + "/ajax/vtimerend";
        const string wdUrl = siteUrl + "/ajax/withdraw";
        const string wdReq = "token={0}&method=address&address={1}";
        const string wdRefUrl = siteUrl + "/withdraw";


        //------------ Variables ------------------------------------

        public List<Advert> adList = new List<Advert>();

        private BackgroundWorker mainThread = new BackgroundWorker();
        private BackgroundWorker loginThread = new BackgroundWorker();
        private BackgroundWorker adsThread = new BackgroundWorker();
        private BackgroundWorker wdrawThread = new BackgroundWorker();

        public WebControl webBrowser = new WebControl();
        private WebProxy proxy = null;
        private CookieContainer cookie = new CookieContainer();

        private Semaphore Sem = new Semaphore(0, 1);

        private bool fistLoop = true;

        protected void doMessage(flag myflag, object message)
        {
            try
            {
                if (MainForm.delegMessage != null)
                {
                    Control target = MainForm.delegMessage.Target as Control;

                    if (target != null && target.InvokeRequired)
                    {
                        target.Invoke(MainForm.delegMessage, new object[] { message, Index, myflag });
                    }
                    else
                    {
                        MainForm.delegMessage(message, Index, myflag);
                    }
                }

            }
            catch (Exception e)
            {
                Utils.AddtoLog(e.Message);
            }
        }



        public BtcBot(MainForm form, string _user, string _pass, string _proxy, string _address, CookieContainer _cookie, int _timeout, int _interval)
        {
            Pos = -1;

            mainThread.WorkerSupportsCancellation = true;
            mainThread.DoWork += new DoWorkEventHandler(mainThread_DoWork);
            mainThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mainThread_Complete);

            loginThread.WorkerSupportsCancellation = true;
            loginThread.DoWork += new DoWorkEventHandler(loginThread_DoWork);

            adsThread.WorkerSupportsCancellation = true;
            adsThread.DoWork += new DoWorkEventHandler(adsThread_DoWork);
            adsThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(adsThread_Complete);

            wdrawThread.WorkerSupportsCancellation = true;
            wdrawThread.DoWork += new DoWorkEventHandler(wdrawThread_DoWork);
           // wdrawThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(wdrawThread_Complete);

            Username = _user;
            Password = _pass;
            Proxy = _proxy;
            Address = _address;
            waitTimeout = _timeout;
            Interval = _interval;
            Cookie = _cookie;


            var prefs = new WebPreferences();

            if (Proxy != "None")
            {
                prefs.ProxyConfig = Proxy;
            }

            prefs.WebAudio = false;
            prefs.Plugins = false;
            webBrowser.WebSession = WebCore.CreateWebSession(prefs);
            webBrowser.ViewType = WebViewType.Offscreen;

            form.Controls.Add(webBrowser);
         }


        public string getAdCount()
        {
            int incr = 0;
            foreach (var item in adList)
            {
                if (item.resInfo.AdResult == AdResult.Solved)
                    incr++;
            }

            return string.Format("{0}/{1}", adList.Count.ToString(), incr);
        }

        public void Login()
        {
            if (loginThread.IsBusy != true)
            {

                loginThread.RunWorkerAsync();
            }

        }

        private void LoadAds()
        {
            if (adsThread.IsBusy != true)
            {

                adsThread.RunWorkerAsync();
            }

        }


        private void ClearBrowser()
        {
            webBrowser.Stop();
            webBrowser.WebSession.ClearCache();
            webBrowser.WebSession.ClearCookies();
            webBrowser.Source = "about:blank".ToUri();
            while (webBrowser.IsLoading)
            {
                Application.DoEvents();
            };
        }


        public void Start(bool isFirst)
        {
            fistLoop = isFirst;

            if (mainThread.IsBusy != true)
            {
                ClearBrowser();

                if (setNextPos())
                {
                    adList[Pos].resInfo.Stat = AdStatus.Process;
                    doMessage(flag.UpdStatus, string.Empty);

                    setCookieToBrowser(cookie, webBrowser);
                    webBrowser.Source = adList[Pos].Link.ToUri();
                    mainThread.RunWorkerAsync();

                }
                else LoadAds();
            }

        }

        public void Stop()
        {
            Sem.Release();
            mainThread.CancelAsync();
            adsThread.CancelAsync();
            ClearBrowser();
        }

        public static void setCookieToBrowser(CookieContainer cook, WebControl wb)
        {
              
            var stcook = cook.GetCookies(new Uri(siteUrl));
            string str = string.Empty;

            for (int i = 0; i < stcook.Count; i++)
            {
                wb.WebSession.SetCookie(siteUrl.ToUri(), stcook[i].Name + "=" + stcook[i].Value, true, true);
            }

        }


        public static CookieContainer GetCookieContainer(string doc)
        {
            CookieContainer container = new CookieContainer();

            foreach (string cookie in doc.Split(';'))
            {
                string name = cookie.Split('=')[0];
                string value = cookie.Substring(name.Length + 1);
                string path = "/";
                string domain = host;
                container.Add(new Cookie(name.Trim(), value.Trim(), path, domain));
            }

            return container;
        }


        public delegate string GetStringHandler();
        public string GetDocumentText()
        {
            if (webBrowser.InvokeRequired)
                return webBrowser.Invoke(new GetStringHandler(GetDocumentText)) as string;
            else
            {
                if (webBrowser.IsDocumentReady)
                    return webBrowser.ExecuteJavascriptWithResult("document.getElementsByTagName('html')[0].innerHTML");
                else
                    return string.Empty;
            }
        }

        public delegate bool GetStateHandler();
        public bool GetState()
        {
            if (webBrowser.InvokeRequired)
            {
                return (bool)webBrowser.Invoke(new GetStateHandler(GetState));
            }
            else
                return (webBrowser.IsLoading);
        }


        public delegate string GetCookHandler();
        public string GetCook()
        {
            if (webBrowser.InvokeRequired)
            {
                return (string)webBrowser.Invoke(new GetCookHandler(GetCook));
            }
            else
                return (webBrowser.ExecuteJavascriptWithResult("document.cookie;"));
        }

        public delegate string GetBitmapHandler();
        public string GetBitmap()
        {
            if (webBrowser.InvokeRequired)
            {
                return (string)webBrowser.Invoke(new GetBitmapHandler(GetBitmap));
            }
            else
            {
                return JsGetImgBase64String("document.getElementById('captcha').firstChild");
            }
        }



        public string JsGetImgBase64String(string getElementQuery, bool leaveOnlyBase64Data = true)
        {
            string data = "undefined";

            if (webBrowser.IsDocumentReady)
            {
                try
                {

                    data = webBrowser.ExecuteJavascriptWithResult(@"
                        function getImgBase64String(img)
                        {
                           var cnv = document.createElement('CANVAS');
                           var ctx = cnv.getContext('2d');
                           ctx.drawImage(img, 0, 0);
                           return cnv.toDataURL();
                        }
                 " + String.Format("getImgBase64String({0});", getElementQuery));

                    if (leaveOnlyBase64Data && data.Contains(","))
                    {
                        data = data.Substring(data.IndexOf(",") + 1);
                    }

                }
                catch (Exception exc)
                {
                    Utils.AddtoLog(exc.Message);
                }
            }
            else Utils.AddtoLog("DOM is not ready!");

            return data;
        }



        private static bool isLogged(CookieContainer cok)
        {
            bool logged = false;
            var stcook = cok.GetCookies(new Uri(BtcBot.siteUrl));

            for (int i = 0; i < stcook.Count; i++)
            {
                if (stcook[i].Name.ToString() == "s")
                {
                    logged = true;
                    break;
                }
            }

            return logged;
        }


        private bool GetBalance(string page)
        {
            if (page.Contains("Log In") | (page == string.Empty) | (!page.Contains("fa fa-user")))
            {
                return false;
            }
            else
            {
                string[] accInfo = Regex.Match(page, "(?<=fa fa-user\"></i>)(.*)(?= mBTC</a>)", RegexOptions.Singleline).ToString().Trim().Split(' ');
                Balance = accInfo[1] + " mBTC";
                doMessage(flag.LoginInfo, string.Empty);
                return true;
            }
        }


        private void loginThread_DoWork(object sender, DoWorkEventArgs e)
        {
            var res = loginRes.UnknownError;

            try
            {
                if (isLogged(Cookie))
                {
                    doMessage(flag.SendText, "Getting Balance...");

                    if (GetBalance(SendGet(siteUrl)))
                    {
                        res = loginRes.AlreadyLogged;
                        return;
                    }
                    else
                        Cookie = null;
                }

                doMessage(flag.SendText, "Getting Info...");
                doMessage(flag.ReportProgress, 10);

                string tokenPage = SendGet(loginRefUrl);

                string token = Regex.Match(tokenPage, "(?<=name=\"token\" value=\")(.*)(?=\" />)", RegexOptions.Singleline).ToString();

                doMessage(flag.SendText, "Checking Account Status...");
                doMessage(flag.ReportProgress, 20);

                string checkReq = string.Format(loginReq, token, Username, Password);
                string checkPage = SendPost(checkReq, checkAcc, loginRefUrl);

                if (checkPage[0] != '{')
                {
                    res = loginRes.NetworkProblem;
                    return;
                }

                var checkJSON = SimpleJSON.ParseJson(checkPage);

                if (checkJSON["result"] == "error")
                {
                    string mess = checkJSON["message"];

                    if (mess.Contains("username"))
                        res = loginRes.BadPassword;
                    else 
                        if (mess.Contains("suspended"))
                        res = loginRes.AccountBlocked;
                    else 
                        res = loginRes.UnknownError;

                }
                else
                    if (checkJSON["result"] == "success")
                    {
                        string mainReq = string.Format(loginReq, token, Username, Password);

                        doMessage(flag.SendText, "Login Attempt...");
                        doMessage(flag.ReportProgress, 50);

                        string BodyResp = SendPost(mainReq, loginUrl, loginRefUrl);

                        doMessage(flag.SendText, "Getting Balance...");

                        if (GetBalance(SendGet(siteUrl)))
                            res = loginRes.LoginSuccess;
                        else
                            res = loginRes.NetworkProblem;
                    }
                    else
                        res = loginRes.UnknownError;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                res = loginRes.CodeException;
            }
            finally
            {
                doMessage(flag.SendText, res.ToString());
                doMessage(flag.ReportProgress, 100);
            }
        }



        private void adsThread_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            adList.Clear();

            doMessage(flag.SendText, "Getting Ad List...");
            doMessage(flag.ReportProgress, 50);
   
            string adsPage = SendGet(adsUrl);

            MatchCollection matches = Regex.Matches(adsPage, "(?<=<div class=\"viewBox\">)(.*?)(?<=mBTC)", RegexOptions.Singleline);

            if (matches.Count != 0)
            {
                foreach (Match match in matches)
                {
                    string currmatch = match.Groups[1].Value;

                    if (currmatch.Contains("Clicked"))
                        continue;

                    string link = Regex.Match(currmatch, "(?<=<a href=\")(.*)(?=\" target)").ToString();
                    string desc = Regex.Match(currmatch, "(?<=btcad\">)(.*)(?=</a>)").ToString();
                    int pos = currmatch.IndexOf("viewReward\">") + 12;

                    string raw_reward = currmatch.Substring(pos, currmatch.Length - pos).Trim();
                   
                    string time_str = raw_reward.Substring(0, raw_reward.IndexOf("sec")).Trim();

                    int time = 0;
                    Int32.TryParse(time_str + "000", out time);

                    string reward = Regex.Match(raw_reward, "(?<=&rarr;)(.*)(?=mBTC)").ToString();

                    adList.Add(new Advert(link, desc, reward, time));
                }

            }

            if (adList.Count != 0)
            {
                GetBalance(adsPage);
            }

            doMessage(flag.ReportProgress, 100);

            doMessage(flag.SendText, "Waiting for Ads...");
            if (!fistLoop)
                Sem.WaitOne(Interval);

            if (worker.CancellationPending == true)
            {
                e.Cancel = true;
            }
        }


        private void sendStopText()
        {
            doMessage(flag.SendText, "Bot Stopped!");
            doMessage(flag.ReportProgress, 0);

            if ((adList.Count != 0) && (Pos != -1))
            {
                adList[Pos].resInfo.AdResult = AdResult.Canselled;
                adList[Pos].resInfo.Stat = AdStatus.Failture;
            }
        }


        /// <summary>
        /// Main Thread for processing one ad in AdList at Pos index value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mainThread_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            Random rnd = new Random();
            var delay = rnd.Next(5000, 10000);
            doMessage(flag.SendText, "Random Delay: " + delay.ToString() + " ms");
            Sem.WaitOne(delay);


            doMessage(flag.SendText, "Loading page...");
            doMessage(flag.ReportProgress, 10);

            while (GetState())
            {
                if (worker.CancellationPending == true)
                {
                    sendStopText();
                    e.Cancel = true;
                    return;
                }

                Application.DoEvents();
                Thread.Sleep(20);
            };


            doMessage(flag.ReportProgress, 50);
            doMessage(flag.SendText, "Waiting " + adList[Pos].Time.ToString() + " ms");
            Sem.WaitOne(adList[Pos].Time);

            if (worker.CancellationPending == true)
            {
                sendStopText();
                e.Cancel = true;
                return;
            }

            doMessage(flag.ReportProgress, 60);
            doMessage(flag.SendText, "Waiting for capcha");

            Bitmap pic = new Bitmap(1, 1);
            Stopwatch stopwatch = new Stopwatch();

            //Starting Timeout watch...
            stopwatch.Start();


            //Starting waiting loop
        Start:

            if (worker.CancellationPending == true)
            {
                sendStopText();
                e.Cancel = true;
                return;
            }

            string html = GetDocumentText();

            //Flash captcha, that we can't solve.
            if (html.Contains("ACPuzzleUtil.callbacks"))
            {
                doMessage(flag.ReportProgress, 0);
                doMessage(flag.SendText, "Bad Captcha!");
                //adList[Pos].Clicked = false;
                adList[Pos].resInfo.AdResult = AdResult.FlashCapcha;
                adList[Pos].resInfo.Stat = AdStatus.Failture;
                doMessage(flag.UpdStatus, string.Empty);
                return;
            }

            //Yiss! We got captcha image.
            if (html.Contains("captcha?c=view"))
            {
                byte[] captchaBytes = Convert.FromBase64String(GetBitmap());
                MemoryStream ms = new MemoryStream(captchaBytes);
                pic = Utils.cropImage(Image.FromStream(ms), new Rectangle(0, 0, 90, 40));
            }
            else
            {
                //Continue waiting for captcha
                Application.DoEvents();

                Sem.WaitOne(1000);

                //Checking Timeout...
                if (stopwatch.ElapsedMilliseconds >= waitTimeout)
                {
                    stopwatch.Stop();
                    doMessage(flag.SendText, "timeout!");
                    doMessage(flag.ReportProgress, 0);
                    //adList[Pos].Clicked = false;
                    adList[Pos].resInfo.AdResult = AdResult.Timeout;
                    adList[Pos].resInfo.Stat = AdStatus.Failture;
                    doMessage(flag.UpdStatus, string.Empty);
                    return;
                }
                else
                    goto Start;

            }

            stopwatch.Stop();


            if (worker.CancellationPending == true)
            {
                sendStopText();
                e.Cancel = true;
                return;
            }


            doMessage(flag.ReportProgress, 80);

            //Solving captcha

            var ocr = new Tesseract();
            //Digits only
            ocr.SetVariable("tessedit_char_whitelist", "0123456789+-"); 
            ocr.Init(null, "eng", false);
           // adList[Pos].resInfo.CapImg = ocr.GetThresholdedImage(pic, Rectangle.Empty);
            adList[Pos].resInfo.CapImg = pic;

            var solved = ocr.DoOCR(Utils.MakeGrayscale(pic), Rectangle.Empty)[0].Text;

            //C'mon, Do Math!
            var dt = new DataTable();
            string result = string.Empty;

            try
            {
                result = dt.Compute(solved, null).ToString();
            }
            catch (Exception)
            {
                Utils.AddtoLog("Unsolved: " + solved);
            }
            finally
            {
                dt.Dispose();
            }

            adList[Pos].resInfo.CapStr = solved + " = " + result;

            if (worker.CancellationPending == true)
            {
                sendStopText();
                e.Cancel = true;
                return;
            }

            //Sending Answer
            doMessage(flag.SendText, "Sending answer");
            doMessage(flag.ReportProgress, 90);

            var pozzz = html.IndexOf("var h = '") + 9;
            string longJonson = html.Substring(pozzz, 128);

            cookie = GetCookieContainer(GetCook());

            string res = SendPost(string.Format(vEndReq, longJonson, result, jsKey), vEndUrl, adList[Pos].Link);

            doMessage(flag.ReportProgress, 100);

            if (res == "1")
            {
                //adList[Pos].Clicked = true;
                adList[Pos].resInfo.AdResult = AdResult.Solved;
                adList[Pos].resInfo.Stat = AdStatus.Success;


            }
            else
            {
                adList[Pos].resInfo.AdResult = AdResult.NotSolved;
                adList[Pos].resInfo.Stat = AdStatus.Failture;
            }
            doMessage(flag.UpdStatus, string.Empty);
        }

        private void mainThread_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
                sendStopText();
            else
                Start(false);
        }

        private void adsThread_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            doMessage(flag.AdsLoaded, string.Empty);

            if (e.Cancelled)
                sendStopText();
            else
                Start(false);
        }

        private bool setNextPos()
        {
            if (adList.Count == 0) return false;

            for (int i = Pos + 1; i < adList.Count; i++)
            {
                //clicked
                if (adList[i].resInfo.AdResult == AdResult.Solved)
                    continue;
                else
                {
                    Pos = i;
                    return true;
                }
            }

            for (int i = 0; i <= Pos; i++)
            {
                if (adList[i].resInfo.AdResult == AdResult.Solved)
                    continue;
                else
                {
                    Pos = i;
                    return true;
                }
            }

            return false;
        }


        private string SendPost(string req, string url, string refer)
        {
            doMessage(flag.StripImg, 0);
            var result = Utils.SendPostRequest(req, url, refer, cookie, proxy);
            doMessage(flag.StripImg, 1);

            return result;
        }

        private string SendGet(string url)
        {
            doMessage(flag.StripImg, 0);
            var result = Utils.GetRequest(url, cookie, proxy);
            doMessage(flag.StripImg, 1);

            return result;

        }


        private void wdrawThread_DoWork(object sender, DoWorkEventArgs e)
        {
            var wdPage = SendGet(wdRefUrl);

            string token = wdPage.Substring(wdPage.IndexOf("token") + 14, 32);

            var fullReq = string.Format(wdReq, token, Address);

            var wdJSON = SimpleJSON.ParseJson(SendPost(fullReq, wdUrl, wdRefUrl));

            if (wdJSON["result"] == "error")
                doMessage(flag.SendText, wdJSON["message"]);
            else
                if (wdJSON["result"] == "success")
                    doMessage(flag.SendText, "Withdraw success!");
        }

        
        public void WithDraw()
        {
            if (wdrawThread.IsBusy != true)
            {
                wdrawThread.RunWorkerAsync();
            }

        }


    }

}

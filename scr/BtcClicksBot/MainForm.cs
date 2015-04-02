using System;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Collections.Generic;
using BtcClicksBot.Properties;
using System.Threading;
using Awesomium.Core;
using System.Drawing.Imaging;

namespace BtcClicksBot
{
    public partial class MainForm : Form
    {
        public BotList botList = new BotList();
        public RegForm regFrm = new RegForm();
        public static int interval = 60000;
        public static int timeout = 60000;

        public static eventDelegate delegMessage;

        public class BotList : List<BtcBot>
        {
            public void Add(MainForm form, string user, string pass, string proxy, string address, CookieContainer cookie)
            {
                var botItem = new BtcBot(form, user, pass, proxy, address, cookie, timeout, interval);
                botItem.Index = this.Count;
                this.Add(botItem);
            }
        }

        [Serializable]
        public class SaveParam
        {
            public SaveParam(string user, string pass, string proxy, string address, CookieContainer cookie)
            {
                this.User = user;
                this.Proxy = proxy;
                this.Pass = pass;
                this.Address = address;
                this.Cookie = cookie;
            }

            public string User { set; get; }
            public string Pass { set; get; }
            public string Proxy { set; get; }
            public string Address { set; get; }
            public CookieContainer Cookie { set; get; }
        }

        [Serializable]
        public class ParamList : List<SaveParam>
        {
            public void Add(string user, string pass, string proxy, string address, CookieContainer cookie)
            {
                this.Add(new SaveParam(user, pass, proxy, address, cookie));
            }
        }

        private Settings settings = Settings.Default;


        public MainForm()
        {
            delegMessage = new eventDelegate(Event_Message);
            InitializeComponent();
        }


        private void loginButton_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];
                botList[sel].Login();
            }
        }


        private void LoadSettings()
        {
            ParamList botParams = new ParamList();

            if (settings.botParams != null)
            {
                botParams = settings.botParams;

                for (int i = 0; i < botParams.Count; i++)
                {
                    var par = botParams[i];
                    botList.Add(this, par.User, par.Pass, par.Proxy, par.Address, par.Cookie);
                }
            }

            UpdateAccListView();

            timeout = settings.timeOut;
            interval = settings.interval;
        }

        private void SaveSettings()
        {
            ParamList botParams = new ParamList();
            for (int i = 0; i < botList.Count; i++)
            {
                var bot = botList[i];
                botParams.Add(bot.Username, bot.Password, bot.Proxy, bot.Address, bot.Cookie);
            }

            settings.botParams = botParams;
            settings.Save();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                //WebCore.Initialize(new WebConfig(), true);
                WebCore.ResourceInterceptor = new CustomInterceptor();

                LoadSettings();
                if (accListView.Items.Count != 0)
                {
                    accListView.Items[0].Selected = true;
                }
            }
            catch (Exception ex )
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var item in botList)
            {
                if (item.isWorking)
                    item.Stop();
            }

            WebCore.Shutdown();
            SaveSettings();
            botList.Clear();
        }



        private void runBotButton_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];

                if (botList[sel].isWorking)
                {
                    botList[sel].Stop();
                }
                else
                {
                    botList[sel].Start(true);
                   
                }
                setButtonState(botList[sel].isWorking);
            }

        }

        private bool setButtonState(bool iswork)
        {
            if (iswork)
            {
                runBotButton.Text = "Stop";
                runBotButton.Image = Properties.Resources.stop;
                return true;
            }
            else
            {
                runBotButton.Text = "Start";
                runBotButton.Image = Properties.Resources.start;
                return false;
            }
        }


        private Color GetRowColor(AdStatus status)
        {
            switch (status)
            {
                case AdStatus.Process: return Color.Yellow;
                case AdStatus.Success: return Color.LightGreen;
                case AdStatus.Failture: return Color.OrangeRed;
                case AdStatus.Iddle: return Color.White;
            }

            return Color.White;

        }



        public void Event_Message(object data, int index, flag myflag)
        {
            if (data == null)
                return;

            switch (myflag)
            {
                case flag.LoginInfo:
                    accListView.Items[index].SubItems[1].Text = botList[index].Balance;
                    break;

                case flag.ReportProgress:
                    toolStripProgressBar1.Value = (int)data;
                    break;

                case flag.StripImg:
                    int stat = (int)data;

                    if (stat == 0)
                        ImageLabel.Image = Properties.Resources.working;
                    else 
                        ImageLabel.Image = Properties.Resources.ready;

                    break;

                case flag.UpdStatus:
                   
                    var currbot = botList[index];

                    accListView.Items[index].SubItems[2].Text = currbot.getAdCount();

                    setDebugInfo(currbot.Pos);

                    break;

                   
                case flag.AdsLoaded:
                    fillListView(botList[index].adList);
                    break;
                case flag.SendText:
                    accListView.Items[index].SubItems[3].Text = (string)data;
                    break;
            }

        }


        private void UpdateAccListView()
        {
            accListView.Items.Clear();
            
            if (botList.Count != 0)
            {
                accListView.BeginUpdate();
                for (int i = 0; i < botList.Count; i++)
                {
                    var bot = botList[i];


                    var item = new ListViewItem(new string[6] { bot.Username, bot.Balance, bot.adList.Count.ToString(), "Ready", bot.Proxy, bot.Address});

                    accListView.Items.Add(item);
                }
                accListView.EndUpdate();
            }

        }


        private void fillListView(List<Advert> lst)
        {
            adsListView.Items.Clear();

            if (lst.Count != 0)
            {
                adsListView.BeginUpdate();

                for (int i = 0; i < lst.Count; i++)
                {
                    var row = lst[i];
                    string time = row.Time.ToString();

                    var item = new ListViewItem(new string[4] { (i + 1).ToString(), row.Desc, row.Reward, time.Remove(time.Length - 3) });
                    item.BackColor = GetRowColor(row.resInfo.Stat);
                    
                    adsListView.Items.Add(item);

                }

                adsListView.EndUpdate();

            }
        }


        //Debug
        void setDebugInfo(int sel)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var bot = botList[accListView.SelectedIndices[0]];

                if ((bot.adList.Count != 0) && (adsListView.Items.Count != 0))
                {
                    var Curr = bot.adList[sel].resInfo;

                    adsListView.Items[sel].BackColor = GetRowColor(bot.adList[sel].resInfo.Stat);
                    label2.Text = "№" + (sel + 1).ToString() + " - " + botList[accListView.SelectedIndices[0]].adList[sel].Desc;
                    label5.Text = Curr.CapStr;
                    label6.Text = Curr.AdResult.ToString();
                    pictureBox1.Image = Curr.CapImg;
                }
            }

        }


        private void adsListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (adsListView.SelectedIndices.Count != 0)
            {
                setDebugInfo(adsListView.SelectedIndices[0]);
            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this.Text + " v" + Application.ProductVersion + Environment.NewLine + 
                "Copyright (c) Maxx53, 2015" + Environment.NewLine +
                "Site: http://maxx53.ru"+ Environment.NewLine +
                "E-mail: demmaxx@gmail.com", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            showToolStripMenuItem.PerformClick();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }



        private void accListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];
                loginTextBox.Text = botList[sel].Username;
                passTextBox.Text = botList[sel].Password;
                proxyComboBox.Text = botList[sel].Proxy;
                btcAddressBox.Text = botList[sel].Address;
                setButtonState(botList[sel].isWorking);

                fillListView(botList[sel].adList);
            }
        }

        private void addAccButton_Click(object sender, EventArgs e)
        {
            Application.DoEvents();
            botList.Add(this, loginTextBox.Text, passTextBox.Text, proxyComboBox.Text, btcAddressBox.Text, null);
            UpdateAccListView();
        }

        private void EditAccButton_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                Application.DoEvents();
                var sel = accListView.SelectedIndices[0];
                botList.RemoveAt(sel);
                var botItem =  new BtcBot(this, loginTextBox.Text, passTextBox.Text, proxyComboBox.Text, btcAddressBox.Text, null, timeout, interval);
                botItem.Index = sel;
                botList.Insert(sel, botItem);
                UpdateAccListView();
            }
        }

        private void RemAccButton_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];
                botList.RemoveAt(sel);
                UpdateAccListView();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];
                botList[sel].Cookie = null;
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];
                botList[sel].Proxy = proxyComboBox.Text;
                accListView.Items[sel].SubItems[4].Text = botList[sel].Proxy;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Random rnd = new Random();

            string Path = Application.StartupPath + @"\unsolved\";

            bool exists = System.IO.Directory.Exists(Path);

            if (!exists)
                System.IO.Directory.CreateDirectory(Path);

            foreach (var bot in botList)
            {
                foreach (var ad in bot.adList)
                {
                    if (ad.resInfo.AdResult == AdResult.NotSolved)
                    {
                        var filename = string.Format("{0}{1}_{2}{3}", Path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm"), rnd.Next(100000).ToString(), ".png");
                        var bmp = new Bitmap(ad.resInfo.CapImg);
                        bmp.Save(filename, ImageFormat.Png);
                    }
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (accListView.SelectedIndices.Count != 0)
            {
                var sel = accListView.SelectedIndices[0];
                botList[sel].WithDraw();
            }

        }

        private void button1_Click_1(object sender, EventArgs e)
        {

            regFrm.ShowDialog();
        }

    }
}

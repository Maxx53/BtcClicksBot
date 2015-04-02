using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace BtcClicksBot
{
    [Flags]
    public enum AdStatus
    {
        Iddle,
        Process,
        Success,
        Failture
    }

    [Flags]
    public enum AdResult
    {
        None,
        Solved,
        NotSolved,
        FlashCapcha,
        Timeout,
        Canselled
    }

    public class Advert
    {

        public class Result
        {
            public Result(AdStatus stat, string capStr, Bitmap capImg, AdResult adResult)
            {
                this.Stat = stat;
                this.CapStr = capStr;
                this.AdResult = adResult;
                this.CapImg = capImg;
            }

            public AdStatus Stat { set; get; }
            public AdResult AdResult { set; get; }
            public string CapStr { set; get; }
            public Bitmap CapImg { set; get; }
        }

        public Advert(string link, string desc, string reward, int time)
        {

            this.Link = link;
            this.Desc = desc;
            this.Reward = reward;
            this.Time = time;
            this.resInfo = new Result(AdStatus.Iddle, string.Empty, new Bitmap(1, 1), AdResult.None);
        }


        public string Desc { set; get; }
        public string Link { set; get; }
        public string Reward { set; get; }
        public int Time { set; get; }
        public Result resInfo { set; get; }

    }
}

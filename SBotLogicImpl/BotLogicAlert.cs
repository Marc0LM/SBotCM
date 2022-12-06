using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using SBotCore;

namespace SBotLogicImpl
{
    public class BotLogicAlert : BotLogic
    {
        public bool do_watch_local_ = true;
        public bool do_watch_ov_ = false;
        bool need_alert_;
        public override string Summary()
        {
            return need_alert_?"Hostile":"Safe";
        }
        //public List<string> hostile_tags_ = new() { "ExampleTag" };

        public override void UpdateCB()
        {
            need_alert_ = false;
            if(do_watch_local_)
            {
                if (ui.localChatwindowStack.members_.Any(m => m.tag.Contains("No") ||
                     m.tag.Contains("Neutral") ||
                     m.tag.Contains("Bad") ||
                     m.tag.Contains("Terrible")))
                {
                    need_alert_ = true;
                }
            }
            if(do_watch_ov_)
            {
                if (ui.overview.NumPlayer() > 0)
                {
                    need_alert_ = true;
                }
            }
            if (need_alert_)
            {
                alertWarningPlayer.controls.play();
            }
            Thread.Sleep(1000);
        }
    }
}

using SBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static SBotCore.EveUIParser;

namespace SBotLogicImpl
{
    public class BotLogicDPS : BotLogic
    {
        public override string GetBotSummary() => "";
        string target_name_="";
        string last_target_name_="";
        public override void OnUpdate()
        {
            //string m_name_ = ui.other_chatwindow_stack_.members_.FirstOrDefault(m => m.tag == m.name).name;
            //if(Singleton.Instance.members.ContainsKey(m_name_))
            //{
            //    Singleton.Instance.members[m_name_] = ui.ship_ui_.hp_;
            //}
            //else
            //{
            //    Singleton.Instance.members.Add(m_name_, ui.ship_ui_.hp_);
            //}
            if (ui.fleetView.last_broadcast_.content.Contains("spotted"))
            {
                ActivatePropMod(true);
            }
            if (ui.fleetView.last_broadcast_.content.Contains("Hold"))
            {
                ActivatePropMod(false);
            }
            ActivateAlwaysOnModules(true);
            if (ui.fleetView.last_broadcast_.content.Contains("Target"))
            {
                target_name_ = ui.fleetView.last_broadcast_.content.Split("Target ")[1].Split(" (")[0];
                if (last_target_name_ != target_name_)
                {
                    last_target_name_ = target_name_;
                    ActivateTurrents(false);
                }
                if (ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => l.Contains(target_name_))))//oe.distance_<300000&& 
                {
                    var target = ui.overview.overviewentrys_.First(oe => oe.labels_.Any(l => l.Contains(target_name_)));
                    if (ui.overview.not_targeted_.Any(nt=>nt.labels_.Any(l=>l.Contains(target_name_))))
                    {
                        input.KeyDown("^");
                        input.MouseClickLeft(ui.fleetView.last_broadcast_.node, ui.root);
                        input.KeyUp("^");
                    }
                    else
                    {
                        if (target.indicators_.Any(i => i.Contains("Active")))
                        {
                            ActivateTurrents(true);
                        }
                        else
                        {
                            input.MouseClickLeft(target.node_, ui.root);
                        }
                    }
                }
            }
            if (ui.overview.targeted_.Count > 3)
            {
                input.MouseClickLeft(ui.overview.targeted_.First(t=>t.labels_.All(l=>!l.Contains(target_name_))).node_, ui.root);
                input.MouseClickLeft(ui.activeItem.actions_.First(a => a.hint.Contains("nlock")).node, ui.root);
            }
            Thread.Sleep(2000);
        }
        int ActivatePropMod(bool activate)
        {

            var prop = ui.shipUI.active_slots_.Where(s => s.text.Equals("F1"));
            if (prop.Any())
            {
                if (prop.First().active ^ activate)
                {
                    input.KeyClick("F1");
                    return 0;
                }
            }
            return 1;
        }
        int ActivateTurrents(bool activate)
        {
            int res = 0;
            for (int i = 3; i <= 4; i++)
            {
                var text = "F" + i.ToString();
                var turrents = ui.shipUI.active_slots_.Where(s => s.text.Equals(text));
                if (turrents.Any())
                {
                    if (turrents.First().active ^ activate)
                    {
                        input.KeyClick(text);
                        res++;
                    }
                }
            }
            return res;
        }
        int ActivateAlwaysOnModules(bool activate)
        {
            int res = 0;
            for (int i = 5; i <= 8; i++)
            {
                var text = "F" + i.ToString();
                var module = ui.shipUI.active_slots_.Where(s => s.text.Equals(text));
                if (module.Any())
                {
                    //if (ui.overview_.targets.Any())
                    {
                        if (module.First().active ^ activate)
                        {
                            input.KeyClick(text);
                            res++;
                        }
                    }
                }
            }
            return res;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SBotCore;
using static SBotCore.EveUIParser;

namespace SBotLogicImpl
{
    public class BotLogicLogi : BotLogic
    {
        public override string Summary() => "";

        public override bool NeedLogOff()
        {
            return false;
        }

        public override void UpdateCB()
        {
            string m_name_ = ui.otherChatwindowStack.members_.FirstOrDefault(m => m.tag == m.name).name;
            if (Singleton.Instance.members.ContainsKey(m_name_))
            {
                Singleton.Instance.members[m_name_] = ui.shipUI.hp_;
            }
            else
            {
                Singleton.Instance.members.Add(m_name_, ui.shipUI.hp_);
            }
            if (!ui.shipUI.navistate_.warp) ActivatePropMod(true);
            var members = Singleton.Instance.members.ToList().OrderBy(kvp => kvp.Value.Item1).ToList();
            string target_name_ = "ajdwqbdlqwnd";
            foreach (var m in members)
            {
                if (m.Value.Item1 < 90)
                {
                    if (m.Value.Item3 == 0) continue;
                    if (ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => l.Contains(m.Key))))
                    {
                        target_name_ = m.Key;
                        break;
                    }
                }
            }
            if (ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => l.Contains(target_name_))))
            {
                var target = ui.overview.overviewentrys_.First(oe => oe.labels_.Any(l => l.Contains(target_name_)));
                if (ui.overview.not_targeted_.Any(nt => nt.labels_.Any(l => l.Contains(target_name_))))
                {
                    input.MouseClickLeft(target.node_, ui.root);
                    input.KeyClick("^");
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
            
            if (ui.overview.targeted_.Count > 5)
            {
                input.MouseClickLeft(ui.overview.targeted_.First(t => t.labels_.All(l => !l.Contains(target_name_))).node_, ui.root);
                input.MouseClickLeft(ui.activeItem.actions_.First(a => a.hint.Contains("nlock")).node, ui.root);
            }
        }

        public override bool PreFlightCheck(EveUI ui)
        {
            return true;
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
            for (int i = 2; i <= 8; i++)
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

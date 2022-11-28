using SBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBotLogicImpl
{
    public class BotLogic0Jump : BotLogic
    {

        public override string GetBotSummary()
        {
            return (is_travelling_ ? "travelling" : "idle");
        }

        bool is_travelling_ = false;
        int docking_state_ = 0;
        public override void OnUpdate()
        {
            is_travelling_ = ui.infoPanelRoute.next_waypoint_marker_ != null;
            if (ui.infoPanelRoute.next_system_ != null)
            {
                if (!ui.shipUI.navistate_.warp)
                {
                    var next_waypoint_oe = ui.overview.overviewentrys_.FirstOrDefault(oe => oe.labels_.Any(l => l.Contains(ui.infoPanelRoute.next_system_)));
                    if (next_waypoint_oe != null)
                    {
                        if (!ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => l.Contains(ui.infoPanelRoute.next_system_) && l.Contains(" - Star"))))
                        {
                            input.KeyDown("d");
                            input.MouseClickLeft(next_waypoint_oe.node_, ui.root);
                            input.KeyUp("d");
                        }
                        else
                        {
                            switch (docking_state_)
                            {
                                case 0:
                                    input.MouseClickRight(ui.infoPanelRoute.next_waypoint_marker_, ui.root);
                                    docking_state_ = 1;
                                    break;
                                case 1:
                                    if (ui.dropdownMenu.Exists())
                                    {
                                        var docking_button = ui.dropdownMenu.menu_entrys_.FirstOrDefault(me => me.text.Contains("Dock"));
                                        if (docking_button != default)
                                        {
                                            input.MouseClickLeft(docking_button.node, ui.root);
                                        }
                                    }
                                    docking_state_ = 0;
                                    break;
                            }
                        }
                    }
                }
            }
            Thread.Sleep(4000);
        }

        public override bool PreFlightCheck(EveUIParser.EveUI ui)
        {
            return ui.overview.Exists();
        }
    }
}

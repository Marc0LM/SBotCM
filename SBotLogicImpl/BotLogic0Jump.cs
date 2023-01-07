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

        public override string Summary()
        {
            return (is_travelling_ ? "travelling" : "idle") + " " + (isDocked ? "docked" : "in space");
        }

        bool is_travelling_ = false;
        bool isDocked=false;
        int docking_state_ = 0;
        public override void UpdateCB()
        {
            is_travelling_ = ui.infoPanelRoute.nextWaypointMarker != null;
            if (!ui.overview.Exist)
            {
                isDocked= true;
                return;
            }

            if (ui.infoPanelRoute.nextSystem != null)
            {
                if (!ui.shipUI.Navistate.warp)
                {
                    var next_waypoint_oe = ui.overview.AllEntrys.FirstOrDefault(oe => oe.labels.Any(l => l.Contains(ui.infoPanelRoute.nextSystem)));
                    if (next_waypoint_oe != null)
                    {
                        if (!ui.overview.AllEntrys.Any(oe => oe.labels.Any(l => l.Contains(ui.infoPanelRoute.nextSystem) && l.Contains(" - Star"))))
                        {
                            input.KeyDown("d");
                            input.MouseClickLeft(next_waypoint_oe.node, ui.root);
                            input.KeyUp("d");
                        }
                        else
                        {
                            switch (docking_state_)
                            {
                                case 0:
                                    input.MouseClickRight(ui.infoPanelRoute.nextWaypointMarker, ui.root);
                                    docking_state_ = 1;
                                    break;
                                case 1:
                                    if (ui.dropdownMenu.Exist)
                                    {
                                        var docking_button = ui.dropdownMenu.menuEntrys.FirstOrDefault(me => me.text.Contains("Dock"));
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
            return ui.overview.Exist;
        }
    }
}

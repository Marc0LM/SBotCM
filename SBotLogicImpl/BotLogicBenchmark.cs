using SBotCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SBotCore.EveUIParser.EveUI.ShipUI.Squadron;

namespace SBotLogicImpl
{
    public class BotLogicBenchmark : BotLogic
    {
        Stopwatch sw=new();
        long durSum = 0;
        int iter = 0;
        long start;
        public override string Summary()
        {
            if (iter == 0)
            {
                start = sw.ElapsedMilliseconds;
            }
            durSum = sw.ElapsedMilliseconds - start;
            return (durSum/iter).ToString();
        }

        public override void UpdateCB()
        {
            ui.shipUI.squadronsUI.squadrons.ToList().ForEach(Sq =>
            {
                Sq.slots.ForEach(sl =>
                {
                    var (x, y) = InputHelper.ClientCoordinateofUITtreeNode(sl.Node, ui.root, false);
                    WinApi.SetCursorPos(x, y);
                    Thread.Sleep(2000);
                    (x, y) = InputHelper.ClientCoordinateofUITtreeNode(sl.Node, ui.root, true);
                    WinApi.SetCursorPos(x, y);
                    Thread.Sleep(2000);
                });
                var (x, y) = InputHelper.ClientCoordinateofUITtreeNode(Sq.node, ui.root, true);
                WinApi.SetCursorPos(x, y);
                Thread.Sleep(2000);
            });

            return;
            sw.Start();
            iter++;
            return;
        }
    }
}

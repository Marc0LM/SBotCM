using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static SBotCore.EveUIParser.EveUI.ProbeScanner;

namespace SBotLogicImpl
{
    public sealed class RSOfficer : BotLogicRat
    {
        string lastBM="";
        Dictionary<string, ulong> BMs = new();
        List<string> goodSpawnBMs = new();
        public string BMPrefix = "0f";
        public List<string> goodSpawnNames = new();
        protected override List<string> BadSpawns { get => base.BadSpawns.Concat(goodSpawnNames).ToList();}
        public override string GetBotSummary() => (goodSpawnBMs.Any() ? goodSpawnBMs.Aggregate((b1, b2) => b1 + " " + b2) :
            ui == null ? "ui==null" : ui.shipUI.hp_ + " / " +
                            state.Select(s => s.Key + ":" + s.Value).Aggregate((a, b) => a + " " + b) + " / " +
                            ticksSinceLastHostile + " / " +
                            camperTicks + " / " +
                            tick)+"/"+(!BMs.Any()?"no BMs" : BMs.Select(bm => bm.Key.Split('<')[0] +"-"+bm.Value).Aggregate((a,b)=>a+"|"+b));
        public override int OnBadSpawn()
        {
            essWarningPlayer.controls.play();
            //CloseDM();
            logWriter.LogWrite("goodSpawn-----runing");
            string? goodSpawnName = BadSpawns.FirstOrDefault(bs => ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => l.Contains(bs))));
            if (goodSpawnName!=null)
            {
                logWriter.LogWrite($"goodSpawn: {goodSpawnName}-{lastBM}");
                if (!goodSpawnBMs.Contains(goodSpawnName + "-" + lastBM.Split('<')[0]))
                {
                    goodSpawnBMs.Add(goodSpawnName + "-" + lastBM.Split('<')[0]);
                    if (BMs.ContainsKey(lastBM))
                    {
                        BMs[lastBM] += 900;//almost 30min
                    }
                }
            }
            if (0 == WarpToAbstract(ui.standaloneBookmarkWindow.labels.First(l => l.text.Contains(safeBookmark)).node))
            {
                logWriter.LogWrite("goodSpawn-----ran");
                return 0;
            }
            return 1;
        }
        public override int OnNavigate()
        {
            string navigate = "navigate";
            if (!state.ContainsKey(navigate)) state.Add(navigate, 0);

            ui.standaloneBookmarkWindow.labels.Where(l => l.text.StartsWith(BMPrefix)).ToList().ForEach(l =>
            {
                if (!BMs.ContainsKey(l.text))
                {
                    BMs.Add(l.text, 0);
                }
            });
            if (!BMs.Any()) return 0;
            BMs = BMs.OrderBy(p => p.Value).ToDictionary(p => p.Key, p => p.Value);

            int res = 1;
            switch (state[navigate])
            {
                case 0:
                    if ((int.Parse(DateTime.Now.ToString("HHmm")) > int.Parse(timeToRest) && !rat_till_next_day) ||
                            (int.Parse(DateTime.Now.ToString("HHmm")) > 1850 && int.Parse(DateTime.Now.ToString("HHmm")) < 1900))
                    {
                        needRest = true;
                        res = 0;
                    }
                    if (ui.droneView.NumDronesIndside() < numDrones && useDronesAsMainDPS)// not enough drones
                    {
                        noDroneTicks++;
                        if (noDroneTicks > 3)
                        {
                            logWriter.LogWrite($"not enough drones!{ui.droneView.NumDronesIndside()}");
                            disabled = true;
                            res = 0;
                        }
                    }
                    else
                    {
                        noDroneTicks = 0;
                    }
                    lastBM = BMs.First().Key;
                    
                    if (WarpToAbstract(ui.standaloneBookmarkWindow.labels.First(l => l.text.Equals(lastBM)).node) == 0)
                    {
                        state[navigate] = 1;
                        BMs[lastBM] = tick;
                        if (!ui.overview.tabs_.First(t => t.text.Contains(tabPve)).selected)
                        {
                            input.MouseClickLeft(ui.overview.tabs_.First(t => t.text.Contains(tabPve)).node, ui.root);
                        }
                    }
                    break;
                case 1://check anom
                    if (avoidBadSpawns && ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => BadSpawns.Any(bs => l.Contains(bs)))))
                    {
                        state[navigate] = 101;
                        goto case 101;
                    }
                    else
                    {
                        if (ui.overview.NumPlayer() > 0||ui.overview.NumNPC()<1)
                        {

                            state[navigate] = 0;
                        }
                        else
                        {
                            state[navigate] = 2;
                        }
                    }
                    break;
                case 101:
                    if (OnBadSpawn() == 0)
                    {
                        state[navigate] = 0;
                    }
                    break;
                case 2://launch drones
                    //ReadyKeyClick();
                    //input.KeyClick(keyReconnectDrones);
                    ActivateAlwaysOnModules(true);
                    if (useDronesAsMainDPS)
                    {
                        Launchdrones();
                    }
                    ActivateF1Module(true);
                    res = 0;
                    break;
            }
            if (res == 0)
            {
                state[navigate] = 0;
                return 0;
            }
            return 1;
        }
        public override bool NeedLogOff()
        {
            return false;
        }
    }
}

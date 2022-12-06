
using SBotCore;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using static SBotCore.EveUIParser;
using static SBotCore.EveUIParser.EveUI.ProbeScanner;

namespace SBotLogicImpl
{
    public sealed class RSAnom : BotLogicRat
    {

        Anom? nextAnom;
        public override int Navigate()
        {
            string navigate = "navigate";
            if (!state.ContainsKey(navigate)) state.Add(navigate, 0);
            int res = 1;
            switch (state[navigate])
            {
                case 0:
                    if ((int.Parse(DateTime.Now.ToString("HHmm")) > int.Parse(timeToRest) && !rat_till_next_day) ||
                            (int.Parse(DateTime.Now.ToString("HHmm")) > 1820 && int.Parse(DateTime.Now.ToString("HHmm")) < 1900))
                    {
                        NeedRest = true;
                        res = 0;
                    }
                    var badanoms = occupiedAnoms.Concat(avoidedAnoms).ToList();//.Concat(bad_anoms_shared_.Keys.ToList());
                    string badanomss = "";
                    badanoms.ToList().ForEach(badanom => badanomss += (" " + badanom));
                    Log("bad anoms " + badanomss);
                    var nextAnomAndDistance = anomsToRun.Select(pa =>
                    {
                        Log(pa);
                        IEnumerable<Anom> candidates = anomOrder switch
                        {
                            AnomOrder.IDF => ui.probescannerView.anoms_.OrderBy(a => a.Id),
                            AnomOrder.IDL => ui.probescannerView.anoms_.OrderByDescending(a => a.Id),
                            AnomOrder.First => ui.probescannerView.anoms_,
                            AnomOrder.Last => ui.probescannerView.anoms_.Reverse<Anom>(),
                            AnomOrder.Nearest => ui.probescannerView.anoms_.OrderBy(a => a.DistanceByKm),
                            _ => ui.probescannerView.anoms_.OrderBy(a => a.DistanceByKm),
                        };
                        return candidates.Where(a => badanoms.All(ba => !ba.Equals(a.Id)) && a.Name.Contains(pa.Split('@')[0]))
                                .Select(a => (a, pa.Contains('@') ? pa.Split('@')[1] : defaultWarpToAnomDistanceKM)).FirstOrDefault();
                    }).FirstOrDefault(naad => naad.a != null);
                    if (nextAnomAndDistance.a == null)
                    {
                        Log("No Anoms to Run");
                        if (clearOccupiedAnomsIfNoRunnableAnoms)
                        {
                            occupiedAnoms.Clear();
                        }
                        res = 0;
                        //state["rat"] = 0;
                    }
                    else
                    {
                        nextAnom = nextAnomAndDistance.a;
                        var tWarpToAnomDistanceKM = nextAnomAndDistance.Item2;
                        //if (!ignore_anoms_without_telecom_ && !ignore_anoms_with_telecom_)  check telecom
                        if (true)
                        {
                            Log(nextAnom.Id + " " + nextAnom.Name + " " + nextAnom.DistanceByKm);
                            var warpAction = WarpToAbstract;
                            if (tWarpToAnomDistanceKM.Equals("10") ||
                                tWarpToAnomDistanceKM.Equals("20") ||
                                tWarpToAnomDistanceKM.Equals("30") ||
                                tWarpToAnomDistanceKM.Equals("50") ||
                                tWarpToAnomDistanceKM.Equals("70") ||
                                tWarpToAnomDistanceKM.Equals("100"))
                            {
                                warpAction = WarpToAbstractWithIn;
                                actualWarpToAnomDistanceKM = tWarpToAnomDistanceKM;
                            }
                            if (ui.overview.tabs_.Any(t => t.text.Contains(tabPve)))//choose pve tab
                            {
                                if (!ui.overview.tabs_.First(t => t.text.Contains(tabPve)).selected)
                                {
                                    input.MouseClickLeft(ui.overview.tabs_.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                                }
                            }
                            if (0 == warpAction(nextAnom.Node) || (nextAnom.DistanceByKm < 150 && !ui.shipUI.navistate_.warp))//fix game ui bug: still shows warp when at 0 of anom
                            {
                                if (ui.overview.tabs_.Any(t => t.text.Contains(tabPve) && t.selected))
                                {
                                    state["warp"] = 0;
                                    state[navigate] = 1;
                                }
                                //state["rat"] = 2;
                            }
                        }
                        else
                        {
                            //WarpToAbstract(nextAnom.Node);
                            //if (ui.shipUI.navistate_.warp)
                            //{
                            //    input.KeyClick(keyStopShip);
                            //    Thread.Sleep(2000);
                            //    return 1;
                            //}
                        }
                    }
                    break;
                case 1://check anom
                    if (avoidBadSpawns&&ui.overview.overviewentrys_.Any(oe => oe.labels_.Any(l => BadSpawns.Any(bs => l.Contains(bs)))))
                    {
                        state[navigate] = 101;
                        goto case 101;
                        //if ()
                        {
                            
                        }
                    }
                    else
                    {
                        if (ui.overview.NumPlayer() > 0)
                        {
                            if (nextAnom != null)
                            {
                                occupiedAnoms.Add(nextAnom.Id);
                            }
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
                    ReadyKeyClick();
                    input.KeyClick(keyReconnectDrones);
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
    }


}

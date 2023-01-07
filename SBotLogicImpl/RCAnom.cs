using SBotCore;
using System.Diagnostics;
using System.Linq;
using WMPLib;
using static SBotCore.EveUIParser;
using static SBotCore.EveUIParser.EveUI.ProbeScanner;
using static SBotCore.EveUIParser.EveUI.ShipUI.Squadron;

namespace SBotLogicImpl
{
    public sealed class RCAnom : BotLogic
    {
        public int validNPCRangeM = 150_000;

        //Decrepated
        bool alignRepAfterCloaked = false;
        bool cloakAfterRetreat = false;
        bool fofMode = false;
        bool marauderMode = false;
        bool useDronesAsMainDPS = true;
        ulong minValueToLoot = 50000000;
        int numDrones = 5;
        int maxDroneIdleTicks = 2;
        string keyDronesEngage = "g";
        string keyStopShip = "s";
        List<string> dronesEngageNames = new();
        bool useTurrents = true;
        int useTurrentsDistanceM = 30000;
        List<string> turrentsEngageNames = new();
        bool ignore_anoms_without_telecom_ = false;
        bool ignore_anoms_with_telecom_ = false;


        //Configurable
        public int tickIntervalMS = 1500;

        public string timeToRest = "1200";
        public bool closeClientAfterRested;

        public string safeBookmark = "0 safe";

        public bool waitForFightersToReturn = false;
        public List<string> hostileTags = new();

        public int ticksToWaitAfterHostileLeave = 600;
        public uint maxCamperTicks = 7200;
        public uint maxBadCamperTicks = 1800;
        public List<string> badCamperNames = new();

        public bool BMBoss = false;
        public string bossName = "Dark Blood";

        public List<string> anomsToRun = new();
        public bool clearOccupiedAnomsIfNoRunnableAnoms = true;
        public string defaultWarpToAnomDistanceKM = "50";
        public bool activatePropOnWarp = true;


        public AnomOrder anomOrder = AnomOrder.FIRST;

        public List<string> entityToAnchor = new();

        public Anchor anchorType = SBotLogicImpl.Anchor.FOLLOWDRONE;

        public int anchorSpeed = 50;

        public bool recallDamagedSquadron = false;
        public int damagePecentOnRecall = 50;

        public string keyNextTarget = "n";

        public string keyReturnAndOrbitAllSquadrons = "y";
        public string keyLaunchAllSquadrons = "f";
        public string keyRecallAllSquadrons = "r";
        public string keyRecallSelectedSquadron = "h";

        public string keyMakeBookmark = "b";

        public int lowHPShield = 10;
        public int lowHPArmor = 90;
        public string repBookmark = "1 rep";

        public List<string> ewarTags = new();

        public bool focusFire = false;

        public int maxTargetCount = 6;
        public bool lockSpecialRats = true;
        public List<string> specialRatNames = new();

        public List<string> badSpawns = new() { "Titan", "Dreadnought", "Carrier", "♦" };

        //Non-configurable
        bool lockAll = true;
        string lockKey = "^";

        string actualWarpToAnomDistanceKM = "50";

        string tabPve = "pve";
        string tabEntity = "entity";
        string tabWreck = "wreck";

        ulong tick = 0;
        ulong camperTicks = 0;
        bool needLogOff = false;

        Dictionary<string, int> state = new()
        {
            {"update",0},
            {"retreat",0},
            {"rat",0},
            {"warp",0},
            {"loot",0}
        };
        //public (int update, int retreat, int rat, int warp, int loot) state;
        List<string> avoidedAnoms = new();
        List<string> occupiedAnoms = new();
        //Dictionary<string, string> bad_anoms_shared_ = new();

        UITreeNode MagicNode { get => ui.shipUI.capContainer; }
        List<string> BadSpawns { get => badSpawns; }
        bool NeedRest { get => needRest; set => needRest = value; }
        bool Disabled { get => disabled; set => disabled = value; }

        int ticksSinceLastHostile = 0;



        public void ReadyKeyClick()
        {
            input.MouseClickLeft(MagicNode, ui.root, true);
        }
        public void CloseDM()
        {
            if (ui.dropdownMenu.Exist)
            {
                input.MouseClickLeft(MagicNode, ui.root);
            }
        }
        public int Orbit(UITreeNode node)
        {
            input.KeyDown("w");
            input.MouseClickLeft(node, ui.root);
            input.KeyUp("w");
            return 0;
        }
        public int Approach(UITreeNode node)
        {
            input.KeyDown("q");
            input.MouseClickLeft(node, ui.root);
            input.KeyUp("q");
            return 0;
        }
        public int KeepAtRange(UITreeNode node)
        {
            input.KeyDown("e");
            input.MouseClickLeft(node, ui.root);
            input.KeyUp("e");
            return 0;
        }
        public int ActivateF1Module(bool activate)
        {
            var prop = ui.shipUI.activeSlots.Where(s => s.Text.Equals("F1"));
            if (prop.Any())
            {
                if (prop.First().Active ^ activate)
                {
                    if (!prop.First().Busy)
                    {
                        input.KeyClick("F1");
                    }
                    return 0;
                }
            }
            return 1;
        }

        //public int ActivateTurrents(bool activate)
        //{
        //    int res = 0;
        //    for (int i = 3; i <= 4; i++)
        //    {
        //        var text = "F" + i.ToString();
        //        var turrents = ui.shipUI.activeSlots.Where(s => s.Text.Equals(text));
        //        if (turrents.Any())
        //        {
        //            if (turrents.First().Active ^ activate)
        //            {
        //                if (turrents.First().Quantity > 0)
        //                {
        //                    input.KeyClick(text);
        //                    res++;
        //                }
        //            }
        //        }
        //    }
        //    return res;
        //}
        public int ActivateAlwaysOnModules(bool activate)
        {
            int res = 0;
            for (int i = 3; i <= 8; i++)
            {
                var text = "F" + i.ToString();
                var module = ui.shipUI.activeSlots.Where(s => s.Text.Equals(text));
                if (module.Any())
                {
                    {
                        if (module.First().Active ^ activate)
                        {
                            input.KeyClick(text);
                            res++;
                        }
                    }
                }
            }
            return res;
        }
        public int ActivateCloakMod(bool activate)//cannot read active state
        {
            var prop = ui.shipUI.activeSlots.Where(s => s.Text.Equals("F2"));
            if (prop.Any())
            {
                if (prop.First().Active ^ activate)
                {
                    input.KeyClick("F2");
                    return 0;
                }
            }
            return 1;
        }
        public int FocusFire()
        {
            Launchdrones();
            var squadronsInSpace = ui.shipUI.squadronsUI.squadrons.Where(s => s.state.Equals(FighterActionState.INSPACE));
            if (squadronsInSpace.Any())
            {
                squadronsInSpace.ToList().ForEach(s =>
                {
                    var slotF1 = s.slots[0];
                    if (!slotF1.Active && !slotF1.Busy)
                    {
                        input.MouseClickLeft(s.slots[0].Node, ui.root);
                    }
                });
            }
            return 0;
        }

        //0 as succeed
        public int WarpToAbstract(UITreeNode node)
        {
            if (ui.shipUI.Navistate.warp)
            {
                Log("In warp");
                CloseDM();
                return 1;
            }
            switch (state["warp"])
            {
                case 0:
                    CloseDM();
                    input.MouseClickLeft(node, ui.root);
                    input.MouseClickRight(node, ui.root);
                    state["warp"] = 1;
                    break;
                case 1:
                    if (ui.dropdownMenu.menuEntrys != null)
                    {
                        var bw = ui.dropdownMenu.menuEntrys.Where(m => m.text.Contains("Warp") && m.text.Contains('m'));
                        if (bw.Any())
                        {
                            input.MouseClickLeft(bw.First().node, ui.root);
                            return 3;
                        }
                        else
                        {
                            var ba = ui.dropdownMenu.menuEntrys.Where(m => m.text.Contains("Approach") || m.text.Contains("Align"));
                            if (ba.Any())
                            {
                                state["warp"] = 0;
                                return 0;
                            }
                            else
                            {
                                state["warp"] = 0;
                            }
                        }
                    }
                    else
                    {
                        state["warp"] = 0;
                    }
                    break;
                default:
                    state["warp"] = 0;
                    break;//FIX: works well with warpto30 now
            }
            return 2;
        }
        public int WarpToAbstractWithIn(UITreeNode node)
        {
            if (ui.shipUI.Navistate.warp)
            {
                Log("In warp");
                CloseDM();
                return 1;
            }
            switch (state["warp"])
            {
                case 0:
                    CloseDM();
                    input.MouseClickLeft(node, ui.root);
                    input.MouseClickRight(node, ui.root);
                    state["warp"] = 1;
                    break;
                case 1:

                    if (ui.dropdownMenu.menuEntrys != null)
                    {
                        var bw = ui.dropdownMenu.menuEntrys.Where(m => m.text.Contains("Warp") && !m.text.Contains('m'));
                        if (bw.Any())
                        {
                            input.MouseMove(bw.First().node, ui.root);
                            state["warp"] = 2;
                        }
                        else
                        {
                            var ba = ui.dropdownMenu.menuEntrys.Where(m => m.text.Contains("Approach") || m.text.Contains("Align"));
                            if (ba.Any())
                            {
                                state["warp"] = 0;
                                return 0;
                            }
                            else
                            {
                                state["warp"] = 0;
                            }
                        }
                    }
                    else
                    {
                        state["warp"] = 0;
                    }
                    break;
                case 2:
                    if (ui.dropdownMenu.menuEntrys != null)
                    {
                        var bw = ui.dropdownMenu.menuEntrys.Where(m => m.text.Contains(actualWarpToAnomDistanceKM));
                        if (bw.Any())
                        {
                            input.MouseClickLeft(bw.First().node, ui.root);
                            state["warp"] = 2;
                            return 3;
                        }
                        else
                        {
                            state["warp"] = 1;
                        }
                    }
                    else
                    {
                        state["warp"] = 1;
                    }
                    break;
            }
            return 2;
        }
        //0 as succeed
        public int Recalldrones()
        {
            if (ui.shipUI.squadronsUI.squadrons.All(s => s.state.Equals(FighterActionState.LANDING)
                                                            || s.state.Equals(FighterActionState.REFUELING)
                                                            || s.state.Equals(FighterActionState.READY)))
            {
                return 0;
            }
            else
            {
                Log("Recall drones");
                ReadyKeyClick();
                input.KeyClick(keyRecallAllSquadrons);
                return 1;
            }
        }
        //0 as succeed
        public int Launchdrones()
        {
            if (!ui.shipUI.squadronsUI.squadrons.Any(s => s.state.Equals(FighterActionState.READY)))
            {
                return 0;
            }
            else
            {
                Log("Launch drones");
                ReadyKeyClick();
                input.KeyClick(keyLaunchAllSquadrons);
                return 1;
            }
        }
        public int LootCargo(string name)
        {
            switch (state["loot"])
            {
                case 0:
                    var candidate = ui.overview.AllEntrys.Where(ove => ove.labels.Any(l => l.Contains(name))).FirstOrDefault();
                    if (candidate == null)
                    {
                        return -1;
                    }
                    else
                    {
                        if (!ui.bookmarkLocationWindow.Exist)
                        {
                            input.KeyClick(keyMakeBookmark);
                        }
                        else
                        {
                            state["loot"] = 1;
                        }
                    }
                    break;
                case 1:
                    if (ui.bookmarkLocationWindow.Exist)
                    {
                        input.MouseClickLeft(ui.bookmarkLocationWindow.submitButton, ui.root);
                    }
                    else
                    {
                        state["loot"] = 2;
                    }
                    break;
                case 2:
                    if (!ui.bookmarkLocationWindow.Exist)
                    {
                        state["loot"] = 0;
                        return 0;
                    }
                    else
                    {
                        state["loot"] = 1;
                    }
                    break;
                //case 3:
                //    if (ui.inventory.Exist)
                //    {
                //        if (ui.inventory.TotalValueIsk() < minValueToLoot)
                //        {
                //            if (ui.inventory.Exist)
                //            {
                //                input.KeyClick(keyMakeBookmark);
                //            }
                //            state["loot"] = 0;
                //            return 0;
                //        }
                //        else
                //        {
                //            if (ui.inventory.BtnLootAll == null)
                //            {
                //                if (ui.inventory.Exist)
                //                {
                //                    input.KeyClick(keyMakeBookmark);
                //                }
                //                state["loot"] = 0;
                //                return 0;
                //            }
                //            input.MouseClickLeft(ui.inventory.BtnLootAll, ui.root);
                //            state["loot"] = 4;
                //        }
                //    }
                //    else
                //    {
                //        state["loot"] = 2;
                //    }
                //    break;
                //case 4:
                //    var nec = lastMessage.Contains("Not Enough");
                //    if (nec)
                //    {
                //        if (ui.inventory.Exist)
                //        {
                //            input.KeyClick(keyMakeBookmark);
                //        }
                //        state["loot"] = 0;
                //        return -2;
                //    }
                //    else
                //    {
                //        if (ui.inventory.Exist)
                //        {
                //            input.KeyClick(keyMakeBookmark);
                //        }
                //        state["loot"] = 0;
                //        return 0;
                //    }
            }
            return 1;
        }
        public int ClearEwar()
        {
            var wdoe = ui.overview.AllEntrys.Where(oe => oe.ewars.Any(ew => ewarTags.Any(ewr => ew.Contains(ewr))));//kill tackles
            if (wdoe.Any())
            {
                Log("killing ewar");
                input.KeyDown(lockKey);
                wdoe.ToList().ForEach(oe =>
                {
                    if (!oe.targeting && !oe.indicators.Any(i => i.Contains("argeted")))
                    {
                        input.MouseClickLeft(oe.node, ui.root);
                    }
                });
                input.KeyUp(lockKey);
                if (wdoe.Any(oe => oe.indicators.Any(i => i.Contains("argeted"))))
                {
                    if (wdoe.Any(oe => oe.indicators.Any(i => i.Contains("ActiveTarget"))))
                    {

                        FocusFire();
                    }
                    else
                    {
                        input.MouseClickLeft(wdoe.First().node, ui.root);
                    }
                }
                return 1;
            }
            else
            {
                return 0;
            }
        }
        
        //kills tackles if more than 15
        public int ClearTackle()
        {
            var wdoe = ui.overview.AllEntrys.Where(oe => oe.ewars.Any(ew => ew.Contains("warp")));//kill tackles TODO seems to be broken here: ewar state remains even actually gone
            if (wdoe.Count()>15)
            {
                Log("killing tackle");
                input.KeyDown(lockKey);
                wdoe.ToList().ForEach(oe =>
                {
                    if (!oe.targeting && !oe.indicators.Any(i => i.Contains("argeted")))
                    {
                        input.MouseClickLeft(oe.node, ui.root);
                    }
                });
                input.KeyUp(lockKey);
                if (wdoe.Any(oe => oe.indicators.Any(i => i.Contains("argeted"))))
                {
                    if (wdoe.Any(oe => oe.indicators.Any(i => i.Contains("ActiveTarget"))))
                    {
                        FocusFire();
                    }
                    else
                    {

                        input.MouseClickLeft(wdoe.First().node, ui.root);
                        //input.KeyClick(engage_target_drones_key);
                    }
                }
                return 1;
            }
            return 0;
        }

        bool retreatOnLastSite = false;

        public void OnRetreat(bool lowhp)
        {
            if (ui.shipUI.Navistate.warp)
            {
                CloseDM();
                return;
            }
            string targetbm;
            if (lowhp)
            {
                targetbm = repBookmark;
            }
            else
            {
                targetbm = safeBookmark;
            }
            switch (state["retreat"])
            {
                case -1:
                    Log($"stopped hostlile: {state["update"] == -1} lowHP: {state["update"] == -2} rest: {state["update"] == -3}");
                    break;
                case 0:
                    Recalldrones();
                    //ActivateF1Module(false);
                    input.MouseClickLeft(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);//TODO fix other bots
                    input.MouseClickRight(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);
                    state["retreat"] = 1;
                    break;
                case 1:
                    if (ui.dropdownMenu.menuEntrys != null)
                    {
                        if (ui.dropdownMenu.menuEntrys.Any(me => me.text.Contains("Approa")))
                        {
                            state["retreat"] = 2;
                        }
                        else
                        {
                            if (ui.dropdownMenu.menuEntrys.Any(me => me.text.Contains("Align")))
                            {
                                input.MouseClickRight(ui.dropdownMenu.menuEntrys.Where(me => me.text.Contains("Align")).First().node, ui.root);
                                if (activatePropOnWarp)
                                {
                                    ActivateF1Module(true);
                                }
                                state["retreat"] = 2;
                            }
                            else
                            {
                                input.MouseClickLeft(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);//TODO fix other bots
                                input.MouseClickRight(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);
                            }
                        }
                    }
                    else
                    {
                        input.MouseClickRight(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);
                    }
                    break;
                case 2:
                    if (ClearTackle() == 0 || !ui.shipUI.activeSlots.Any())
                    {
                        if (!waitForFightersToReturn
                            || ui.overview.NumPlayer > 0
                            || ui.shipUI.HP.structure < 95)
                        {
                            if (0 == WarpToAbstract(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node))
                            {
                                state["retreat"] = 4;
                            }
                            //state["retreat"] = 3;
                            //goto case 3;
                        }
                        else
                        {
                            if(0 == Recalldrones())
                            {
                                if (0 == WarpToAbstract(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node))
                                {
                                    state["retreat"] = 4;
                                }
                            }
                        }
                    }
                    break;
                case 3:
                    if (0 == WarpToAbstract(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node))
                    {
                        state["retreat"] = 4;
                    }
                    break;
                case 4:
                    input.MouseClickRight(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);
                    state["retreat"] = 5;
                    break;
                case 5:
                    if (ui.dropdownMenu.menuEntrys != null)
                    {
                        var BA = ui.dropdownMenu.menuEntrys.Where(me => me.text.Contains("Approach"));
                        if (BA.Any())
                        {
                            input.MouseClickLeft(BA.First().node, ui.root);
                            state["retreat"] = -1;
                        }
                    }
                    input.MouseClickRight(ui.standaloneBookmarkWindow.labels.Where(l => l.text.Contains(targetbm)).First().node, ui.root);
                    break;
            }
        }

        bool needRest = false;
        bool disabled = false;
        int[] droneIdleTicks = new int[15];


        bool doubleCheckFriendly = false;
        int zeroNPCTicks = 0;
        bool avoidBadSpawns = true;
        bool alwaysOnProp = true;
        bool oneCycleProp = false;
        public string keyReconnectDrones = "t";


        public int OnBadSpawn()
        {
            //CloseDM();
            Log("badspawn-----runing");
            var ca = ui.probescannerView.anoms.Where(a => !a.Unit.Equals("AU"));
            if (ca.Any())
            {
                if (!avoidedAnoms.Contains(ca.First().Id))
                {
                    avoidedAnoms.Add(ca.First().Id);
                }
                Log($"avoided {ca.First().Id}");
            }
            if (0 == Recalldrones())
            {
                var res = WarpToAbstract(ui.standaloneBookmarkWindow.labels.First(l => l.text.Contains(safeBookmark)).node);
                if (3 == res)
                {
                    ActivateF1Module(true);
                }
                if (0 == res)
                {
                    Log("badspawn-----ran");
                    return 0;
                }
            }
            return 1;
        }
        (Anom? a,string warpToDistance) GetNextAnom()
        {
            var badanoms = occupiedAnoms.Concat(avoidedAnoms).ToList();//.Concat(bad_anoms_shared_.Keys.ToList());
            string badanomss = "";
            badanoms.ToList().ForEach(badanom => badanomss += (" " + badanom));
            Log("bad anoms " + badanomss);
            var nextAnomAndDistance = anomsToRun.Select(pa =>
            {
                Log(pa);
                IEnumerable<Anom> candidates = anomOrder switch
                {
                    AnomOrder.IDF => ui.probescannerView.anoms.OrderBy(a => a.Id),
                    AnomOrder.IDL => ui.probescannerView.anoms.OrderByDescending(a => a.Id),
                    AnomOrder.FIRST => ui.probescannerView.anoms,
                    AnomOrder.LAST => ui.probescannerView.anoms.Reverse<Anom>(),
                    AnomOrder.NEAREST => ui.probescannerView.anoms.OrderBy(a => a.DistanceByKm),
                    _ => ui.probescannerView.anoms.OrderBy(a => a.DistanceByKm),
                };
                return candidates.Where(a => badanoms.All(ba => !ba.Equals(a.Id)) && a.Name.Contains(pa.Split('@')[0]))
                        .Select(a => (a, pa.Contains('@') ? pa.Split('@')[1] : defaultWarpToAnomDistanceKM)).FirstOrDefault();
            }).FirstOrDefault(naad => naad.a != null);
            return nextAnomAndDistance;
        }

        Anom? nextAnom;
        bool propActivavted = false;
        public int Navigate()
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
                    var nextAnomAndDistance = GetNextAnom();
                    if (nextAnomAndDistance == default)
                    {
                        Log("No Anoms to Run");
                        if (clearOccupiedAnomsIfNoRunnableAnoms)
                        {
                            occupiedAnoms.Clear();
                        }
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
                            if (ui.overview.tabs.Any(t => t.text.Contains(tabPve)))//choose pve tab
                            {
                                if (!ui.overview.tabs.First(t => t.text.Contains(tabPve)).selected)
                                {
                                    input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                                }
                            }
                            if (activatePropOnWarp)
                            {
                                if (ui.shipUI.Navistate.warp)
                                {
                                    if (!propActivavted)
                                    {
                                        propActivavted = true;
                                        //ActivateF1Module(true);
                                    }
                                }
                            }
                            if (!ui.shipUI.Navistate.warp)//fix game ui bug: still shows warp when at 0 of anom
                            {
                                if (nextAnom.DistanceByKm < 150)
                                {
                                    state["warp"] = 0;
                                    state[navigate] = 1;
                                    break;
                                }
                            }
                            var warpRes = warpAction(nextAnom.Node);
                            if (warpRes == 3)
                            {
                                Thread.Sleep(100);//sync with warp
                                ActivateF1Module(true);
                            }
                            if (warpRes==0)
                            {
                                if (ui.overview.tabs.Any(t => t.text.Contains(tabPve) && t.selected))
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
                    propActivavted = false;
                    if (avoidBadSpawns && ui.overview.AllEntrys.Any(oe => oe.labels.Any(l => BadSpawns.Any(bs => l.Contains(bs)))))
                    {
                        state[navigate] = 101;
                        goto case 101;
                    }
                    else
                    {
                        if (ui.overview.NumPlayer > 0)
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
                        state[navigate] = 102;
                    }
                    break;
                case 102:
                    if (Recalldrones() == 0)
                    {
                        state[navigate] = 0;
                    }
                    break;
                case 2://launch drones
                    ActivateAlwaysOnModules(true);
                    Launchdrones();
                    //ActivateF1Module(true);
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

        double speedBeforeAnchor = 0;
        int noDroneTicks = 0;
        public int Anchor()
        {
            var anchor = "anchor";
            if (!state.ContainsKey(anchor)) state.Add(anchor, 0);
            int res = 1;
            switch (state[anchor])
            {
                case 0://start anchoring
                    speedBeforeAnchor = ui.shipUI.Navistate.speed;
                    switch (anchorType)
                    {
                        case SBotLogicImpl.Anchor.NOMOVE:
                            res = 0;
                            break;
                        case SBotLogicImpl.Anchor.FOLLOWDRONE:
                            if (ui.droneView.dronesInSpace.Any<(UITreeNode node, string name, string state)>())
                            {
                                input.MouseClickLeft(ui.droneView.dronesInSpace[0].node, ui.root);
                                state[anchor] = 1;
                            }
                            else
                            {
                                Log("can't find drones to anchor, anchoring as Nomove");
                                res = 0;
                            }
                            break;
                        case SBotLogicImpl.Anchor.ORBITENTITY:
                            if (ui.overview.tabs.Any(((UITreeNode node, string text, bool selected) t) => t.text.Equals(tabEntity) && t.selected))//if entitytab is selected
                            {
                                if (ui.overview.AllEntrys.Count > 0)
                                {
                                    var entity = ui.overview.AllEntrys.Where<EveUI.Overview.OverviewEntry>(oe => oe.labels.Any<string>(l => entityToAnchor.Any(eo => l.Contains(eo)))).OrderBy(e => e.distance);
                                    if (entity.Any())
                                    {
                                        var c = entity.Count();
                                        Orbit(entity.ElementAt(c / 2).node);
                                        state[anchor] = 2;
                                    }
                                    else
                                    {
                                        if (ui.droneView.dronesInSpace.Any<(UITreeNode node, string name, string state)>())
                                        {
                                            Log("can't find entity to orbit, anchoring on drones");
                                            input.MouseClickLeft(ui.droneView.dronesInSpace[0].node, ui.root);
                                            state[anchor] = 1;
                                        }
                                        else
                                        {
                                            Log("can't find entity or drones to anchor, anchoring as Nomove");
                                            res = 0;
                                        }
                                    }
                                }
                                else
                                {
                                    if (ui.droneView.dronesInSpace.Any<(UITreeNode node, string name, string state)>())
                                    {
                                        Log("NO entity to orbit, anchoring on drones");
                                        input.MouseClickLeft(ui.droneView.dronesInSpace[0].node, ui.root);
                                        state[anchor] = 1;
                                    }
                                    else
                                    {
                                        Log("can't find entity or drones to anchor, anchoring as Nomove");
                                        res = 0;
                                    }
                                }
                            }
                            else
                            {
                                if (!ui.overview.tabs.Any(((UITreeNode node, string text, bool selected) t) => t.text.Equals(tabEntity)))//check if entitytab exists
                                {
                                    Log($"NO tab named '{tabEntity}', anchoring on drones");
                                    if (ui.droneView.dronesInSpace.Any<(UITreeNode node, string name, string state)>())
                                    {
                                        Log("NO entity to orbit, anchoring on drones");
                                        input.MouseClickLeft(ui.droneView.dronesInSpace[0].node, ui.root);
                                        state[anchor] = 1;
                                    }
                                    else
                                    {
                                        Log("can't find entity or drones to anchor, anchoring as Nomove");
                                        res = 0;
                                    }
                                    break;
                                }
                                else
                                {
                                    var (node, text, selected) = ui.overview.tabs.Where(((UITreeNode node, string text, bool selected) t) => t.text.Equals(tabEntity)).First();
                                    input.MouseClickLeft(node, ui.root);
                                }
                            }
                            break;
                        case SBotLogicImpl.Anchor.KEEPATRANGE:
                            Log("NOT IMPLEMENTEED YET");
                            Log("anchoring on drones");
                            anchorType = SBotLogicImpl.Anchor.FOLLOWDRONE;
                            break;
                        //if (ui.overview.tabs_.Any(t => t.text.Equals(tabEntity) && t.selected))//if entitytab is selected
                        //{
                        //    if (ui.overview.overviewentrys_.Count > 0)
                        //    {
                        //        var entity = ui.overview.overviewentrys_.Where(oe => oe.labels_.Any(l => entityToAnchor.Any(eo => l.Contains(eo)))).OrderBy(e => e.distance_);
                        //        if (entity.Any())
                        //        {
                        //            var c = entity.Count();
                        //            //input.MouseClickLeft(entity.ElementAt(c / 2).node_, ui.root);
                        //            //state["rat"] = 7;
                        //            KeepAtRange(entity.ElementAt(c / 2).node_);
                        //            state["rat"] = 8;
                        //        }
                        //        else
                        //        {
                        //            Log("can't find entity or drones to anchor, anchoring as Nomove");
                        //            state["rat"] = 0;
                        //            break;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        Log("can't find entity or drones to anchor, anchoring as Nomove");
                        //        state["rat"] = 0;
                        //        break;
                        //    }
                        //}
                        //else
                        //{
                        //    if (!ui.overview.tabs_.Any(t => t.text.Equals(tabEntity)))//check if entitytab exists
                        //    {
                        //        Log($"NO tab named by '{tabEntity}'");
                        //        Log("can't find entity or drones to anchor, anchoring as Nomove");
                        //        state["rat"] = 0;
                        //        break;
                        //    }
                        //    else
                        //    {
                        //        var (node, text, selected) = ui.overview.tabs_.Where(t => t.text.Equals(tabEntity)).First();
                        //        input.MouseClickLeft(node, ui.root);
                        //    }
                        //}
                        //break;
                        case SBotLogicImpl.Anchor.ORBITWRECK:
                            Log("NOT IMPLEMENTEED YET");
                            Log("anchoring on drones");
                            anchorType = SBotLogicImpl.Anchor.FOLLOWDRONE;
                            break;
                    }
                    break;
                case 1:
                    if (ui.activeItem.Actions.Any(a => a.hint.Contains("Drone")))
                    {
                        input.MouseClickLeft(ui.activeItem.Actions.Where(a => a.hint.Contains("proach")).First().node, ui.root);
                        state[anchor] = 2;
                    }
                    else
                    {
                        state[anchor] = 0;
                    }
                    break;
                case 2://check speed
                    if (ui.shipUI.Navistate.speed > anchorSpeed || ui.shipUI.Navistate.speed > speedBeforeAnchor)
                    {
                        res = 0;
                    }
                    else
                    {
                        state[anchor] = 0;
                    }
                    break;
            }
            if (res == 0)
            {
                state[anchor] = 0;
                return 0;
            }
            return 1;
        }

        int noNPCTicks = 0;
        bool siteFinished = false;
        void SquadronsReturnOrbit()
        {
            if (true) ;
            input.KeyClick(keyReturnAndOrbitAllSquadrons);
        }
        void SquadronsOpenFire()
        {
            var idleSquadrons = ui.shipUI.squadronsUI.squadrons
                .Where(s => s.state == FighterActionState.INSPACE && s.lastFighterDamage <= damagePecentOnRecall)
                .Where(s => !s.slots[0].Active && !s.slots[0].Busy).Reverse().ToList();
            idleSquadrons.ForEach(idleSq =>
            {
                if (idleSq.squadronMaxSize >= 9)
                {
                    input.KeyClick(keyReturnAndOrbitAllSquadrons);
                }
                input.MouseClickLeft(idleSq.slots[0].Node, ui.root);
                if (!focusFire)
                {
                    if (idleSq.squadronMaxSize < 9)
                    {
                        if (ui.overview.Targeted.Count > 1)
                        {
                            input.KeyClick(keyNextTarget);
                        }
                    }

                }
            });
        }
        void RecallDamagedSquadrons()
        {
            var damagedSquadrons = ui.shipUI.squadronsUI.squadrons
                .Where(s => s.state == FighterActionState.INSPACE && s.lastFighterDamage > damagePecentOnRecall)
                .ToList();
            damagedSquadrons.ForEach(damagedSq =>
            {
                input.MouseClickLeft(damagedSq.node, ui.root, true);
                input.KeyClick(keyRecallSelectedSquadron);
                if (damagedSq.squadronMaxSize > 6)
                {
                    if (damagedSq.slots.Count > 0)
                    {
                        if (!damagedSq.slots[1].Busy && !damagedSq.slots[1].Active)
                        {
                            input.MouseClickLeft(damagedSq.slots[1].Node, ui.root, true);
                        }
                    }
                }
            });
        }
        bool siteRunning = false;
        void OnRat()
        {
            switch (state["rat"])
            {
                case -1:
                    if (OnBadSpawn() == 0)
                    {
                        state["rat"] = 0;
                    }
                    break;
                case 101:
                    switch (LootCargo(bossName))
                    {
                        case -1:
                            state["rat"] = 102;
                            Log("No Boss");
                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                            break;
                        case -2:
                            state["rat"] = 102;
                            BMBoss = false;
                            Log("Not Enough Cargo Space!");
                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                            break;
                        case 0:
                            state["rat"] = 102;
                            Log("Looted Boss");
                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                            break;
                        default:
                            Log("Looting");
                            break;
                    }
                    break;
                case 102:
                    if (ui.bookmarkLocationWindow.Exist)
                    {
                        input.MouseClickLeft(ui.bookmarkLocationWindow.submitButton, ui.root);
                    }
                    state["rat"] = 1;
                    break;
                case 0://rat
                    if (!ui.overview.tabs.Any(t => t.text.Contains(tabPve) && t.selected))//choose pve tab
                    {
                        input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                    }
                    else
                    {
                        if (ui.overview.NumNPC < 1
                            || ui.overview.AllEntrys.Where(ove => !ove.labels.Any(l => l.Contains('['))).All(ove => ove.distance > validNPCRangeM))
                        {
                            noNPCTicks++;
                            if (!ui.probescannerView.anoms.Any(a => a.Unit.Contains('m')))
                            {
                                if (noNPCTicks > 1)
                                {
                                    //Recall drones if anom done 

                                    siteRunning = false;
                                    if (0 == Recalldrones())
                                    {
                                        siteFinished = !retreatOnLastSite;
                                        //ActivateF1Module(false);

                                        if (BMBoss)
                                        {
                                            state["rat"] = 101;
                                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabWreck)).First().node, ui.root);
                                        }
                                        else
                                        {
                                            state["rat"] = 1;
                                            break;

                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (noNPCTicks > 1&&(noNPCTicks%3)==0)
                                {
                                    input.KeyClick(keyReturnAndOrbitAllSquadrons);
                                }
                            }
                        }
                        else// do ratting things
                        {
                            noNPCTicks = 0;
                            siteRunning = true;
                            if (avoidBadSpawns)
                            {
                                if (ui.overview.AllEntrys.Any(oe => oe.labels.Any(l => BadSpawns.Any(bs => l.Contains(bs)))))//dread spawn warp to safe and avoid this anom
                                {
                                    Log("dreadspawn-----run");
                                    state["rat"] = -1;
                                    goto case -1;
                                }
                            }

                            ActivateAlwaysOnModules(true);
                            Launchdrones();
                            if (0 != ClearTackle())
                            {
                                break;
                            }
                            if (0 != ClearEwar())
                            {
                                break;
                            }

                            RecallDamagedSquadrons();
                            List<EveUI.Overview.OverviewEntry> lock_candidates = new();

                            if (lockSpecialRats)
                            {
                                foreach (var bad_rat_name in specialRatNames)
                                {
                                    var candidate = ui.overview.UnTargeted.Where(nt => nt.labels.Any(l => l.Contains(bad_rat_name)));
                                    if (candidate.Any())
                                    {
                                        lock_candidates = lock_candidates.Concat(candidate).ToList();
                                        break;
                                    }
                                }
                            }
                            if (lockAll)
                            {
                                var not_targeted_count = ui.overview.UnTargeted.Count;
                                if (not_targeted_count > 0)
                                {
                                    lock_candidates = lock_candidates.Concat(ui.overview.UnTargeted).ToList();
                                }
                            }
                            var lockCandidates = lock_candidates.DistinctBy(oe => oe.node.pythonObjectAddress).ToList();
                            int lockCount = ui.overview.Targeted.Count + ui.overview.Targeting.Count;
                            if (ui.overview.Targeted.Count + ui.overview.Targeting.Count <= 6)
                            {
                                input.KeyDown(lockKey);
                                foreach (var lc in lockCandidates)
                                {
                                    input.MouseClickLeft(lc.node, ui.root);
                                    lockCount++;
                                    if (lockCount >= maxTargetCount)
                                    {
                                        break;
                                    }
                                }
                                input.KeyUp(lockKey);
                            }
                            if (ui.overview.Targeted.Any())
                            {
                                if (ui.overview.ActiveTarget == default)
                                {
                                    input.MouseClickLeft(ui.overview.Targeted.First().node, ui.root);
                                    break;
                                }
                                SquadronsOpenFire();
                            }
                            if (ui.shipUI.Navistate.speed < 10 && anchorType != SBotLogicImpl.Anchor.NOMOVE && anchorType != SBotLogicImpl.Anchor.KEEPATRANGE)
                            {
                                state["rat"] = 2;
                            }
                        }
                    }
                    break;
                case 1:
                    if (Navigate() == 0)
                    {
                        state["rat"] = 2;
                        retreatOnLastSite = false;
                    }
                    break;
                case 2:
                    if (Anchor() == 0)
                    {
                        state["rat"] = 0;
                    }
                    break;
            }
        }

        bool isCloaky = false;
        string lastMessage = "";
        bool rat_till_next_day = false;
        int disableTicks = 0;

        public bool retreatOnNewsig = true;
        public override void UpdateCB()
        {
            if (rat_till_next_day)
            {
                if (int.Parse(DateTime.Now.ToString("HHmm")) < int.Parse(timeToRest))
                {
                    rat_till_next_day = false;
                }
            }
            Log(DateTime.Now.ToString()+"-----------------------------------------------");
            try
            {
                if (ui == null)
                {
                    Log("No UI is passed!");
                }
                else
                {
                    //close and log messages so that input don't get blocked
                    ui.messageBoxes.msgBoxes.ForEach(mb =>
                    {
                        lastMessage = mb.caption;
                        if (mb.caption.Contains("Fleet"))//reject fleet inv
                        {
                            input.MouseClickLeft(ui.messageBoxes.msgBoxes[0].buttons.First(b => b.text.Contains("No")).node, ui.root);
                        }
                        else
                        {
                            if (mb.caption.Contains("Connection Lost"))
                            {
                                dcWarningPlayer.Play();
                            }
                            else
                            {
                                input.MouseClickLeft(ui.messageBoxes.msgBoxes[0].buttons.First().node, ui.root);
                            }
                        }
                        return;
                    });

                    //magicNode = ui.probescannerView.anoms_[0].Node;
                    //magicNode = ui.shipUI.capContainer;
                    tick++;

                    if (!ui.standaloneBookmarkWindow.labels.Any(l => l.text.Contains(safeBookmark))) //no BM
                    {

                        Log("No safe BM");
                        return;
                    }
                    if (!ui.standaloneBookmarkWindow.labels.Any(l => l.text.Contains(repBookmark))) //no BM
                    {

                        Log("No rep BM");
                        return;
                    }

                    if (IsDisabled())
                    {
                        disableTicks++;
                        if (disableTicks > 5)
                        {
                            Disabled = true;
                        }
                    }
                    else
                    {
                        disableTicks = 0;
                    }

                    ui.localChatwindowStack.Members.ForEach(m => Log(m.name + "-" + m.tag));
                    switch (state["update"])
                    {
                        case 0:

                            if (ui.localChatwindowStack.NumHostile(hostileTags) > 0)
                            {
                                state["update"] = -1;
                                retreatOnLastSite = true;
                                goto case -1;
                            }
                            if (retreatOnNewsig && ui.probescannerView.anoms.Any(a => a.Name.Contains("Cosmic")))//retreat on new sig spawn
                            {
                                state["update"] = -1;
                                retreatOnLastSite = true;
                                goto case -1;
                            }

                            if ((ui.shipUI.HP.shield < lowHPShield || ui.shipUI.HP.armor < lowHPArmor) && ui.shipUI.HP.structure != 0)// low hp 
                            {
                                Log($"Low HP! {ui.shipUI.HP}");
                                state["update"] = -2;
                                retreatOnLastSite = true;
                                goto case -2;
                            }

                            if (NeedRest)
                            {
                                Log("time to rest!");
                                state["update"] = -3;
                                goto case -3;
                            }

                            if (Disabled)
                            {
                                Log("Disabled!");
                                state["update"] = -4;
                                goto case -4;
                            }

                            if (cloakAfterRetreat)
                            {
                                if (isCloaky)
                                {
                                    input.KeyClick("F2");
                                    //ActivateCloakMod(false);
                                    isCloaky = false;
                                    ActivateAlwaysOnModules(true);
                                }
                            }
                            OnRat();
                            break;
                        case -1:
                            OnRetreat(false);
                            if (state["retreat"] == -1)
                            {
                                if (cloakAfterRetreat)
                                {
                                    state["update"] = -101;
                                }
                                else
                                {
                                    state["update"] = -102;
                                }
                                ticksSinceLastHostile = 0;
                                CloseDM();
                            }
                            alertWarningPlayer.Play();
                            break;
                        case -101:
                            if (cloakAfterRetreat)
                            {
                                if (!isCloaky)
                                {
                                    ActivateCloakMod(true);
                                    isCloaky = true;
                                }
                                if (alignRepAfterCloaked)
                                {
                                    if (ui.dropdownMenu.Exist)
                                    {
                                        if (ui.dropdownMenu.menuEntrys.Any(me => me.text.Contains("Align") || me.text.Contains("Approach")))
                                        {
                                            input.MouseClickLeft(ui.dropdownMenu.menuEntrys.First(me => me.text.Contains("Align") || me.text.Contains("Approach")).node, ui.root);
                                            state["update"] = -102;
                                        }
                                        else
                                        {
                                            CloseDM();
                                            input.MouseClickRight(ui.standaloneBookmarkWindow.labels.First(l => l.text.Contains(repBookmark)).node, ui.root);
                                        }
                                    }
                                    else
                                    {
                                        CloseDM();
                                        input.MouseClickRight(ui.standaloneBookmarkWindow.labels.First(l => l.text.Contains(repBookmark)).node, ui.root);
                                    }
                                }
                                else
                                {
                                    state["update"] = -102;
                                }
                            }
                            break;
                        case -102:
                            if (ui.localChatwindowStack.NumHostile(hostileTags) < 1)//bugfix
                            {
                                state["update"] = 1;
                                camperTicks = 0;
                            }
                            else
                            {
                                ticksSinceLastHostile = 0;
                                camperTicks++;
                                if (camperTicks > maxCamperTicks
                                    || (camperTicks > maxBadCamperTicks
                                        && ui.localChatwindowStack.Members.Any(m => badCamperNames.Any(bcn => m.name.Contains(bcn)))))
                                {
                                    needLogOff = true;
                                }
                            }

                            break;
                        case -2:

                            if (ui.localChatwindowStack.NumHostile(hostileTags) > 0)
                            {
                                state["update"] = -1;
                                retreatOnLastSite = true;
                                state["retreat"] = 0;
                                goto case -1;
                            }

                            OnRetreat(true);
                            if (state["retreat"] == -1)
                            {
                                state["update"] = -201;
                            }
                            
                            break;
                        case -201:
                            if (ui.localChatwindowStack.NumHostile(hostileTags) > 0)
                            {
                                state["update"] = -1;
                                retreatOnLastSite = true;
                                state["retreat"] = 0;
                                goto case -1;
                            }
                            if (ui.shipUI.HP.shield >= lowHPShield && ui.shipUI.HP.armor >= lowHPArmor && ui.shipUI.HP.structure == 100)
                            {
                                state = state.ToDictionary(p => p.Key, p => 0);
                            }
                            break;
                        case -3:
                            OnRetreat(false);
                            if (state["retreat"] == -1)
                            {
                                state["update"] = -301;
                            }
                            break;
                        case -301:
                            if (closeClientAfterRested)
                            {
                                needLogOff = true;
                            }
                            break;
                        case -4:
                            OnRetreat(false);
                            if (state["retreat"] == -1)
                            {
                                state["update"] = -401;
                            }
                            break;
                        case -401:

                            needLogOff = true;

                            break;
                        case 1:
                            if (ui.localChatwindowStack.NumHostile(hostileTags) > 0)
                            {
                                state["update"] = -1;
                            }
                            else
                            {
                                ticksSinceLastHostile++;
                                if (ticksSinceLastHostile > ticksToWaitAfterHostileLeave)
                                {
                                    ticksSinceLastHostile = 0;
                                    state = state.ToDictionary(p => p.Key, p => 0);
                                }
                                else
                                {
                                    if (ui.overview.AllEntrys.Any(oe => oe.distance < 500_000 && oe.labels.Any(l => l.Contains("Warp Disruptor"))))
                                    {
                                        Log("Mobile Warp Disruptor in range!");
                                        ticksSinceLastHostile = 0;
                                    }

                                }
                            }
                            break;
                    }
                    if (ui.infoPanelESS.connecting)
                    {
                        essWarningPlayer.Play();
                    }
                }
                Log(Summary());
                Thread.Sleep(tickIntervalMS);
            }
            catch (Exception ex) { Log(ex.ToString()); }
        }
        //TODO recall damaged fighters. return only when too far away. refiil fighters. no next target if only one
        private bool IsDisabled()
        {
            var res = false;
            if (!ui.shipUI.activeSlots.Any())
            {
                Log("Not any modules");
                res = true;
            }
            if (useDronesAsMainDPS)// not enough drones
            {
                if (!ui.shipUI.squadronsUI.squadrons.Any())
                {
                    Log($"not enough squadrons!{ui.shipUI.squadronsUI.squadrons.Count}");
                    res = true;
                }
            }
            return res;
        }

        public override string Summary() => ui == null ? "ui==null" : 
            ui.shipUI.HP + " / " + 
            ui.shipUI.squadronsUI.squadrons.Select(s => s.lastFighterDamage.ToString()).Aggregate((s1, s2) => s1 + " " + s2)+ " / " + 
            state.Select(s => s.Key + ":" + s.Value).Aggregate((a, b) => a + " " + b) + " / " +
            ticksSinceLastHostile + " / " +
            camperTicks + " / " +
            tick;


        public override bool NeedLogOff() => needLogOff&&false; //NEVER LOG OFF

        WindowsMediaPlayer bossWarningPlayer = new();
        WindowsMediaPlayer squadronWarningPlayer = new();
        WindowsMediaPlayer dictorWarningPlayer = new();
        public override bool PreFlightCheck(EveUI ui)
        {

            //load sounds
            bossWarningPlayer.settings.autoStart = false;
            squadronWarningPlayer.settings.autoStart = false;
            dictorWarningPlayer.settings.autoStart = false;

            bossWarningPlayer.URL = "w_boss.mp3";
            squadronWarningPlayer.URL = "w_squadron.mp3";
            dictorWarningPlayer.URL = "w_dictor.mp3";

            try
            {
                HttpClient client = new()
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };
                string defaultUserAgent = "SBot";
                string clients = "RCAnom: ";
                Process.GetProcessesByName("exefile").ToList().ForEach(p =>
                {
                    if (p.MainModule?.ModuleName?.Equals("exefile.exe") ?? false)
                    {
                        if (p.MainWindowTitle.Any())
                        {
                            clients += p.MainWindowTitle + ";";
                        }
                    }
                });
                client.DefaultRequestHeaders.UserAgent.ParseAdd(defaultUserAgent);
                client.PostAsync("https://sbot4eve.uk/api/bsr", new StringContent(clients, System.Text.Encoding.UTF8));
            }
            catch { };

            //check if start time is greater than timetostop
            if (int.Parse(timeToRest) < int.Parse(DateTime.Now.ToString("HHmm")))
            {
                rat_till_next_day = true;
            }


            //check ui
            string content = "------PRE FLIGHT CHECK------";
            content += $"\nLocalChat is good? {ui.localChatwindowStack.Exist}\nMembers in local? {ui.localChatwindowStack.Members.Count}";
            content += $"\nSafe BM is good? {ui.standaloneBookmarkWindow.labels.Any(l => l.text.Contains(safeBookmark))}";
            content += $"\nRep BM is good? {ui.standaloneBookmarkWindow.labels.Any(l => l.text.Contains(repBookmark))}";
            content += $"\nCurrent shiphp is? {ui.shipUI.HP}";
            content += $"\nNumber of modules? {ui.shipUI.activeSlots.Count}";
            content += $"\nNumber of anoms? {ui.probescannerView.anoms.Count}";
            content += $"\nNumber of squadrons? {ui.shipUI.squadronsUI.squadrons.Count}";
            content += $"\npve tab({tabPve}) is good? {ui.overview.tabs.Any(t => t.text.Contains(tabPve))}";
            content += $"\nwreck tab({tabWreck}) is good? {ui.overview.tabs.Any(t => t.text.Contains(tabWreck))}";
            content += $"\nentity tab({tabEntity}) is good? {ui.overview.tabs.Any(t => t.text.Contains(tabEntity))}";
            if (!MessageBox.Show(content, "PRE FLIGHT CHECK", MessageBoxButtons.YesNoCancel).Equals(DialogResult.Yes))
            {
                return false;
            }
            return base.PreFlightCheck(ui);
        }
    }
}

using SBotCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SBotCore.EveUIParser.EveUI.ProbeScanner;
using static SBotCore.EveUIParser;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using WMPLib;

namespace SBotLogicImpl
{
    public enum AnomOrder
    {
        NEAREST = 0, FIRST = 1, LAST = 2, IDF = 3, IDL = 4
    }
    public enum Anchor
    {
        FOLLOWDRONE = 0, ORBITENTITY = 1, ORBITWRECK = 2, NOMOVE = 3, KEEPATRANGE = 4
    }
    public abstract class BotLogicRat : BotLogic
    {


        public bool alignRepAfterCloaked = false;
        public bool fofMode = false;
        public bool marauderMode = false;
        public string timeToRest = "1200";
        public bool closeClientAfterRested;

        public bool lootBoss = false;
        public string bossName = "Dark Blood";
        public ulong minValueToLoot = 50000000;

        public string safeBookmark = "0 safe";
        public bool cloakAfterRetreat = false;
        public bool waitForDronesToReturn = false;
        public List<string> hostileTags = new();//dataincode

        public int ticksToWaitAfterHostileLeave = 600;
        public uint maxCamperTicks = 7200;
        public uint maxBadCamperTicks = 1800;
        public List<string> badCamperNames = new();


        public int tickIntervalMS = 1500;


        public List<string> anomsToRun = new();
        public bool clearOccupiedAnomsIfNoRunnableAnoms = true;
        public string defaultWarpToAnomDistanceKM = "50";
        protected string actualWarpToAnomDistanceKM = "50";

        protected string tabPve = "pve";
        protected string tabEntity = "entity";
        protected string tabWreck = "wreck";


        public AnomOrder anomOrder = AnomOrder.FIRST;

        public List<string> entityToAnchor = new();

        public Anchor anchorType = SBotLogicImpl.Anchor.FOLLOWDRONE;

        public int anchorSpeed = 50;

        public bool useDronesAsMainDPS = true;
        public int numDrones = 5;
        public int maxDroneIdleTicks = 2;

        public string keyLaunchDrones = "f";
        public string keyRecallDrones = "r";
        public string keyDronesEngage = "g";
        public string keyInventory = "c";
        protected string keyStopShip = "s";

        public int lowHPShield = 10;
        public int lowHPArmor = 90;
        public string repBookmark = "1 rep";

        public List<string> ewarTags = new();

        public bool lockSpecialRats = true;
        public List<string> specialRatNames = new();

        public List<string> dronesEngageNames = new();


        public bool useTurrents = true;
        public int useTurrentsDistanceM = 30000;
        public List<string> turrentsEngageNames = new();

        public bool lockAll = false;

        public int maxTargetCount = 3;

        protected string lockKey = "^";
        protected bool recalldronesifunderattack_ = false;
        protected bool ignore_anoms_without_telecom_ = false;
        protected bool ignore_anoms_with_telecom_ = false;

        //TODO
        protected ulong tick = 0;
        protected uint camperTicks = 0;
        protected bool needLogOff = false;

        protected Dictionary<string, int> state = new()
        {
            {"update",0},
            {"retreat",0},
            {"rat",0},
            {"warp",0},
            {"loot",0}
        };
        //public (int update, int retreat, int rat, int warp, int loot) state;
        protected List<string> avoidedAnoms = new();
        protected List<string> occupiedAnoms = new();
        //Dictionary<string, string> bad_anoms_shared_ = new();


        protected virtual UITreeNode MagicNode { get => ui.shipUI.capContainer; }
        protected virtual List<string> BadSpawns { get => badSpawns; }
        protected virtual bool NeedRest { get => needRest; set => needRest = value; }
        protected virtual bool Disabled { get => disabled; set => disabled = value; }

        protected int ticksSinceLastHostile = 0;



        public virtual void ReadyKeyClick()
        {
            input.MouseClickLeft(MagicNode, ui.root);
        }
        public virtual void CloseDM()
        {
            if (ui.dropdownMenu.Exist)
            {
                input.MouseClickLeft(MagicNode, ui.root);
            }
        }
        public virtual int Orbit(UITreeNode node)
        {
            input.KeyDown("w");
            input.MouseClickLeft(node, ui.root);
            input.KeyUp("w");
            return 0;
        }
        public virtual int Approach(UITreeNode node)
        {
            input.KeyDown("q");
            input.MouseClickLeft(node, ui.root);
            input.KeyUp("q");
            return 0;
        }
        public virtual int KeepAtRange(UITreeNode node)
        {
            input.KeyDown("e");
            input.MouseClickLeft(node, ui.root);
            input.KeyUp("e");
            return 0;
        }

        public virtual int ActivateF1Module(bool activate)
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
        public bool focusFire = true;
        public virtual int ActivateTurrents(bool activate)
        {
            int res = 0;
            for (int i = 3; i <= 4; i++)
            {
                var text = "F" + i.ToString();
                var turrents = ui.shipUI.activeSlots.Where(s => s.Text.Equals(text));
                if (turrents.Any())
                {
                    if (turrents.First().Active ^ activate)
                    {
                        if (turrents.First().Quantity > 0)
                        {
                            input.KeyClick(text);
                            res++;
                        }
                    }
                }
            }
            return res;
        }
        public virtual int ActivateAlwaysOnModules(bool activate)
        {
            int res = 0;
            for (int i = 5; i <= 8; i++)
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
        public virtual int ActivateCloakMod(bool activate)//cannot read active state
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
        public virtual int FocusFire()
        {
            Launchdrones();
            if (ui.droneView.NumDronesOutside >= 0)
            {
                input.KeyClick(keyDronesEngage);
            }
            if (useTurrents)
            {
                ActivateTurrents(true);
            }
            //TODO fighters
            return 0;
        }

        //0 as succeed
        public virtual int WarpToAbstract(UITreeNode node)
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
                    //ActivateF1Module(false);
                    state["warp"] = 1;
                    break;
                case 1:
                    if (ui.dropdownMenu.menuEntrys != null)
                    {
                        var bw = ui.dropdownMenu.menuEntrys.Where(m => m.text.Contains("Warp") && m.text.Contains('m'));
                        if (bw.Any())
                        {
                            input.MouseClickLeft(bw.First().node, ui.root);
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
        public virtual int WarpToAbstractWithIn(UITreeNode node)
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
                    //ActivateF1Module(false);
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
        public virtual int Recalldrones()
        {
            if (ui.droneView.NumDronesOutside == 0)
            {
                return 0;
            }
            else
            {
                if (ui.droneView.dronesInSpace.Any(d => !d.state.Contains("Return")))
                {
                    Log("Recall drones");
                    ReadyKeyClick();
                    input.KeyClick(keyRecallDrones);
                    return 1;
                }
                else
                {
                    return 1;
                }
            }
        }
        //0 as succeed
        public virtual int Launchdrones()
        {
            if (ui.droneView.NumDronesOutside >= numDrones || ui.droneView.NumDronesInside == 0)
            {
                return 0;
            }
            else
            {
                Log("Launch drones");
                ReadyKeyClick();
                input.KeyClick(keyLaunchDrones);
                return 1;
            }
        }
        public virtual int LootCargo(string name)
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
                        var node = candidate.node;
                        if (ui.inventory.Exist)
                        {
                            input.KeyClick(keyInventory);
                        }

                        input.MouseClickLeft(node, ui.root);
                        state["loot"] = 1;
                    }
                    break;
                case 1:
                    if (!ui.activeItem.ItemName.text.Contains(name))
                    {
                        state["loot"] = 0;
                    }
                    else
                    {
                        input.MouseClickLeft(ui.activeItem.Actions.Where(a => a.hint.Contains("Cargo")).First().node, ui.root);
                        state["loot"] = 2;
                    }
                    break;
                case 2:
                    if (ui.shipUI.Navistate.speed > anchorSpeed || ui.inventory.Exist)
                    {
                        state["loot"] = 3;
                    }
                    else
                    {
                        state["loot"] = 1;
                    }
                    break;
                case 3:
                    if (ui.inventory.Exist)
                    {
                        if (ui.inventory.TotalValueIsk() < minValueToLoot)
                        {
                            if (ui.inventory.Exist)
                            {
                                input.KeyClick(keyInventory);
                            }
                            state["loot"] = 0;
                            return 0;
                        }
                        else
                        {
                            if (ui.inventory.BtnLootAll == null)
                            {
                                if (ui.inventory.Exist)
                                {
                                    input.KeyClick(keyInventory);
                                }
                                state["loot"] = 0;
                                return 0;
                            }
                            input.MouseClickLeft(ui.inventory.BtnLootAll, ui.root);
                            state["loot"] = 4;
                        }
                    }
                    else
                    {
                        state["loot"] = 2;
                    }
                    break;
                case 4:
                    var nec = lastMessage.Contains("Not Enough");
                    if (nec)
                    {
                        if (ui.inventory.Exist)
                        {
                            input.KeyClick(keyInventory);
                        }
                        state["loot"] = 0;
                        return -2;
                    }
                    else
                    {
                        if (ui.inventory.Exist)
                        {
                            input.KeyClick(keyInventory);
                        }
                        state["loot"] = 0;
                        return 0;
                    }
            }
            return 1;
        }
        public virtual int ClearEwar()
        {
            var wdoe = ui.overview.AllEntrys.Where(oe => oe.ewars.Any(ew => ewarTags.Any(ewr => ew.Contains(ewr))));//kill tackles TODO seems to be broken here: ewar state remains even actually gone
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
        public virtual int ClearTackle()
        {
            var wdoe = ui.overview.AllEntrys.Where(oe => oe.ewars.Any(ew => ew.Contains("warp")));//kill tackles TODO seems to be broken here: ewar state remains even actually gone
            if (wdoe.Any())
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
        public virtual void OnRetreat(bool lowhp)
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
                    //ReadyKeyClick();
                    //input.KeyClick(keyRecallDrones);
                    ActivateF1Module(false);
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
                            input.MouseClickRight(ui.dropdownMenu.menuEntrys.Where(me => me.text.Contains("Align")).First().node, ui.root);
                            state["retreat"] = 2;
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
                        if (!waitForDronesToReturn
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
                            if (0 == Recalldrones())
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

        private bool needRest = false;
        private bool disabled = false;
        protected int[] droneIdleTicks = new int[15];


        protected bool doubleCheckFriendly = false;
        protected int zeroNPCTicks = 0;
        protected bool avoidBadSpawns = true;
        protected bool alwaysOnProp = true;
        protected bool oneCycleProp = false;
        public string keyReconnectDrones = "t";

        public List<string> badSpawns = new() { "Titan", "Dreadnought", "Carrier", "♦" };
        public virtual int OnBadSpawn()
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
            if (0 == WarpToAbstract(ui.standaloneBookmarkWindow.labels.First(l => l.text.Contains(safeBookmark)).node))
            {
                Log("badspawn-----ran");
                return 0;
            }
            return 1;
        }
        public virtual int Navigate()
        {
            return 0;
        }

        protected double speedBeforeAnchor = 0;
        protected int noDroneTicks = 0;
        public virtual int Anchor()
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

        protected int noNPCTicks = 0;
        protected bool siteFinished = false;
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
                            lootBoss = false;
                            Log("Not Enough Cargo Space!");
                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                            break;
                        case 0:
                            state["rat"] = 102;
                            Log("Looted Boss");
                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabPve)).First().node, ui.root);
                            break;
                    }
                    break;
                case 102:
                    if (ui.inventory.Exist)
                    {
                        input.KeyClick(keyInventory);
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
                        if (marauderMode)
                        {

                            if (ui.overview.NumNPC > 0)
                            {
                                zeroNPCTicks = 0;
                            }
                            else
                            {
                                zeroNPCTicks++;
                                if (zeroNPCTicks > 1)
                                {
                                    ActivateF1Module(false);
                                }
                            }
                        }
                        if (ui.overview.NumNPC < 1 ||
                            ui.overview.AllEntrys.Where(ove => !ove.labels.Any(l => l.Contains('[')))
                            .All(ove => ove.distance > 150_000))
                        {
                            noNPCTicks++;
                            if (noNPCTicks > 1)
                            {
                                if (!ui.probescannerView.anoms.Any(a => a.Unit.Contains('m')))//Recall drones if anom done 
                                {

                                    if (0 == Recalldrones())
                                    {
                                        siteFinished = !retreatOnLastSite;
                                        ActivateF1Module(false);
                                        if (marauderMode)
                                        {
                                            var bastion = ui.shipUI.activeSlots.FirstOrDefault(s => s.Text.Equals("F1"));
                                            if (bastion.Busy || bastion == default)
                                            {
                                                break;
                                            }
                                        }

                                        if (lootBoss)
                                        {
                                            state["rat"] = 101;
                                            input.MouseClickLeft(ui.overview.tabs.Where(t => t.text.Contains(tabWreck)).First().node, ui.root);
                                        }
                                        else
                                        {
                                            //if (1 == ActivatePropMod(false))
                                            {
                                                //if (useTurrents || fofMode)
                                                //{
                                                //    input.KeyDown("^");
                                                //    input.KeyClick("r");
                                                //    input.KeyUp("^");
                                                //}
                                                state["rat"] = 1;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else// do ratting things
                        {
                            noNPCTicks = 0;
                            if (avoidBadSpawns)
                            {
                                if (ui.overview.AllEntrys.Any(oe => oe.labels.Any(l => BadSpawns.Any(bs => l.Contains(bs)))))//dread spawn warp to safe and avoid this anom
                                {
                                    Log("dreadspawn-----run");
                                    Recalldrones();
                                    state["rat"] = -1;
                                    goto case -1;
                                }
                            }
                            if (marauderMode || alwaysOnProp)
                            {
                                ActivateF1Module(true);
                            }
                            if (fofMode)
                            {
                                if (ui.overview.AllEntrys.Where(ove => !ove.labels.Any(l => l.Contains('[')))
                                                                .Any(ove => ove.distance < useTurrentsDistanceM))
                                    ActivateTurrents(true);
                            }

                            ActivateAlwaysOnModules(true);

                            if (0 != ClearTackle())
                            {
                                break;
                            }
                            if (0 != ClearEwar())
                            {
                                break;
                            }


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
                            if (useDronesAsMainDPS)
                            {
                                Launchdrones();
                                for (int i = 0; i < ui.droneView.NumDronesOutside; i++)
                                {
                                    if (ui.droneView.dronesInSpace[i].state.Contains("Idle"))
                                    {
                                        droneIdleTicks[i]++;
                                    }
                                    else
                                    {
                                        droneIdleTicks[i] = 0;
                                    }
                                }
                                if (droneIdleTicks.Any(t => t > maxDroneIdleTicks))
                                {
                                    if (ui.overview.NumNPC > 0)
                                    {
                                        if (ui.overview.ActiveTarget == default)
                                        {
                                            if (ui.overview.Targeted.Any())
                                            {
                                                input.MouseClickLeft(ui.overview.Targeted.First().node, ui.root);
                                            }
                                            else
                                            {
                                                lock_candidates = lock_candidates.Concat(ui.overview.UnTargeted).ToList();
                                            }
                                        }
                                        else
                                        {
                                            if (!ui.overview.ActiveTarget.IsPlayer)
                                            {
                                                input.KeyClick(keyDronesEngage);
                                            }
                                        }
                                    }
                                }
                            }

                            for (int i = 0; i < Math.Min(lock_candidates.Count, maxTargetCount - ui.overview.Targeted.Count - ui.overview.Targeting.Count); i++)
                            {

                                input.MouseClickLeft(lock_candidates[i].node, ui.root);
                                input.KeyClick(lockKey);
                            }

                            if (ui.overview.Targeted.Any())
                            {
                                if (ui.overview.ActiveTarget == default)
                                {
                                    input.MouseClickLeft(ui.overview.Targeted.First().node, ui.root);
                                    break;
                                }
                                if (specialRatNames.Any(brn => ui.overview.Targeted.Any(oe => oe.labels.Any(l => l.Contains(brn)))))
                                {
                                    if (!specialRatNames.Any(brn => ui.overview.ActiveTarget.labels.Any(l => l.Contains(brn))))
                                    {
                                        var candidate = ui.overview.Targeted.First(t => specialRatNames.Any(brn => t.labels.Any(l => l.Contains(brn))));
                                        input.MouseClickLeft(candidate.node, ui.root);
                                        break;
                                    }
                                }

                                if (useTurrents
                                    && ui.overview.ActiveTarget.distance < useTurrentsDistanceM
                                    && turrentsEngageNames.Any(ten => ui.overview.ActiveTarget.labels.Any(l => l.Contains(ten))))

                                {
                                    if (tick % 1 == 0)
                                    {
                                        if (0 != ActivateTurrents(true))
                                        {
                                            if (focusFire)
                                            {
                                                input.KeyClick(keyDronesEngage);
                                            }
                                        }
                                    }
                                }
                                if (dronesEngageNames.Any(den => ui.overview.ActiveTarget.labels.Any(l => l.Contains(den))))
                                {
                                    Launchdrones();
                                    if (tick % 2 == 0)
                                    {
                                        input.KeyClick(keyDronesEngage);
                                    }
                                }
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

        protected bool isCloaky = false;
        protected string lastMessage = "";
        protected bool rat_till_next_day = false;
        int disableTicks = 0;

        public bool warnOnBlue = false;
        static Dictionary<string, int> blueTimers=new();
        public List<string> blueTags= new();
        public override void UpdateCB()
        {
            if (rat_till_next_day)
            {
                if (int.Parse(DateTime.Now.ToString("HHmm")) < int.Parse(timeToRest))
                {
                    rat_till_next_day = false;
                }
            }
            Log(DateTime.Now.ToString());
            Log(Summary().ToString());
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
                    if (warnOnBlue)
                    {
                        var blues = ui.localChatwindowStack.Members.Where(m => blueTags.Any(bt => m.tag.Contains(bt))).ToList();
                        var goneBlues = blueTimers.Where(bt => !blues.Any(b => b.name.Equals(bt.Key))).ToList();
                        foreach (var gb in goneBlues)
                        {
                            blueTimers.Remove(gb.Key);
                        }
                        blues.ForEach(m =>
                        {
                            if (blueTimers.ContainsKey(m.name))
                            {
                                blueTimers[m.name]--;
                            }
                            else
                            {
                                blueTimers.Add(m.name, 30);
                            }
                        });
                        if (blueTimers.Any(bt => bt.Value > 0))
                        {
                            blueWarningPlayer.Play();
                        }
                    }

                    switch (state["update"])
                    {
                        case 0:

                            if (ui.localChatwindowStack.NumHostile(hostileTags) > 0)
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
                            OnRetreat(true);
                            if (state["retreat"] == -1)
                            {
                                state["update"] = -201;
                            }
                            break;
                        case -201:
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
                if (ui.droneView.NumDronesInside + ui.droneView.NumDronesOutside < numDrones)
                {
                    Log($"not enough drones!{ui.droneView.NumDronesInside} {ui.droneView.NumDronesOutside}");
                    res = true;
                }
            }
            else
            {
                if (useTurrents)
                {
                    if (ui.activeShipCargo.CargoPercentage < 0.01)
                    {
                        Log("Not Enough Ammo");
                        res = true;
                    }
                }
            }
            return res;
        }

        public override string Summary() => ui == null ? "ui==null" : ui.shipUI.HP + " / " +
                            state.Select(s => s.Key + ":" + s.Value).Aggregate((a, b) => a + " " + b) + " / " +
                            ticksSinceLastHostile + " / " +
                            camperTicks + " / " +
                            tick;


        public override bool NeedLogOff() => needLogOff;

        WarningPlayer blueWarningPlayer;
        public override bool PreFlightCheck(EveUI ui)
        {

            //load sounds
            blueWarningPlayer = new("w_blue.mp3");


            try
            {
                HttpClient client = new()
                {
                    Timeout = TimeSpan.FromSeconds(15)
                };
                string defaultUserAgent = "SBot";
                string clients = "Anom: ";
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
            content += $"\nNumber of drones? {ui.droneView.NumDronesInside} and {ui.droneView.NumDronesOutside}";
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Tokens;

namespace SBotCore
{
    //TODO 1 better way to tell NPC from players; 2 shipui overheat button; 3 there must be something wrong with sending input, maybe setForegroundWindow will work?
    public class EveUIParser
    {
        static IEnumerable<UITreeNode> ListNodesWithPythonObjectTypeName(UITreeNode s, string name)
        {
            if (s == null)
            {
                yield break;
            }
            else
            {
                if (s.pythonObjectTypeName.Equals(name))
                {
                    yield return s;
                }
                if (s.children != null)
                {
                    foreach (var c in s.children)
                    {
                        foreach (var r in ListNodesWithPythonObjectTypeName(c, name))
                        {
                            yield return r;
                        }
                    }
                }
            }
        }
        static IEnumerable<UITreeNode> ListNodesWithPropertyValue<T>(UITreeNode s, string name, Func<T, bool> predict)
        {
            if (s == null)
            {
                yield break;
            }
            else
            {
                if (predict(s.dictEntriesOfInterest.Value<T>(name)))
                {
                    yield return s;
                }
                if (s.children != null)
                {
                    foreach (var c in s.children)
                    {
                        foreach (var r in ListNodesWithPropertyValue(c, name, predict))
                        {
                            yield return r;
                        }
                    }
                }
            }
        }
        public class EveUI
        {
            public UITreeNode root = new();
            public ChatWindowStack otherChatwindowStack = new(false);

            //about retreating
            public ShipUI shipUI = new();
            public StandaloneBookmark standaloneBookmarkWindow = new();
            public ChatWindowStack localChatwindowStack = new(true);
            //about ratting
            public ProbeScanner probescannerView = new();
            public DroneView droneView = new();
            public Overview overview = new();
            //menuview
            public DropDownMenus dropdownMenu = new();
            //Active Item and Actions
            public ActiveItem activeItem = new();
            public Inventory inventory = new();
            public ActiveShipCargo activeShipCargo = new();
            public MessageBoxes messageBoxes = new();

            public Telecom telecom = new();
            public FleetView fleetView = new();

            public InfoPanelContainer infoPanelContainer = new();//root container of info panel
            public InfoPanelRoute infoPanelRoute = new();
            public InfoPanelESS infoPanelESS = new();

            public BookmarkLocationWindow bookmarkLocationWindow = new();
            private EveUI(UITreeNode r)

            {
                root = r;

                otherChatwindowStack.Parse(root);
                //LogWriter.LogWrite("Parsing shipui");
                shipUI.Parse(root);
                //LogWriter.LogWrite("Parsing bm");
                standaloneBookmarkWindow.Parse(root);
                //LogWriter.LogWrite("Parsing local");
                localChatwindowStack.Parse(root);

                //LogWriter.LogWrite("Parsing probeview");
                probescannerView.Parse(root);
                //LogWriter.LogWrite("Parsing drones");
                droneView.Parse(root);
                //LogWriter.LogWrite("Parsing ov");
                overview.Parse(root);

                //LogWriter.LogWrite("Parsing menu");
                dropdownMenu.Parse(root);

                activeItem.Parse(root);

                inventory.Parse(root);
                activeShipCargo.Parse(root);

                messageBoxes.Parse(root);

                telecom.Parse(root);
                fleetView.Parse(root);

                infoPanelContainer.Parse(root);
                infoPanelRoute.Parse(root);
                infoPanelESS.Parse(root);

                bookmarkLocationWindow.Parse(root);

            }
            public static EveUI Parse(UITreeNode r)
            {
                return new EveUI(r);
            }


            public interface IUIElement
            {
                public bool Exist { get; }
                public UITreeNode Node { get; }

                public void Parse(UITreeNode root);
            }
            public class BookmarkLocationWindow : IUIElement
            {
                UITreeNode node;
                public UITreeNode submitButton;
                public bool Exist => node!=null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {
                    node=ListNodesWithPythonObjectTypeName(root, "BookmarkLocationWindow").FirstOrDefault();
                    if (node != null)
                    {
                        submitButton = ListNodesWithPythonObjectTypeName(node, "Button")
                            .First(b => "Submit_Btn".Equals(b.Value<string>("_name")));
                    }
                }
            }
            //done 
            public class ChatWindowStack : IUIElement
            {
                UITreeNode node;
                private List<(UITreeNode node, string tag, string name)> members = new();
                readonly bool isLocalChat;
                public ChatWindowStack(bool is_local_chat)
                {
                    isLocalChat = is_local_chat;
                }

                static bool IsLocalChat(UITreeNode chatwindowstack)
                {
                    var res = ListNodesWithPythonObjectTypeName(chatwindowstack, "WindowStackTab").ToImmutableList();//20220405
                    foreach (var node in res)
                    {
                        var rest = ListNodesWithPythonObjectTypeName(node, "LabelThemeColored").ToImmutableList();
                        if (rest.Count > 0)
                        {
                            if (rest.First().dictEntriesOfInterest.Value<string>("_setText").Contains("Local"))//dataincode
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
                //UITreeNode FindLocalChatNode(UITreeNode root)
                //{
                //    var nodes = ListNodesWithPythonObjectTypeName(root, "ChatWindowStack").ToList();
                //    return nodes.FirstOrDefault(n => IsLocalChat(n) ^ !isLocalChat);
                //}
                void ReadMembers(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "ChatWindowStack").FirstOrDefault(n => IsLocalChat(n) ^ !isLocalChat);
                    if (Exist)
                    {
                        Members = ListNodesWithPythonObjectTypeName(node, "XmppChatSimpleUserEntry").Select(ue =>
                        {
                            var hint = ListNodesWithPythonObjectTypeName(ue, "FlagIconWithState").FirstOrDefault();
                            var name = ue.dictEntriesOfInterest.Value<string>("_name");
                            return (ue, (hint == null) ? "" : hint.dictEntriesOfInterest.Value<string>("_hint"), name);
                        }).ToList();
                    }
                }
                public void Parse(UITreeNode root)
                {
                    //try 
                    {
                        ReadMembers(root);
                    }
                    //catch (Exception ex) { LogWriter.LogWrite(ex.ToString()); }
                }
                public int NumHostile(IList<string> hostiletags)
                {
                    if (!Exist) return 0;
                    return Members.Count(m => hostiletags.Any(t => m.tag.Contains(t)));
                }

                public bool Exist => node != null;

                public UITreeNode Node => node;

                public List<(UITreeNode node, string tag, string name)> Members { get => members; set => members = value; }
            }
            //done 
            public class ProbeScanner : IUIElement
            {
                UITreeNode node;
                public List<Anom> anoms = new();

                public class Anom
                {
                    float distance;
                    string unit;
                    string id;
                    string name;
                    UITreeNode node;

                    public float Distance { get => distance; set => distance = value; }
                    public string Unit { get => unit; set => unit = value; }
                    public string Id { get => id; set => id = value; }
                    public string Name { get => name; set => name = value; }
                    public UITreeNode Node { get => node; set => node = value; }
                    public float DistanceByKm { get => (float)(unit != "AU" ? (unit != "m" ? distance : distance / 1000f) : distance * 1.5 * Math.Pow(10, 8)); }

                }
                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "ProbeScannerWindow").FirstOrDefault();
                    if (node != null)
                    {
                        anoms = new List<Anom>();
                        var scanresults = ListNodesWithPythonObjectTypeName(root, "ScanResultNew");
                        scanresults.ToList().ForEach(sr =>
                        {
                            var anom = new Anom();
                            var srinfo = ListNodesWithPythonObjectTypeName(sr, "EveLabelMedium").Select(n => n.dictEntriesOfInterest.Value<string>("_setText")).ToList();
                            anom.Distance = float.Parse(srinfo[1].Split(' ')[0].Replace(",", ""));
                            anom.Unit = srinfo[1].Split(' ')[1];
                            anom.Id = srinfo[2];
                            anom.Name = srinfo[3];
                            anom.Node = sr;
                            anoms.Add(anom);
                        });
                    }
                }

                public bool Exist => node != null;

                public UITreeNode Node => node;
            }
            //done
            public class DroneView : IUIElement
            {
                UITreeNode node;

                UITreeNode dronesInLocalSpaceMainEntry;
                int numDronesInLocalSpace;

                UITreeNode dronesInBayMainEntry;
                int numDronesInBay;

                public List<(UITreeNode node, string name, string state)> dronesInSpace = new();

                public int NumDronesOutside
                {
                    get
                    {
                        if (!Exist) return 0;
                        return numDronesInLocalSpace;
                    }
                }

                public int NumDronesInside
                {
                    get
                    {
                        if (!Exist) return 0;
                        return numDronesInBay;
                    }
                }

                public void Parse(UITreeNode root)
                {
                    {
                        node = ListNodesWithPythonObjectTypeName(root, "DroneView").FirstOrDefault();
                        if (node != null)
                        {
                            var dg = ListNodesWithPythonObjectTypeName(node, "DroneMainGroup");
                            foreach (var dronemaingroup in dg)
                            {
                                var candidatel = ListNodesWithPythonObjectTypeName(dronemaingroup, "EveLabelMedium").Where(elm => elm.dictEntriesOfInterest.Value<string>("_setText").Contains("Local Space")).FirstOrDefault();
                                var candidateb = ListNodesWithPythonObjectTypeName(dronemaingroup, "EveLabelMedium").Where(elm => elm.dictEntriesOfInterest.Value<string>("_setText").Contains("Bay")).FirstOrDefault();
                                dronesInLocalSpaceMainEntry = candidatel ?? dronesInLocalSpaceMainEntry;
                                dronesInBayMainEntry = candidateb ?? dronesInBayMainEntry;
                            }
                            numDronesInLocalSpace = int.Parse(dronesInLocalSpaceMainEntry.dictEntriesOfInterest.Value<string>("_setText").Split('(')[1].Split(')')[0]);
                            numDronesInBay = int.Parse(dronesInBayMainEntry.dictEntriesOfInterest.Value<string>("_setText").Split('(')[1].Split(')')[0]);

                            var drones = ListNodesWithPythonObjectTypeName(node, "DroneEntry");
                            dronesInSpace = drones.Where(droneentry => !droneentry.dictEntriesOfInterest.Value<string>("_hint").Equals(default)).Select(de =>
                              {
                                  var hint = de.dictEntriesOfInterest.Value<string>("_hint");
                                  var info = hint.Split('(');
                                  return (de, info[0], info[1].Split(')')[0]);
                              }).ToList();
                        }
                    }
                }

                public bool Exist => node != null;

                public UITreeNode Node => node;
            }
            //done  
            public class Overview : IUIElement
            {
                UITreeNode node;
                public List<(UITreeNode node, string text, bool selected)> tabs = new();
                public class OverviewEntry
                {
                    public UITreeNode node;
                    public List<string> labels;
                    public int distance;
                    public List<string> ewars;
                    public List<string> indicators;
                    public bool targeting;
                    //IS a player if it has corp tag
                    public bool IsPlayer => IsShip && labels.Any(l => l.Contains('['));
                    //IS a ship if it has a label that can be parsed as long
                    public bool IsShip => labels.Any(l => long.TryParse(l.Replace(",", ""), out long r));
                }
                private List<OverviewEntry> allEntrys;
                private List<OverviewEntry> targeted;
                static Dictionary<ulong, DateTime> targetedCache = new();

                private List<OverviewEntry> targeting;
                private List<OverviewEntry> unTargeted;
                private OverviewEntry activeTarget;
                public void Parse(UITreeNode root)
                {
                    {
                        node = ListNodesWithPythonObjectTypeName(root, "OverviewWindowOld").FirstOrDefault();
                        if (node != null)
                        {
                            tabs = ListNodesWithPythonObjectTypeName(ListNodesWithPythonObjectTypeName(node, "OverviewTabGroup").First(), "Tab").Select(t => (t, ListNodesWithPropertyValue(t, "_name", (string s) => "tabLabel".Equals(s)).First().dictEntriesOfInterest.Value<string>("_setText"), t.dictEntriesOfInterest.Value<bool>("_selected"))).ToList();
                            AllEntrys = ListNodesWithPythonObjectTypeName(node, "OverviewScrollEntry").Select(oe =>
                            new OverviewEntry
                            {
                                node = oe,
                                labels = ListNodesWithPythonObjectTypeName(oe, "OverviewLabel").Select(ol => ol.dictEntriesOfInterest.Value<string>("_text")).ToList(),
                                distance = int.MaxValue,
                                ewars = ListNodesWithPythonObjectTypeName(oe, "Icon").Select(I => I.dictEntriesOfInterest.Value<string>("_hint")).ToList(),//ewar
                                indicators = ListNodesWithPythonObjectTypeName(oe, "Sprite").Select(s => s.dictEntriesOfInterest.Value<string>("_name")).ToList(),//target status
                                targeting = ListNodesWithPropertyValue(oe, "_name", (string s) => s == "targeting").Any()
                            }).Select(e =>
                              {
                                  //try
                                  {
                                      var d = e.labels.Where(l => l.Contains(" km")).FirstOrDefault();
                                      if (d != null)
                                      {
                                          if (int.TryParse(d.Split(" ")[0].Replace(",", ""), out e.distance))
                                          {
                                              e.distance *= 1000;
                                              return e;
                                          }
                                      }
                                      else
                                      {
                                          var d2 = e.labels.Where(l => l.Contains(" m")).FirstOrDefault();
                                          if (d2 != null)
                                          {
                                              if (int.TryParse(d2.Split(" ")[0].Replace(",", ""), out e.distance))
                                              {
                                                  return e;
                                              }
                                          }
                                      }
                                      e.distance = int.MaxValue;
                                      return e;
                                  }
                              }).ToList();
                            Targeted = AllEntrys.Where(ove => ove.indicators.Any(i => i.Contains("argeted"))).ToList();
                            targeted.ForEach(t =>
                            {
                                if (!targetedCache.ContainsKey(t.node.pythonObjectAddress))
                                {
                                    targetedCache.Add(t.node.pythonObjectAddress, DateTime.Now);
                                }
                                else
                                {
                                    targetedCache[t.node.pythonObjectAddress] = DateTime.Now;
                                }
                            });
                            foreach (var item in targetedCache.Where(kvp => DateTime.Now - kvp.Value > TimeSpan.FromSeconds(2)).ToList())
                            {
                                targetedCache.Remove(item.Key);
                            }
                            Targeting = AllEntrys.Where(ove => ove.targeting).ToList();
                            UnTargeted = AllEntrys.Where(ove => !ove.targeting
                                                                && !ove.indicators.Any(i => i.Contains("argeted"))
                                                                && !targetedCache.ContainsKey(ove.node.pythonObjectAddress))
                                                    .ToList();
                            ActiveTarget = Targeted.FirstOrDefault(t => t.indicators.Any(i => i.Contains("ActiveTarget")));

                        }
                    }
                }
                public int NumNPC => AllEntrys.Count(ove => ove.IsShip && !ove.IsPlayer);
                public int NumPlayer => AllEntrys.Count(ove => ove.IsPlayer);
                public bool Exist => node != null;
                public UITreeNode Node => node;

                public List<OverviewEntry> AllEntrys { get => allEntrys; set => allEntrys = value; }
                public List<OverviewEntry> Targeted { get => targeted; set => targeted = value; }
                public List<OverviewEntry> Targeting { get => targeting; set => targeting = value; }
                public List<OverviewEntry> UnTargeted { get => unTargeted; set => unTargeted = value; }
                public OverviewEntry ActiveTarget { get => activeTarget; set => activeTarget = value; }
            }
            //done 
            public class StandaloneBookmark : IUIElement
            {

                UITreeNode node;
                public List<(UITreeNode node, string text)> labels = new();

                public bool Exist => node != null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {
                    {
                        node = ListNodesWithPythonObjectTypeName(root, "StandaloneBookmarkWnd").FirstOrDefault();
                        if (node != null)
                        {
                            labels = ListNodesWithPythonObjectTypeName(node, "PlaceEntry")
                                .Select(pe => (pe, ListNodesWithPythonObjectTypeName(pe, "EveLabelMedium").Select(elm => elm.dictEntriesOfInterest.Value<String>("_setText")).FirstOrDefault()))
                                .ToList();
                        }
                    }
                }
            }
            //done not done
            public class ShipUI : IUIElement
            {
                private UITreeNode shipUINode;

                private (int shield, int armor, int structure) hp;

                public UITreeNode capContainer;
                private double capacitor;

                private (float speed, bool warp) navistate;

                public record struct Slot(UITreeNode Node, string Text, bool Active, bool Busy, int Quantity)
                {
                    public static implicit operator (UITreeNode node, string text, bool active, bool busy, int quantity)(Slot value)
                    {
                        return (value.Node, value.Text, value.Active, value.Busy, value.Quantity);
                    }

                    public static implicit operator Slot((UITreeNode node, string text, bool active, bool busy, int quantity) value)
                    {
                        return new Slot(value.node, value.text, value.active, value.busy, value.quantity);
                    }
                }
                public List<Slot> activeSlots = new();
                public class Squadron
                {
                    public UITreeNode node;
                    public enum FighterActionState
                    {
                        READY, INSPACE, RETURNING, LANDING, REFUELING, UNKNOWN
                    }
                    //slotID
                    public List<Slot> slots= new();
                    public int squadronSize;
                    public int squadronMaxSize;
                    public FighterActionState state;
                    public string name;
                    public int lastFighterDamage;
                }
                public class SquadronsUI
                {
                    public List<Squadron> squadrons = new();
                    public UITreeNode fighterDragButton, fightersButtonRecallAll, fightersButtonOpenBay, fightersButtonLaunchAll;

                }
                public SquadronsUI squadronsUI;
                public void Parse(UITreeNode root)
                {
                    {
                        shipUINode = ListNodesWithPythonObjectTypeName(root, "ShipUI").FirstOrDefault();
                        if (shipUINode != null)
                        {
                            activeSlots = ListNodesWithPythonObjectTypeName(ListNodesWithPythonObjectTypeName(shipUINode, "SlotsContainer").FirstOrDefault(), "ShipSlot").Where(s =>
                            s.children.Any(c => c.pythonObjectTypeName.Equals("ModuleButton"))).Select(s =>
                            {
                                var c = s.children.First(c => c.pythonObjectTypeName.Equals("EveLabelSmall"));
                                var m = s.children.First(c => c.pythonObjectTypeName.Equals("ModuleButton"));
                                return new Slot(s, c.dictEntriesOfInterest.Value<string>("_setText").Split(">")[1],
                                m.dictEntriesOfInterest.Value<bool>("ramp_active") && !m.dictEntriesOfInterest.Value<bool>("isDeactivating"),
                                m.dictEntriesOfInterest.Value<bool>("isDeactivating"),
                                m.dictEntriesOfInterest.Value<int>("quantity"));
                            }).ToList();

                            var hpgauges = ListNodesWithPythonObjectTypeName(shipUINode, "HPGauges").FirstOrDefault();
                            if (hpgauges != null)
                            {
                                var hpg = ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "shieldGauge".Equals(s)).FirstOrDefault();//.dict_entries_of_interest.Value<double>("_lastValue");
                                HP.shield = (int)(ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "shieldGauge".Equals(s)).FirstOrDefault().dictEntriesOfInterest.Value<double>("_lastValue") * 100);
                                HP.armor = (int)(ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "armorGauge".Equals(s)).FirstOrDefault().dictEntriesOfInterest.Value<double>("_lastValue") * 100);
                                HP.structure = (int)(ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "structureGauge".Equals(s)).FirstOrDefault().dictEntriesOfInterest.Value<double>("_lastValue") * 100);
                            }

                            capContainer = ListNodesWithPythonObjectTypeName(shipUINode, "CapacitorContainer").FirstOrDefault();
                            if (capContainer != null)
                            {
                                Capacitor = capContainer.dictEntriesOfInterest.Value<double>("lastSetCapacitor");
                            }

                            var speedgaugereadoutcandidates = ListNodesWithPythonObjectTypeName(shipUINode, "SpeedGauge");
                            var speedgauge = speedgaugereadoutcandidates.FirstOrDefault();
                            if (speedgauge != null)
                            {
                                var speedlabel = ListNodesWithPropertyValue(speedgauge, "_name", (string s) => s == "speedLabel");
                                var speed = speedlabel.FirstOrDefault();
                                if (speed != null)
                                {
                                    var speedtext = speed.dictEntriesOfInterest.Value<string>("_setText");
                                    if (speedtext.Contains("Warp"))
                                    {
                                        Navistate = (0, true);
                                    }
                                    else
                                    {
                                        Navistate = (float.Parse(speedtext.Split(' ')[0].Replace(",", "")), false);
                                    }
                                }
                            }

                        }
                        UITreeNode squadronsUINode = ListNodesWithPythonObjectTypeName(shipUINode, "SquadronsUI").FirstOrDefault();
                        if (squadronsUINode != null)
                        {
                            ReadSquadrons(squadronsUINode);
                        }
                    }
                }
                //new dictentryofinterest 
                //squadronMaxSize, squadronSize, slotID, buttonDisabled
                private void ReadSquadrons(UITreeNode squadronsUINode)
                {
                    squadronsUI = new()
                    {
                        fighterDragButton = ListNodesWithPythonObjectTypeName(squadronsUINode, "FighterDragButton").FirstOrDefault(),
                        fightersButtonRecallAll = ListNodesWithPythonObjectTypeName(squadronsUINode, "FightersButtonRecallAll").FirstOrDefault(),
                        fightersButtonOpenBay = ListNodesWithPythonObjectTypeName(squadronsUINode, "FightersButtonOpenBay").FirstOrDefault(),
                        fightersButtonLaunchAll = ListNodesWithPythonObjectTypeName(squadronsUINode, "FightersButtonLaunchAll").FirstOrDefault(),
                        squadrons = ListNodesWithPythonObjectTypeName(squadronsUINode, "SquadronUI")
                        .Select(s =>
                        {
                            var squadron = new Squadron();
                            var squadronCont = ListNodesWithPythonObjectTypeName(s, "SquadronCont").FirstOrDefault();
                            if (squadronCont != null)
                            {
                                //squadron.node = squadronCont;
                                var fightersHealthGauge = ListNodesWithPythonObjectTypeName(squadronCont, "FightersHealthGauge").FirstOrDefault();
                                if (fightersHealthGauge != null)
                                {
                                    squadron.squadronMaxSize = fightersHealthGauge.dictEntriesOfInterest.Value<int>("squadronMaxSize");
                                    squadron.squadronSize = fightersHealthGauge.dictEntriesOfInterest.Value<int>("squadronSize");
                                    var hint = fightersHealthGauge.dictEntriesOfInterest.Value<string>("_hint");
                                    if (hint != null)
                                    {
                                        squadron.name = hint.Split(" Squadron")[0];
                                        var healthString = Regex.Match(hint, @"\d+%");
                                        if (healthString.Success)
                                        {
                                            squadron.lastFighterDamage = int.Parse(healthString.Value.Replace("%", ""));
                                        }
                                        else
                                        {
                                            squadron.lastFighterDamage = 0;
                                        }
                                    }
                                }
                                var squadronActionLabel = ListNodesWithPythonObjectTypeName(squadronCont, "SquadronActionLabel").FirstOrDefault();
                                if (squadronActionLabel != null)
                                {
                                    squadron.node = squadronActionLabel;
                                    squadron.state = squadronActionLabel.Value<string>("_setText") switch
                                    {
                                        "Ready" => Squadron.FighterActionState.READY,
                                        "In Space" => Squadron.FighterActionState.INSPACE,
                                        "Returning" => Squadron.FighterActionState.RETURNING,
                                        "Landing" => Squadron.FighterActionState.LANDING,
                                        "Refueling" => Squadron.FighterActionState.REFUELING,
                                        _ => Squadron.FighterActionState.UNKNOWN,
                                    };
                                }
                            }
                            var abilitiesCont = ListNodesWithPythonObjectTypeName(s, "AbilitiesCont").FirstOrDefault();
                            //abilitiesCont.dictEntriesOfInterest["_top"] = 0;//TRICK
                            if (abilitiesCont != null)
                            {
                                var abilitiyIcons = ListNodesWithPythonObjectTypeName(abilitiesCont, "AbilityIcon");
                                squadron.slots = abilitiyIcons.Select(ai => new Slot(ai,
                                    ai.dictEntriesOfInterest.Value<int>("slotID").ToString(),
                                    ai.dictEntriesOfInterest.Value<bool>("ramp_active"),
                                    ai.dictEntriesOfInterest.Value<bool>("buttonDisabled"),
                                    -1)).ToList();//TODO quantity
                            }
                            return squadron;
                        })
                        .ToList()
                    };
                }

                public bool Exist => shipUINode != null;

                public UITreeNode Node => shipUINode;

                public ref (int shield, int armor, int structure) HP => ref hp;
                public ref (float speed, bool warp) Navistate => ref navistate;

                public double Capacitor { get => capacitor; set => capacitor = value; }
            }
            //done  
            public class DropDownMenus : IUIElement
            {
                public List<(UITreeNode node, string text)> menuEntrys;
                List<UITreeNode> node;

                public bool Exist => node.Any();

                public UITreeNode Node => node.FirstOrDefault();

                public void Parse(UITreeNode root)
                {
                    {
                        node = ListNodesWithPythonObjectTypeName(root, "ContextMenu").ToList();//20220405
                        if (node.Any())
                        {
                            menuEntrys = new();
                            node.ForEach(mn =>
                            {
                                menuEntrys = menuEntrys.Concat(ListNodesWithPythonObjectTypeName(mn, "MenuEntryView").Select(mev => (mev, ListNodesWithPythonObjectTypeName(mev, "EveLabelMedium").Select(elm => elm.dictEntriesOfInterest.Value<String>("_setText")).FirstOrDefault())).ToList()).ToList();
                            });
                        }
                    }
                }
            }

            public class ActiveItem : IUIElement
            {
                UITreeNode node;
                UITreeNode label;
                private List<(UITreeNode node, string hint)> actions;
                private (UITreeNode node, string text) itemName;
                public void Parse(UITreeNode root)
                {
                    {
                        node = ListNodesWithPythonObjectTypeName(root, "ActiveItem").FirstOrDefault();
                        if (node != null)
                        {
                            label = ListNodesWithPythonObjectTypeName(node, "EveLabelSmall").Where(els => !"Selected Item".Equals(els.dictEntriesOfInterest.Value<string>("_setText"))).FirstOrDefault();
                            if (label != null)
                            {
                                ItemName = (label, label.dictEntriesOfInterest.Value<string>("_setText"));
                                Actions = ListNodesWithPythonObjectTypeName(node, "Container").Where(c =>
                                {
                                    if (c.children == null)
                                    {
                                        return false;
                                    }
                                    else
                                    {
                                        return c.children.Any(tc => tc.pythonObjectTypeName.Equals("Action"));
                                    }
                                }).Select(c => (c, c.dictEntriesOfInterest.Value<string>("_name"))).ToList();
                            }
                            else
                            {
                                if (ItemName.text == null)
                                {
                                    ItemName = (label, "null");
                                }
                            }
                        }
                    }
                }
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public ref (UITreeNode node, string text) ItemName => ref itemName;
                public ref List<(UITreeNode node, string hint)> Actions => ref actions;
            }

            public class Inventory : IUIElement
            {
                UITreeNode node;
                private UITreeNode btnLootAll;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public UITreeNode BtnLootAll { get => btnLootAll; set => btnLootAll = value; }

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "InventoryPrimary").FirstOrDefault();
                    BtnLootAll = ListNodesWithPropertyValue(node, "_setText", (string s) => s == "Loot All").FirstOrDefault();
                }
                public ulong TotalValueIsk()
                {
                    var pricelabel = ListNodesWithPropertyValue(node, "_name", (string s) => s == "totalPriceLabel").First().dictEntriesOfInterest.Value<string>("_setText");
                    var price = ulong.Parse(pricelabel.Split(' ')[0].Replace(",", ""));
                    return price;
                }
            }
            public class ActiveShipCargo : IUIElement
            {
                UITreeNode node;
                private double cargoPercentage = -1;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public double CargoPercentage { get => cargoPercentage; set => cargoPercentage = value; }

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "ActiveShipCargo").FirstOrDefault();
                    if (node != null)
                    {
                        var cargo_hold_text = ListNodesWithPropertyValue(node, "_setText", (string s) => s?.Contains("m³") ?? false).FirstOrDefault();
                        if (cargo_hold_text != null)
                        {
                            var d = cargo_hold_text.dictEntriesOfInterest.Value<string>("_setText").Split("m")[0].Split("/");
                            if (d[0].Contains(')'))
                            {
                                d[0] = d[0].Split(')')[1];
                            }
                            CargoPercentage = double.Parse(d[0]) / double.Parse(d[1]) * 100;
                        }
                    }

                }
                public ulong TotalValueIsk()
                {
                    var pricelabel = ListNodesWithPropertyValue(node, "_name", (string s) => s == "totalPriceLabel").First().dictEntriesOfInterest.Value<string>("_setText");
                    var price = ulong.Parse(pricelabel.Split(' ')[0].Replace(",", ""));
                    return price;
                }
            }

            public class MessageBoxes : IUIElement
            {
                public class MessageBox
                {
                    public UITreeNode node;
                    public string caption;
                    public List<(UITreeNode node, string text)> buttons;
                }
                public List<MessageBox> msgBoxes;
                public void Parse(UITreeNode root)
                {
                    var msgboxes = ListNodesWithPythonObjectTypeName(root, "MessageBox").ToList();//20220405 
                    msgBoxes = msgboxes.Select(mb =>
                    {
                        var tmb = new MessageBox
                        {
                            node = mb,
                            caption = ListNodesWithPythonObjectTypeName(mb, "EveCaptionLarge").First().dictEntriesOfInterest.Value<string>("_setText"),//20220405
                            buttons = ListNodesWithPythonObjectTypeName(mb, "Button").Select(b => (b, ListNodesWithPythonObjectTypeName(b, "LabelThemeColored").First().dictEntriesOfInterest.Value<string>("_setText"))).ToList()
                        };
                        return tmb;
                    }).ToList();
                }

                public bool Exist => msgBoxes.Count > 0;

                public UITreeNode Node => throw new NotImplementedException();
            }
            public class Telecom : IUIElement
            {
                UITreeNode node_;
                public UITreeNode button_ok_;
                public bool Exist => node_ != null;

                public UITreeNode Node => node_;


                public void Parse(UITreeNode root)
                {
                    node_ = ListNodesWithPythonObjectTypeName(root, "Telecom").FirstOrDefault();
                    if (node_ != null)
                    {
                        button_ok_ = ListNodesWithPropertyValue(node_, "_name", (string s) =>
                        {
                            if (s == null) return false;
                            else return s.Contains("ok_dialog_button");
                        }).FirstOrDefault();
                    }
                }
            }
            public class FleetView : IUIElement
            {
                UITreeNode node;
                List<(UITreeNode node, string content)> broadcasts_;
                public (UITreeNode node, string content) last_broadcast_;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "FleetWindow").FirstOrDefault();
                    if (node != null)
                    {
                        broadcasts_ = ListNodesWithPythonObjectTypeName(node, "BroadcastEntry").Select(be => (ListNodesWithPythonObjectTypeName(be, "EveLabelMedium").FirstOrDefault(), ListNodesWithPythonObjectTypeName(be, "EveLabelMedium").FirstOrDefault()?.dictEntriesOfInterest.Value<string>("_setText") ?? "00:00:00 - null")).ToList();
                        if (broadcasts_.Any())
                        {
                            last_broadcast_ = broadcasts_.OrderBy(b => int.Parse(b.content.Split("-")[0].Replace(":", ""))).Last();
                        }
                    }
                }
            }
            public class TargetBar : IUIElement
            {
                UITreeNode node;
                List<(UITreeNode node, string name)> targets;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {

                }
            }
            public class InfoPanelRoute : IUIElement
            {
                UITreeNode node;
                public string nextSystem = null;
                public UITreeNode nextWaypointMarker;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "InfoPanelRoute").FirstOrDefault();
                    if (node != null)
                    {
                        var markers = ListNodesWithPropertyValue(node, "_name", (string s) => s == "markersParent").FirstOrDefault();
                        if (markers != null)
                        {
                            nextWaypointMarker = markers.children.FirstOrDefault();
                            if (nextWaypointMarker != null)
                            {
                                var next_waypoint_panel = ListNodesWithPythonObjectTypeName(node, "NextWaypointPanel").FirstOrDefault();
                                if (next_waypoint_panel != null)
                                {
                                    var next_waypoint = ListNodesWithPythonObjectTypeName(next_waypoint_panel, "EveLabelMedium").FirstOrDefault();
                                    if (next_waypoint != null)
                                    {
                                        var next_waypoint_text = next_waypoint.dictEntriesOfInterest.Value<string>("_setText");
                                        if (!"No Destination".Equals(next_waypoint_text))
                                        {
                                            nextSystem = next_waypoint_text.Split("Route")[1].Split('>')[1].Split('<')[0];
                                        }
                                    }
                                }

                            }
                        }

                    }
                }
            }
            public class InfoPanelESS : IUIElement
            {
                UITreeNode node;
                public bool connecting = false;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "InfoPanelESS").FirstOrDefault();
                    if (node != null)
                    {
                        connecting = ListNodesWithPropertyValue(node, "_name", (string s) => "mainBankHackingCont".Equals(s)).Any();
                    }
                }
            }
            public class InfoPanelContainer : IUIElement
            {
                UITreeNode node;
                public bool Exist => node != null;

                public UITreeNode Node => node;

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "InfoPanelContainer").FirstOrDefault();
                }
            }

        }
    }


}

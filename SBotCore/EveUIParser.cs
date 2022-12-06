using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
                if (s.python_object_type_name.Equals(name))
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
                if (predict(s.dict_entries_of_interest.Value<T>(name)))
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

            }
            public static EveUI Parse(UITreeNode r)
            {
                return new EveUI(r);
            }


            public interface IUiElement
            {
                public bool Exists();
                public UITreeNode Node();
                public void Parse(UITreeNode root);
            }
            //done 
            public class ChatWindowStack : IUiElement
            {
                UITreeNode local_chat_;
                public List<(UITreeNode node, string tag, string name)> members_ = new();
                readonly bool is_local_chat_;
                public ChatWindowStack(bool is_local_chat)
                {
                    is_local_chat_ = is_local_chat;
                }

                static bool IsLocalChatStack(UITreeNode chatwindowstack)
                {
                    var res = ListNodesWithPythonObjectTypeName(chatwindowstack, "WindowStackTab").ToImmutableList();//20220405
                    foreach (var node in res)
                    {
                        var rest = ListNodesWithPythonObjectTypeName(node, "LabelThemeColored").ToImmutableList();
                        if (rest.Count > 0)
                        {
                            if (rest.First().dict_entries_of_interest.Value<string>("_setText").Contains("Local"))//dataincode
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
                UITreeNode FindLocalChatNode(UITreeNode root)
                {
                    var nodes = ListNodesWithPythonObjectTypeName(root, "ChatWindowStack").ToList();
                    return nodes.FirstOrDefault(n => IsLocalChatStack(n) ^ !is_local_chat_);
                }
                void ReadMembers(UITreeNode root)
                {
                    local_chat_ = FindLocalChatNode(root);
                    if (Exists())
                    {
                        members_ = ListNodesWithPythonObjectTypeName(local_chat_, "XmppChatSimpleUserEntry").Select(ue =>
                        {
                            var hint = ListNodesWithPythonObjectTypeName(ue, "FlagIconWithState").FirstOrDefault();
                            var name = ue.dict_entries_of_interest.Value<string>("_name");
                            return (ue, (hint == null) ? name : hint.dict_entries_of_interest.Value<string>("_hint"), name);
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
                    if (!Exists()) return 0;
                    return members_.Count(m => hostiletags.Any(t => m.tag.Contains(t)));
                }

                public bool Exists()
                {
                    return local_chat_ != null;
                }

                public UITreeNode Node()
                {
                    return local_chat_;
                }
            }
            //done 
            public class ProbeScanner : IUiElement
            {
                UITreeNode node_;
                public List<Anom> anoms_ = new();

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
                    node_ = ListNodesWithPythonObjectTypeName(root, "ProbeScannerWindow").FirstOrDefault();
                    if (node_ != null)
                    {
                        anoms_ = new List<Anom>();
                        var scanresults = ListNodesWithPythonObjectTypeName(root, "ScanResultNew");
                        scanresults.ToList().ForEach(sr =>
                        {
                            var anom = new Anom();
                            var srinfo = ListNodesWithPythonObjectTypeName(sr, "EveLabelMedium").Select(n => n.dict_entries_of_interest.Value<string>("_setText")).ToList();
                            anom.Distance = float.Parse(srinfo[1].Split(' ')[0].Replace(",", ""));
                            anom.Unit = srinfo[1].Split(' ')[1];
                            anom.Id = srinfo[2];
                            anom.Name = srinfo[3];
                            anom.Node = sr;
                            anoms_.Add(anom);
                        });
                    }
                }

                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }
            }
            //done
            public class DroneView : IUiElement
            {
                UITreeNode node_;


                UITreeNode drones_in_local_space_main_entry_;
                int num_drones_in_local_space_;

                UITreeNode drones_in_bay_main_entry_;
                int num_drones_in_bay_;

                public List<(UITreeNode node, string name, string state)> drones_in_space_ = new();

                public int NumDronesOutside()
                {
                    if (!Exists()) return 0;
                    return num_drones_in_local_space_;
                }

                public int NumDronesInside()
                {
                    if (!Exists()) return 0;
                    return num_drones_in_bay_;
                }
                public void Parse(UITreeNode root)
                {
                    //try
                    {
                        node_ = ListNodesWithPythonObjectTypeName(root, "DroneView").FirstOrDefault();
                        if (node_ != null)
                        {
                            var dg = ListNodesWithPythonObjectTypeName(node_, "DroneMainGroup");
                            foreach (var dronemaingroup in dg)
                            {
                                var candidatel = ListNodesWithPythonObjectTypeName(dronemaingroup, "EveLabelMedium").Where(elm => elm.dict_entries_of_interest.Value<string>("_setText").Contains("Local Space")).FirstOrDefault();
                                var candidateb = ListNodesWithPythonObjectTypeName(dronemaingroup, "EveLabelMedium").Where(elm => elm.dict_entries_of_interest.Value<string>("_setText").Contains("Bay")).FirstOrDefault();
                                drones_in_local_space_main_entry_ = candidatel ?? drones_in_local_space_main_entry_;
                                drones_in_bay_main_entry_ = candidateb ?? drones_in_bay_main_entry_;
                            }
                            num_drones_in_local_space_ = int.Parse(drones_in_local_space_main_entry_.dict_entries_of_interest.Value<string>("_setText").Split('(')[1].Split(')')[0]);
                            num_drones_in_bay_ = int.Parse(drones_in_bay_main_entry_.dict_entries_of_interest.Value<string>("_setText").Split('(')[1].Split(')')[0]);

                            var drones = ListNodesWithPythonObjectTypeName(node_, "DroneEntry");
                            drones_in_space_ = drones.Where(droneentry => droneentry.dict_entries_of_interest.Value<string>("_hint").Any()).Select(de =>
                              {
                                  var hint = de.dict_entries_of_interest.Value<string>("_hint");
                                  var info = hint.Split('(');
                                  return (de, info[0], info[1].Split(')')[0]);
                              }).ToList();
                        }
                    }
                    //catch (Exception ex) { LogWriter.LogWrite(ex.ToString()); }
                }

                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }
            }
            //done  
            public class Overview : IUiElement
            {
                UITreeNode overview_;
                public List<(UITreeNode node, string text, bool selected)> tabs_ = new();
                public class OverviewEntry
                {
                    public UITreeNode node_;
                    public List<string> labels_;
                    public int distance_;
                    public List<string> ewars_;
                    public List<string> indicators_;
                    public bool targeting_;
                    public bool IsPlayer()
                    {
                        return IsShip() && labels_.Any(l => l.Contains('['));
                    }
                    public bool IsShip()
                    {
                        return labels_.Any(l => long.TryParse(l.Replace(",",""), out long r));
                    }
                }
                public List<OverviewEntry> overviewentrys_;
                public List<OverviewEntry> targeted_;
                public List<OverviewEntry> targeting_;
                public List<OverviewEntry> not_targeted_;
                public OverviewEntry active_target_;
                public void Parse(UITreeNode root)
                {
                    //try
                    {
                        overview_ = ListNodesWithPythonObjectTypeName(root, "OverviewWindowOld").FirstOrDefault();
                        if (overview_ != null)
                        {
                            tabs_ = ListNodesWithPythonObjectTypeName(ListNodesWithPythonObjectTypeName(overview_, "OverviewTabGroup").First(), "Tab").Select(t => (t, ListNodesWithPropertyValue(t, "_name", (string s) => "tabLabel".Equals(s)).First().dict_entries_of_interest.Value<string>("_setText"), t.dict_entries_of_interest.Value<bool>("_selected"))).ToList();
                            overviewentrys_ = ListNodesWithPythonObjectTypeName(overview_, "OverviewScrollEntry").Select(oe =>
                            new OverviewEntry
                            {
                                node_ = oe,
                                labels_ = ListNodesWithPythonObjectTypeName(oe, "OverviewLabel").Select(ol => ol.dict_entries_of_interest.Value<string>("_text")).ToList(),
                                distance_ = int.MaxValue,
                                ewars_ = ListNodesWithPythonObjectTypeName(oe, "Icon").Select(I => I.dict_entries_of_interest.Value<string>("_hint")).ToList(),//ewar
                                indicators_ = ListNodesWithPythonObjectTypeName(oe, "Sprite").Select(s => s.dict_entries_of_interest.Value<string>("_name")).ToList(),//target status
                                targeting_ = ListNodesWithPropertyValue(oe, "_name", (string s) => s == "targeting").Any()
                            }).ToList();
                            overviewentrys_ = overviewentrys_.Select(e =>
                              {
                                  try
                                  {
                                      var d = e.labels_.Where(l => l.Contains(" km")).FirstOrDefault();
                                      if (d != null)
                                      {
                                          e.distance_ = int.Parse(d.Split(" ")[0].Replace(",", "")) * 1000;
                                          return e;
                                      }
                                      else
                                      {
                                          var d2 = e.labels_.Where(l => l.Contains(" m")).FirstOrDefault();
                                          if (d2 != null)
                                          {
                                              e.distance_ = int.Parse(d2.Split(" ")[0].Replace(",", ""));
                                              return e;
                                          }
                                          else
                                          {
                                              e.distance_ = int.MaxValue;
                                              return e;
                                          }
                                      }
                                  }
                                  catch 
                                  {
                                      e.distance_ = int.MaxValue;
                                      return e;
                                  }
                              }).ToList();
                            targeted_ = overviewentrys_.Where(ove => ove.indicators_.Any(i => i.Contains("argeted"))).ToList();
                            targeting_ = overviewentrys_.Where(ove => ove.targeting_).ToList();
                            not_targeted_ = overviewentrys_.Where(ove => !ove.IsPlayer() && !ove.targeting_ && !ove.indicators_.Any(i => i.Contains("argeted"))).ToList();
                            active_target_ = targeted_.FirstOrDefault(t => t.indicators_.Any(i => i.Contains("ActiveTarget")));

                        }
                    }
                    //catch (Exception ex) { LogWriter.LogWrite(ex.ToString()); }
                }
                public int NumNPC()//TODO
                {
                    if (Exists())
                    {
                        return overviewentrys_.Count(ove => ove.IsShip()&&!ove.IsPlayer());
                    }
                    else
                    {
                        return 0;
                    }
                }
                public int NumPlayer()//IS a player if it has corp tag
                {
                    if (Exists())
                    {
                        return overviewentrys_.Count(ove => ove.IsPlayer());
                    }
                    else
                    {
                        return 0;
                    }
                }

                public bool Exists()
                {
                    return overview_ != null;
                }

                public UITreeNode Node()
                {
                    return overview_;
                }
            }
            //done 
            public class StandaloneBookmark : IUiElement
            {

                UITreeNode node;
                public List<(UITreeNode node, string text)> labels = new();

                public bool Exists()
                {
                    return node != null;
                }

                public UITreeNode Node()
                {
                    return node;
                }

                public void Parse(UITreeNode root)
                {
                    //try
                    {
                        node = ListNodesWithPythonObjectTypeName(root, "StandaloneBookmarkWnd").FirstOrDefault();
                        if (node != null)
                        {
                            labels = ListNodesWithPythonObjectTypeName(node, "PlaceEntry")
                                .Select(pe => (pe, ListNodesWithPythonObjectTypeName(pe, "EveLabelMedium").Select(elm => elm.dict_entries_of_interest.Value<String>("_setText")).FirstOrDefault()))
                                .ToList();
                        }
                    }
                    //catch (Exception ex) { LogWriter.LogWrite(ex.ToString()); }
                }
            }
            //done not done
            public class ShipUI : IUiElement
            {
                private UITreeNode shipui_;

                public (int shield, int armor, int structure) hp_;

                public UITreeNode capContainer;
                public double capacitor;

                public (float speed, bool warp) navistate_;


                public List<(UITreeNode node, string text, bool active, bool busy, int quantity)> active_slots_ = new();

                public void Parse(UITreeNode root)
                {
                    //try
                    {
                        shipui_ = ListNodesWithPythonObjectTypeName(root, "ShipUI").FirstOrDefault();
                        if (shipui_ != null)
                        {
                            active_slots_ = ListNodesWithPythonObjectTypeName(ListNodesWithPythonObjectTypeName(shipui_, "SlotsContainer").FirstOrDefault(), "ShipSlot").Where(s =>
                            s.children.Any(c => c.python_object_type_name.Equals("ModuleButton"))).Select(s =>
                            {
                                var c = s.children.First(c => c.python_object_type_name.Equals("EveLabelSmall"));
                                var m = s.children.First(c => c.python_object_type_name.Equals("ModuleButton"));
                                return (s, c.dict_entries_of_interest.Value<string>("_setText").Split(">")[1],
                                m.dict_entries_of_interest.Value<bool>("ramp_active") && !m.dict_entries_of_interest.Value<bool>("isDeactivating"),
                                m.dict_entries_of_interest.Value<bool>("isDeactivating"),
                                m.dict_entries_of_interest.Value<int>("quantity"));
                            }).ToList();

                            var hpgauges = ListNodesWithPythonObjectTypeName(shipui_, "HPGauges").FirstOrDefault();
                            if (hpgauges != null)
                            {
                                var hpg = ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "shieldGauge".Equals(s)).FirstOrDefault();//.dict_entries_of_interest.Value<double>("_lastValue");
                                hp_.shield = (int)(ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "shieldGauge".Equals(s)).FirstOrDefault().dict_entries_of_interest.Value<double>("_lastValue") * 100);
                                hp_.armor = (int)(ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "armorGauge".Equals(s)).FirstOrDefault().dict_entries_of_interest.Value<double>("_lastValue") * 100);
                                hp_.structure = (int)(ListNodesWithPropertyValue(hpgauges, "_name", (string s) => "structureGauge".Equals(s)).FirstOrDefault().dict_entries_of_interest.Value<double>("_lastValue") * 100);
                            }

                            capContainer = ListNodesWithPythonObjectTypeName(shipui_, "CapacitorContainer").FirstOrDefault();
                            if (capContainer != null)
                            {
                                capacitor = capContainer.dict_entries_of_interest.Value<double>("lastSetCapacitor");
                            }

                            var speedgaugereadoutcandidates = ListNodesWithPythonObjectTypeName(shipui_, "SpeedGauge");
                            var speedgauge = speedgaugereadoutcandidates.FirstOrDefault();
                            if (speedgauge != null)
                            {
                                var speedlabel = ListNodesWithPropertyValue(speedgauge, "_name", (string s) => s == "speedLabel");
                                var speed = speedlabel.FirstOrDefault();
                                if (speed != null)
                                {
                                    var speedtext = speed.dict_entries_of_interest.Value<string>("_setText");
                                    if (speedtext.Contains("Warp"))
                                    {
                                        navistate_ = (0, true);
                                    }
                                    else
                                    {
                                        navistate_ = (float.Parse(speedtext.Split(' ')[0].Replace(",", "")), false);
                                    }
                                }
                            }
                        }
                    }
                    //catch (Exception ex) { LogWriter.LogWrite(ex.ToString()); }
                }

                public bool Exists()
                {
                    return shipui_ != null;
                }

                public UITreeNode Node()
                {
                    return shipui_;
                }
            }
            //done  
            public class DropDownMenus : IUiElement
            {
                public List<(UITreeNode node, string text)> menu_entrys_;
                List<UITreeNode> menu_node_;

                public bool Exists()
                {
                    return menu_node_.Any();
                }

                public UITreeNode Node()
                {
                    return menu_node_.FirstOrDefault();
                }

                public void Parse(UITreeNode root)
                {
                    //try
                    {
                        menu_node_ = ListNodesWithPythonObjectTypeName(root, "ContextMenu").ToList();//20220405
                        if (menu_node_.Any())
                        {
                            menu_entrys_ = new();
                            menu_node_.ForEach(mn =>
                            {
                                menu_entrys_ = menu_entrys_.Concat(ListNodesWithPythonObjectTypeName(mn, "MenuEntryView").Select(mev => (mev, ListNodesWithPythonObjectTypeName(mev, "EveLabelMedium").Select(elm => elm.dict_entries_of_interest.Value<String>("_setText")).FirstOrDefault())).ToList()).ToList();
                            });
                        }
                    }
                    //catch(Exception ex) { LogWriter.LogWrite(ex.ToString()); }
                }
            }

            public class ActiveItem : IUiElement
            {
                UITreeNode node_;
                UITreeNode label_;
                public List<(UITreeNode node, string hint)> actions_;
                public (UITreeNode node, string text) item_name_;
                public void Parse(UITreeNode root)
                {
                    //try
                    {
                        node_ = ListNodesWithPythonObjectTypeName(root, "ActiveItem").FirstOrDefault();
                        if (node_ != null)
                        {
                            label_ = ListNodesWithPythonObjectTypeName(node_, "EveLabelSmall").Where(els => !"Selected Item".Equals(els.dict_entries_of_interest.Value<string>("_setText"))).FirstOrDefault();
                            if (label_ != null)
                            {
                                item_name_ = (label_, label_.dict_entries_of_interest.Value<string>("_setText"));
                                actions_ = ListNodesWithPythonObjectTypeName(node_, "Container").Where(c =>
                                {
                                    if (c.children == null)
                                    {
                                        return false;
                                    }
                                    else
                                    {
                                        return c.children.Any(tc => tc.python_object_type_name.Equals("Action"));
                                    }
                                }).Select(c => (c, c.dict_entries_of_interest.Value<string>("_name"))).ToList();
                            }
                            else
                            {
                                if (item_name_.text == null)
                                {
                                    item_name_ = (label_, "null");
                                }
                            }
                        }
                    }
                    //catch (Exception ex) { LogWriter.LogWrite(ex.ToString()); }

                }
                public bool HasValue()
                {
                    return label_ != null;
                }
                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }
            }

            public class Inventory : IUiElement
            {
                UITreeNode node_;
                public UITreeNode btn_loot_all_;
                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }

                public void Parse(UITreeNode root)
                {
                    node_ = ListNodesWithPythonObjectTypeName(root, "InventoryPrimary").FirstOrDefault();
                    btn_loot_all_ = ListNodesWithPropertyValue(node_, "_setText", (string s) => s == "Loot All").FirstOrDefault();
                }
                public ulong TotalValueIsk()
                {
                    var pricelabel = ListNodesWithPropertyValue(node_, "_name", (string s) => s == "totalPriceLabel").First().dict_entries_of_interest.Value<string>("_setText");
                    var price = ulong.Parse(pricelabel.Split(' ')[0].Replace(",", ""));
                    return price;
                }
            }
            public class ActiveShipCargo : IUiElement
            {
                UITreeNode node_;
                public double cargo_percentage_ = -1;
                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }

                public void Parse(UITreeNode root)
                {
                    node_ = ListNodesWithPythonObjectTypeName(root, "ActiveShipCargo").FirstOrDefault();
                    if (node_ != null)
                    {
                        var cargo_hold_text = ListNodesWithPropertyValue(node_, "_setText", (string s) => s?.Contains("m³") ?? false).FirstOrDefault();
                        if (cargo_hold_text != null)
                        {
                            var d = cargo_hold_text.dict_entries_of_interest.Value<string>("_setText").Split("m")[0].Split("/");
                            if (d[0].Contains(')'))
                            {
                                d[0] = d[0].Split(')')[1];
                            }
                            cargo_percentage_ = double.Parse(d[0]) / double.Parse(d[1]) * 100;
                        }
                    }

                }
                public ulong TotalValueIsk()
                {
                    var pricelabel = ListNodesWithPropertyValue(node_, "_name", (string s) => s == "totalPriceLabel").First().dict_entries_of_interest.Value<string>("_setText");
                    var price = ulong.Parse(pricelabel.Split(' ')[0].Replace(",", ""));
                    return price;
                }
            }

            public class MessageBoxes : IUiElement
            {
                public class MessageBox
                {
                    public UITreeNode node_;
                    public string caption_;
                    public List<(UITreeNode node, string text)> buttons_;
                }
                public List<MessageBox> msg_boxes_;
                public void Parse(UITreeNode root)
                {
                    var msgboxes = ListNodesWithPythonObjectTypeName(root, "MessageBox").ToList();//20220405 //TODO telecom is NOT messagebox! !!!Fleet inv IS messagebox!!!
                    msg_boxes_ = msgboxes.Select(mb =>
                    {
                        var tmb = new MessageBox
                        {
                            node_ = mb,
                            caption_ = ListNodesWithPythonObjectTypeName(mb, "EveCaptionLarge").First().dict_entries_of_interest.Value<string>("_setText"),//20220405
                            buttons_ = ListNodesWithPythonObjectTypeName(mb, "Button").Select(b => (b, ListNodesWithPythonObjectTypeName(b, "LabelThemeColored").First().dict_entries_of_interest.Value<string>("_setText"))).ToList()
                        };
                        return tmb;
                    }).ToList();
                }

                public bool Exists()
                {
                    return msg_boxes_.Count > 0;
                }

                public UITreeNode Node()
                {
                    throw new NotImplementedException();
                }
            }
            public class Telecom : IUiElement
            {
                UITreeNode node_;
                public UITreeNode button_ok_;
                public bool Exists() => node_ != null;

                public UITreeNode Node() => node_;


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
            public class FleetView : IUiElement
            {
                UITreeNode node;
                List<(UITreeNode node, string content)> broadcasts_;
                public (UITreeNode node, string content) last_broadcast_;
                public bool Exists()
                {
                    return node != null;
                }

                public UITreeNode Node()
                {
                    return node;
                }

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "FleetWindow").FirstOrDefault();
                    if (node != null)
                    {
                        broadcasts_ = ListNodesWithPythonObjectTypeName(node, "BroadcastEntry").Select(be => (ListNodesWithPythonObjectTypeName(be, "EveLabelMedium").FirstOrDefault(), ListNodesWithPythonObjectTypeName(be, "EveLabelMedium").FirstOrDefault()?.dict_entries_of_interest.Value<string>("_setText") ?? "00:00:00 - null")).ToList();
                        if (broadcasts_.Any())
                        {
                            last_broadcast_ = broadcasts_.OrderBy(b => int.Parse(b.content.Split("-")[0].Replace(":", ""))).Last();
                        }
                    }
                }
            }
            public class TargetBar : IUiElement
            {
                readonly UITreeNode node_;
                readonly List<(UITreeNode node, string name)> targets_;
                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }

                public void Parse(UITreeNode root)
                {

                }
            }
            public class InfoPanelRoute : IUiElement
            {
                UITreeNode node_;
                public string next_system_ = null;
                public UITreeNode next_waypoint_marker_;
                public bool Exists()
                {
                    return node_ != null;
                }

                public UITreeNode Node()
                {
                    return node_;
                }

                public void Parse(UITreeNode root)
                {
                    node_ = ListNodesWithPythonObjectTypeName(root, "InfoPanelRoute").FirstOrDefault();
                    if (node_ != null)
                    {
                        var markers = ListNodesWithPropertyValue(node_, "_name", (string s) => s == "markersParent").FirstOrDefault();
                        if (markers != null)
                        {
                            next_waypoint_marker_ = markers.children.FirstOrDefault();
                            if (next_waypoint_marker_ != null)
                            {
                                var next_waypoint_panel = ListNodesWithPythonObjectTypeName(node_, "NextWaypointPanel").FirstOrDefault();
                                if (next_waypoint_panel != null)
                                {
                                    var next_waypoint = ListNodesWithPythonObjectTypeName(next_waypoint_panel, "EveLabelMedium").FirstOrDefault();
                                    if (next_waypoint != null)
                                    {
                                        var next_waypoint_text = next_waypoint.dict_entries_of_interest.Value<string>("_setText");
                                        if (!"No Destination".Equals(next_waypoint_text))
                                        {
                                            next_system_ = next_waypoint_text.Split("Route")[1].Split('>')[1].Split('<')[0];
                                        }
                                    }
                                }

                            }
                        }

                    }
                }
            }
            public class InfoPanelESS : IUiElement
            {
                UITreeNode node;
                public bool connecting = false;
                public bool Exists()
                {
                    return node != null;
                }

                public UITreeNode Node()
                {
                    return node;
                }

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "InfoPanelESS").FirstOrDefault();
                    if (node != null)
                    {
                        connecting = ListNodesWithPropertyValue(node, "_name", (string s) => "mainBankHackingCont".Equals(s)).Any();
                    }
                }
            }
            public class InfoPanelContainer : IUiElement
            {
                UITreeNode node;
                public bool Exists()
                {
                    return node != null;
                }

                public UITreeNode Node()
                {
                    return node;
                }

                public void Parse(UITreeNode root)
                {
                    node = ListNodesWithPythonObjectTypeName(root, "InfoPanelContainer").FirstOrDefault();
                }
            }

        }
    }
}

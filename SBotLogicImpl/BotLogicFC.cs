using SBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static SBotCore.EveUIParser;

namespace SBotLogicImpl
{
    public class BotLogicFC : BotLogic
    {
        public override string Summary()=> "";

        public override void UpdateCB()
        {
            string m_name_ = ui.otherChatwindowStack.Members.FirstOrDefault(m => m.tag == m.name).name;
            if (Singleton.Instance.members.ContainsKey(m_name_))
            {
                Singleton.Instance.members[m_name_] = ui.shipUI.HP;
            }
            else
            {
                Singleton.Instance.members.Add(m_name_, ui.shipUI.HP);
            }
        }
    }

}

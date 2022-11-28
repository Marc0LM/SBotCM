using SBotCore;
using System;

namespace SBotCore
{
    public interface IInput
    {
        void KeyClick(string k);
        void KeyUp(string k);
        void KeyDown(string k);
        void MouseMove(UITreeNode node, UITreeNode root);
        void MouseClickLeft(UITreeNode node, UITreeNode root);
        void MouseClickRight(UITreeNode node, UITreeNode root);
    }
    public class InputHelper
    {
        static (int x, int y) ClientCoordinateofUITtreeNodeR(UITreeNode node, UITreeNode s)
        {
            if (s == node)
            {
                int tx = Math.Max(s.dict_entries_of_interest.Value<int>("_displayX"), s.dict_entries_of_interest.Value<int>("_left"));
                int ty = Math.Max(s.dict_entries_of_interest.Value<int>("_displayY"), s.dict_entries_of_interest.Value<int>("_top"));
                return (tx, ty);
            }
            if (s.children != null)
            {
                foreach (var c in s.children)
                {
                    var (x, y) = ClientCoordinateofUITtreeNodeR(node, c);
                    if (!(x == -9999))
                    {
                        int tx = Math.Max(s.dict_entries_of_interest.Value<int>("_displayX"), s.dict_entries_of_interest.Value<int>("_left"));
                        int ty = Math.Max(s.dict_entries_of_interest.Value<int>("_displayY"), s.dict_entries_of_interest.Value<int>("_top"));
                        return (tx + x, ty + y);
                    }
                }
            }
            return (-9999, -1);
        }

        public static (int x, int y) ClientCoordinateofUITtreeNode(UITreeNode node,UITreeNode root)
        {
            return ClientCoordinateofUITtreeNodeR(node, root);
        }
    }
}

using SBotCore;
using System;

namespace SBotCore
{
    public interface IInput
    {
        void KeyClick(string k);
        void KeyUp(string k);
        void KeyDown(string k);
        void MouseMove(UITreeNode node, UITreeNode root, bool center = false);
        void MouseClickLeft(UITreeNode node, UITreeNode root, bool center = false);
        void MouseClickRight(UITreeNode node, UITreeNode root, bool center = false);
    }
    public class InputHelper
    {
        static (int x, int y) ClientCoordinateofUITtreeNodeR(UITreeNode node, UITreeNode s)
        {
            if (s == node)
            {
                //int tx = Math.Max(s.dictEntriesOfInterest.Value<int>("_displayX"), s.dictEntriesOfInterest.Value<int>("_left"));
                //int ty = Math.Max(s.dictEntriesOfInterest.Value<int>("_displayY"), s.dictEntriesOfInterest.Value<int>("_top"));
                int tx = s.dictEntriesOfInterest.Value<int>("_displayX");
                int ty = s.dictEntriesOfInterest.Value<int>("_displayY");
                return (tx, ty);
            }
            if (s.children != null)
            {
                foreach (var c in s.children)
                {
                    var (x, y) = ClientCoordinateofUITtreeNodeR(node, c);
                    if (!(x == -9999))
                    {
                        //int tx = Math.Max(s.dictEntriesOfInterest.Value<int>("_displayX"), s.dictEntriesOfInterest.Value<int>("_left"));
                        //int ty = Math.Max(s.dictEntriesOfInterest.Value<int>("_displayY"), s.dictEntriesOfInterest.Value<int>("_top"));
                        int tx = s.dictEntriesOfInterest.Value<int>("_displayX");
                        int ty = s.dictEntriesOfInterest.Value<int>("_displayY");
                        return (tx + x, ty + y);
                    }
                }
            }
            return (-9999, -1);
        }

        public static (int x, int y) ClientCoordinateofUITtreeNode(UITreeNode node,UITreeNode root,bool center=false)
        {
            var p = ClientCoordinateofUITtreeNodeR(node, root);
            if (center)
            {
                p.x += node.Value<int>("_width")/2;
                p.y += node.Value<int>("_height")/2;
            }
            return p;
        }
    }
}

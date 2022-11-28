
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using SBotCore;
using static SBotCore.EveUIHelper;
namespace SBotCore
{
    [Obsolete("deprecated")]
    public class InputFocused : IInput
    {

        public InputFocused()
        {
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private static readonly Random random_ = new();
        public static int ms_before_click_ = 200;
        public static int ms_during_click_ = 90;
        public static int ms_after_click_ = 200;
        public static int randx = 10;
        public static int randy = 10;
        public static int biasy = 32;
        public static int biasx = 2;
        public void KeyClick(string k)
        {
            //System.Windows.Forms.SendKeys.SendWait(k);
        }

        public void MouseClickLeft(UITreeNode node, UITreeNode root)
        {
            var (dx, dy) = InputHelper.ClientCoordinateofUITtreeNode(node,root);
            var rx = dx + biasx + random_.Next() % randx;
            var ry = dy + biasy + random_.Next() % randy;

            SetCursorPosP(rx, ry);
            Thread.Sleep(ms_before_click_);
            MouseEvent((int)MouseEventFlags.LeftDown, rx, ry);
            Thread.Sleep(ms_during_click_);
            MouseEvent((int)MouseEventFlags.LeftUp, rx, ry);
            Thread.Sleep(ms_after_click_);

        }

        public void MouseClickRight(UITreeNode node, UITreeNode root)
        {
            var (dx, dy) = InputHelper.ClientCoordinateofUITtreeNode(node,root);
            var rx = dx + biasx + random_.Next() % randx;
            var ry = dy + biasy + random_.Next() % randy;
            SetCursorPosP(rx, ry);
            Thread.Sleep(ms_before_click_);
            MouseEvent((int)MouseEventFlags.RightDown, rx, ry);
            Thread.Sleep(ms_during_click_);
            MouseEvent((int)MouseEventFlags.RightUp, rx, ry);
            Thread.Sleep(ms_after_click_);
        }

        [Flags]
        enum MouseEventFlags
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }
        public bool SetCursorPosP(int x, int y)
        {
            return SetCursorPos(x, y);
        }
        private void MouseEvent(int dwFlags, int dx, int dy)
        {
            mouse_event(dwFlags, dx, dy, 0, 0);
        }

        public void KeyUp(string k)
        {
            throw new NotImplementedException();
        }

        public void KeyDown(string k)
        {
            throw new NotImplementedException();
        }

        public void MouseMove(UITreeNode node, UITreeNode root)
        {
            throw new NotImplementedException();
        }
    }
}

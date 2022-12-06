using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

using SBotCore;


namespace SBotCore
{
    public class InputMsg : IInput
    {
        public int main_window_handle_;
        readonly IDictionary<string, int> key_codes_ = new Dictionary<string, int>();

        static Random random = new();
        static int msduringClick = 40;
        static int randdurclick = 10;
        static int randx = 5;
        static int randy = 5;
        static int biasy = 1;
        static int biasx = 1;

        public InputMsg(int handle)
        {
            main_window_handle_ = handle;
            for (char i = 'a'; i <= 'z'; i++)
            {
                key_codes_.Add(i.ToString(), 0x41 + i - 'a');
            }
            key_codes_.Add("^", 0x11);
            for (int i = 1; i <= 8; i++)
            {
                key_codes_.Add("F" + i.ToString(), 0x70 + i - 1);
            }
        }


        public void KeyClick(string k)
        {
            if (key_codes_.ContainsKey(k))
            {
                SendMessage(main_window_handle_, (int)KeyboardInput.WM_KEYDOWN, key_codes_[k], 0);
                Thread.Sleep(msduringClick);
                SendMessage(main_window_handle_, (int)KeyboardInput.WM_KEYUP, key_codes_[k], 0);
                Thread.Sleep(msduringClick + random.Next() % randdurclick);
            }
        }

        public void KeyUp(string k)
        {
            if (key_codes_.ContainsKey(k))
            {

                SendMessage(main_window_handle_, (int)KeyboardInput.WM_KEYUP, key_codes_[k], 0);
                Thread.Sleep(msduringClick);
            }
        }

        public void KeyDown(string k)
        {
            if (key_codes_.ContainsKey(k))
            {

                SendMessage(main_window_handle_, (int)KeyboardInput.WM_KEYDOWN, key_codes_[k], 0);
                Thread.Sleep(msduringClick);
            }
        }

        public void MouseMove(UITreeNode node, UITreeNode root)
        {
            var (dx, dy) = InputHelper.ClientCoordinateofUITtreeNode(node, root);
            var rx = dx + biasx + random.Next() % randx;
            var ry = dy + biasy + random.Next() % randy;
            MouseMsgMove(rx, ry);
        }
        public void MouseClickLeft(UITreeNode node, UITreeNode root)
        {
            var (dx, dy) = InputHelper.ClientCoordinateofUITtreeNode(node, root);
            var rx = dx + biasx + random.Next() % randx;
            var ry = dy + biasy + random.Next() % randy;

            MouseMsgLBC(rx, ry);

        }
        public void MouseClickRight(UITreeNode node, UITreeNode root)
        {
            var (dx, dy) = InputHelper.ClientCoordinateofUITtreeNode(node, root);
            var rx = dx + biasx + random.Next() % randx;
            var ry = dy + biasy + random.Next() % randy;

            MouseMsgRBC(rx, ry);
        }

        private InputMsg()
        { }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SendMessage(int hWnd, int msg, int wParam, int lParam);



        public enum MouseInput : int
        {
            WM_NCHITTEST = 0x0084,
            WM_SETCURSOR = 0x0020,
            WM_MOUSEMOVE = 0x0200,
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205
        }
        public enum KeyboardInput : int
        {
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101
        }
        private void MouseMsgLBC(int dx, int dy)
        {
            int p = dy << 16 | dx;
            MouseMsgMove(dx, dy);
            SendMessage(main_window_handle_, (int)MouseInput.WM_LBUTTONDOWN, 0, p);
            Thread.Sleep(msduringClick + random.Next() % randdurclick);
            SendMessage(main_window_handle_, (int)MouseInput.WM_LBUTTONUP, 0, p);
            Thread.Sleep(msduringClick + random.Next() % randdurclick);
        }
        private void MouseMsgRBC(int dx, int dy)
        {
            int p = dy << 16 | dx;
            MouseMsgMove(dx, dy);
            SendMessage(main_window_handle_, (int)MouseInput.WM_RBUTTONDOWN, 0, p);
            Thread.Sleep(msduringClick + random.Next() % randdurclick);
            SendMessage(main_window_handle_, (int)MouseInput.WM_RBUTTONUP, 0, p);
            Thread.Sleep(msduringClick + random.Next() % randdurclick);
        }
        private void MouseMsgMove(int dx, int dy)
        {
            int p = dy << 16 | dx;
            SendMessage(main_window_handle_, (int)MouseInput.WM_MOUSEMOVE, 0, p);
            Thread.Sleep(msduringClick + random.Next() % randdurclick);
        }


    }
}

using Google.Protobuf;
using SBotCore;
using System.Data;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using static SBotCore.WinApi;

namespace SBotCore
{
    public class EveUITreeReaderCM : IEveUITreeReader
    {
        public EveUITreeReaderCM(byte[] injection_dll_bytes)
        {
            dll_data_=injection_dll_bytes;
        }
        private EveUITreeReaderCM() { }

        IntPtr proc_handle_, start_handle_, stop_handle_, dllmain_handle_;
        IntPtr alloc_mem_address_;
        byte[] dll_data_;

        MemoryMappedFile read_uitree_stats_;

        Semaphore sr_, sw_;
        ulong ra_ = 0;
        [DllImport("version.dll")]
        static extern ulong FindRootAddressEx(Int32 pid);
        private MemoryMappedViewAccessor controlBlockReader;
        bool debug = false;
        ulong readAll = 0;
        ulong readMode = 5;
        public ulong FindRootAddress(int pid)
        {
            if (File.Exists("prerelease"))
            {
                readMode = 5;
            }
            if (File.Exists("debug"))
            {
                readAll = 11212;
                debug = true;
            }
            string dllName = "data.dll";
            if (readMode == 5) dllName = "version.dll";
            string dll_name_ = System.AppContext.BaseDirectory + dllName;
            proc_handle_ = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, pid);

            // searching for the address of LoadLibraryA and storing it in a pointer
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // name of the dll we want to inject


            // alocating some memory on the target process - enough to store the name of the dll
            // and storing its address in a pointer
            alloc_mem_address_ = VirtualAllocEx(proc_handle_, IntPtr.Zero, (uint)((dll_name_.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

            // writing the name of the dll there
            WriteProcessMemory(proc_handle_, alloc_mem_address_, Encoding.Default.GetBytes(dll_name_), (uint)((dll_name_.Length + 1) * Marshal.SizeOf(typeof(char))), out _);

            // creating a thread that will call LoadLibraryA with allocMemAddress as argument
            dllmain_handle_ = CreateRemoteThread(proc_handle_, IntPtr.Zero, 0, loadLibraryAddr, alloc_mem_address_, 0, IntPtr.Zero);
            if (dllmain_handle_ == IntPtr.Zero)
            {
                return 0;
            }
            WaitForSingleObject(dllmain_handle_, 0xFFFFFFFF);
            sw_ = Semaphore.OpenExisting(pid.ToString() + "W");
            sr_ = Semaphore.OpenExisting(pid.ToString() + "R");
            string sznamebase = "Local\\";
            read_uitree_stats_ = MemoryMappedFile.CreateOrOpen(sznamebase + pid.ToString(), 1024);
            controlBlockReader = read_uitree_stats_.CreateViewAccessor();
            Stopwatch stopwatchRA = new();
            stopwatchRA.Start();
            //var raTask=Task.Run(() => ra_ = FindRootAddressEx(pid));
            //raTask.Wait(60000);
            //statsreader.Write(0, ra_);
            while (ra_ == 0)
            {
                ra_ = controlBlockReader.ReadUInt64(0);
                Thread.Sleep(100);
                if (stopwatchRA.ElapsedMilliseconds > 300_000) break;
            }
            sr_.WaitOne(1000);//FIX crashed consumer

            controlBlockReader.Write(48, readAll);
            controlBlockReader.Write(32, readMode);

            return ra_;
        }

        (ulong dur_read, ulong dur_trans, ulong byte_count) stat;

        public UITreeNode ReadUITree(int depth)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            UITreeNode res = null;
            var readTask = Task.Run(() =>
            {
                WaitHandle.SignalAndWait(sw_, sr_);

                var ra = controlBlockReader.ReadUInt64(0);
                var byte_count = controlBlockReader.ReadUInt64(8);
                var rta = controlBlockReader.ReadUInt64(16);
                var dur = controlBlockReader.ReadDouble(24);

                switch (readMode)
                {
                    case 3:
                        {
                            byte[] buf_ = new byte[byte_count];
                            controlBlockReader.ReadArray(1024, buf_, 0, (int)byte_count);

                            res = FromUITreeNodePB(UITreeNodePB.Parser.ParseFrom(buf_));
                        }
                        break;
                    case 4:
                        {
                            byte[] buf_ = new byte[byte_count];
                            var bytesRead = UIntPtr.Zero;
                            ReadProcessMemory(proc_handle_, rta, buf_, (UIntPtr)byte_count, ref bytesRead);
                            if (bytesRead.ToUInt64() == 0)
                            {
                                res = null;
                            }
                            res = FromUITreeNodePB(UITreeNodePB.Parser.ParseFrom(buf_));
                        }
                        break;
                    case 5:
                        {
                            byte[] buf_ = new byte[byte_count];
                            var bytesRead = UIntPtr.Zero;
                            ReadProcessMemory(proc_handle_, rta, buf_, (UIntPtr)byte_count, ref bytesRead);
                            if (bytesRead.ToUInt64() == 0)
                            {
                                res = null;
                            }
                            res = FromUITreeNodePB2211(UITreeNodePB2211.Parser.ParseFrom(buf_));
                        }
                        break;
                    default:
                        break;
                }
                stat.dur_read = (ulong)(dur * 1000);
                stat.dur_trans = (ulong)stopwatch.ElapsedMilliseconds;
                stat.byte_count = (ulong)(byte_count);
            });
            readTask.Wait(5000);

            stopwatch.Stop();

            return res;
        }

        public object Stat()
        {
            return stat;
        }

        static string GetStringFromCustomBytes(ByteString input)
        {
            var byte_array = input.ToByteArray();
            if (byte_array.Any())
            {
                if (input[0] == '0')
                {
                    return Encoding.ASCII.GetString(byte_array, 1, byte_array.Length - 1);
                }
                if (input[0] == '1')
                {
                    return Encoding.Unicode.GetString(byte_array, 1, byte_array.Length - 1);
                }
                if (input[0] == '2')
                {
                    return "";
                }
            }
            return "";
        }
        UITreeNode FromUITreeNodePB(UITreeNodePB tree_in)
        {
            var res = new UITreeNode
            {
                pythonObjectTypeName = tree_in.PythonObjectTypeName,
                dictEntriesOfInterest = new()
            };

            res.dictEntriesOfInterest.Add("_name", GetStringFromCustomBytes(tree_in.Name));
            res.dictEntriesOfInterest.Add("_text", GetStringFromCustomBytes(tree_in.Text));
            res.dictEntriesOfInterest.Add("_setText", GetStringFromCustomBytes(tree_in.SetText));
            res.dictEntriesOfInterest.Add("_hint", GetStringFromCustomBytes(tree_in.Hint));

            res.dictEntriesOfInterest.Add("_top", tree_in.Top);
            res.dictEntriesOfInterest.Add("_left", tree_in.Left);
            res.dictEntriesOfInterest.Add("_height", tree_in.Height);
            res.dictEntriesOfInterest.Add("_width", tree_in.Width);
            res.dictEntriesOfInterest.Add("_displayX", tree_in.DisplayX);
            res.dictEntriesOfInterest.Add("_displayY", tree_in.DisplayY);
            res.dictEntriesOfInterest.Add("_lastValue", tree_in.LastValue);
            res.dictEntriesOfInterest.Add("_selected", tree_in.Selected);
            res.dictEntriesOfInterest.Add("ramp_active", tree_in.Active);
            res.dictEntriesOfInterest.Add("isDeactivating", tree_in.IsDeactivating);
            res.dictEntriesOfInterest.Add("_display", tree_in.Display);
            res.dictEntriesOfInterest.Add("quantity", tree_in.Quantity);

            res.children = new UITreeNode[tree_in.Children.Count];
            for (int i = 0; i < tree_in.Children.Count; i++)
            {
                res.children[i] = FromUITreeNodePB(tree_in.Children[i]);
            }
            return res;

        }
        UITreeNode FromUITreeNodePB2211(UITreeNodePB2211 tree_in)
        {
            var res = new UITreeNode
            {
                pythonObjectAddress = tree_in.PythonObjectAddress,
                pythonObjectTypeName = tree_in.PythonObjectTypeName,
                dictEntriesOfInterest = new()
            };
            foreach(var f in tree_in.Fields)
            {
                switch (f.Value.KindCase)
                {
                    case Value.KindOneofCase.Int32Value:
                        res.dictEntriesOfInterest[f.Key] = f.Value.Int32Value;
                        break;
                    case Value.KindOneofCase.DoubleValue:
                        res.dictEntriesOfInterest[f.Key] = f.Value.DoubleValue;
                        break;
                    case Value.KindOneofCase.StringValue:
                        res.dictEntriesOfInterest[f.Key] = GetStringFromCustomBytes(f.Value.StringValue);
                        break;
                    case Value.KindOneofCase.BoolValue:
                        res.dictEntriesOfInterest[f.Key] = f.Value.BoolValue;
                        break;
                }
            }
            if (debug)
            {
                res.dictEntriesOfInterest= res.dictEntriesOfInterest.OrderBy(d => d.Key).ToDictionary(d => d.Key, d => d.Value);
            }
            res.children = new UITreeNode[tree_in.Children.Count];
            for (int i = 0; i < tree_in.Children.Count; i++)
            {
                res.children[i] = FromUITreeNodePB2211(tree_in.Children[i]);
            }
            return res;

        }
    }
}

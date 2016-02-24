/*
The MIT License (MIT)

Copyright (c) 2014 sarbian

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using KSP.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Resources = GCMonitor.Properties.Resources;

namespace GCMonitor
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class GCMonitor : MonoBehaviour
    {
        private const int width = 1024;
        private const int height = 512;

        const int GraphLabels = 4;
        const float labelSpace = 20f * (GraphLabels + 1) / GraphLabels; //fraction because we add Space 1 less time than we draw a Label

        private Rect windowPos = new Rect(80, 80, 400, 200);
        private Rect windowConfigPos = new Rect(80 + 410, 80, 200, 100);
        private Rect fpsPos = new Rect(10, 200, 80, 50);
        private bool showUI = false;
        private bool showConfUI = false;

        private bool hiddenUI = false;

        readonly Texture2D memoryTexture = new Texture2D(width, height);
        float ratio;

        private GUIStyle fpsLabelStyle;

        int timeScale = 1;

        bool killThread = false;

        memoryState[] memoryHistory = new memoryState[width];

        int activeSecond = 0;
        int previousActiveSecond = 0;
        int displayUpToSecond = 0;
        int lastDisplayedSecond = 0;

        bool fullUpdate = true;

        [Persistent]
        bool OnlyUpdateWhenDisplayed = true;
        [Persistent]
        bool memoryGizmo = true;
        [Persistent]
        bool realMemory = true;
        [Persistent]
        bool gpuMemory = false;
        [Persistent]
        bool relative = false;
        [Persistent]
        bool colorfulMode = false;
        [Persistent]
        bool useAppLauncher = false;
        [Persistent]
        double warnPercent = 0.90d;
        [Persistent]
        double alertPercent = 0.95d;

        [Persistent]
        int fpsSize = 10;

        [Persistent]
        bool displayMem = true;

        [Persistent]
        bool displayMemRss = false;

        [Persistent]
        bool displayPeakRss = false;

        [Persistent]
        bool displayGpu = true;

        [Persistent]
        bool displayFps = true;

        [Persistent]
        float fpsX = 10;
        [Persistent]
        float fpsY = 200;

        private IButton tbButton;
        private ApplicationLauncherButton alButton;

        long displayMaxMemory = 200;
        long displayMinMemory = 0;
        static ulong peakMemory;
        long topMemory;

        private readonly float updateInterval = 4;

        private float dt = 0;
        private int frameCount = 0;
        private float updateRate;
        private float fps;
        private string fpsString = string.Empty;

        private static Stopwatch watch;

        int lastColCount = 0;

        Texture2D cat;
        Color[] catPixels;
        Color[] blackSquare;
        Color[] blackLine;
        Color[] line;

        private static NumberFormatInfo spaceFormat;

        Color color = new Color();

        struct memoryState
        {
            public long min;
            public long max;
            public ulong rss;
            public long gc;
            public long gpu;
        }

        struct processMemory
        {
            public ulong rss;
            public ulong vsz;
            public ulong max;

            public processMemory(ulong rss, ulong vsz, ulong max)
            {
                this.rss = rss;
                this.vsz = vsz;
                this.max = max;
            }
        }

        //// Windows x86
        //private const string kDllPath_Win_x86 = "GameData/GCMonitor/getRSS_x86.dll";
        //[DllImport(dllName: kDllPath_Win_x86, EntryPoint = "getCurrentVM")]
        //private static extern UIntPtr getCurrentVM_Win_x86();
        //[DllImport(dllName: kDllPath_Win_x86, EntryPoint = "getMaximumVM")]
        //private static extern UIntPtr getMaximumVM_Win_x86();
        //
        //// Windows x64
        //private const string kDllPath_Win_x64 = "GameData/GCMonitor/getRSS_x64.dll";
        //[DllImport(dllName: kDllPath_Win_x64, EntryPoint = "getCurrentVM")]
        //private static extern UIntPtr getCurrentVM_Win_x64();
        //[DllImport(dllName: kDllPath_Win_x64, EntryPoint = "getMaximumVM")]
        //private static extern UIntPtr getMaximumVM_Win_x64();

        [DllImport("psapi.dll")]
        private static extern int GetProcessMemoryInfo(IntPtr hProcess, [Out] PROCESS_MEMORY_COUNTERS counters, int size);

        [StructLayout(LayoutKind.Sequential)]
        private class PROCESS_MEMORY_COUNTERS
        {
            public uint cb;
            public uint PageFaultCount;
            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer); // use of [In, Out] is voluntary. see http://www.pinvoke.net/default.aspx/kernel32/GlobalMemoryStatusEx.html

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        private static MEMORYSTATUSEX msex;
        private static PROCESS_MEMORY_COUNTERS pmc;

        private static processMemory getProcessMemoryWin()
        {
            GlobalMemoryStatusEx(msex);

            GetProcessMemoryInfo(new IntPtr(-1), pmc, Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS)));
            return new processMemory(pmc.WorkingSetSize.ToUInt64(), msex.ullTotalVirtual - msex.ullAvailVirtual, msex.ullTotalVirtual);
        }
        
        // next struct and method from https://github.com/dotnet/corefx/blob/master/src/Common/src/Interop/Linux/procfs/Interop.ProcFsStat.cs
        
        internal struct ParsedStat
        {
            // Commented out fields are available in the stat data file but 
            // are currently not used.  If/when needed, they can be uncommented,
            // and the corresponding entry can be added back to StatParser, replacing
            // the MoveNext() with the appropriate ParseNext* call and assignment.

            internal int pid;
            internal string comm;
            internal char state;
            //internal int ppid;
            //internal int pgrp;
            internal int session;
            //internal int tty_nr;
            //internal int tpgid;
            //internal uint flags;
            //internal ulong minflt;
            //internal ulong cminflt;
            //internal ulong majflt;
            //internal ulong cmajflt;
            internal ulong utime;
            internal ulong stime;
            //internal long cutime;
            //internal long cstime;
            //internal long priority;
            internal long nice;
            //internal long num_threads;
            //internal long itrealvalue;
            internal ulong starttime;
            internal ulong vsize;
            internal long rss;
            internal ulong rsslim;
            //internal ulong startcode;
            //internal ulong endcode;
            internal ulong startstack;
            //internal ulong kstkesp;
            //internal ulong kstkeip;
            //internal ulong signal;
            //internal ulong blocked;
            //internal ulong sigignore;
            //internal ulong sigcatch;
            //internal ulong wchan;
            //internal ulong nswap;
            //internal ulong cnswap;
            //internal int exit_signal;
            //internal int processor;
            //internal uint rt_priority;
            //internal uint policy;
            //internal ulong delayacct_blkio_ticks;
            //internal ulong guest_time;
            //internal long cguest_time;
        }

        private const string SelfStat = "/proc/self/stat";

        private static void ParseStatFile()
        {
            string statFileContents = System.IO.File.ReadAllText(SelfStat);

            var parser = new StringParser(statFileContents, ' ');

            linuxStat.pid = parser.ParseNextInt32();
            linuxStat.comm = parser.ParseRaw(delegate (string str, ref int startIndex, ref int endIndex)
            {
                if (str[startIndex] == '(')
                {
                    int i;
                    for (i = endIndex; i < str.Length && str[i - 1] != ')'; i++) ;
                    if (str[i - 1] == ')')
                    {
                        endIndex = i;
                        return str.Substring(startIndex + 1, i - startIndex - 2);
                    }
                }
                throw new InvalidDataException();
            });
            linuxStat.state = parser.ParseNextChar();
            parser.MoveNextOrFail(); // ppid
            parser.MoveNextOrFail(); // pgrp
            linuxStat.session = parser.ParseNextInt32();
            parser.MoveNextOrFail(); // tty_nr
            parser.MoveNextOrFail(); // tpgid
            parser.MoveNextOrFail(); // flags
            parser.MoveNextOrFail(); // majflt
            parser.MoveNextOrFail(); // cmagflt
            parser.MoveNextOrFail(); // minflt
            parser.MoveNextOrFail(); // cminflt
            linuxStat.utime = parser.ParseNextUInt64();
            linuxStat.stime = parser.ParseNextUInt64();
            parser.MoveNextOrFail(); // cutime
            parser.MoveNextOrFail(); // cstime
            parser.MoveNextOrFail(); // priority
            linuxStat.nice = parser.ParseNextInt64();
            parser.MoveNextOrFail(); // num_threads
            parser.MoveNextOrFail(); // itrealvalue
            linuxStat.starttime = parser.ParseNextUInt64();
            linuxStat.vsize = parser.ParseNextUInt64();
            linuxStat.rss = parser.ParseNextInt64();
            linuxStat.rsslim = parser.ParseNextUInt64();
            parser.MoveNextOrFail(); // startcode
            parser.MoveNextOrFail(); // endcode
            linuxStat.startstack = parser.ParseNextUInt64();
            parser.MoveNextOrFail(); // kstkesp
            parser.MoveNextOrFail(); // kstkeip
            parser.MoveNextOrFail(); // signal
            parser.MoveNextOrFail(); // blocked
            parser.MoveNextOrFail(); // sigignore
            parser.MoveNextOrFail(); // sigcatch
            parser.MoveNextOrFail(); // wchan
            parser.MoveNextOrFail(); // nswap
            parser.MoveNextOrFail(); // cnswap
            parser.MoveNextOrFail(); // exit_signal
            parser.MoveNextOrFail(); // processor
            parser.MoveNextOrFail(); // rt_priority
            parser.MoveNextOrFail(); // policy
            parser.MoveNextOrFail(); // delayacct_blkio_ticks
            parser.MoveNextOrFail(); // guest_time
            parser.MoveNextOrFail(); // cguest_time
        }

        private static ParsedStat linuxStat;

        private static processMemory getProcessMemoryLinux()
        {
            ParseStatFile();
            return new processMemory((ulong)linuxStat.rss, linuxStat.vsize, linuxStat.rsslim);
        }

        // OSX
        private struct time_value
        {
            public int seconds;
            public int microseconds;
        }

        private struct task_basic_info
        {
            public int suspend_count;
            public IntPtr virtual_size;
            public IntPtr resident_size;
            public time_value user_time;
            public time_value system_time;
            public int policy;
        } 
        
        private enum task_flavor_t
        {
            TASK_BASIC_INFO_32 = 4,
            TASK_BASIC_INFO_64 = 5,
        }

        [DllImport("libc.dylib")]
        private static extern IntPtr mach_task_self();

        [DllImport("libc.dylib")]
        private static extern int task_info(IntPtr task, task_flavor_t flavor, IntPtr taskInfoStructure, ref int structureLength);

        private static int structureLengthOSX;
        private static task_flavor_t taskFlavorOSX;

        private static unsafe processMemory getProcessMemoryOSX()
        {
            var resultOSX = new task_basic_info();
            task_info(mach_task_self(), taskFlavorOSX, new IntPtr(&resultOSX), ref structureLengthOSX);
            return new processMemory((ulong)resultOSX.resident_size, (ulong)resultOSX.virtual_size, maxAllowedMem);
        }

        public static ulong warnMem;
        public static ulong alertMem;
        public static string memoryVszString;
        public static ulong memoryVsz;
        public static string memoryRssString;
        public static string memoryPeakRssString;
        public static string gpuMemoryRssString;
        public static ulong memoryRss;
        public static ulong maxAllowedMem;

        private static processMemory getProcessMemory_unimplemented()
        {
            return new processMemory(0, 0, 0);
        }

        private delegate processMemory GetProcessMemory();
        private GetProcessMemory getProcessMemory;

        internal void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (File.Exists<GCMonitor>("GCMonitor.cfg"))
            {
                ConfigNode config = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "GCMonitor.cfg"));
                ConfigNode.LoadObjectFromConfig(this, config);
            }

            fpsPos.Set(fpsX, fpsY, 200, 50);

            updateRate = updateInterval;

            line = new Color[height];
            blackLine = new Color[height];
            for (int i = 0; i < blackLine.Length; i++)
                blackLine[i] = Color.black;

            cat = new Texture2D(33, 20, TextureFormat.ARGB32, false);
            cat.LoadImage(Resources.cat);
            catPixels = cat.GetPixels();
            blackSquare = new Color[32 * height];
            for (int i = 0; i < blackSquare.Length; i++)
                blackSquare[i] = Color.black;

            spaceFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            spaceFormat.NumberGroupSeparator = " ";

            Debug.Log("[GCMonitor] Setting up getRSS delegates");
            switch (Application.platform)
            {
                case RuntimePlatform.LinuxPlayer:
                    linuxStat = new ParsedStat();
                    getProcessMemory = getProcessMemoryLinux;
                    break;
                case RuntimePlatform.OSXPlayer:
                    taskFlavorOSX = IsX64() ? task_flavor_t.TASK_BASIC_INFO_64 : task_flavor_t.TASK_BASIC_INFO_32;
                    structureLengthOSX = Marshal.SizeOf(typeof(task_basic_info)) / IntPtr.Size;
                    getProcessMemory = getProcessMemoryOSX;
                    break;
                case RuntimePlatform.WindowsPlayer:
                    msex = new MEMORYSTATUSEX();
                    pmc = new PROCESS_MEMORY_COUNTERS();
                    getProcessMemory = getProcessMemoryWin;
                    InitGpuMonitor();
                    break;
                default:
                    getProcessMemory = getProcessMemory_unimplemented;
                    break;
            }

            try
            {
                processMemory p = getProcessMemory();
            }
            catch (Exception e)
            {
                Debug.Log("[GCMonitor] Unable to find getRSS implementation\n" + e);
                getProcessMemory = getProcessMemory_unimplemented;
            }

            
            maxAllowedMem = IsX64() ?  ((ulong)SystemInfo.systemMemorySize) << 20 : uint.MaxValue;

            
            if (!IsX64() && Application.platform == RuntimePlatform.WindowsPlayer && getProcessMemory().max != 0)
            {
                maxAllowedMem = getProcessMemory().max;
                Debug.Log("[GCMonitor] Maximum usable memoryVsz " + ConvertToGBString(maxAllowedMem) + " / " + ConvertToMBString(maxAllowedMem));
            }

            warnMem = (ulong)(maxAllowedMem * warnPercent);
            alertMem = (ulong)(maxAllowedMem * alertPercent);

            GameEvents.onShowUI.Add(ShowGUI);
            GameEvents.onHideUI.Add(HideGUI);
            
            watch = new Stopwatch();
            watch.Start();
            // Does not exist on our mono version :(
            //GC.RegisterForFullGCNotification(10, 10);
            Thread threadGCCollector = new Thread(GCCollector);
            threadGCCollector.Start();
        }

        internal void OnDestroy()
        {
            killThread = true;

            GameEvents.onShowUI.Remove(ShowGUI);
            GameEvents.onHideUI.Remove(HideGUI);

            ConfigNode node = new ConfigNode("GCMonitor");
            ConfigNode.CreateConfigFromObject(this, node);
            node.Save(IOUtils.GetFilePathFor(this.GetType(), "GCMonitor.cfg"));
        }

        private static bool IsX64()
        {
            return (IntPtr.Size == 8);
        }

        enum MemState
        {
            NORMAL,
            WARNING,
            ALERT1,
            ALERT2
        }

        private bool rmb;
        private bool lmb;

        private MemState activeMemState;

        
        public void Update()
        {
            frameCount++;
            //dt += Time.timeScale / Time.deltaTime;
            dt += Time.deltaTime;

            if (dt > 1.0f / updateRate)
            {
                fps = frameCount / dt;
                fpsString = fps.ToString("##0.0") + " FPS";
                frameCount = 0;
                dt -= 1.0f / updateRate;
            }

            processMemory memory = getProcessMemory();
            memoryVsz = memory.vsz;
            memoryVszString = ConvertToMBString(memoryVsz);

            memoryRss = memory.rss;
            memoryRssString = ConvertToMBString(memoryRss);

            if (memoryVsz > peakMemory)
                peakMemory = memoryVsz;
            memoryPeakRssString = ConvertToMBString(peakMemory);

            if (adapter != null)
            {
                adapter.UpdateValues();
                gpuMemoryRssString = ConvertToMBString(adapter.DedicatedVramUsage);
            }

            lmb = lmb | Input.GetMouseButtonDown(0);
            rmb = rmb | Input.GetMouseButtonDown(1);

            UpdateButton();

            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F1))
            {
                showUI = !showUI;
            }

            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F))
            {
                memoryGizmo = !memoryGizmo;
            }

            if (!showUI)
                return;

            long newdisplayMaxMemory = displayMaxMemory;
            long newdisplayMinMemory = displayMinMemory;

            if (fullUpdate)
            {
                newdisplayMaxMemory = newdisplayMinMemory = 0;
            }

            long maxMemory = 0;
            long minMemory = 0;
            for (int i = 0; i < memoryHistory.Length; i++)
            {
                long mem = realMemory ? (long)memoryHistory[i].rss : (gpuMemory ? memoryHistory[i].gpu : memoryHistory[i].max);

                if (relative)
                {
                    int index = i != 0 ? (i - 1) : memoryHistory.Length - 1;
                    long pmem = realMemory ? (long)memoryHistory[index].rss : (gpuMemory ? memoryHistory[index].gpu : memoryHistory[index].max);
                    mem = pmem == 0 ? 0 : mem - pmem;
                }
                
                if (mem > maxMemory)
                    maxMemory = mem;
                if (mem < minMemory)
                    minMemory = mem;
            }

            if ((maxMemory * 1.1f) > newdisplayMaxMemory)
            {
                newdisplayMaxMemory = (long)(maxMemory * 1.2f);
            }

            if ((minMemory * 1.1f) < newdisplayMinMemory)
            {
                newdisplayMinMemory = (long)(minMemory * 1.2f);
            }

            if (relative)
            {
                newdisplayMaxMemory = Math.Min(Math.Max(newdisplayMaxMemory, -newdisplayMinMemory), 1024 * 1024 * 5);
                newdisplayMinMemory = Math.Max(Math.Min(newdisplayMinMemory, -newdisplayMaxMemory), -1024 * 1024 * 5);
            }

            if (newdisplayMaxMemory != displayMaxMemory || newdisplayMinMemory != displayMinMemory)
            {
                displayMaxMemory = newdisplayMaxMemory;
                displayMinMemory = newdisplayMinMemory;
                fullUpdate = true;
            }

            if (fullUpdate)
            {
                fullUpdate = false;
                ratio = (float)memoryTexture.height / (displayMaxMemory - displayMinMemory);
                UpdateTexture(displayUpToSecond);
            }
            else
            {
                // To avoid problem with the thread updating
                // displayUpToSecond while we're in the while loop
                int localdisplayUpToSecond = displayUpToSecond;
                if (lastDisplayedSecond != localdisplayUpToSecond)
                {
                    while (lastDisplayedSecond != localdisplayUpToSecond)
                    {
                        lastDisplayedSecond = (lastDisplayedSecond + 1) % width;
                        UpdateTexture(lastDisplayedSecond, localdisplayUpToSecond);

                    }
                    memoryTexture.Apply();
                }
            }
        }

        private void UpdateButton()
        {

            MemState state;

            if (memoryVsz < warnMem)
            {
                state = MemState.NORMAL;
            }
            else if (memoryVsz < alertMem)
            {
                state = MemState.WARNING;
            }
            else
            {
                if (Mathf.FloorToInt(Time.realtimeSinceStartup) % 2 == 0)
                {
                    state = MemState.ALERT1;
                }
                else
                {
                    state = MemState.ALERT2;
                }
            }

            if (tbButton == null && ToolbarManager.ToolbarAvailable)
            {
                print("Toolbar config");
                tbButton = ToolbarManager.Instance.add("GCMonitor", "GCMonitor");
                tbButton.ToolTip = "GCMonitor";
                tbButton.TexturePath = "GCMonitor/GCMonitor24N";
                tbButton.OnClick += tbButtonClick;
                tbButton.Visible = true;
            }

            if (useAppLauncher && alButton == null && ApplicationLauncher.Ready)
            {
                print("Launcher config");

                Texture2D buttonTexture = GameDatabase.Instance.GetTexture("GCMonitor/GCMonitor38N", false);

                alButton = ApplicationLauncher.Instance.AddModApplication(
                    alButtonClick, alButtonClick,
                    alHover, alHover,
                    null, null,
                    ApplicationLauncher.AppScenes.ALWAYS,
                    buttonTexture);
            }

            if (!useAppLauncher && alButton != null)
            {
                ApplicationLauncher.Instance.RemoveApplication(alButton);
            }

            if (tbButton != null)
            {
                tbButton.ToolTip = memoryVszString;
            }

            if (state == activeMemState)
                return;

            activeMemState = state;

            switch (state)
            {
                case MemState.NORMAL:
                    updateLauncher("GCMonitor/GCMonitor38N");
                    updateToolBar("GCMonitor/GCMonitor24N");
                    break;
                case MemState.WARNING:
                    updateLauncher("GCMonitor/GCMonitor38W");
                    updateToolBar("GCMonitor/GCMonitor24W");
                    break;
                case MemState.ALERT1:
                    updateLauncher("GCMonitor/GCMonitor38A1");
                    updateToolBar("GCMonitor/GCMonitor24A1");
                    break;
                case MemState.ALERT2:
                    updateLauncher("GCMonitor/GCMonitor38A2");
                    updateToolBar("GCMonitor/GCMonitor24A2");
                    break;
            }
        }

        private void tbButtonClick(ClickEvent e)
        {
            if (e.MouseButton == 0)
                memoryGizmo = !memoryGizmo;
            if (e.MouseButton == 1)
                showUI = !showUI;
        }

        private void alHover()
        {
            lmb = false;
            rmb = false;
        }

        private void alButtonClick()
        {
            if (lmb)
                memoryGizmo = !memoryGizmo;
            if (rmb)
                showUI = !showUI;
        }

        private void updateToolBar(string texture)
        {
            if (tbButton == null)
                return;
            tbButton.TexturePath = texture;
        }

        private void updateLauncher(string texture)
        {
            if (alButton == null)
                return;

            Texture2D buttonTexture = GameDatabase.Instance.GetTexture(texture, false);
            alButton.SetTexture(buttonTexture);
        }

        public void memoryHistoryUpdate()
        {

            activeSecond = Mathf.FloorToInt(watch.ElapsedMilliseconds * 0.001f * timeScale) % width;

            long mem = GC.GetTotalMemory(false);

            if (activeSecond != previousActiveSecond)
            {
                memoryHistory[activeSecond].min = long.MaxValue;
                memoryHistory[activeSecond].max = 0;
                memoryHistory[activeSecond].rss = 0;

                // GC.MaxGeneration is always 0 on our current mono
                // Sum all gen if it ever changes
                int colCount = GC.CollectionCount(GC.MaxGeneration);

                memoryHistory[previousActiveSecond].gc = colCount - lastColCount;
                lastColCount = colCount;
                displayUpToSecond = previousActiveSecond;
                previousActiveSecond = activeSecond;
            }

            if (mem < memoryHistory[activeSecond].min)
                memoryHistory[activeSecond].min = mem;
            if (mem > memoryHistory[activeSecond].max)
                memoryHistory[activeSecond].max = mem;

            ulong rss = getProcessMemory().vsz;
            memoryHistory[activeSecond].rss = rss;

            if (rss > peakMemory)
                peakMemory = rss;

            if (adapter != null)
            {
                adapter.UpdateValues();
                memoryHistory[activeSecond].gpu = adapter.DedicatedVramUsage;
            }

        }

        private void ShowGUI()
        {
            hiddenUI = false;
        }
        private void HideGUI()
        {
            hiddenUI = true;
        }

        private void GCCollector()
        {
            while (true)
            {
                if (!OnlyUpdateWhenDisplayed || showUI)
                {
                    memoryHistoryUpdate();
                }
                // Removed since those does not work under our mono version :(
                // Keep commented in hope we get a mono upgrade after U5
                //while (loopThread)
                //{
                //    GCNotificationStatus error = GC.WaitForFullGCApproach();
                //    if (error == GCNotificationStatus.Succeeded)
                //    {
                //        memoryHistoryUpdate();
                //    }
                //    else
                //    {
                //        break;
                //    }
                //
                //    error = GC.WaitForFullGCComplete();
                //    if (error == GCNotificationStatus.Succeeded)
                //    {
                //        memoryHistory[activeSecond].gc++;
                //        memoryHistoryUpdate();
                //    }
                //    else
                //    {
                //        break;
                //    }
                //}

                // Since windows time make this sleep way more I could
                // remove it or use 0. Need testing on a slower CPU.
                Thread.Sleep(5);

                if (killThread)
                {
                    break;
                }
            }

        }


        // TODO Convert texture display to GL.quads
        // http://docs.unity3d.com/ScriptReference/GL.QUADS.html
        // http://docs.unity3d.com/ScriptReference/GL.LoadPixelMatrix.html

        private void UpdateTexture(int last)
        {
            for (int x = 0; x < memoryTexture.width; x++)
            {
                UpdateTexture(x, last);
            }
            memoryTexture.Apply();
        }

        private void UpdateTexture(int x, int last)
        {
            int min = Mathf.RoundToInt(ratio * (realMemory ? (long)memoryHistory[x].rss : (gpuMemory ? memoryHistory[x].gpu : memoryHistory[x].min)));
            int max = Mathf.RoundToInt(ratio * (realMemory ? (long)memoryHistory[x].rss : (gpuMemory ? memoryHistory[x].gpu : memoryHistory[x].max)));

            int zero = -Mathf.RoundToInt(ratio * displayMinMemory);
            if (relative)
            {
                int index = x != 0 ? (x - 1) : memoryHistory.Length - 1;
                int pmem = Mathf.RoundToInt(ratio * (realMemory ? memoryHistory[index].rss : (gpuMemory ? (ulong)memoryHistory[index].gpu : (ulong)memoryHistory[index].max)));
                max = pmem == 0 ? 0 : max - pmem;
                max = max + zero;
            }

            int xPlus1 = (x + 1) % width;
            int xPlus2 = (x + 2) % width;

            if (!colorfulMode)
            {
                for (int y = 0; y < memoryTexture.height; y++)
                {
                    if (!relative)
                    {
                        if (!(realMemory || gpuMemory) && y < 10 * memoryHistory[x].gc)
                            color = Color.red;
                        else if (y <= min && max != 0)
                            color = Color.grey;
                        else if (y <= max && max != 0)
                            color = Color.green;
                        else
                            color = Color.black;
                    }
                    else
                    {
                        if (y < zero && y >= max)
                            color = Color.green;
                        else if (y > zero && y <= max)
                            color = Color.red;
                        else if (y == zero)
                            color = Color.grey;
                        else
                            color = Color.black;
                    }

                    line[y] = color;
                }

                memoryTexture.SetPixels(x, 0, 1, height, line);
                if (x == last)
                {
                    memoryTexture.SetPixels(xPlus1, 0, 1, height, blackLine);
                    memoryTexture.SetPixels(xPlus2, 0, 1, height, blackLine);
                }
            }
            else
            {
                memoryTexture.SetPixels(x, 0, cat.width - 1, height, blackSquare);
                if (max != 0)
                    memoryTexture.SetPixels(x, max, cat.width, cat.height, catPixels);
            }
        }

        public void OnGUI()
        {
            if (memoryGizmo && !hiddenUI)
            {
                if (fpsLabelStyle == null)
                    fpsLabelStyle = new GUIStyle(GUI.skin.label);

                fpsLabelStyle.fontSize = fpsSize;

                Vector2 size = fpsLabelStyle.CalcSize(new GUIContent(memoryVszString));

                fpsX = Mathf.Clamp(fpsX, 0, Screen.width);
                fpsY = Mathf.Clamp(fpsY, 0, Screen.height);

                fpsPos.Set(fpsX, fpsY, 200, size.y);
                if (displayMem)
                {
                    DrawOutline(fpsPos, memoryVszString, 1, fpsLabelStyle, Color.black,
                        memoryVsz > alertMem ? Color.red : memoryVsz > warnMem ? XKCDColors.Orange : Color.white);
                    fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 200, size.y);
                }

                if (displayMemRss)
                {
                    DrawOutline(fpsPos, memoryRssString, 1, fpsLabelStyle, Color.black, Color.white);
                    fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 300, size.y);
                }

                if (displayPeakRss)
                {
                    DrawOutline(fpsPos, memoryPeakRssString, 1, fpsLabelStyle, Color.black, Color.white);
                    fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 300, size.y);
                }

                if (displayGpu)
                {
                    DrawOutline(fpsPos, gpuMemoryRssString, 1, fpsLabelStyle, Color.black, Color.white);
                    fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 300, size.y);
                }

                if (displayFps)
                {
                    DrawOutline(fpsPos, fpsString, 1, fpsLabelStyle, Color.black, Color.white);
                }
            }

            if (showUI && !hiddenUI)
            {
                windowPos = GUILayout.Window(8785478, windowPos, WindowGUI, "GCMonitor", GUILayout.Width(420), GUILayout.Height(220));
            }

            if (showConfUI & showUI && !hiddenUI)
            {
                windowConfigPos.Set(windowPos.xMax + 10, windowPos.yMin, windowConfigPos.width, windowConfigPos.height);
                windowConfigPos = GUILayout.Window(8785479, windowConfigPos, WindowConfigGUI, "Config", GUILayout.Width(80), GUILayout.Height(50));
            }
        }

        public void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            GUILayout.Label("Precision ", GUILayout.ExpandWidth(false));
            // We could collect at x8 speed and then aggregate
            // for the asked display level. But lazy
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                if (timeScale > 1)
                {
                    timeScale = timeScale / 2;
                    memoryHistory = new memoryState[width];
                    fullUpdate = true;
                }
            }
            GUILayout.Label((1f / timeScale).ToString("0.###") + "s", GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (timeScale < 8)
                {
                    timeScale = timeScale * 2;
                    memoryHistory = new memoryState[width];
                    fullUpdate = true;
                }
            }
            GUILayout.Space(30);

            bool preveReal = realMemory;
            realMemory = GUILayout.Toggle(realMemory, "KSP process", GUILayout.ExpandWidth(false));
            if (preveReal != realMemory)
            {
                fullUpdate = true;
                if (gpuMemory)
                    gpuMemory = false;
            }

            bool preveGpu = gpuMemory;
            gpuMemory = adapter != null && GUILayout.Toggle(gpuMemory, "GPU", GUILayout.ExpandWidth(false));
            if (preveGpu != gpuMemory)
            {
                fullUpdate = true;
                if (realMemory)
                    realMemory = false;
            }

            bool preveRel = relative;
            relative = GUILayout.Toggle(relative, "Relative Mode", GUILayout.ExpandWidth(false));
            if (preveRel != relative)
                fullUpdate = true;

            bool prevColor = colorfulMode;
            colorfulMode = GUILayout.Toggle(colorfulMode, "More Color Mode", GUILayout.ExpandWidth(false));
            if (colorfulMode)
                timeScale = 10;

            if (prevColor != colorfulMode)
                fullUpdate = true;

            OnlyUpdateWhenDisplayed = GUILayout.Toggle(OnlyUpdateWhenDisplayed, "Only Update When Display is visible", GUILayout.ExpandWidth(false));

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            processMemory mem = getProcessMemory();

            GUILayout.Label("KSP: " + ConvertToKBString(mem.vsz) + " / " + ConvertToKBString(maxAllowedMem), GUILayout.ExpandWidth(false));
            if (adapter != null)
                GUILayout.Label("GPU: " + ConvertToKBString(adapter.DedicatedVramUsage) + " / " + ConvertToKBString(adapter.DedicatedVramLimit), GUILayout.ExpandWidth(false));

            GUILayout.Space(20);

            GUILayout.Label(
                "Mono allocated:" + ConvertToMBString(Profiler.GetTotalAllocatedMemory())
                + " min: " + ConvertToMBString(memoryHistory[activeSecond].min)
                + " max: " + ConvertToMBString(memoryHistory[activeSecond].max)
                + " GC : " + memoryHistory[previousActiveSecond].gc.ToString(), GUILayout.ExpandWidth(false));

            GUILayout.Space(20);

            GUILayout.Label("FPS: " + fps.ToString("0.0"), GUILayout.ExpandWidth(false));

            GUILayout.Space(20);

            if (GUILayout.Button("Top", GUILayout.ExpandWidth(false)))
            {
                topMemory = (long) memoryVsz;
            }

            GUILayout.Label("Since top: " + (topMemory != 0 ? ConvertToKBString(((long)memoryVsz - topMemory)) : "0"), GUILayout.ExpandWidth(true));


            GUILayout.Space(20);

            if (GUILayout.Button("Config"))
                showConfUI = !showConfUI;

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Height(height));

            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(ConvertToMBString(displayMaxMemory)));

            GUILayout.BeginVertical(GUILayout.MinWidth(size.x));

            for (int i = 0; i <= GraphLabels; i++)
            {
                GUILayout.Label(ConvertToMBString(displayMaxMemory - (displayMaxMemory - displayMinMemory) * i / GraphLabels), new GUIStyle(GUI.skin.label) { wordWrap = false });
                if (i != GraphLabels) //only do it if it's not the last one
                    GUILayout.Space(height / GraphLabels - labelSpace);
            }
            GUILayout.EndVertical();

            GUILayout.Box(memoryTexture);

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public void WindowConfigGUI(int windowID)
        {
            GUILayout.BeginVertical();

            useAppLauncher = GUILayout.Toggle(useAppLauncher, "Display Launcher Icon", GUILayout.ExpandWidth(false));
            memoryGizmo = GUILayout.Toggle(memoryGizmo, "Display KSP memoryVsz and FPS", GUILayout.ExpandWidth(false));

            GUILayout.BeginHorizontal();
            displayFps = GUILayout.Toggle(displayFps, "FPS");
            displayMem = GUILayout.Toggle(displayMem, "Memory");
            displayMemRss = GUILayout.Toggle(displayMemRss, "Memory (RSS)");
            displayPeakRss = GUILayout.Toggle(displayPeakRss, "Peak");
            if (displayPeakRss)
                OnlyUpdateWhenDisplayed = true;
            GUILayout.EndHorizontal();

            displayGpu = adapter != null && GUILayout.Toggle(displayGpu, "Memory (GPU)");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Size", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                fpsSize--;
            GUILayout.Label(fpsSize.ToString(), GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                fpsSize++;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("X", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                fpsX = fpsX - 5;
            GUILayout.Label(fpsPos.xMin.ToString("F0"), GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                fpsX = fpsX + 5;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                fpsY = fpsY - 5;
            GUILayout.Label(fpsPos.yMin.ToString("F0"), GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                fpsY = fpsY + 5;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            fpsX = Mathf.Clamp(fpsX, 0, Screen.width);
            fpsY = Mathf.Clamp(fpsY, 0, Screen.height);

        }

        static String ConvertToGBString(long bytes)
        {
            return (bytes / 1024f / 1024f / 1024f).ToString("#,0.0", spaceFormat) + " GB";
        }

        static String ConvertToGBString(ulong bytes)
        {
            return ((bytes >> 20) / 1024f).ToString("#,0.0", spaceFormat) + " GB";
        }

        static String ConvertToMBString(long bytes)
        {
            return (bytes / 1024f / 1024f).ToString("#,0", spaceFormat) + " MB";
        }

        static String ConvertToMBString(ulong bytes)
        {
            return (bytes >> 20).ToString("#,0", spaceFormat) + " MB";
        }

        static String ConvertToKBString(long bytes)
        {
            return (bytes / 1024f).ToString("#,0", spaceFormat) + " kB";
        }

        static String ConvertToKBString(ulong bytes)
        {
            return (bytes >> 10).ToString("#,0", spaceFormat) + " kB";
        }

        void DrawOutline(Rect r, string t, int strength, GUIStyle style, Color outColor, Color inColor)
        {
            Color backup = style.normal.textColor;
            style.normal.textColor = outColor;
            for (int i = -strength; i <= strength; i++)
            {
                GUI.Label(new Rect(r.x - strength, r.y + i, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + strength, r.y + i, r.width, r.height), t, style);
            }
            for (int i = -strength + 1; i <= strength - 1; i++)
            {
                GUI.Label(new Rect(r.x + i, r.y - strength, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + i, r.y + strength, r.width, r.height), t, style);
            }
            style.normal.textColor = inColor;
            GUI.Label(r, t, style);
            style.normal.textColor = backup;
        }

        public new static void print(object message)
        {
            MonoBehaviour.print("[GCMonitor] " + message);
        }
        
        public void InitGpuMonitor()
        {
            try
            {
                print("Getting Adapters info");

                D3DKMT_ENUMADAPTERS d3DkmtEnumadapters = new D3DKMT_ENUMADAPTERS();

                IntPtr dkmtEnumadapters = Marshal.AllocHGlobal(Marshal.SizeOf(d3DkmtEnumadapters)); //Allocate unmanaged Memory
                Marshal.StructureToPtr(d3DkmtEnumadapters, dkmtEnumadapters, true);

                if (D3DKMT.Nt_Success(D3DKMT.D3DKMTEnumAdapters(dkmtEnumadapters)))
                {
                    d3DkmtEnumadapters =
                        (D3DKMT_ENUMADAPTERS) Marshal.PtrToStructure(dkmtEnumadapters, typeof (D3DKMT_ENUMADAPTERS));
                    
                    // Look for the adapter with the highest memory Usage and hope this is the one we want
                    long max = 0;
                    for (int i = 0; i < d3DkmtEnumadapters.Adapters.Length; i++)
                    {
                        Adapter a = new Adapter(d3DkmtEnumadapters.Adapters[i].AdapterLuid, "GPU" + i);

                        a.UpdateValues();

                        if (a.DedicatedVramUsage > max)
                        {
                            max = a.DedicatedVramUsage;
                            adapter = a;
                        }
                    }
                }
                print("Selected " + adapter.Description + " as our main adapter");

                Marshal.FreeHGlobal(dkmtEnumadapters);
            }
            catch
            {
                // Well, things got weird. adapter will be null so we can test that.
            }
        }

        public enum WinVersion
        {
            Windows_95 = 4000,
            Windows_NT = 4000,
            Windows_98 = 4010,
            Windows_ME = 4090,
            Windows_2000 = 5000,
            Windows_XP = 5001,
            Windows_2003 = 5002,
            Windows_Vista = 6000,
            Windows_2008 = 6000,
            Windows_7 = 6001,
            Windows_2008_R2 = 6001,
            Windows_8 = 6002,
        }
        
        private Adapter adapter;
        
        public class Adapter
        {
            private static int WinVersion_Current = Environment.OSVersion.Version.Major * 1000 + Environment.OSVersion.Version.Minor;
            
            private long oldTotalRunningTime;
            #region Properties
            public LUID Luid { get; private set; }
            public string Description { get; private set; }
            private long _SharedVramUsage;
            private long _DedicatedVramUsage;
            private long _SharedVramLimit;
            private long _DedicatedVramLimit;
            
            public long DedicatedVramLimit
            {
                get
                {
                    return _DedicatedVramLimit;
                }
                private set
                {
                    _DedicatedVramLimit = value;
                }
            }
            public long SharedVramLimit
            {
                get { return _SharedVramLimit; }
                private set
                {
                    _SharedVramLimit = value;
                }
            }
            public long DedicatedVramUsage
            {
                get { return _DedicatedVramUsage; }
                private set
                {
                    _DedicatedVramUsage = value;
                }
            }
            public long SharedVramUsage
            {
                get { return _SharedVramUsage; }
                private set
                {
                    _SharedVramUsage = value;
                }
            }
            #endregion
            #region Construction
            public Adapter(LUID Luid, string Description)
            {
                this.Luid = Luid;
                this.Description = Description;
                UpdateValues();
            }
            #endregion
            #region Methods
            public void UpdateValues()
            {
                //Check for Video Memory
                D3DKMT_QUERYSTATISTICS queryStatistics = new D3DKMT_QUERYSTATISTICS();
                queryStatistics.Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_ADAPTER;
                queryStatistics.AdapterLuid = Luid;
                IntPtr queryStatisticsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(queryStatistics)); //Allocate unmanaged Memory
                Marshal.StructureToPtr(queryStatistics, queryStatisticsPtr, true);
                if (D3DKMT.Nt_Success(D3DKMT.D3DKMTQueryStatistics(queryStatisticsPtr)))
                {
                    queryStatistics = (D3DKMT_QUERYSTATISTICS)Marshal.PtrToStructure(queryStatisticsPtr, typeof(D3DKMT_QUERYSTATISTICS));
                    uint segmentCount = queryStatistics.QueryResult.AdapterInformation.NbSegments;

                    ulong GpuSharedLimit = 0;
                    ulong GpuDedicatedLimit = 0;
                    ulong GpuSharedBytesUsed = 0;
                    ulong GpuDedicatedBytesUsed = 0;
                    /*
                     * In this Part we query the Vram Usage by D3DKMT. You can make it better if you use a float or decimal instead of an int.
                     * Since we cant get all segments (some of them are locked) ~7mb missing, we check via WMI how many Mb are missing and add them to our Value (memoryOffset).
                     */
                    for (uint i = 0; i < segmentCount; i++)
                    {
                        queryStatistics = new D3DKMT_QUERYSTATISTICS();
                        queryStatistics.Type = D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_SEGMENT;
                        queryStatistics.AdapterLuid = Luid;
                        queryStatistics.QueryUnion.QuerySegment.SegmentId = i;
                        Marshal.StructureToPtr(queryStatistics, queryStatisticsPtr, true);
                        if (D3DKMT.Nt_Success(D3DKMT.D3DKMTQueryStatistics(queryStatisticsPtr)))
                        {
                            queryStatistics = (D3DKMT_QUERYSTATISTICS)Marshal.PtrToStructure(queryStatisticsPtr, typeof(D3DKMT_QUERYSTATISTICS));
                            UInt64 commitLimit;
                            UInt32 aperture; //Boolean; For System and Graphics
                            UInt64 bytesCommitted;
                            if (WinVersion_Current >= (int)WinVersion.Windows_8)
                            {
                                bytesCommitted = queryStatistics.QueryResult.SegmentInformation.BytesCommitted;
                                commitLimit = queryStatistics.QueryResult.SegmentInformation.CommitLimit;
                                aperture = queryStatistics.QueryResult.SegmentInformation.Aperture;
                            }
                            else
                            {
                                bytesCommitted = queryStatistics.QueryResult.SegmentInformation.BytesCommitted;
                                commitLimit = queryStatistics.QueryResult.SegmentInformationV1.CommitLimit;
                                aperture = queryStatistics.QueryResult.SegmentInformationV1.Aperture;
                            }
                            if (aperture == 1)
                            {
                                GpuSharedLimit += commitLimit;
                                GpuSharedBytesUsed += bytesCommitted;
                            }
                            else
                            {
                                GpuDedicatedLimit += commitLimit;
                                GpuDedicatedBytesUsed += bytesCommitted;
                            }
                        }
                    }

                    DedicatedVramUsage = (long)GpuDedicatedBytesUsed;
                    DedicatedVramLimit = (long)GpuDedicatedLimit;
                    SharedVramUsage = (long)GpuSharedBytesUsed;
                    SharedVramLimit = (long)GpuSharedLimit;
                }

                Marshal.FreeHGlobal(queryStatisticsPtr); //Free Allocated Memory
            }
            #endregion
        }












    }
}

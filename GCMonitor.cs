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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using KSP.IO;
using KSP.UI;
using KSP.UI.Screens;
using KSPAssets;
using KSPAssets.Loaders;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
//using UnityEngine.UI.Extensions;
using Debug = UnityEngine.Debug;
using Resources = GCMonitor.Properties.Resources;

namespace GCMonitor
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class GCMonitor : MonoBehaviour
    {
        private const int width = 1024;
        private const int height = 512;

        const int GraphLabelsCount = 4;
        const float labelSpace = 20f * (GraphLabelsCount + 1) / GraphLabelsCount; //fraction because we add Space 1 less time than we draw a Label

        private Rect windowPos = new Rect(80, 80, 400, 200);
        private Rect windowConfigPos = new Rect(80 + 410, 80, 200, 100);
        
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
        int fpsSize = 30;

        [Persistent]
        bool displayMem = true;

        [Persistent]
        bool displayMemRss = true;

        [Persistent]
        bool displayPeakRss = false;

        [Persistent]
        bool displayGpu = false;

        [Persistent]
        bool displayFps = true;

        int CountersX
        {
            get { return HighLogic.LoadedSceneIsEditor ? fpsXEditor : fpsX; }
            set
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    fpsXEditor = value;
                }
                else
                {
                    fpsX = value;
                }
            }
        }

        int CountersY
        {
            get { return HighLogic.LoadedSceneIsEditor ? fpsYEditor : fpsY; }
            set
            {
                if (HighLogic.LoadedSceneIsEditor)
                {
                    fpsYEditor = value;
                }
                else
                {
                    fpsY = value;
                }
            }
        }

        [Persistent]
        int fpsX = 10;
        [Persistent]
        int fpsY = 200;

        [Persistent]
        int fpsXEditor = 10;
        [Persistent]
        int fpsYEditor = 200;

        private IButton tbButton;
        private ApplicationLauncherButton alButton;

        private RectTransform panelPos;
//        private Canvas countersCanvas;
        private Text memVszText;
        private Text memRssText;
        private Text memPeakRssText;
        private Text memGpuText;
        private Text memFpsText;

        long displayMaxMemory = 200;
        long displayMinMemory = 0;
        static ulong peakMemory;
        long topMemory;
       
        public float updateInterval = 0.25f;
        private float accum = 0; // FPS accumulated over the interval
        private int frames = 0; // Frames drawn over the interval
        private float timeleft; // Left time for current interval

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


        internal void Start()
        {
            print("Start - Creating UI");

            //UItests();
            //
            //if (countersCanvas == null)
            //{
            //    UICreateCounters();
            //
            //    window = addWindow(UIMasterController.Instance.appCanvas.gameObject, "MyWindow");
            //}
        }

        private void UItests()
        {
            Canvas uiCanvas = UIMasterController.Instance.appCanvas;

            print(uiCanvas.renderMode);
            print(uiCanvas.pixelPerfect);
            print(uiCanvas.sortingLayerID);
            print(uiCanvas.sortingLayerName);
            print(uiCanvas.renderOrder);
            print(uiCanvas.scaleFactor);

            print("Camera");
            print(uiCanvas.worldCamera);
            print(uiCanvas.worldCamera.transform.position);
            print(uiCanvas.worldCamera.orthographic);
            print(uiCanvas.worldCamera.pixelRect);
            print(uiCanvas.worldCamera.orthographicSize);


            GameObject camh = uiCanvas.worldCamera.gameObject;

            print("Camera Hierarchy Start");
            while (camh.transform.parent != null)
            {
                print("Parent " + camh.GetType().FullName + " / " + camh.name + " " + camh.transform.position);
                camh = camh.transform.parent.gameObject;
            }
            print("Camera Hierarchy End");



            print("RectTransform");
            RectTransform cr = uiCanvas.GetComponent<RectTransform>();
            print("localPosition    " + cr.localPosition);
            print("localScale       " + cr.localScale);
            print("anchorMin        " + cr.anchorMin);
            print("anchorMax        " + cr.anchorMax);
            print("anchoredPosition " + cr.anchoredPosition);
            print("sizeDelta        " + cr.sizeDelta);
            print("pivot            " + cr.pivot);


            print(cr.rect);
            print(cr.anchoredPosition3D);


            print("Component listing");
            var comps = uiCanvas.GetComponents<Component>();
            foreach (Component component in comps)
            {
                print(component.GetType().Name);
            }

            print("Child Listing");
            var parent = uiCanvas.gameObject.transform.parent;
            if (parent == null)
            {
                print("No parent");
            }
            else
            {
                int children = parent.transform.childCount;
                for (int i = 0; i < children; ++i)
                    print("For loop: " + parent.transform.GetChild(i));
            }

            GameObject o = uiCanvas.gameObject;

            print("Hierarchy Start");
            while (o.transform.parent != null)
            {
                print("Parent " + o.GetType().FullName + " / " + o.name + " " + o.transform.position);
                o = o.transform.parent.gameObject;
            }
            print("Hierarchy End");

            foreach (var c in uiCanvas.gameObject.GetComponents<Component>())
            {
                print(c.GetType().Name);
            }

            print("CanvasScaler");
            var s = uiCanvas.gameObject.GetComponent<CanvasScaler>();
            print(s.uiScaleMode);
            print(s.scaleFactor);
            print(s.referencePixelsPerUnit);
            print(s.screenMatchMode);
            
        }

        private void UICreateCounters()
        {
            panelPos = addEmptyPanel(UIMasterController.Instance.appCanvas.gameObject);
            panelPos.localPosition = new Vector3(-Screen.width >> 1, Screen.height >> 1, 0);
            panelPos.sizeDelta = new Vector2(100, 100);
            panelPos.anchorMin = new Vector2(0, 1);
            panelPos.anchorMax = new Vector2(0, 1);
            panelPos.pivot = new Vector2(0, 1);
            panelPos.localScale = new Vector3(1, 1, 1);

            var cg = panelPos.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            var layout = panelPos.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            var csf = panelPos.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            memVszText = addTextOutline(panelPos.gameObject, "Mem");
            memRssText = addTextOutline(panelPos.gameObject, "MemRss");
            memPeakRssText = addTextOutline(panelPos.gameObject, "PeakRss");
            memGpuText = addTextOutline(panelPos.gameObject, "Gpu");
            memFpsText = addTextOutline(panelPos.gameObject, "Fps");
        }

        private static Text addTextOutline(GameObject parent, string s)
        {
            GameObject text1Obj = new GameObject("Text");

            text1Obj.layer = LayerMask.NameToLayer("UI");

            RectTransform trans = text1Obj.AddComponent<RectTransform>();
            trans.localScale = new Vector3(1, 1, 1);
            trans.localPosition.Set(0, 0, 0);
            
            Text text = text1Obj.AddComponent<Text>();
            text.supportRichText = true;
            text.text = s;
            text.fontSize = 24;
            text.font = UISkinManager.defaultSkin.font;

            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            text1Obj.transform.SetParent(parent.transform, false);

            NicerOutline outline = text1Obj.AddComponent<NicerOutline>();
            outline.effectColor = Color.black;
            
            return text;
        }


        private static Text addText(GameObject parent, string s)
        {
            GameObject text1Obj = new GameObject("Text");

            text1Obj.layer = LayerMask.NameToLayer("UI");

            RectTransform trans = text1Obj.AddComponent<RectTransform>();
            trans.anchorMin = new Vector2(0, 0);
            trans.anchorMax = new Vector2(1, 1);
            trans.localScale = new Vector3(1, 1, 1);
            trans.localPosition.Set(0, 0, 0);

            Text text = text1Obj.AddComponent<Text>();
            text.supportRichText = true;
            text.text = s;
            text.fontSize = 18;
            text.resizeTextForBestFit = false;
            text.font = UISkinManager.defaultSkin.font;

            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = Color.white;
            text1Obj.transform.SetParent(parent.transform, false);
            return text;
        }

        private RectTransform addWindow(GameObject parent, string title)
        {
            float width = 150;
            float height = 200;
            float headerHeight = 40;

            // The whole window
            RectTransform window = addEmptyPanel(parent);
            window.localPosition = new Vector3(0, 0, 0);
            window.sizeDelta = new Vector2(width, height);
            window.anchorMin = new Vector2(0, 1);
            window.anchorMax = new Vector2(0, 1);
            window.pivot = new Vector2(0, 1);

            var image = window.gameObject.AddComponent<Image>();
            //image.color = new Color(0f, 0.5f, 0f, 1);
            image.sprite = UISkinManager.defaultSkin.window.normal.background;
            image.fillCenter = false;
            
            var csf = window.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var vlg = window.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childForceExpandHeight = true;
            vlg.childForceExpandWidth = true;

            var le = window.gameObject.AddComponent<LayoutElement>();
            // Set the min size of the window
            le.minWidth = width; 
            le.minHeight = height;
            // this would set a max size
            //le.preferredWidth = width;
            //le.preferredHeight = height;
            
            // The Header
            RectTransform header = addEmptyPanel(window.gameObject);
            // Stretch to parent width
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = new Vector2(1, 1);
            header.pivot = new Vector2(0, 1);
            
            header.sizeDelta = new Vector2(0, headerHeight);
            header.localPosition = new Vector2(0, 0);
            
            //var image2 = header.gameObject.AddComponent<Image>();
            ////image2.color = new Color(1, 1, 1, 0.5f);
            //image2.sprite = UISkinManager.defaultSkin.window.normal.background;

            var leh = header.gameObject.AddComponent<LayoutElement>();
            leh.minHeight = headerHeight;
            leh.preferredHeight = headerHeight;

            GameObject textHeaderObj = new GameObject("Text");
            textHeaderObj.layer = LayerMask.NameToLayer("UI");

            RectTransform trans = textHeaderObj.AddComponent<RectTransform>();
            trans.anchorMin = new Vector2(0.5f, 0.5f);
            trans.anchorMax = new Vector2(0.5f, 0.5f);
            trans.pivot = new Vector2(0.5f, 0.5f);
            
            Text text = textHeaderObj.AddComponent<Text>();
            text.supportRichText = true;
            text.text = title;
            text.fontSize = 14;
            text.resizeTextForBestFit = false;
            text.font = UISkinManager.defaultSkin.font;

            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.color = Color.white;
            textHeaderObj.transform.SetParent(header.transform, false);

            // The Content
            RectTransform content = addEmptyPanel(window.gameObject);
            // Stretch to parent size
            content.anchorMin = new Vector2(0, 0);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0, 1);
            content.sizeDelta = new Vector2(0, -headerHeight);
            content.anchoredPosition = new Vector2(0, -headerHeight);

            //var image3 = content.gameObject.AddComponent<Image>();
            //image3.color = new Color(0,0,1,0.5f);
            
            var drag = header.gameObject.AddComponent<DragHandler>();

            drag.AddEvents(OnInitializePotentialDrag, OnBeginDrag, OnDrag, OnEndDrag);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 2;

            addText(content.gameObject, "Text1");
            addText(content.gameObject, "Text2");
            addText(content.gameObject, "Text3ThatIsVeryLong");
            addText(content.gameObject, "Text4EvenLongerThanTheVeryLongText");
            addText(content.gameObject, "Text5");
            addText(content.gameObject, "Text6");
            addText(content.gameObject, "Text7");
            addText(content.gameObject, "Text8");
            addText(content.gameObject, "Text9");
            addText(content.gameObject, "Text9");
            addText(content.gameObject, "Text9");
            addText(content.gameObject, "Text9");
            addText(content.gameObject, "Text9");
            addText(content.gameObject, "Text9");

            return window;
        }

        private RectTransform window;

        private Vector2 originalLocalPointerPosition;
        private Vector3 originalPanelLocalPosition;
        
        private void OnInitializePotentialDrag(PointerEventData e)
        {
            print("OnInitializePotentialDrag");
            originalPanelLocalPosition = window.localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.parent.transform, e.position, e.pressEventCamera, out originalLocalPointerPosition);
        }
        
        private void OnBeginDrag(PointerEventData e)
        {
            //print("onBeginDrag");
        }

        private void OnDrag(PointerEventData e)
        {
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)window.parent.transform, e.position, e.pressEventCamera, out localPointerPosition))
            {
                Vector3 offsetToOriginal = localPointerPosition - originalLocalPointerPosition;
                window.localPosition = originalPanelLocalPosition + offsetToOriginal;
            }
        }

        private void OnEndDrag(PointerEventData e)
        {
            //print("onEndDrag");
        }

        private static RectTransform addEmptyPanel(GameObject parent)
        {
            GameObject panelObj = new GameObject(parent.name + "Panel");
            panelObj.layer = LayerMask.NameToLayer("UI");

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();

            // Top Left corner as base
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.localPosition = new Vector3(0, 0, 0);
            panelRect.localScale = new Vector3(1, 1, 1);

            panelObj.transform.SetParent(parent.transform, true);

            return panelRect;
        }

        private bool rmb;
        private bool lmb;

        private MemState activeMemState;



        private RawImage graph;
        private Text[] graphLabels;
        private Text infoKSP;
        private Text infoGPU;
        private Text infoMono;
        private Text infoFPS;
        private Text precision;
        private Text topLabel;
        private UnityEngine.UI.Button precisionPlus;
        private UnityEngine.UI.Button precisionMinus;
        private Toggle toggleKSP;
        private Toggle toggleGPU;
        private Toggle toggleMono;
        private Toggle toggleRelative;
        private Toggle toggleColor;
        private Toggle toggleUpdate;
        private UnityEngine.UI.Button topButton;
        private UnityEngine.UI.Button configButton;        

        private Toggle toggleLauncher;
        private Toggle toggleCounters;
        private Toggle toggleCounterFPS;
        private Toggle toggleCounterVSZ;
        private Toggle toggleCounterRSS;
        private Toggle toggleCounterPeak;
        private Toggle toggleCounterGPU;

        private Text counterSize;
        private Text counterX;
        private Text counterY;

        private UnityEngine.UI.Button sizePlus;
        private UnityEngine.UI.Button sizeMinus;

        private UnityEngine.UI.Button xPlus;
        private UnityEngine.UI.Button xMinus;

        private UnityEngine.UI.Button yPlus;
        private UnityEngine.UI.Button yMinus;

        private UICollapsible configCollapsible;


        private RectTransform prefabWindow;

        // It seems the main menu unload all asset bundle after a couple of time
        // So I rebuild my UI after a few seconds
        void OnLevelWasLoaded()
        {
            if (HighLogic.LoadedScene != GameScenes.MAINMENU || !AssetLoader.Ready)
                return;
            print("OnLevelWasLoaded");
            StartCoroutine(ReloadAfterSec(1));
        }

        private IEnumerator ReloadAfterSec(int time)
        {
            yield return new WaitForSeconds(time);
            UILoad();
        }

        void UILoad()
        {
            if (!AssetLoader.Ready)
            {
                print("Asset Loader not ready");
                return;
            }

            if (prefabWindow != null)
            {
                print("Killing Previous UI");
                Destroy(prefabWindow.gameObject);
            }

            print("Asset bundles load");
            var assetDefinition = AssetLoader.GetAssetDefinitionWithName("GCMonitor/gcmonitor", "Window");

            print(AssetLoader.LoadAssets(UIInit, assetDefinition));
        }


        void UIInit(AssetLoader.Loader loader)
        {
            print("UIInit " + loader.definitions.Length + " defs");
            for (int i = 0; i < loader.definitions.Length; i++)
            {
                UnityEngine.Object o = loader.objects[i];
                if (o == null)
                {
                    print("Null def ??");
                    continue;
                }
                
                Type oType = o.GetType();

                print("UIInit " + oType.Name + " " + o.name );

                print("Asset bundles Instantiate");
                GameObject go = Instantiate(o as GameObject);

                // Set the parrent to the stock appCanvas
                go.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);

                prefabWindow = go.transform as RectTransform;

                graph = go            .GetComponentInChild<RawImage>("Graph");
                graphLabels = go .GetChild("Labels").GetComponentsInChildren<Text>();
                infoKSP = go          .GetComponentInChild<Text>("KSP");
                infoGPU = go          .GetComponentInChild<Text>("GPU");
                infoMono = go         .GetComponentInChild<Text>("Mono");
                infoFPS = go          .GetComponentInChild<Text>("FPS");
                precision = go        .GetComponentInChild<Text>("PrecisionCurrent");
                topLabel = go        .GetComponentInChild<Text>("TopLabel");
                precisionPlus = go    .GetComponentInChild<UnityEngine.UI.Button>("PrecisionPlus");
                precisionMinus = go   .GetComponentInChild<UnityEngine.UI.Button>("PrecisionMinus");
                toggleKSP = go        .GetComponentInChild<Toggle>("ToggleKSP");
                toggleGPU = go        .GetComponentInChild<Toggle>("ToggleGPU");
                toggleMono = go       .GetComponentInChild<Toggle>("ToggleMono");
                                       
                toggleRelative = go   .GetComponentInChild<Toggle>("ToggleRelative");
                toggleColor = go      .GetComponentInChild<Toggle>("ToggleColor");
                toggleUpdate = go     .GetComponentInChild<Toggle>("ToggleUpdate");

                topButton = go     .GetComponentInChild<UnityEngine.UI.Button>("TopButton");
                configButton = go     .GetComponentInChild<UnityEngine.UI.Button>("ConfigButton");
                                       
                configCollapsible = go.GetComponentInChild<UICollapsible>("ConfigPanel");
                                       
                toggleLauncher = go   .GetComponentInChild<Toggle>("ToggleLauncher");
                toggleCounters = go   .GetComponentInChild<Toggle>("ToggleCounters");
                toggleCounterFPS = go .GetComponentInChild<Toggle>("ToggleCounterFPS");
                toggleCounterVSZ = go .GetComponentInChild<Toggle>("ToggleCounterVSZ");
                toggleCounterRSS = go .GetComponentInChild<Toggle>("ToggleCounterRSS");
                toggleCounterPeak = go.GetComponentInChild<Toggle>("ToggleCounterPeak");
                toggleCounterGPU = go .GetComponentInChild<Toggle>("ToggleCounterGPU");
                                       
                counterSize = go      .GetComponentInChild<Text>("CounterSizeCurrent");
                counterX = go         .GetComponentInChild<Text>("CounterXCurrent");
                counterY = go         .GetComponentInChild<Text>("CounterYCurrent");
                                       
                sizePlus = go         .GetComponentInChild<UnityEngine.UI.Button>("CounterSizePlus");
                sizeMinus = go        .GetComponentInChild<UnityEngine.UI.Button>("CounterSizeMinus");
                                       
                xPlus = go            .GetComponentInChild<UnityEngine.UI.Button>("CounterXPlus");
                xMinus = go           .GetComponentInChild<UnityEngine.UI.Button>("CounterXMinus");
                                       
                yPlus = go            .GetComponentInChild<UnityEngine.UI.Button>("CounterYPlus");
                yMinus = go           .GetComponentInChild<UnityEngine.UI.Button>("CounterYMinus");

                precisionMinus.onClick.AddListener(() =>
                {
                    if (timeScale > 1)
                    {
                        timeScale = timeScale >> 1;
                        memoryHistory = new memoryState[width];
                        fullUpdate = true;
                    }
                });

                precisionPlus.onClick.AddListener(() =>
                {
                    if (timeScale < 8)
                    {
                        timeScale = timeScale << 1;
                        memoryHistory = new memoryState[width];
                        fullUpdate = true;
                    }
                });

                configButton.onClick.AddListener(() =>
                {
                    showConfUI = !showConfUI;
                    configCollapsible.OnValueChanged(showConfUI);
                });

                topButton.onClick.AddListener(() => { topMemory = (long) memoryVsz; });

                toggleRelative.isOn = relative;
                toggleColor.isOn = colorfulMode;
                toggleUpdate.isOn = OnlyUpdateWhenDisplayed;

                toggleRelative.onValueChanged.AddListener(b => { relative = b; fullUpdate = true; });
                toggleColor.onValueChanged.AddListener(b =>
                {
                    colorfulMode = b;
                    fullUpdate = true;
                    if (colorfulMode) timeScale = 10;
                });
                toggleUpdate.onValueChanged.AddListener(b => { OnlyUpdateWhenDisplayed = b; });

                toggleLauncher.isOn = useAppLauncher;
                toggleCounters.isOn = memoryGizmo;
                toggleCounterFPS.isOn = displayFps;
                toggleCounterVSZ.isOn = displayMem;
                toggleCounterRSS.isOn = displayMemRss;
                toggleCounterPeak.isOn = displayPeakRss;
                toggleCounterGPU.isOn = displayGpu;

                toggleLauncher.onValueChanged.AddListener(b => { useAppLauncher = b; });
                toggleCounters.onValueChanged.AddListener(b => { memoryGizmo = b; });
                toggleCounterFPS.onValueChanged.AddListener(b => { displayFps = b; });
                toggleCounterVSZ.onValueChanged.AddListener(b => { displayMem = b; });
                toggleCounterRSS.onValueChanged.AddListener(b => { displayMemRss = b; });
                toggleCounterPeak.onValueChanged.AddListener(b => { displayPeakRss = b; });
                toggleCounterGPU.onValueChanged.AddListener(b => { displayGpu = b; });

                counterSize.text = fpsSize.ToString();
                counterX.text = CountersX.ToString();
                counterY.text = CountersY.ToString();

                sizePlus.onClick.AddListener(() =>
                {
                    if (Event.current.button == 0)
                        fpsSize++;
                    else if (Event.current.button == 1)
                        fpsSize += 10;
                    counterSize.text = fpsSize.ToString();
                });
                sizeMinus.onClick.AddListener(() =>
                {
                    print(Event.current.button);
                    if (Event.current.button == 0)
                        fpsSize--;
                    else if (Event.current.button == 1)
                        fpsSize -= 10;
                    counterSize.text = fpsSize.ToString();
                });

                xPlus.onClick.AddListener(() =>
                {
                    if (Event.current.button == 0)
                        CountersX++;
                    else if (Event.current.button == 1)
                        CountersX += 10;
                    counterX.text = CountersX.ToString();
                });
                xMinus.onClick.AddListener(() =>
                {
                    if (Event.current.button == 0)
                        CountersX--;
                    else if (Event.current.button == 1)
                        CountersX -= 10;
                    counterX.text = CountersX.ToString();
                });

                yPlus.onClick.AddListener(() =>
                {
                    if (Event.current.button == 0)
                        CountersY++;
                    else if (Event.current.button == 1)
                        CountersY += 10;
                    counterY.text = CountersY.ToString();
                });
                yMinus.onClick.AddListener(() =>
                {
                    if (Event.current.button == 0)
                        CountersY--;
                    else if (Event.current.button == 1)
                        CountersY -= 10;
                    counterY.text = CountersY.ToString();
                });

                toggleKSP.isOn = realMemory;
                toggleGPU.isOn = gpuMemory;
                toggleMono.isOn = !realMemory && !gpuMemory;

                toggleKSP.onValueChanged.AddListener(MemoryToogleEvent);
                toggleGPU.onValueChanged.AddListener(MemoryToogleEvent);
                toggleMono.onValueChanged.AddListener(MemoryToogleEvent);
                
                graph.texture = memoryTexture;
            }
        }

        private void MemoryToogleEvent(bool b)
        {
            fullUpdate = true;
            gpuMemory = toggleGPU.isOn;
            realMemory = toggleKSP.isOn;
        }
        
        private void UIUpdate(processMemory mem)
        {
            if (prefabWindow == null)
                return;

            if (prefabWindow.gameObject.activeSelf != showUI)
                prefabWindow.gameObject.SetActive(showUI);

            if (!showUI)
                return;


            precision.text = (1f / timeScale).ToString("0.###") + "s";
            
            infoKSP.text = "KSP: " + ConvertToKBString(mem.vsz) + " / " + ConvertToKBString(maxAllowedMem);
            infoGPU.text = (adapter != null) ? "GPU: " + ConvertToKBString(adapter.DedicatedVramUsage) + " / " + ConvertToKBString(adapter.DedicatedVramLimit) : "N/A";
            infoMono.text = "Mono allocated:" + ConvertToMBString(Profiler.GetTotalAllocatedMemory())
                + " min: " + ConvertToMBString(memoryHistory[activeSecond].min)
                + " max: " + ConvertToMBString(memoryHistory[activeSecond].max)
                + " GC : " + memoryHistory[previousActiveSecond].gc.ToString();
            infoFPS.text = "FPS: " + fps.ToString("0.0");

            topLabel.text = "Since top: " + (topMemory != 0 ? ConvertToKBString(((long) memoryVsz - topMemory)) : "0");
            
            for (int i = 0; i <= GraphLabelsCount; i++)
            {
                graphLabels[i].text = ConvertToMBString(displayMaxMemory - (displayMaxMemory - displayMinMemory) * i / GraphLabelsCount);
            }
        }


        public void Update()
        {
            if (panelPos == null)
            {
                //UItests();

                UICreateCounters();

                //window = addWindow(UIMasterController.Instance.appCanvas.gameObject, "MyWindow");
                //Canvas.ForceUpdateCanvases();
                
                return;
            }
            
            if (prefabWindow == null && AssetLoader.Ready)
            {
                //print("Asset bundles");
                //print(AssetLoader.BundleDefinitions.Count);
                //foreach (BundleDefinition b in AssetLoader.BundleDefinitions)
                //{
                //    print(b.name + " " + b.createdTime + " " + b.path + " " + b.info + " " + b.urlName);
                //}
                //print(AssetLoader.AssetDefinitions.Count);
                //foreach (AssetDefinition a in AssetLoader.AssetDefinitions)
                //{
                //    print(a.name + " " + a.type + " " + a.path);
                //}

                UILoad();
            }
            
            memVszText.gameObject.SetActive(memoryGizmo && displayMem);
            memRssText.gameObject.SetActive(memoryGizmo && displayMemRss);
            memPeakRssText.gameObject.SetActive(memoryGizmo && displayPeakRss);
            memGpuText.gameObject.SetActive(memoryGizmo && displayGpu && adapter != null);
            memFpsText.gameObject.SetActive(memoryGizmo && displayFps);

            panelPos.localPosition = new Vector3((-(Screen.width >> 1) + CountersX) / GameSettings.UI_SCALE, ((Screen.height >> 1) - CountersY) / GameSettings.UI_SCALE, 0);

            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;
            // Interval ended - update GUI text and start new interval
            if (timeleft <= 0f)
            {
                fps = accum / frames;
                timeleft = updateInterval;
                accum = 0;
                frames = 0;

                if (displayFps)
                {
                    fpsString = StringFormater.Format("{0:##0.0} FPS", fps);
                    memFpsText.text = fpsString;
                    memFpsText.fontSize = fpsSize;
                }
            }
            
            processMemory memory = getProcessMemory();
            
            if (displayMem)
            {
                memoryVsz = memory.vsz;
                memoryVszString = ConvertToMBString(memoryVsz);
                memVszText.text = memoryVszString;
                memVszText.fontSize = fpsSize;
            }

            if (displayMemRss)
            {
                memoryRss = memory.rss;
                memoryRssString = ConvertToMBString(memoryRss);
                memRssText.text = memoryRssString;
                memRssText.fontSize = fpsSize;
            }

            if (memoryVsz > peakMemory)
                peakMemory = memoryVsz;

            if(displayPeakRss)
            {
                memoryPeakRssString = ConvertToMBString(peakMemory);
                memPeakRssText.text = memoryPeakRssString;
                memPeakRssText.fontSize = fpsSize;
            }

            if (displayGpu && adapter != null)
            {
                adapter.UpdateValues();
                gpuMemoryRssString = ConvertToMBString(adapter.DedicatedVramUsage);
                memGpuText.text = gpuMemoryRssString;
                memGpuText.fontSize = fpsSize;
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

            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.K))
            {
                KillAllHumans();
            }

            UIUpdate(memory);

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

        private bool eradicating = false;

        private void KillAllHumans()
        {
            if (eradicating)
            {
                print("Humans extermination already in progress. Please standby");
                return;
            }

            print("Initialing extermination protocols. Please standby");

            StartCoroutine(Exterminate());

        }

        private IEnumerator Exterminate()
        {
            while (true)
            {
                print("Currently at  VSZ " + memoryVszString + " RSS " + memoryRssString);
                for (int i = 0; i < 30; i++)
                {
                    Texture2D memoryWorm = new Texture2D(1024, 1024, TextureFormat.ARGB32, false);
                    memoryWorm.Apply();
                }
                yield return new WaitForSeconds(5);
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
                print("Removing Launcher");
                ApplicationLauncher.Instance.RemoveModApplication(alButton);
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

        //public void OnGUI()
        //{
        //    //if (memoryGizmo && !hiddenUI)
        //    //{
        //    //    if (fpsLabelStyle == null)
        //    //        fpsLabelStyle = new GUIStyle(GUI.skin.label);
        //    //
        //    //    fpsLabelStyle.fontSize = fpsSize;
        //    //
        //    //    Vector2 size = fpsLabelStyle.CalcSize(new GUIContent(memoryVszString));
        //    //
        //    //    Counters = Mathf.Clamp(Counters, 0, Screen.width);
        //    //    CountersY = Mathf.Clamp(CountersY, 0, Screen.height);
        //    //
        //    //    fpsPos.Set(Counters, CountersY, 200, size.y);
        //    //    if (displayMem)
        //    //    {
        //    //        DrawOutline(fpsPos, memoryVszString, 1, fpsLabelStyle, Color.black,
        //    //            memoryVsz > alertMem ? Color.red : memoryVsz > warnMem ? XKCDColors.Orange : Color.white);
        //    //        fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 200, size.y);
        //    //    }
        //    //
        //    //    if (displayMemRss)
        //    //    {
        //    //        DrawOutline(fpsPos, memoryRssString, 1, fpsLabelStyle, Color.black, Color.white);
        //    //        fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 300, size.y);
        //    //    }
        //    //
        //    //    if (displayPeakRss)
        //    //    {
        //    //        DrawOutline(fpsPos, memoryPeakRssString, 1, fpsLabelStyle, Color.black, Color.white);
        //    //        fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 300, size.y);
        //    //    }
        //    //
        //    //    if (displayGpu)
        //    //    {
        //    //        DrawOutline(fpsPos, gpuMemoryRssString, 1, fpsLabelStyle, Color.black, Color.white);
        //    //        fpsPos.Set(fpsPos.xMin, fpsPos.yMin + size.y, 300, size.y);
        //    //    }
        //    //
        //    //    if (displayFps)
        //    //    {
        //    //        DrawOutline(fpsPos, fpsString, 1, fpsLabelStyle, Color.black, Color.white);
        //    //    }
        //    //}

        //    if (showUI && !hiddenUI)
        //    {
        //        windowPos = GUILayout.Window(8785478, windowPos, WindowGUI, "GCMonitor", GUILayout.Width(420), GUILayout.Height(220));
        //    }

        //    if (showConfUI & showUI && !hiddenUI)
        //    {
        //        windowConfigPos.Set(windowPos.xMax + 10, windowPos.yMin, windowConfigPos.width, windowConfigPos.height);
        //        windowConfigPos = GUILayout.Window(8785479, windowConfigPos, WindowConfigGUI, "Config", GUILayout.Width(80), GUILayout.Height(50));
        //    }
        //}

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

            for (int i = 0; i <= GraphLabelsCount; i++)
            {
                GUILayout.Label(ConvertToMBString(displayMaxMemory - (displayMaxMemory - displayMinMemory) * i / GraphLabelsCount), new GUIStyle(GUI.skin.label) { wordWrap = false });
                if (i != GraphLabelsCount) //only do it if it's not the last one
                    GUILayout.Space(height / GraphLabelsCount - labelSpace);
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
            {
                if (Event.current.button == 0)
                    fpsSize--;
                else if (Event.current.button == 1)
                    fpsSize -= 10;
            }
            GUILayout.Label(fpsSize.ToString(), GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (Event.current.button == 0)
                    fpsSize++;
                else if (Event.current.button == 1)
                    fpsSize += 10;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("X", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                if (Event.current.button == 0)
                    CountersX--;
                else if (Event.current.button == 1)
                    CountersX -= 10;
            }
            GUILayout.Label(CountersX.ToString("F0"), GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (Event.current.button == 0)
                    CountersX++;
                else if (Event.current.button == 1)
                    CountersX += 10;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Y", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                if (Event.current.button == 0)
                    CountersY--;
                else if (Event.current.button == 1)
                    CountersY -= 10;;
            }
            GUILayout.Label(CountersY.ToString("F0"), GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (Event.current.button == 0)
                    CountersY++;
                else if (Event.current.button == 1)
                    CountersY += 10;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            CountersX = Mathf.Clamp(CountersX, 0, Screen.width);
            CountersY = Mathf.Clamp(CountersY, 0, Screen.height);
        }

        static String ConvertToGBString(long bytes)
        {
            return StringFormater.Format("{0:#,0.0} GB", bytes / 1024f / 1024f / 1024f);
        }

        static String ConvertToGBString(ulong bytes)
        {
            return StringFormater.Format("{0:#,0.0} GB", (bytes >> 20) / 1024f);
        }

        static String ConvertToMBString(long bytes)
        {
            return StringFormater.Format(spaceFormat, "{0:#,0} MB", bytes / 1024f / 1024f);
        }

        static String ConvertToMBString(ulong bytes)
        {
            return StringFormater.Format(spaceFormat, "{0:#,0} MB", bytes >> 20);
        }

        static String ConvertToKBString(long bytes)
        {
            return StringFormater.Format(spaceFormat, "{0:#,0} kB", bytes / 1024f);
        }

        static String ConvertToKBString(ulong bytes)
        {
            return StringFormater.Format(spaceFormat, "{0:#,0} kB", bytes >> 10);
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

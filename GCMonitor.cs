﻿/*
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
using UnityEngine;
using System.Threading;
using Debug = UnityEngine.Debug;

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
        private bool showUI = false;
        readonly Texture2D memoryTexture = new Texture2D(width, height);
        float ratio;
        bool colorfulMode = false;

        int timeScale = 1;

        bool killThread = false;

        memoryState[] memoryHistory = new memoryState[width];

        int activeSecond = 0;
        int previousActiveSecond = 0;
        int displayUpToSecond = 0;
        int lastDisplayedSecond = 0;

        bool fullUpdate = true;
        bool OnlyUpdateWhenDisplayed = true;
        bool memoryGizmo = true;
        bool realMemory = true;
        bool relative = false;

        long displayMaxMemory = 200;
        long displayMinMemory = 0;
        long maxMemory;

        private readonly float updateInterval = 0.5F;

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
            // technically it should be an ulong but it generate too many cast requirement
            public long rss;
            public long gc;
        }

        // Windows x86
        private const string kDllPath_Win_x86 = "GameData/GCMonitor/getRSS_x86.dll";
        [DllImport(dllName: kDllPath_Win_x86, EntryPoint = "getCurrentRSS")]
        private static extern UIntPtr getCurrentRSS_Win_x86();
        [DllImport(dllName: kDllPath_Win_x86, EntryPoint = "getPeakRSS")]
        private static extern UIntPtr getPeakRSS_Win_x86();

        // Windows x64 (not implemented yet)
        private const string kDllPath_Win_x64 = "GameData/GCMonitor/getRSS_x64.dll";
        [DllImport(dllName: kDllPath_Win_x64, EntryPoint = "getCurrentRSS")]
        private static extern UIntPtr getCurrentRSS_Win_x64();
        [DllImport(dllName: kDllPath_Win_x64, EntryPoint = "getPeakRSS")]
        private static extern UIntPtr getPeakRSS_Win_x64();

        // Linux x86
        private const string kDllPath_linux_x86 = "GameData/GCMonitor/getRSS_x86.so";
        [DllImport(dllName: kDllPath_linux_x86, EntryPoint = "getCurrentRSS")]
        private static extern UIntPtr getCurrentRSS_Linux_x86();
        [DllImport(dllName: kDllPath_linux_x86, EntryPoint = "getPeakRSS")]
        private static extern UIntPtr getPeakRSS_Linux_x86();

        // Linux x64
        private const string kDllPath_linux_x64 = "GameData/GCMonitor/getRSS_x64.so";
        [DllImport(dllName: kDllPath_linux_x64, EntryPoint = "getCurrentRSS")]
        private static extern UIntPtr getCurrentRSS_Linux_x64();
        [DllImport(dllName: kDllPath_linux_x64, EntryPoint = "getPeakRSS")]
        private static extern UIntPtr getPeakRSS_Linux_x64();

        // OSX x86
        private const string kDllPath_OSX_x86 = "GameData/GCMonitor/getRSS.so_OSX_x86";
        [DllImport(dllName: kDllPath_OSX_x86, EntryPoint = "getCurrentRSS")]
        private static extern UIntPtr getCurrentRSS_OSX_x86();
        [DllImport(dllName: kDllPath_OSX_x86, EntryPoint = "getPeakRSS")]
        private static extern UIntPtr getPeakRSS_OSX_x86();


        private static UIntPtr unimplemented()
        {
            return (UIntPtr)0;
        }

        private delegate UIntPtr GetCurrentRSS();
        private static GetCurrentRSS getCurrentRSS;
        private delegate UIntPtr GetPeakRSS();
        private static GetPeakRSS getPeakRSS;
        private long warnMem;
        private long alertMem;
        private string memoryString;
        private long memory;

        internal void Awake()
        {
            DontDestroyOnLoad(gameObject);
            // I know. Should be ulong. User with TB of memory can file a bug
            long maxAllowedMem = IsX64() ? long.MaxValue : uint.MaxValue;

            warnMem = (long)(maxAllowedMem * 0.8d);
            alertMem = (long)(maxAllowedMem * 0.9d);

            timeleft = updateInterval;

            line = new Color[height];
            blackLine = new Color[height];
            for (int i = 0; i < blackLine.Length; i++)
                blackLine[i] = Color.black;

            cat = new Texture2D(33, 20, TextureFormat.ARGB32, false);
            cat.LoadImage(Properties.Resources.cat);
            catPixels = cat.GetPixels();
            blackSquare = new Color[32 * height];
            for (int i = 0; i < blackSquare.Length; i++)
                blackSquare[i] = Color.black;


            spaceFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            spaceFormat.NumberGroupSeparator = " ";

            Debug.Log("[GCMonitor] Setting up getRSS delegates");
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    if (IsX64())
                    {
                        getCurrentRSS = getCurrentRSS_Linux_x64;
                        getPeakRSS = getPeakRSS_Linux_x64;
                    }
                    else
                    {
                        getCurrentRSS = getCurrentRSS_Linux_x86;
                        getPeakRSS = getPeakRSS_Linux_x86;
                    }
                    break;
                case PlatformID.MacOSX:
                    getCurrentRSS = getCurrentRSS_OSX_x86;
                    getPeakRSS = getPeakRSS_OSX_x86;
                    break;
                case PlatformID.Win32NT:
                    if (IsX64())
                    {
                        getCurrentRSS = getCurrentRSS_Win_x64;
                        getPeakRSS = getPeakRSS_Win_x64;
                    }
                    else
                    {
                        getCurrentRSS = getCurrentRSS_Win_x86;
                        getPeakRSS = getPeakRSS_Win_x86;
                    }
                    break;
                default:
                    getCurrentRSS = unimplemented;
                    getPeakRSS = unimplemented;
                    break;
            }

            try
            {
                UIntPtr c = getCurrentRSS();
                UIntPtr p = getPeakRSS();
                Debug.Log("[GCMonitor] Delegates OK " + ConvertToKBString((long)c) + " / " + ConvertToKBString((long)p));
            }
            catch (Exception e)
            {
                Debug.Log("[GCMonitor] Unable to find getRSS implementation\n" + e.ToString());
                getCurrentRSS = unimplemented;
                getPeakRSS = unimplemented;
            }


            watch = new Stopwatch();
            watch.Start();
            // Does not exist on our mono version :(
            //GC.RegisterForFullGCNotification(10, 10);
            Thread threadGCCollector = new Thread(GCCollector);
            threadGCCollector.Start();
        }

        private static bool IsX64()
        {
            return (IntPtr.Size == 8);
        }

        internal void OnDestroy()
        {
            killThread = true;
        }

        public void Update()
        {
            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            frames++;

            // Interval ended - update fps and start new interval
            if (timeleft <= 0.0)
            {
                fps = accum / frames;
                fpsString = fps.ToString("##0.0") + " FPS";
                
                timeleft = updateInterval;
                accum = 0.0F;
                frames = 0;
            }

            memory = (long)getCurrentRSS();
            memoryString = ConvertToMBString(memory);


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

            maxMemory = 0;
            long minMemory = 0;
            for (int i = 0; i < memoryHistory.Length; i++)
            {
                long mem = realMemory ? memoryHistory[i].rss : memoryHistory[i].max;
                if (relative)
                {
                    int index = i != 0 ? (i - 1) : memoryHistory.Length - 1;
                    long pmem = realMemory ? memoryHistory[index].rss : memoryHistory[index].max;
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
                        UpdateTexture(lastDisplayedSecond, localdisplayUpToSecond);
                        lastDisplayedSecond = (lastDisplayedSecond + 1) % width;
                    }
                    memoryTexture.Apply();
                }
            }
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

            memoryHistory[activeSecond].rss = (long)getCurrentRSS();
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
            int min = Mathf.RoundToInt(ratio * (realMemory ? memoryHistory[x].rss : memoryHistory[x].min));
            int max = Mathf.RoundToInt(ratio * (realMemory ? memoryHistory[x].rss : memoryHistory[x].max));

            int zero = -Mathf.RoundToInt(ratio * displayMinMemory);
            if (relative)
            {
                int index = x != 0 ? (x - 1) : memoryHistory.Length - 1;
                int pmem = Mathf.RoundToInt(ratio * (realMemory ? memoryHistory[index].rss : memoryHistory[index].max));
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
                        if (!realMemory && y < 10 * memoryHistory[x].gc)
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
            if (memoryGizmo)
            {
                Vector2 size = GUI.skin.label.CalcSize(new GUIContent(memoryString));

                DrawOutline(new Rect(10, 100, 200, size.y), memoryString, 1, GUI.skin.label, Color.black, memory > alertMem ? Color.red : memory > warnMem ? XKCDColors.Orange : Color.white);
                DrawOutline(new Rect(10, 100 + size.y, 200, size.y), fpsString, 1, GUI.skin.label, Color.black, Color.white);
            }


            if (showUI)
            {
                windowPos = GUILayout.Window(8785478, windowPos, WindowGUI, "GCMonitor", GUILayout.Width(420), GUILayout.Height(220));
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
                fullUpdate = true;

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

            memoryGizmo = GUILayout.Toggle(memoryGizmo, "Display KSP memory and FPS", GUILayout.ExpandWidth(false));
            
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            GUILayout.Label("KSP: " + ConvertToKBString((long)getCurrentRSS()) + " / " + ConvertToKBString((long)getPeakRSS()), GUILayout.ExpandWidth(false));
            GUILayout.Label(
                "Mono allocated:" + ConvertToMBString(Profiler.GetTotalAllocatedMemory())
                + " min: " + ConvertToMBString(memoryHistory[activeSecond].min)
                + " max: " + ConvertToMBString(memoryHistory[activeSecond].max)
                + " GC : " + memoryHistory[previousActiveSecond].gc.ToString(), GUILayout.ExpandWidth(false));

            GUILayout.Label("FPS: " + fps.ToString("0.0"), GUILayout.ExpandWidth(false));

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

        static String ConvertToMBString(long bytes)
        {
            return (bytes >> 20).ToString("#,0", spaceFormat) + " MB";
        }

        static String ConvertToKBString(long bytes)
        {
            return (bytes >> 10).ToString("#,0", spaceFormat) + " kB";
        }

        void DrawOutline(Rect r, string t, int strength, GUIStyle style, Color outColor, Color inColor)
        {
            Color backup = style.normal.textColor;
            style.normal.textColor = outColor;
            int i;
            for (i = -strength; i <= strength; i++)
            {
                GUI.Label(new Rect(r.x - strength, r.y + i, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + strength, r.y + i, r.width, r.height), t, style);
            } for (i = -strength + 1; i <= strength - 1; i++)
            {
                GUI.Label(new Rect(r.x + i, r.y - strength, r.width, r.height), t, style);
                GUI.Label(new Rect(r.x + i, r.y + strength, r.width, r.height), t, style);
            }
            style.normal.textColor = inColor;
            GUI.Label(r, t, style);
            style.normal.textColor = backup;
        }
 
    }
}

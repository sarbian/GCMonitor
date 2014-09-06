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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.Threading;

namespace GCMonitor
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    public class GCMonitor : MonoBehaviour
    {
        private const int width = 800;
        private const int height = 350;

        public Rect windowPos = new Rect(40, 40, 400, 200);
        public bool showUI = true;
        Texture2D memoryTexture = new Texture2D(width, height);
        float ratio;
        bool colorfulMode = false;

        int timeScale = 1;

        bool killThread = false;
        //bool loopThread = false;

        memoryState[] memoryHistory = new memoryState[width];

        int activeSecond = 0;
        int previousActiveSecond = 0;
        int displayUpToSecond = 0;
        int lastDisplayedSecond = 0;

        bool fullUpdate = true;
        bool OnlyUpdateWhenDisplayed = false;

        long displayMaxMemory = 200;
        long maxMemory = 200;
        long lastMem = long.MaxValue;

        int lastColCount = 0;

        Texture2D cat;
        Color[] catPixels;
        Color[] blackSquare;

        //object CurrentProcess;
        //MethodInfo PrivateMemorySize64;

        Color color = new Color();

        struct memoryState
        {
            public long min;
            public long max;
            public long avg;
            public long gc;
        }

        internal void Awake()
        {
            DontDestroyOnLoad(this.gameObject);

            cat = new Texture2D(33, 20, TextureFormat.ARGB32, false);
            cat.LoadImage(Properties.Resources.cat);
            catPixels = cat.GetPixels();
            blackSquare = new Color[32 * height];
            for (int i = 0; i < blackSquare.Length; i++)
                blackSquare[i] = Color.black;

            //UpdateTexture(0);


            //Type Process = Type.GetType("System.Diagnostics.Process,System");
            //CurrentProcess = Process.GetMethod("GetCurrentProcess", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
            //PrivateMemorySize64 = Process.GetMethod("get_PrivateMemorySize64", BindingFlags.Instance | BindingFlags.Public);

            //long mem = (long)PrivateMemorySize64.Invoke(CurrentProcess, null);

            // Does not exist on our mono version :(
            //GC.RegisterForFullGCNotification(10, 10);
            Thread threadGCCollector = new Thread(new ThreadStart(GCCollector));
            threadGCCollector.Start();
        }

        internal void OnDestroy()
        {
            killThread = true;
        }

        public void Update()
        {
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F1))
            {
                showUI = !showUI;
            }

            if (!showUI)
                return;

            if (fullUpdate)
            {
                fullUpdate = false;
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

        public void memoryHistoryUpdate()
        {
            activeSecond = Mathf.FloorToInt(Time.unscaledTime * timeScale) % width;

            long mem = GC.GetTotalMemory(false);

            if (mem > maxMemory)
                maxMemory = mem;

            if ((mem * 1.1f) > displayMaxMemory)
            {
                displayMaxMemory = (long)(mem * 1.2f);
                ratio = (float)memoryTexture.height / displayMaxMemory;
                fullUpdate = true;
            }

            if (activeSecond != previousActiveSecond)
            {
                // Bad idea since I now call from a thread
                //UpdateTexture(lastSecond);
                //memoryTexture.Apply();
                memoryHistory[activeSecond].min = long.MaxValue;
                memoryHistory[activeSecond].max = 0;
                memoryHistory[activeSecond].avg = 0;

                //int colCount = GC.CollectionCount(GC.MaxGeneration);
                int colCount = GC.CollectionCount(0);

                memoryHistory[previousActiveSecond].gc = colCount - lastColCount; // Should I check generation other than 0 ?
                lastColCount = colCount;
                displayUpToSecond = previousActiveSecond;
                previousActiveSecond = activeSecond;
            }

            if (mem < lastMem)
                memoryHistory[activeSecond].gc = memoryHistory[activeSecond].gc + 1;

            if (mem < memoryHistory[activeSecond].min)
                memoryHistory[activeSecond].min = mem;
            if (mem > memoryHistory[activeSecond].max)
                memoryHistory[activeSecond].max = mem;

            lastMem = mem;
        }

        public void GCCollector()
        {
            while (true)
            {
                if (!OnlyUpdateWhenDisplayed || showUI)
                {
                    memoryHistoryUpdate();
                }
                // Removed since those does not work under our mono version :(
                //while (loopThread)
                //{
                //    GCNotificationStatus status = GC.WaitForFullGCApproach();
                //    if (status == GCNotificationStatus.Succeeded)
                //    {
                //        memoryHistoryUpdate();
                //    }
                //    else
                //    {
                //        break;
                //    }
                //
                //    status = GC.WaitForFullGCComplete();
                //    if (status == GCNotificationStatus.Succeeded)
                //    {
                //        memoryHistory[activeSecond].gc++;
                //        memoryHistoryUpdate();
                //    }
                //    else
                //    {
                //        break;
                //    }
                //}

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
            int min = Mathf.RoundToInt(ratio * (float)memoryHistory[x].min);
            int max = Mathf.RoundToInt(ratio * (float)memoryHistory[x].max);

            int xPlus1 = (x + 1) % width;
            int xPlus2 = (x + 2) % width;

            if (!colorfulMode)
            {
                for (int y = 0; y < memoryTexture.height; y++)
                {
                    if (y < 10 * memoryHistory[x].gc)
                        color = Color.red;
                    else if (y <= min && max != 0)
                        color = Color.grey;
                    else if (y <= max && max != 0)
                        color = Color.green;
                    else
                        color = Color.black;

                    memoryTexture.SetPixel(x, y, color);

                    if (x == last)
                    {
                        memoryTexture.SetPixel(xPlus1, y, Color.black);
                        memoryTexture.SetPixel(xPlus2, y, Color.black);
                    }
                }
            }
            else
            {
                memoryTexture.SetPixels(x, 0, cat.width-1, height, blackSquare);
                if (max != 0)
                    memoryTexture.SetPixels(x, max, cat.width, cat.height, catPixels);
            }
        }

        public void OnGUI()
        {
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
                    // We could collect at x8 speed and then agreate
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
                    GUILayout.Label((1f / (float)timeScale).ToString("0.###") + "s", GUILayout.ExpandWidth(false));
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

                    GUILayout.Label("Last interval min: " + ConvertToMBString(memoryHistory[activeSecond].min)
                        + " max: " + ConvertToMBString(memoryHistory[activeSecond].max)
                        + " GC : " + memoryHistory[previousActiveSecond].gc);
                    GUILayout.Label("Maximum reported: " + ConvertToMBString(maxMemory));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                    colorfulMode = GUILayout.Toggle(colorfulMode, "More Color Mode");
                    if (colorfulMode)
                        timeScale = 10;

                    OnlyUpdateWhenDisplayed = GUILayout.Toggle(OnlyUpdateWhenDisplayed, "Only Update When Display is visible");

                    GUILayout.Label("Allocated: " + ConvertToMBString(Profiler.GetTotalAllocatedMemory()));
                    GUILayout.Label("Reserved: " + ConvertToMBString(Profiler.GetTotalReservedMemory()));
                    GUILayout.Label("FPS: " + (1 / Time.smoothDeltaTime).ToString("##"));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(GUILayout.Height(height));

                    GUILayout.BeginVertical(GUILayout.MinWidth(50));

                        const int MaxLabels = 4;
                        const float labelSpace = 20f * (MaxLabels+1)/MaxLabels; //fraction because we add Space 1 less time than we draw a Label
                        for (int i = 0; i <= MaxLabels; i++)
                        {
                            GUILayout.Label(ConvertToMBString(displayMaxMemory - displayMaxMemory * i/MaxLabels));
                            if (i != MaxLabels) //only do it if it's not the last one
                                GUILayout.Space(height / MaxLabels - labelSpace);
                        }
                    GUILayout.EndVertical();

                    GUILayout.Box(memoryTexture);

                GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        static String ConvertToMBString(long bytes)
        {
            return (bytes / 1024 / 1024).ToString("##0") + "MB";
        }
        static String ConvertToMBString(UInt64 bytes)
        {
            return (bytes / 1024 / 1024).ToString("##0") + "MB";
        }    
    }
}

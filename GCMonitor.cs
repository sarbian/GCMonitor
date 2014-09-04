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
        private const int size = 800;

        public Rect windowPos = new Rect(40, 40, 400, 200);
        public bool showUI = false;
        Texture2D memoryTexture = new Texture2D(size, 350);
        float ratio;

        int timeScale = 1;

        bool killThread = false;
        //bool loopThread = false;

        memoryState[] memoryHistory = new memoryState[size];

        int activeSecond = 0;
        int previousActiveSecond = 0;
        int displayUpToSecond = 0;
        int lastDisplayedSecond = 0;

        bool fullUpdate = true;

        long displayMaxMemory = 200;
        long maxMemory = 200;
        long lastMem = long.MaxValue;

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

            UpdateTexture(0);

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
            if (GameSettings.MODIFIER_KEY.GetKey() && Input.GetKeyDown(KeyCode.F2))
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
                        lastDisplayedSecond = (lastDisplayedSecond + 1) % size;
                        UpdateTexture(lastDisplayedSecond, localdisplayUpToSecond);
                    }
                    memoryTexture.Apply();
                }
            }
        }

        public void memoryHistoryUpdate()
        {
            activeSecond = Mathf.FloorToInt(Time.unscaledTime * timeScale) % size;

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
                memoryHistory[activeSecond].gc = 0;
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
                memoryHistoryUpdate();

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

            int xPlus1 = (x + 1) % size;
            int xPlus2 = (x + 2) % size;

            for (int y = 0; y < memoryTexture.height; y++)
            {
                if (y < 10 * memoryHistory[x].gc)
                    color = Color.red;
                else if (y <= min)
                    color = Color.grey;
                else if (y <= max)
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
                    memoryHistory = new memoryState[size];
                    fullUpdate = true;
                }
            }
            GUILayout.Label((1f / (float)timeScale).ToString("0.###") + "s", GUILayout.ExpandWidth(false));
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (timeScale < 8)
                {
                    timeScale = timeScale * 2;
                    memoryHistory = new memoryState[size];
                    fullUpdate = true;
                }
            }

            GUILayout.Space(30);

            GUILayout.Label("Last interval min: " + (memoryHistory[activeSecond].min / 1024 / 1024).ToString("###") + "MB"
                + " max: " + (memoryHistory[activeSecond].max / 1024 / 1024).ToString("###") + "MB" );
            GUILayout.Label("Maximum reported: " + (maxMemory / 1024 / 1024).ToString("###") + "MB");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Allocated: " + (Profiler.GetTotalAllocatedMemory() / 1024 / 1024).ToString("###") + "MB");
            GUILayout.Label("Reserved: " + (Profiler.GetTotalReservedMemory() / 1024 / 1024).ToString("###") + "MB");
            GUILayout.EndHorizontal();

            GUILayout.Box(memoryTexture);

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}

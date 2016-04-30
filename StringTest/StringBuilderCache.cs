
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See https://github.com/dotnet/coreclr/blob/master/LICENSE.TXT for full license information.

// Old mono compatible version of MS coreclr code

using System;
using System.Text;

namespace GCMonitor
{
    internal static class StringBuilderCache
    {
        // The value 360 was chosen in discussion with performance experts as a compromise between using
        // as litle memory (per thread) as possible and still covering a large part of short-lived
        // StringBuilder creations on the startup path of VS designers.
        private const int MAX_BUILDER_SIZE = 360;

        [ThreadStatic]
        private static StringBuilder CachedInstance;

        //private static readonly FieldInfo strFieldInfo;
        //private static readonly FieldInfo CachedStrFieldInfo;
        //
        //static StringBuilderCache()
        //{
        //    strFieldInfo = typeof(StringBuilder).GetField("_str", BindingFlags.NonPublic | BindingFlags.Instance);
        //    CachedStrFieldInfo = typeof(StringBuilder).GetField("_cached_str", BindingFlags.NonPublic | BindingFlags.Instance);
        //}

        public static StringBuilder Acquire(int capacity = 256)
        {
            if (capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilder sb = StringBuilderCache.CachedInstance;
                if (sb != null)
                {
                    // Avoid stringbuilder block fragmentation by getting a new StringBuilder
                    // when the requested size is larger than the current capacity
                    if (capacity <= sb.Capacity)
                    {
                        StringBuilderCache.CachedInstance = null;
                        //sb.Clear(); // stupid old mono :(
                        sb.Length = 0;
                        return sb;
                    }
                }
            }
            return new StringBuilder(capacity);
        }

        public static void Release(this StringBuilder sb)
        {
            if (sb.Capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilderCache.CachedInstance = sb;
            }
        }

        //public static string ToStringFree(this StringBuilder sb)
        //{
        //    string str = (string)strFieldInfo.GetValue(sb);
        //    return str;
        //}
        //
        //public static string ToStringFreeAndRelease(this StringBuilder sb)
        //{
        //    string result = sb.ToStringFree();
        //
        //    Release(sb);
        //    return result;
        //}

        public static string ToStringAndRelease(this StringBuilder sb)
        {
            string result = sb.ToString();

            Release(sb);
            return result;
        }

    }
}

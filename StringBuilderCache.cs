
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See https://github.com/dotnet/coreclr/blob/master/LICENSE.TXT for full license information.

// Old mono compatible version of MS coreclr code

using System;
using System.Text;

namespace GCMonitor
{
    internal  static class StringFormater
    {
        public static String Format(String format, params Object[] args)
        {
            return FormatHelper(null, format, args);
        }

        public static String Format(IFormatProvider provider, String format, params Object[] args)
        {
            return FormatHelper(provider, format, args);
        }

        private static String FormatHelper(IFormatProvider provider, String format, Object[] args)
        {
            return StringBuilderCache.GetStringAndRelease(
                StringBuilderCache
                    .Acquire(format.Length + args.Length * 8).AppendFormat(provider, format, args));
        }
    }

    internal static class StringBuilderCache
    {
        // The value 360 was chosen in discussion with performance experts as a compromise between using
        // as litle memory (per thread) as possible and still covering a large part of short-lived
        // StringBuilder creations on the startup path of VS designers.
        private const int MAX_BUILDER_SIZE = 360;

        [ThreadStatic]
        private static StringBuilder CachedInstance;

        public static StringBuilder Acquire(int capacity = 20)
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

        public static void Release(StringBuilder sb)
        {
            if (sb.Capacity <= MAX_BUILDER_SIZE)
            {
                StringBuilderCache.CachedInstance = sb;
            }
        }

        public static string GetStringAndRelease(StringBuilder sb)
        {
            string result = sb.ToString();
            Release(sb);
            return result;
        }
    }
}

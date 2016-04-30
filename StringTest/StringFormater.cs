using System;

namespace GCMonitor
{
    public static class StringFormater
    {
        public static string ConcatFormat<A>(String format_string, A arg1)
            where A : IConvertible
        {
            return ConcatFormat<A, int, int, int>(format_string, arg1, 0, 0, 0);
        }

        public static string ConcatFormat<A, B>(String format_string, A arg1, B arg2)
            where A : IConvertible
            where B : IConvertible
        {
            return ConcatFormat<A, B, int, int>(format_string, arg1, arg2, 0, 0);
        }

        public static string ConcatFormat<A, B, C>(String format_string, A arg1, B arg2, C arg3)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
        {
            return ConcatFormat<A, B, C, int>(format_string, arg1, arg2, arg3, 0);
        }

        public static string ConcatFormat<A, B, C, D>(String format_string, A arg1, B arg2, C arg3, D arg4)
            where A : IConvertible
            where B : IConvertible
            where C : IConvertible
            where D : IConvertible
        {
            return StringBuilderCache.Acquire(format_string.Length + 2 * 8).ConcatFormat<A, B, C, D>(format_string, arg1, arg2, arg3, arg4).ToStringAndRelease();
        }
    }
}
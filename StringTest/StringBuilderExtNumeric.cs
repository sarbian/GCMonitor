/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// File:	StringBuilderExtNumeric.cs
// Date:	9th March 2010
// Author:	Gavin Pugh
// Details:	Extension methods for the 'StringBuilder' standard .NET class, to allow garbage-free concatenation of
//			a selection of simple numeric types.  
//
// Copyright (c) Gavin Pugh 2010 - Released under the zlib license: http://www.opensource.org/licenses/zlib-license.php
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace GCMonitor
{
    public static partial class StringBuilderExtensions
    {
        // These digits are here in a static array to support hex with simple, easily-understandable code. 
        // Since A-Z don't sit next to 0-9 in the ascii table.
        private static readonly char[]	ms_digits = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        private static readonly uint	ms_default_decimal_places = 5; //< Matches standard .NET formatting dp's
        private static readonly char	ms_default_pad_char = '0';

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat( this StringBuilder string_builder, ulong ulong_val, uint pad_amount, char pad_char, bool thousand_sep, uint base_val )
        {
            Debug.Assert( base_val > 0 && base_val <= 16 );

            // Calculate length of integer when written out
            uint length = 0;
            ulong length_calc = ulong_val;

            do
            {
                length_calc /= base_val;
                length++;
            }
            while ( length_calc > 0 );

            if (thousand_sep)
                length += (length - 1) / 3;

            //uint str_length = thousand_sep ? length + (length - 1) / 3 : length;
            uint str_length = length;

            // Pad out space for writing.
            string_builder.Append( pad_char, (int)Math.Max( pad_amount, length));

            int strpos = string_builder.Length;

            // We're writing backwards, one character at a time.
            while ( length > 0 )
            {
                strpos--;

                if (thousand_sep && (str_length - length) % 4 == 3)
                {
                    string_builder[strpos] = ' ';
                }
                else
                {
                    // Lookup from static char array, to cover hex values too
                    string_builder[strpos] = ms_digits[ulong_val % base_val];

                    ulong_val /= base_val;
                }
                length--;
            }

            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public static StringBuilder Concat( this StringBuilder string_builder, ulong ulong_val )
        {
            string_builder.Concat( ulong_val, 0, ms_default_pad_char, false, 10 );
            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat( this StringBuilder string_builder, ulong ulong_val, uint pad_amount )
        {
            string_builder.Concat( ulong_val, pad_amount, ms_default_pad_char, false, 10 );
            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat( this StringBuilder string_builder, ulong ulong_val, uint pad_amount, char pad_char )
        {
            string_builder.Concat( ulong_val, pad_amount, pad_char, false, 10 );
            return string_builder;
        }

        //! Convert a given unsigned integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat(this StringBuilder string_builder, ulong ulong_val, uint pad_amount, char pad_char, bool thousand_sep)
        {
            string_builder.Concat(ulong_val, pad_amount, pad_char, thousand_sep, 10);
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Any base value allowed.
        public static StringBuilder Concat( this StringBuilder string_builder, long int_val, uint pad_amount, char pad_char, bool thousand_sep, uint base_val )
        {
            Debug.Assert( base_val > 0 && base_val <= 16 );

            // Deal with negative numbers
            if (int_val < 0)
            {
                string_builder.Append( '-' );
                ulong uint_val = ulong.MaxValue - ((ulong) int_val ) + 1; //< This is to deal with Int32.MinValue
                string_builder.Concat( uint_val, pad_amount, pad_char, thousand_sep, base_val );
            }
            else
            {
                string_builder.Concat((ulong)int_val, pad_amount, pad_char, thousand_sep, base_val );
            }

            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume no padding and base ten.
        public static StringBuilder Concat( this StringBuilder string_builder, long int_val )
        {
            string_builder.Concat( int_val, 0, ms_default_pad_char, false, 10 );
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat( this StringBuilder string_builder, long int_val, uint pad_amount )
        {
            string_builder.Concat( int_val, pad_amount, ms_default_pad_char, false, 10 );
            return string_builder;
        }

        //! Convert a given signed integer value to a string and concatenate onto the stringbuilder. Assume base ten.
        public static StringBuilder Concat( this StringBuilder string_builder, long int_val, uint pad_amount, char pad_char )
        {
            string_builder.Concat( int_val, pad_amount, pad_char, false, 10 );
            return string_builder;
        }

        public static StringBuilder Concat(this StringBuilder string_builder, long int_val, uint pad_amount, char pad_char, bool thousand_sep)
        {
            string_builder.Concat(int_val, pad_amount, pad_char, thousand_sep, 10);
            return string_builder;
        }

        //! Convert a given double value to a string and concatenate onto the stringbuilder
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places, uint pad_amount, char pad_char, bool thousand_sep)
        {
            if (decimal_places == 0)
            {
                // No decimal places, just round up and print it as an int
                long int_val = (long) Math.Floor(double_val);

                string_builder.Concat(int_val, pad_amount, ' ', thousand_sep, 10);
            }
            else
            {
                long int_part = (long)double_val;

                // First part is easy, just cast to an integer
                string_builder.Concat(int_part, pad_amount, ' ', thousand_sep, 10);

                // Decimal point
                string_builder.Append('.');

                // Work out remainder we need to print after the d.p.
                double remainder = Math.Abs(double_val - int_part);

                double r = remainder;

                //Leading zeros in the decimal portion
                remainder *= 10;
                decimal_places--;
                while (decimal_places > 0 && ((uint)remainder % 10) == 0)
                {
                    remainder *= 10;
                    decimal_places--;
                    string_builder.Append('0');
                }
                // Multiply up to become an int that we can print
                while (decimal_places > 0)
                {
                    remainder *= 10;
                    decimal_places--;
                }

                // Round up. It's guaranteed to be a positive number, so no extra work required here.
                // Commented since it adds a 0 for numbers like 0.96 ( prints 0.10 instead of 0.1)
                // Good enough for my use...
                //remainder += 0.5f;

                if ((ulong)remainder > 9)
                    MonoBehaviour.print("Wrong "   + (ulong)remainder + " " + remainder.ToString("F5") + " " +r.ToString("F5") + " "+ double_val.ToString("F5"));

                // All done, print that as an int!
                string_builder.Concat((ulong)remainder, 0, '0', false, 10);
            }
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes five decimal places, and no padding.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val)
        {
            string_builder.Concat(double_val, ms_default_decimal_places, 0, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder. Assumes no padding.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places)
        {
            string_builder.Concat(double_val, decimal_places, 0, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places, uint pad_amount)
        {
            string_builder.Concat(double_val, decimal_places, pad_amount, ms_default_pad_char, false);
            return string_builder;
        }

        //! Convert a given float value to a string and concatenate onto the stringbuilder.
        public static StringBuilder Concat(this StringBuilder string_builder, double double_val, uint decimal_places, uint pad_amount, char pad_char)
        {
            string_builder.Concat(double_val, decimal_places, pad_amount, pad_char, false);
            return string_builder;
        }
    }
}

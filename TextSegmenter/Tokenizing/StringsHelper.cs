using System;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.core
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class StringsHelper
    {
        private static char*     _UPPER_INVARIANT_MAP;
        private static CharType* _CHARTYPE_MAP;

        static StringsHelper()
        {
            _UPPER_INVARIANT_MAP = xlat_Unsafe.Inst._UPPER_INVARIANT_MAP;
            _CHARTYPE_MAP        = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }

        [M(O.AggressiveInlining)] public static string ToUpperInvariant( string value )
        {
            var len = value.Length;
            if ( 0 < len )
            {
                string valueUpper;

                const int THRESHOLD = 1024;
                if ( len <= THRESHOLD )
                {
                    var chars = stackalloc char[ len ];
                    fixed ( char* value_ptr = value )
                    {
                        for ( var i = 0; i < len; i++ )
                        {
                            chars[ i ] = _UPPER_INVARIANT_MAP[ value_ptr[ i ] ];
                        }
                    }
                    valueUpper = new string( chars, 0, len );
                }
                else
                {
                    valueUpper = new string( '\0', len ); // string.Copy( value ); // => [Obsolete( "This API should not be used to create mutable strings. See https://go.microsoft.com/fwlink/?linkid=2084035 for alternatives." )]
                    fixed ( char* value_ptr = value )
                    fixed ( char* valueUpper_ptr = valueUpper )
                    {
                        for ( var i = 0; i < len; i++ )
                        {
                            valueUpper_ptr[ i ] = _UPPER_INVARIANT_MAP[ value_ptr[ i ] ];
                        }
                    }                    
                }

                return (valueUpper);
            }
            return (string.Empty);
        }
        //[M(O.AggressiveInlining)] public static string ToUpperInvariant( string value )
        //{
        //    var len = value.Length;
        //    if ( 0 < len )
        //    {
        //        var valueUpper = string.Copy( value );
        //        fixed ( char* valueUpper_ptr = valueUpper )
        //        {
        //            for ( int i = 0; i < len; i++ )
        //            {
        //                var ptr = valueUpper_ptr + i;
        //                *ptr = *(_UPPER_INVARIANT_MAP + *ptr);
        //            }
        //        }
        //        return (valueUpper);
        //    }
        //    return (string.Empty);
        //}
        [M(O.AggressiveInlining)] public static string ToUpperInvariant( string value, out bool isNullOrWhiteSpace )
        {
            isNullOrWhiteSpace = true;
            var len = value.Length;
            if ( 0 < len )
            {
                string valueUpper;

                const int THRESHOLD = 1024;
                if ( len <= THRESHOLD )
                {
                    var chars = stackalloc char[ len ];
                    fixed ( char* value_ptr = value )
                    {
                        for ( var i = 0; i < len; i++ )
                        {
                            chars[ i ] = _UPPER_INVARIANT_MAP[ value_ptr[ i ] ];
                            if ( (_CHARTYPE_MAP[ chars[ i ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace )
                            {
                                isNullOrWhiteSpace = false;
                            }
                        }
                    }
                    valueUpper = new string( chars, 0, len );
                }
                else
                {
                    valueUpper = new string( '\0', len ); // string.Copy( value ); // => [Obsolete( "This API should not be used to create mutable strings. See https://go.microsoft.com/fwlink/?linkid=2084035 for alternatives." )]
                    fixed ( char* value_ptr = value )
                    fixed ( char* valueUpper_ptr = valueUpper )
                    {
                        for ( var i = 0; i < len; i++ )
                        {
                            valueUpper_ptr[ i ] = _UPPER_INVARIANT_MAP[ value_ptr[ i ] ];
                            if ( (_CHARTYPE_MAP[ valueUpper_ptr[ i ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace )
                            {
                                isNullOrWhiteSpace = false;
                            }
                        }
                    }                    
                }

                return (valueUpper);
            }
            return (string.Empty);
        }
        //[M(O.AggressiveInlining)] public static string ToUpperInvariant( string value, out bool isNullOrWhiteSpace )
        //{
        //    isNullOrWhiteSpace = true;
        //    var len = value.Length;
        //    if ( 0 < len )
        //    {
        //        var valueUpper = string.Copy( value );
        //        fixed ( char* valueUpper_ptr = valueUpper )
        //        {
        //            for ( int i = 0; i < len; i++ )
        //            {
        //                var ptr = valueUpper_ptr + i;
        //                *ptr = *(_UPPER_INVARIANT_MAP + *ptr);
        //                if ( (_CHARTYPE_MAP[ *ptr ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace )
        //                {
        //                    isNullOrWhiteSpace = false;
        //                }
        //            }
        //        }
        //        return (valueUpper);
        //    }
        //    return (string.Empty);
        //}
        [M(O.AggressiveInlining)] public static void   ToUpperInvariant( char* wordFrom, char* bufferTo )
        {
            for ( ; ; wordFrom++, bufferTo++ )
            {
                var ch = *wordFrom;
                *bufferTo = *(_UPPER_INVARIANT_MAP + ch);
                if ( ch == '\0' )
                {
                    return;
                }
            }            
        }
        [M(O.AggressiveInlining)] public static void   ToUpperInvariantInPlace( string value )
        {
            fixed ( char* value_ptr = value )
            {
                ToUpperInvariantInPlace( value_ptr );
            }
        }
        [M(O.AggressiveInlining)] public static void   ToUpperInvariantInPlace( char* word )
        {
            for ( ; ; word++ )
            {
                var ch = *word;
                if ( ch == '\0' )
                {
                    return;
                }
                *word = *(_UPPER_INVARIANT_MAP + ch);
            }
        }
        [M(O.AggressiveInlining)] public static void   ToUpperInvariantInPlace( char* word, out bool isNullOrWhiteSpace )
        {
            isNullOrWhiteSpace = true;
            for ( ; ; word++ )
            {
                var ch = *word;
                if ( ch == '\0' )
                {
                    return;
                }
                else if ( (_CHARTYPE_MAP[ ch ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace )
                {
                    isNullOrWhiteSpace = false;
                }
                *word = *(_UPPER_INVARIANT_MAP + ch);
            }
        }
        [M(O.AggressiveInlining)] public static void   ToUpperInvariantInPlace( char* word, int length )
        {
            for ( length--; 0 <= length; length-- )
            {
                word[ length ] = _UPPER_INVARIANT_MAP[ word[ length ] ];
            }
        }

        [M(O.AggressiveInlining)] public static string ToLowerInvariant( string value ) => value.ToLowerInvariant();

        [M(O.AggressiveInlining)] public static bool IsEqual( string first, string second )
        {
            int length = first.Length;
            if ( length != second.Length )
            {
                return (false);
            }
            if ( length == 0 )
            {
                return (true);
            }

            fixed ( char* first_ptr  = first )
            fixed ( char* second_ptr = second )
            {
                for ( int i = 0; i < length; i++ )
                {
                    if ( *(first_ptr + i) != *(second_ptr + i) ) //if ( GetLetter( first, i ) != GetLetter( second, i ) )
                    {
                        return (false);
                    }
                }
            }
            return (true);
        }
        [M(O.AggressiveInlining)] public static bool IsEqual( string first, char* second_ptr, int secondLength )
        {
            #region comm
            /*
            if ( first.Length != secondLength )
            {
                return (false);
            }
            if ( secondLength == 0 )
            {
                return (true);
            }
            */ 
            #endregion

            fixed ( char* first_ptr  = first )
            {
                for ( int i = 0; i < secondLength; i++ )
                {
                    if ( *(first_ptr + i) != *(second_ptr + i) )
                    {
                        return (false);
                    }
                }
            }
            return (true);
        }
        [M(O.AggressiveInlining)] public static bool IsEqual( string first, int firstIndex, string second )
        {
            int length = first.Length - firstIndex;
            if ( length != second.Length )
            {
                return (false);
            }
            if ( length == 0 )
            {
                return (true);
            }

            fixed ( char* first_base = first  )
            fixed ( char* second_ptr = second )
            {
                char* first_ptr = first_base + firstIndex;
                for ( int i = 0; i < length; i++ )
                {
                    if ( *(first_ptr + i) != *(second_ptr + i) ) //if ( GetLetter( first, i ) != GetLetter( second, i ) )
                    {
                        return (false);
                    }
                }
            }
            return (true);
        }
        [M(O.AggressiveInlining)] public static bool IsEqual( string first, int firstIndex, char* second_ptr, int secondLength )
        {
            int length = first.Length - firstIndex;
            if ( length != secondLength )
            {
                return (false);
            }
            if ( secondLength == 0 )
            {
                return (true);
            }

            fixed ( char* first_base = first  )
            {
                char* first_ptr = first_base + firstIndex;
                for ( int i = 0; i < secondLength; i++ )
                {
                    if ( *(first_ptr + i) != *(second_ptr + i) ) //if ( GetLetter( first, i ) != GetLetter( second, i ) )
                    {
                        return (false);
                    }
                }
            }
            return (true);
        }
        [M(O.AggressiveInlining)] public static bool IsEqual( IntPtr x, IntPtr y )
        {
            if ( x == y )
                return (true);

            for ( char* x_ptr = (char*) x.ToPointer(),
                        y_ptr = (char*) y.ToPointer(); ; x_ptr++, y_ptr++ )
            {
                var x_ch = *x_ptr;

                if ( x_ch != *y_ptr )
                    return (false);
                if ( x_ch == '\0' )
                    return (true);
            }
        }
        [M(O.AggressiveInlining)] public static bool IsEqual( char* x, char* y )
        {
            if ( x == y )
                return (true);

            for ( ; ; x++, y++)
            {
                var x_ch = *x;

                if ( x_ch != *y )
                    return (false);
                if ( x_ch == '\0' )
                    return (true);
            }
        }

        [M(O.AggressiveInlining)] public static int GetLength( char* _base )
        {
            for ( var ptr = _base; ; ptr++ )
            {
                if ( *ptr == '\0' )
                {
                    return ((int)(ptr - _base));
                }
            }
        }
        [M(O.AggressiveInlining)] public static int GetLength( IntPtr _base ) => GetLength( (char*) _base );

        [M(O.AggressiveInlining)] public static string ToString( char* value )
        {
            if ( value == null )
            {
                return (null);
            }

            var length = GetLength( value );
            if ( length == 0 )
            {
                return (string.Empty);
            }

            var str = new string( '\0', length );
            fixed ( char* str_ptr = str )
            {                
                for ( var wf_ptr = str_ptr; ; )
                {
                    var ch = *(value++);
                    if ( ch == '\0' )
                        break;
                    *(wf_ptr++) = ch;
                }
            }
            return (str);
        }
        [M(O.AggressiveInlining)] public static string ToString( char* value, int length )
        {
            if ( value == null )
            {
                return (null);
            }

            if ( length == 0 )
            {
                return (string.Empty);
            }

            var str = new string( '\0', length );
            fixed ( char* str_ptr = str )
            {
                for ( var wf_ptr = str_ptr; 0 < length; length-- )
                {
                    var ch = *(value++);
                    if ( ch == '\0' )
                        break;
                    *(wf_ptr++) = ch;
                }
            }
            return (str);
        }
        [M(O.AggressiveInlining)] public static string ToString( IntPtr value ) => ToString( (char*) value );
        [M(O.AggressiveInlining)] public static string ToString( IntPtr value, int length ) => ToString( (char*) value, length );

        [M(O.AggressiveInlining)] public static bool HasLetters( string value )
        {
            fixed ( char* value_ptr = value )
            {
                return (HasLetters( value_ptr ));
            }
        }
        [M(O.AggressiveInlining)] public static bool HasLetters( IntPtr value ) => HasLetters( (char*) value );
        [M(O.AggressiveInlining)] public static bool HasLetters( char* value )
        {
            if ( value == null )
            {
                return (false);
            }

            for ( ; ; )
            {
                var ch = *(value++);
                if ( ch == '\0' )
                {
                    return (false);
                }
                if ( (_CHARTYPE_MAP[ ch ] & CharType.IsLetter) == CharType.IsLetter )
                {
                    return (true);
                }
            }
        }

        [M(O.AggressiveInlining)] public static bool IsLetters( char ch ) => ((_CHARTYPE_MAP[ ch ] & CharType.IsLetter) == CharType.IsLetter);
        [M(O.AggressiveInlining)] public static bool IsDash( char ch )
        {
            switch ( ch )
            {
                case '‒':
                case '–':
                case '—':
                case '―':
                    return (true);

                default:
                    return (false);
            }
        }
        [M(O.AggressiveInlining)] public static bool IsLettersOrDash( char ch ) => (IsLetters( ch ) || IsDash( ch ));
    }
}

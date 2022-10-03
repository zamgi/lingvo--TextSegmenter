using System;
using System.Collections.Generic;

using lingvo.core;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static void ThrowIfNull( this object obj, string paramName )
        {
            if ( obj == null ) throw (new ArgumentNullException( paramName ));
        }
        public static void ThrowIfNullOrWhiteSpace( this string text, string paramName )
        {
            if ( string.IsNullOrWhiteSpace( text ) ) throw (new ArgumentNullException( paramName ));
        }     
        public static void ThrowIfNullOrWhiteSpaceAnyElement( this IEnumerable< string > seq, string paramName )
        {
            if ( seq == null ) throw (new ArgumentNullException( paramName ));

            foreach ( var c in seq )
            {
                if ( string.IsNullOrWhiteSpace( c ) )
                {
                    throw (new ArgumentNullException( $"'{paramName}' => some collection element is Null-or-WhiteSpace." ));
                }
            }
        }

        [M(O.AggressiveInlining)] public static bool IsNullOrWhiteSpace( this string text ) => string.IsNullOrWhiteSpace( text );
        [M(O.AggressiveInlining)] public static bool IsNullOrEmpty( this string text ) => string.IsNullOrEmpty( text );
        [M(O.AggressiveInlining)] private static ModelRecord ToModelRecord( this KeyValuePair< string, double > p ) => new ModelRecord() { Ngram = p.Key, Probability = p.Value };
        [M(O.AggressiveInlining)] private static ModelRecord ToModelRecord( this KeyValuePair< IntPtr, double > p ) => new ModelRecord() { Ngram = StringsHelper.ToString( p.Key ), Probability = p.Value };
        [M(O.AggressiveInlining)] public static IEnumerable< ModelRecord > GetAllModelRecords( this Dictionary< string, double > dict )
        {
            foreach ( var p in dict )
            {
                yield return (p.ToModelRecord());
            }            
        }
        [M(O.AggressiveInlining)] public static IEnumerable< ModelRecord > GetAllModelRecords( this Dictionary< IntPtr, double > dict )
        {
            foreach ( var p in dict )
            {
                yield return (p.ToModelRecord());
            }
        }
    }
}

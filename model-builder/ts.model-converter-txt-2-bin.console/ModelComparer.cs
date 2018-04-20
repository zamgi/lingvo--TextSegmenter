#if DEBUG

using System;
using System.Diagnostics;

namespace lingvo.ts.modelconverter
{
    /// <summary>
    /// 
    /// </summary>
    internal static class ModelComparer
    {
        unsafe public static void Compare( NativeTextMMFModelBinary model_1, ManagedTextModel model_2 )
        {
            var set  = NativeTextMMFModelBinary.FuskingTraitor.GetSet( model_1 );
            var dict = ManagedTextModel        .FuskingTraitor.GetDictionary( model_2 ); 

            Debug.Assert( set.Count == dict.Count );

            using ( var set_enm  = set .GetEnumerator() )
            using ( var dict_enm = dict.GetEnumerator() )
            {
                for ( ; set_enm.MoveNext() && dict_enm.MoveNext(); )
                {
                    var set_m      = NativeTextMMFModelBinary.ToModelRecord( set_enm.Current );
                    var dict_ngram = dict_enm.Current.Key;

                    Debug.Assert( set_m.Ngram == dict_ngram );

                    fixed ( char* dict_ngram_ptr = dict_ngram )
                    {
                        if ( !set.TryGetValue( (IntPtr) dict_ngram_ptr, out var exitstsPtr ) )
                        {
                            Debugger.Break();
                        }
                        if ( !dict.TryGetValue( set_m.Ngram, out var dict_prob ) )
                        {
                            Debugger.Break();
                        }
                        
                        Debug.Assert( set_m.Probability == dict_prob );
                    }
                }
            }
        }
    }
}

#endif
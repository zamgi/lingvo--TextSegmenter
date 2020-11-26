using System.Collections.Generic;

using lingvo.core;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ViterbiTextSegmentation
    {
        private IModel _Model;
        public ViterbiTextSegmentation( IModel model ) => _Model = model;

        [M(O.AggressiveInlining)] private double GetWordProbability( string word )
        {
            if ( (word.Length == 1) && !StringsHelper.IsLettersOrDash( word[ 0 ] ) )
            {
                return (1);
            }

            return (_Model.TryGetProbability( word, out var prob ) ? prob : 0);
        }

        public (List< string > words, List< double > probs) Run_v0_( string text )
        {
            var probs = new List< double > { 1.0 };
            var lasts = new List< int > { 0 };
            for ( int i = 0, len = text.Length; i < len; i++ )
            {
                var prob_k = 0.0;
                var k = 0;
                for ( int j = 0, x = i; j < x; j++ )
                {
                    var w = text.Substring( j, i - j );
                    var currProb = probs[ j ] * GetWordProbability( w );
                    if ( prob_k < currProb )
                    {
                        prob_k = currProb;
                        k = j;
                    }
                }
                probs.Add( prob_k );
                lasts.Add( k );
            }

            var words = new List< string >();
            for ( var i = text.Length; 0 < i; )
            {
                var j = lasts[ i ];
                var w = text.Substring( j, i - j );
                words.Add( w );
                i = j;
            }
            words.Reverse();
            return (words, probs);
        }

        [M(O.AggressiveInlining)] public List< TermProbability > Run_Debug( string text )
        {
            var length = text.Length;
            var probs  = new double[ length + 1 ]; 
            var terms  = new string[ length + 1 ];
            probs[ 0 ] = 1;
            for ( var i = 0; i <= length; i++ )
            {
                var probs_i = probs[ i ];
                for ( var j = 0; j < i; j++ ) 
                {
                    var term = text.Substring( j, i - j );
                    var term_prob = probs[ i - term.Length ] * GetWordProbability( term );
                    if ( probs_i <= term_prob )
                    {
                        probs[ i ] = probs_i = term_prob;
                        terms[ i ] = term;
                    }
                }
            }

            var tuples = new List< TermProbability >( length >> 2 );
            for ( var i = length; 0 < i; )
            {
                var term = terms[ i ];
                tuples.Add( new TermProbability() { Term = term, Probability = probs[ i ] } );
                i = i - term.Length;
            }
            tuples.Reverse();
            return (tuples);
        }
        [M(O.AggressiveInlining)] unsafe public List< TermProbability > Run( string text )
        {
            const int MAX_LENGTH_4_STATCK_ALLOC = 128;

            var terms = new string[ text.Length + 1 ];
            if ( text.Length < MAX_LENGTH_4_STATCK_ALLOC )
            {
                var probs = stackalloc double[ text.Length + 1 ];

                return (RunInternal( text, probs, terms ));
            }
            else
            {
                var probs = new double[ text.Length + 1 ];
                fixed ( double* probs_ptr = probs )
                {
                    return (RunInternal( text, probs_ptr, terms ));
                }
            }
        }
        [M(O.AggressiveInlining)] unsafe private List< TermProbability > RunInternal( string text, double* probs, string[] terms )
        {
            var length = text.Length;
            //var probs = new double[ length + 1 ]; 
            //var terms = new string[ length + 1 ];
            probs[ 0 ] = 1;
            for ( var i = 0; i <= length; i++ )
            {
                var probs_i = probs[ i ];
                for ( var j = 0; j < i; j++ ) 
                {
                    var term = text.Substring( j, i - j );
                    var term_prob = probs[ i - term.Length ] * GetWordProbability( term );
                    if ( probs_i <= term_prob )
                    {
                        probs[ i ] = probs_i = term_prob;
                        terms[ i ] = term;
                    }
                }
            }

            var tuples = new List< TermProbability >( length >> 2 );
            for ( var i = length; 0 < i; )
            {
                var term = terms[ i ];
                tuples.Add( new TermProbability() { Term = term, Probability = probs[ i ] } );
                i = i - term.Length;
            }
            tuples.Reverse();
            return (tuples);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class ViterbiTextSegmentation_Offset
    {
        private IModel _Model;
        public ViterbiTextSegmentation_Offset( IModel model ) => _Model = model;

        [M(O.AggressiveInlining)] unsafe private double GetWordProbability( in NativeOffset no )
        {
            if ( (no.Length == 1) && !StringsHelper.IsLettersOrDash( no.BasePtr[ no.StartIndex ] ) )
            {
                return (1);
            }

            return (_Model.TryGetProbability( in no, out var prob ) ? prob : 0);
        }

        [M(O.AggressiveInlining)] unsafe public List< TermProbability_Offset > Run_Debug( char* text, int length )
        {
            var probs   = new double      [ length + 1 ];
            var offsets = new NativeOffset[ length + 1 ];
            var no      = new NativeOffset() { BasePtr = text };
            probs[ 0 ] = 1;
            for ( var i = 0; i <= length; i++ )
            {
                var probs_i = probs[ i ];
                for ( var j = 0; j < i; j++ ) 
                {
                    no.StartIndex = j;
                    no.Length     = i - j;
                    var term_prob = probs[ i - no.Length ] * GetWordProbability( in no );
                    if ( probs_i <= term_prob )
                    {
                        probs  [ i ] = probs_i = term_prob;
                        offsets[ i ] = no;
                    }
                }
            }

            var tuples = new List< TermProbability_Offset >( length >> 2 );
            for ( var i = length; 0 < i; )
            {
                var no_ptr = offsets[ i ];
                tuples.Add( new TermProbability_Offset()
                {
                    StartIndex  = no_ptr.StartIndex,
                    Length      = no_ptr.Length,
                    Probability = probs[ i ],
                });
                i = i - no_ptr.Length;
            }
            tuples.Reverse();
            return (tuples);
        }
        [M(O.AggressiveInlining)] unsafe public List< TermProbability_Offset > Run( char* text, int length )
        {
            const int MAX_LENGTH_4_STATCK_ALLOC = 128;

            if ( length < MAX_LENGTH_4_STATCK_ALLOC )
            {
                var probs   = stackalloc double      [ length + 1 ];
                var offsets = stackalloc NativeOffset[ length + 1 ];

                return (RunInternal( text, length, probs, offsets ));
            }
            else
            {
                var probs   = new double      [ length + 1 ];
                var offsets = new NativeOffset[ length + 1 ];

                fixed ( double*       probs_ptr   = probs )
                fixed ( NativeOffset* offsets_ptr = offsets )
                {
                    return (RunInternal( text, length, probs_ptr, offsets_ptr ));
                }
            }
        }
        [M(O.AggressiveInlining)] unsafe private List< TermProbability_Offset > RunInternal( char* text, int length, double* probs, NativeOffset* offsets )
        {
            //var probs   = new double      [ length + 1 ];
            //var offsets = new NativeOffset[ length + 1 ];
            probs[ 0 ] = 1;
            var no = new NativeOffset() { BasePtr = text };
            for ( var i = 0; i <= length; i++ )
            {
                var probs_i = probs[ i ];
                for ( var j = 0; j < i; j++ ) 
                {
                    no.StartIndex = j;
                    no.Length     = i - j;
                    var term_prob = probs[ i - no.Length ] * GetWordProbability( in no );
                    if ( probs_i <= term_prob )
                    {
                        probs  [ i ] = probs_i = term_prob;
                        offsets[ i ] = no;
                    }
                }
            }

            var tuples = new List< TermProbability_Offset >( length >> 2 );
            for ( var i = length; 0 < i; )
            {
                var no_ptr = &offsets[ i ];
                tuples.Add( new TermProbability_Offset()
                {
                    StartIndex  = no_ptr->StartIndex,
                    Length      = no_ptr->Length,
                    Probability = probs[ i ],
                });
                i = i - no_ptr->Length;
            }
            tuples.Reverse();
            return (tuples);
        }
    }
}
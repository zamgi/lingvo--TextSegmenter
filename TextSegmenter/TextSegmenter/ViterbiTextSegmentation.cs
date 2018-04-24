using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class ViterbiTextSegmentation
    {
        private IModel _Model;

        public ViterbiTextSegmentation( IModel model )
        {
            _Model = model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double GetWordProbability( string word )
        {
            if ( (word.Length == 1) && !StringsHelper.IsLettersOrDash( word[ 0 ] ) )
            {
                return (1);
            }

            return (_Model.TryGetProbability( word, out var prob ) ? prob : 0);
        }

        public Tuple< List< string >, List< double > > Run_v0_( string text )
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

            {
                var words = new List< string >();
                for ( var i = text.Length; 0 < i; )
                {
                    var j = lasts[ i ];
                    var w = text.Substring( j, i - j );
                    words.Add( w );
                    i = j;
                }
                words.Reverse();
                return (Tuple.Create( words, probs ));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List< TermProbability > Run( string text )
        {
            var len   = text.Length;
            var probs = new double[ len + 1 ]; probs[ 0 ] = 1;
            var terms = new string[ len + 1 ];      
            for ( var i = 0; i <= len; i++ )
            {
                for ( var j = 0; j < i; j++ ) 
                {
                    var term = text.Substring( j, i - j );
                    var term_prob = probs[ i - term.Length ] * GetWordProbability( term );
                    if ( probs[ i ] <= term_prob )
                    {
                        probs[ i ] = term_prob;
                        terms[ i ] = term;
                    }
                }
            }

            var tuples = new List< TermProbability >();
            for ( var i = len; 0 < i; )
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

        public ViterbiTextSegmentation_Offset( IModel model )
        {
            _Model = model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe private double GetWordProbability( ref NativeOffset no )
        {
            if ( (no.Length == 1) && !StringsHelper.IsLettersOrDash( no.BasePtr[ no.StartIndex ] ) )
            {
                return (1);
            }

            return (_Model.TryGetProbability( ref no, out var prob ) ? prob : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public List< TermProbability_Offset > Run( char* text, int length )
        {
            var probs   = stackalloc double      [ length + 1 ]; probs[ 0 ] = 1;
            var offsets = stackalloc NativeOffset[ length + 1 ];
            var no      = new NativeOffset() { BasePtr = text };
            for ( var i = 0; i <= length; i++ )
            {
                for ( var j = 0; j < i; j++ ) 
                {
                    no.StartIndex = j;
                    no.Length     = i - j;
                    var term_prob = probs[ i - no.Length ] * GetWordProbability( ref no );
                    if ( probs[ i ] <= term_prob )
                    {
                        probs  [ i ] = term_prob;
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
                } );
                i = i - no_ptr->Length;
            }
            tuples.Reverse();
            return (tuples);
        }
    }
}
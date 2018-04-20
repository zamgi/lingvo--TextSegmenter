using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace lingvo.core.algorithm
{
    /// <summary>
    /// 
    /// </summary>
    internal enum NGramsEnum
    {
        ngram_1 = 1,
        ngram_2 = 2,
        ngram_3 = 3,
        ngram_4 = 4,
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class TermFrequency
    {
        public string Term;
        public int    Frequency;
#if DEBUG
        public override string ToString() => $"{Term}:{Frequency}";
#endif
    }
    /// <summary>
    /// 
    /// </summary>
    internal sealed class TermFrequencyComparer : IComparer<TermFrequency>
    {
        public static readonly TermFrequencyComparer Instance = new TermFrequencyComparer();
        private TermFrequencyComparer() { }

        public int Compare( TermFrequency x, TermFrequency y )
        {
            var d = y.Frequency - x.Frequency;
            if ( d != 0 )
                return (d);

            return (string.CompareOrdinal( x.Term, y.Term ));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class TFProcessor
    {
        /// <summary>
        /// 
        /// </summary>
        public struct TermProbability_t
        {
            public string Term;
            public double Probability;
#if DEBUG
            public override string ToString() => $"{Term}: {Probability}"; 
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        private struct TermProbabilityIComparer_t : IComparer< TermProbability_t >
        {
            public int Compare( TermProbability_t x, TermProbability_t y )
            {
                var d = y.Probability.CompareTo( x.Probability );
                if ( d != 0 )
                    return (d);

                return (string.CompareOrdinal( x.Term, y.Term ));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private struct TermFrequency_t
        {
            public string Term;
            public int    Frequency;
#if DEBUG
            public override string ToString() => $"{Term}: {Frequency}"; 
#endif
        }
        /// <summary>
        /// 
        /// </summary>
        private struct TermFrequencyIComparer_t : IComparer< TermFrequency_t >
        {
            public int Compare( TermFrequency_t x, TermFrequency_t y )
            {
                var d = y.Frequency - x.Frequency;
                if ( d != 0 )
                    return (d);

                return (string.CompareOrdinal( x.Term, y.Term ));
            }
        }

        private TFProcessor() { }

        private int _TotalWordCount;
        private Dictionary< string, int > _TermFrequency;

        public int TotalWordCount => _TotalWordCount;
        public int DictionarySize => _TermFrequency.Count;

        [ MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTerm( string term )
        {
            _TermFrequency.AddOrUpdate( term );
            _TotalWordCount++;
        }

        #region [.begin-end add terms.]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginAddDocumentTerms() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDocumentTerms( Dictionary< string, int > dict )
        {
            foreach ( var p in dict )
            {
                _TermFrequency.AddOrUpdate( p.Key, p.Value );
            }            
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddDocumentTerms( SortedSet< TermFrequency > ss )
        {
            foreach ( var tf in ss )
            {
                _TermFrequency.AddOrUpdate( tf.Term, tf.Frequency );
            }      
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndAddDocumentTerms( int documentWordCount ) => _TotalWordCount += documentWordCount;
        #endregion

        private Dictionary< string, double > CalcProbabilityWithCut( float leavePercent )
        {
            var ss = new SortedSet< TermFrequency_t >( default(TermFrequencyIComparer_t) );
            var sum = 0;
            foreach ( var p in _TermFrequency )
            {
                sum += p.Value;
                ss.Add( new TermFrequency_t() { Term = p.Key, Frequency = p.Value } );
            }

            var threshold = sum * leavePercent / 100.0;
            var threshold_current = 0;
                
            var dict = new Dictionary< string, double >( _TermFrequency.Count );
            _TermFrequency = null; //_TermFrequency.Clear();
            foreach ( var tf in ss )
            {
                threshold_current += tf.Frequency;
                if ( threshold < threshold_current )
                {
                    break;
                }

                //---_TermFrequency.Add( tf.Term, tf.Frequency );
                dict.Add( tf.Term, (1.0 * tf.Frequency / _TotalWordCount) );
            }
            return (dict);
        }
        public SortedSet< TermProbability_t > CalcProbabilityOrdered( float? cutPercent = null )
        {
            var ss = new SortedSet< TermProbability_t >( default(TermProbabilityIComparer_t) );

            if ( cutPercent.HasValue )
            {
                var leavePercent = 100 - cutPercent.Value;
                var dict = CalcProbabilityWithCut( leavePercent );
                
                foreach ( var p in dict )
                {
                    ss.Add( new TermProbability_t() { Term = p.Key, Probability = p.Value } );
                }
            }
            else
            {
                foreach ( var p in _TermFrequency )
                {
                    ss.Add( new TermProbability_t() { Term = p.Key, Probability = (1.0 * p.Value / _TotalWordCount) } );
                }
            }

            _TermFrequency = null;
            return (ss);
        }

        public static SortedSet< TermFrequency > CreateSortedSetAndCutIfNeed( IEnumerable< TermFrequency > tfs, float? cutPercent, int sum )
        {
            var ss = new SortedSet< TermFrequency >( TermFrequencyComparer.Instance );

            if ( cutPercent.HasValue )
            {
                var leavePercent      = 100 - cutPercent.Value;
                var threshold         = sum * leavePercent / 100.0;
                var threshold_current = 0;

                foreach ( var tf in tfs )
                {
                    threshold_current += tf.Frequency;
                    if ( threshold < threshold_current )
                    {
                        break;
                    }
                    ss.Add( tf );
                }
            }
            else
            {
                foreach ( var word in tfs )
                {
                    ss.Add( word );
                }                    
            }

            return (ss);
        }
        public static SortedSet< TermFrequency > CreateSortedSetAndCutIfNeed( IEnumerable< TermFrequency > tfs )
        {
            var ss = new SortedSet< TermFrequency >( TermFrequencyComparer.Instance );
            foreach ( var word in tfs )
            {
                ss.Add( word );
            }
            return (ss);
        }

        public static TFProcessor Create( int capacity ) => new TFProcessor() { _TermFrequency = new Dictionary< string, int >( capacity ) };

        public static void CutDictionaryIfNeed( Dictionary< string, int > dict, float? cutPercent = null )
        {
            if ( cutPercent.HasValue )
            {
                var ss = new SortedSet< TermFrequency_t >( default(TermFrequencyIComparer_t) );
                var sum = 0;
                foreach ( var p in dict )
                {
                    sum += p.Value;
                    ss.Add( new TermFrequency_t() { Term = p.Key, Frequency = p.Value } );
                }

                var leavePercent      = 100 - cutPercent.Value;
                var threshold         = sum * leavePercent / 100.0;
                var threshold_current = 0;

                dict.Clear();
                foreach ( var tf in ss )
                {
                    threshold_current += tf.Frequency;
                    if ( threshold < threshold_current )
                    {
                        break;
                    }

                    dict.Add( tf.Term, tf.Frequency );
                }
                ss = null;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddOrUpdate( this Dictionary< string, int > dict, string key )
        {
            if ( dict.TryGetValue( key, out var count ) )
            {
                dict[ key ] = count + 1;
            }
            else
            {
                dict.Add( key, 1 );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddOrUpdate( this Dictionary< string, int > dict, string key, int countValue )
        {
            if ( dict.TryGetValue( key, out var count ) )
            {
                dict[ key ] = count + countValue;
            }
            else
            {
                dict.Add( key, countValue );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Append( this Dictionary< string, int > dict, IList< string > words )
        {
            foreach ( var word in words )
            {
                dict.AddOrUpdate( word );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendDictionary( this Dictionary< string, int > masterDict, Dictionary< string, int > slaveDict )
        {
            foreach ( var p in slaveDict )
            {
                masterDict.AddOrUpdate( p.Key, p.Value );
            }
        }
    }
}

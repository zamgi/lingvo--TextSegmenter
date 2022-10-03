using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class UnionTextSegmenter : ITextSegmenter, IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        public enum LanguageEnum
        {
            RU, EN, DE, ES, FR
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct InitParam_v1
        {
            public LanguageEnum      Language     { get; init; }
            public BinaryModelConfig ModelConfigs { get; init; }

            public static InitParam_v1 Create( BinaryModelConfig cfg, LanguageEnum lang ) => new InitParam_v1() { ModelConfigs = cfg, Language = lang };
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct InitParam_v2
        {
            public LanguageEnum Language { get; init; }
            public IModel       Model    { get; init; }

            public static InitParam_v2 Create( IModel m, LanguageEnum lang ) => new InitParam_v2() { Model = m, Language = lang };
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct Result
        {
            public LanguageEnum Language { get; init; }
            public IReadOnlyList< TermProbability > TPS { get; init; }

            public static Result Create( IReadOnlyList< TermProbability > tps, LanguageEnum lang ) => new Result() { TPS = tps, Language = lang };
            public override string ToString() => $"'{Language}', ({string.Join( ", ", TPS )})";
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct Result_Offset
        {
            public LanguageEnum Language { get; init; }
            public IReadOnlyList< TermProbability_Offset > TPS { get; init; }

            public static Result_Offset Create( IReadOnlyList< TermProbability_Offset > tps, LanguageEnum lang ) => new Result_Offset() { TPS = tps, Language = lang };
        }

        #region [.ctor().]
        private NativeTextMMFModelBinary[] _NativeTextMMFModelsBinary;
        private TextSegmenter[]            _TextSegmenters;
        private LanguageEnum[]             _Languages;

        public UnionTextSegmenter( params InitParam_v1[] ps )
        {
            if ( (ps == null) || !ps.Any() ) throw (new ArgumentNullException( nameof(ps) ));

            #region comm. [.consecutively.]
            /*
            _NativeTextMMFModelsBinary = new NativeTextMMFModelBinary[ ps.Length ];
            _TextSegmenters            = new TextSegmenter           [ ps.Length ];
            _Languages                 = new LanguageEnum            [ ps.Length ];

            for ( var i = 0; i < ps.Length; i++ )
            {
                ref readonly var p = ref ps[ i ];
                var m = new NativeTextMMFModelBinary( p.ModelConfigs );
                _NativeTextMMFModelsBinary[ i ] = m;
                _TextSegmenters           [ i ] = new TextSegmenter( m );
                _Languages                [ i ] = p.Language;
            }
            //*/
            #endregion

            #region [.parallel.]
            var nativeTextMMFModelsBinary = new NativeTextMMFModelBinary[ ps.Length ];
            var textSegmenters            = new TextSegmenter           [ ps.Length ];
            var languages                 = new LanguageEnum            [ ps.Length ];

            Parallel.ForEach( ps, (p, _, i) =>
            {
                var m = new NativeTextMMFModelBinary( p.ModelConfigs );
                nativeTextMMFModelsBinary[ i ] = m;
                textSegmenters           [ i ] = new TextSegmenter( m );
                languages                [ i ] = p.Language;
            });

            _NativeTextMMFModelsBinary = nativeTextMMFModelsBinary;
            _TextSegmenters            = textSegmenters;
            _Languages                 = languages;
            #endregion
        }
        public UnionTextSegmenter( params InitParam_v2[] ps )
        {
            if ( (ps == null) || !ps.Any() ) throw (new ArgumentNullException( nameof(ps) ));

            _NativeTextMMFModelsBinary = null;
            _TextSegmenters            = new TextSegmenter[ ps.Length ];
            _Languages                 = new LanguageEnum [ ps.Length ];

            for ( var i = 0; i < ps.Length; i++ )
            {
                ref readonly var p = ref ps[ i ];
                _TextSegmenters[ i ] = new TextSegmenter( p.Model );
                _Languages     [ i ] = p.Language;
            }
        }

        public void Dispose()
        {
            if ( _NativeTextMMFModelsBinary != null )
            {
                foreach ( var m in _NativeTextMMFModelsBinary )
                {
                    m.Dispose();
                }
                _NativeTextMMFModelsBinary = null;
            }
            if ( _TextSegmenters != null )
            {
                foreach ( var ts in _TextSegmenters )
                {
                    ts.Dispose();
                }
                _TextSegmenters = null;
            }
            _Languages = null;
        }
        #endregion

        public IReadOnlyList< TermProbability > Run( string text ) => RunBest( text ).TPS;
        public IReadOnlyList< TermProbability > Run_Debug( string text ) => RunBest_Debug( text ).TPS;
        public IReadOnlyList< TermProbability_Offset > Run_Offset( string text ) => RunBest_Offset( text ).TPS;

        private TextSegmenter GetTextSegmenter( LanguageEnum lang )
        {
            for ( var i = _Languages.Length - 1; 0 <= i; i-- )
            {
                if ( _Languages[ i ] == lang )
                {
                    return (_TextSegmenters[ i ]);
                }
            }

            throw (new ArgumentException( $"Language not found: '{lang}'" ));
        }
        public IReadOnlyList< TermProbability > Run( string text, LanguageEnum lang ) => GetTextSegmenter( lang ).Run( text );
        public IReadOnlyList< TermProbability > Run_Debug( string text, LanguageEnum lang ) => GetTextSegmenter( lang ).Run_Debug( text );
        public IReadOnlyList< TermProbability_Offset > Run_Offset( string text, LanguageEnum lang ) => GetTextSegmenter( lang ).Run_Offset( text );

        public Result RunBest( string text )
        {
            var bestResult = default(Result);
            var bestProb   = double.NegativeInfinity;
            for ( var i = _TextSegmenters.Length - 1; 0 <= i; i-- )
            {
                var tps  = _TextSegmenters[ i ].Run( text );
                var prob = tps.Sum( tp => Math.Log( tp.Probability ) ).N();
                if ( bestProb < prob )
                {
                    bestProb   = prob;
                    bestResult = Result.Create( tps, _Languages[ i ]);
                }
            }
            return (bestResult);
        }
        public Result RunBest_Debug( string text )
        {
            var bestResult = default(Result);
            var bestProb   = double.NegativeInfinity;
            for ( var i = _TextSegmenters.Length - 1; 0 <= i; i-- )
            {
                var tps = _TextSegmenters[ i ].Run_Debug( text );
                var prob = tps.Sum( tp => Math.Log( tp.Probability ) ).N();
                if ( bestProb < prob )
                {
                    bestProb   = prob;
                    bestResult = Result.Create( tps, _Languages[ i ] );
                }
            }
            return (bestResult);
        }
        public Result_Offset RunBest_Offset( string text )
        {
            var bestResult = default(Result_Offset);

            #region [.parallel.]
            if ( _TextSegmenters.Length == 1 )
            {
                var tps = _TextSegmenters[ 0 ].Run_Offset( text );
                bestResult = Result_Offset.Create( tps, _Languages[ 0 ] );                
            }
            else
            {
                #region comm. v1
                /*
                var res = new (IReadOnlyList< TermProbability_Offset > tps, double prob, LanguageEnum lang)[ _TextSegmenters.Length ];
                Parallel.For( 0, _TextSegmenters.Length, i =>
                {
                    var tps  = _TextSegmenters[ i ].Run_Offset( text );
                    var prob = tps.Sum( tp => Math.Log( tp.Probability ) ).N();
                    res[ i ] = (tps, prob, _Languages[ i ]);
                });

                var bestProb = double.NegativeInfinity;
                for ( var i = res.Length - 1; 0 <= i; i-- )
                {
                    ref readonly var t = ref res[ i ];
                    if ( bestProb < t.prob )
                    {
                        bestProb   = t.prob;
                        bestResult = Result_Offset.Create( t.tps, t.lang );
                    }
                }                 
                */
                #endregion

                var llock = new LightLock();
                var bestProb = double.NegativeInfinity;
                Parallel.For( 0, _TextSegmenters.Length, i =>
                {
                    var tps  = _TextSegmenters[ i ].Run_Offset( text );
                    var prob = tps.Sum( tp => Math.Log( tp.Probability ) ).N();

                    llock.Enter();
                    if ( bestProb < prob )
                    {
                        bestProb   = prob;
                        bestResult = Result_Offset.Create( tps, _Languages[ i ] );
                    }
                    llock.Exit();
                });
            }

            return (bestResult);
            #endregion

            #region comm. [.consecutively.]
            /*
            var bestResult = default(Result_Offset);
            var bestProb   = double.NegativeInfinity;
            for ( var i = _TextSegmenters.Length - 1; 0 <= i; i-- )
            {
                var tps  = _TextSegmenters[ i ].Run_Offset( text );
                var prob = tps.Sum( tp => Math.Log( tp.Probability ) ).N();
                if ( bestProb < prob )
                {
                    bestProb   = prob;
                    bestResult = Result_Offset.Create( tps, _Languages[ i ] );
                }
            }
            return (bestResult);
            //*/
            #endregion
        }

        public IReadOnlyCollection< (double prob, Result r) > Run4All( string text )
        {
            var res = new (double prob, Result r)[ _TextSegmenters.Length ];
            var prob_min = double.MaxValue;

            for ( var i = _TextSegmenters.Length - 1; 0 <= i; i-- )
            {
                var tps    = _TextSegmenters[ i ].Run( text );
                var result = Result.Create( tps, _Languages[ i ] );
                var prob   = tps.Sum( tp => Math.Log( tp.Probability ) ).N();

                res[ i ] = (prob, result);

                if ( prob < prob_min )
                {
                    prob_min = prob;
                }
            }

            prob_min = Math.Abs( prob_min );
            var exps_sum = 0.0;
            for ( int len = res.Length - 1, i = len; 0 <= i; i-- )
            {
                ref var t = ref res[ i ];

                var exp = Math.Exp( t.prob / prob_min );
                t.prob = exp;
                exps_sum += exp;
            }
            
            for ( int len = res.Length - 1, i = len; 0 <= i; i-- )
            {
                ref var t = ref res[ i ];

                t.prob /= exps_sum;
            }

            return (res);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal struct LightLock
    {
        private const int OCCUPIED = 1;
        private const int FREE     = 0;

        private int _Lock;

        [M(O.AggressiveInlining)] public bool TryEnter() => (Interlocked.CompareExchange( ref _Lock, OCCUPIED, FREE ) == FREE);
        [M(O.AggressiveInlining)] public void Enter()
        {
            if ( Interlocked.CompareExchange( ref _Lock, OCCUPIED, FREE ) != FREE )
            {
                var spinWait = default(SpinWait);
                while ( Interlocked.CompareExchange( ref _Lock, OCCUPIED, FREE ) != FREE )
                {
                    spinWait.SpinOnce();
                }
            }
        }
        [M(O.AggressiveInlining)] public void Exit() => Interlocked.Exchange( ref _Lock, FREE ); //Volatile.Write( ref _Locker, FREE );
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class UnionTextSegmenterExtensions
    {
        [M(O.AggressiveInlining)] public static double N( this double d ) => (double.IsNegativeInfinity( d ) ? double.MinValue : d);
    }
}

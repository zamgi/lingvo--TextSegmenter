using System;
using System.Collections.Generic;
using System.Linq;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public struct UnionTextSegmenter : ITextSegmenter, IDisposable
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
        public struct InitParam_v1
        {
            public LanguageEnum      Language     { get; set; }
            public BinaryModelConfig ModelConfigs { get; set; }

            public static InitParam_v1 Create( BinaryModelConfig cfg, LanguageEnum lang ) => new InitParam_v1() { ModelConfigs = cfg, Language = lang };
        }
        /// <summary>
        /// 
        /// </summary>
        public struct InitParam_v2
        {
            public LanguageEnum Language { get; set; }
            public IModel       Model    { get; set; }

            public static InitParam_v2 Create( IModel m, LanguageEnum lang ) => new InitParam_v2() { Model = m, Language = lang };
        }
        /// <summary>
        /// 
        /// </summary>
        public struct Result
        {
            public LanguageEnum Language { get; private set; }
            public IReadOnlyList< TermProbability > TPS { get; private set; }

            public static Result Create( IReadOnlyList< TermProbability > tps, LanguageEnum lang ) => new Result() { TPS = tps, Language = lang };
        }
        /// <summary>
        /// 
        /// </summary>
        public struct Result_Offset
        {
            public LanguageEnum Language { get; private set; }
            public IReadOnlyList< TermProbability_Offset > TPS { get; private set; }

            public static Result_Offset Create( IReadOnlyList< TermProbability_Offset > tps, LanguageEnum lang ) => new Result_Offset() { TPS = tps, Language = lang };
        }

        private NativeTextMMFModelBinary[] _NativeTextMMFModelsBinary;
        private TextSegmenter[]            _TextSegmenters;
        private LanguageEnum[]             _Languages;

        public UnionTextSegmenter( params InitParam_v1[] ps )
        {
            if ( ps == null || !ps.Any() ) throw (new ArgumentNullException( nameof(ps) ));

            _NativeTextMMFModelsBinary = new NativeTextMMFModelBinary[ ps.Length ];
            _TextSegmenters            = new TextSegmenter           [ ps.Length ];
            _Languages                 = new LanguageEnum            [ ps.Length ];

            for ( var i = 0; i < ps.Length; i++ )
            {
                ref var p = ref ps[ i ];
                var m = new NativeTextMMFModelBinary( p.ModelConfigs );
                _NativeTextMMFModelsBinary[ i ] = m;
                _TextSegmenters           [ i ] = new TextSegmenter( m );
                _Languages                [ i ] = p.Language;
            }
        }
        public UnionTextSegmenter( params InitParam_v2[] ps )
        {
            if ( ps == null || !ps.Any() ) throw (new ArgumentNullException( nameof(ps) ));

            _NativeTextMMFModelsBinary = null;
            _TextSegmenters            = new TextSegmenter[ ps.Length ];
            _Languages                 = new LanguageEnum [ ps.Length ];

            for ( var i = 0; i < ps.Length; i++ )
            {
                ref var p = ref ps[ i ];
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

        public IReadOnlyList< TermProbability > Run( string text ) => RunBest( text ).TPS;
        public IReadOnlyList< TermProbability_Offset > Run_Offset( string text ) => RunBest_Offset( text ).TPS;

        private TextSegmenter GetByLanguage( LanguageEnum lang )
        {
            for ( var i = _Languages.Length - 1; 0 <= i; i-- )
            {
                if ( _Languages[ i ] == lang )
                {
                    return (_TextSegmenters[ i ]);
                }
            }
            return (null);
        }
        public IReadOnlyList< TermProbability > Run( string text, LanguageEnum lang )
        {
            var ts = GetByLanguage( lang );
            if ( ts == null )
            {
                throw (new ArgumentException( $"Language not found: '{lang}'" ));
            }
            return (ts.Run( text ));
        }
        public IReadOnlyList< TermProbability_Offset > Run_Offset( string text, LanguageEnum lang )
        {
            var ts = GetByLanguage( lang );
            if ( ts == null )
            {
                throw (new ArgumentException( $"Language not found: '{lang}'" ));
            }
            return (ts.Run_Offset( text ));
        }

        public Result RunBest( string text )
        {
            var bestResult = default(Result);
            var bestProb   = double.NegativeInfinity;
            for ( var i = _TextSegmenters.Length - 1; 0 <= i; i-- )
            {
                var tps  = _TextSegmenters[ i ].Run( text );
                var prob = tps.Sum( tp => Math.Log( tp.Probability ) );
                if ( bestProb < prob )
                {
                    bestProb   = prob;
                    bestResult = Result.Create( tps, _Languages[ i ]);
                }
            }
            return (bestResult);
        }
        public Result_Offset RunBest_Offset( string text )
        {
            var bestResult = default(Result_Offset);
            var bestProb   = double.NegativeInfinity;
            for ( var i = _TextSegmenters.Length - 1; 0 <= i; i-- )
            {
                var tps  = _TextSegmenters[ i ].Run_Offset( text );
                var prob = tps.Sum( tp => Math.Log( tp.Probability ) );
                if ( bestProb < prob )
                {
                    bestProb   = prob;
                    bestResult = Result_Offset.Create( tps, _Languages[ i ] );
                }
            }
            return (bestResult);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using JP = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using JI = System.Text.Json.Serialization.JsonIgnoreAttribute;
using _ULanguage_     = lingvo.ts.UnionTextSegmenter.LanguageEnum;
using _UResultOffset_ = lingvo.ts.UnionTextSegmenter.Result_Offset;

namespace lingvo.ts.webService
{
    /// <summary>
    /// 
    /// </summary>
    public struct InitParamsVM
    {
        [JP("text")] public string Text  { get; set; }
        [JP("lang")] public string _Lang { get; set; }
        public _ULanguage_? GetLang() => (Enum.TryParse<_ULanguage_>( _Lang, true, out var x ) ? x : null);
#if DEBUG
        public override string ToString() => $"{(GetLang()?.ToString() ?? "[-=ANY=-]")}, '{Text}'";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    internal readonly struct ResultVM
    {
        /// <summary>
        /// 
        /// </summary>
        public struct tuple_TermProbability_Offset
        {
            public string          Text;
            public _UResultOffset_ Result;

            public static tuple_TermProbability_Offset Create( string text, _UResultOffset_ result ) =>
                new tuple_TermProbability_Offset() { Text = text, Result = result };
        }

        /// <summary>
        /// 
        /// </summary>
        public readonly struct term_prob_t
        {
            [JP("term")] public string term { get; init; }
            [JP("prob")] public double prob { get; init; }
        }
        /// <summary>
        /// 
        /// </summary>
        public readonly struct term_probs_t
        {
            [JP("tps" )] public term_prob_t[] term_probs { get; init; }
            [JP("text")] public string        text       { get; init; }
            [JP("lang")] public string        lang       { get; init; }
        }

        public ResultVM( in InitParamsVM m, Exception ex ) : this() => (init_params, exception_message) = (m, ex.Message);
        public ResultVM( in InitParamsVM m, ICollection< tuple_TermProbability_Offset > termProbsByRows ) : this()
        {
            init_params = m;
            if ( termProbsByRows.AnyEx() )
            {
                term_probs_by_rows = (from x in termProbsByRows
                                      let text = x.Text
                                      let lang = x. Result.Language.ToString()
                                      let term_probs = new term_probs_t()
                                      {
                                          text       = text,
                                          lang       = lang,
                                          term_probs = (x.Result.TPS != null && x.Result.TPS.Any())
                                                       ? (from t in x.Result.TPS
                                                          select
                                                          new term_prob_t()
                                                          {
                                                              term = t.GetTerm( text ),
                                                              prob = t.Probability,
                                                          }
                                                         ).ToArray()
                                                       : new[] { new term_prob_t() { term = "-= EMPTY =-" } }
                                      }
                                      select term_probs
                                     ).ToList();
            }
        }

        [JP("ip")  ] public InitParamsVM                  init_params        { get; }
        [JP("ttps")] public IReadOnlyList< term_probs_t > term_probs_by_rows { get; }
        [JP("err") ] public string                        exception_message  { get; }
    }
}

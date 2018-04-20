using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

using Newtonsoft.Json;
using _UInitParam_    = lingvo.ts.UnionTextSegmenter.InitParam_v2;
using _ULanguage_     = lingvo.ts.UnionTextSegmenter.LanguageEnum;
using _UResultOffset_ = lingvo.ts.UnionTextSegmenter.Result_Offset;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RESTProcessHandler : IHttpHandler
    {
        /// <summary>
        /// 
        /// </summary>
        internal struct result
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
            public struct term_prob_t
            {
                [JsonProperty(PropertyName="term")] public string term { get; set; }
                [JsonProperty(PropertyName="prob")] public double prob { get; set; }
            }
            /// <summary>
            /// 
            /// </summary>
            public struct term_probs_t
            {
                [JsonProperty(PropertyName="tps" )] public term_prob_t[] term_probs { get; set; }
                [JsonProperty(PropertyName="text")] public string        text       { get; set; }
                [JsonProperty(PropertyName="lang")] public string        lang       { get; set; }
            }

            public result( Exception ex ) : this()
            {
                exception_message = ex.ToString();
            }
            public result( ICollection< tuple_TermProbability_Offset > termProbsByRows ) : this()
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
                                     ).ToArray();
            }

            [JsonProperty(PropertyName="ttps")] public term_probs_t[] term_probs_by_rows { get; private set; }
            [JsonProperty(PropertyName="err" )] public string         exception_message  { get; private set; }
        }

        /// <summary>
        /// 
        /// </summary>
        private struct processing_context
        {
            private static readonly object _SyncLock = new object();

            //public processing_context( language language )
            //{
            //    switch ( language )
            //    {
            //        case language.en : GetConcurrentFactory = GetConcurrentFactory_EN; break;
            //        case language.ru : GetConcurrentFactory = GetConcurrentFactory_RU; break;
            //        case language.any: GetConcurrentFactory = null; break;
            //        default:
            //            throw (new ArgumentException( language.ToString() ));
            //    }
            //}

            //public Func< ConcurrentFactory > GetConcurrentFactory;

            //private static ConcurrentFactory _ConcurrentFactory_RU;
            //private static ConcurrentFactory _ConcurrentFactory_EN;

            //public static ConcurrentFactory GetConcurrentFactory_RU()
            //{
            //    var f = _ConcurrentFactory_RU;
            //    if ( f == null )
            //    {
            //        lock ( _SyncLock )
            //        {
            //            f = _ConcurrentFactory_RU;
            //            if ( f == null )
            //            {
            //                var model = new NativeTextMMFModelBinary( Config.Inst.GetBinaryModelConfig_RU() );

            //                f = new ConcurrentFactory( model, Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT );
            //                _ConcurrentFactory_RU = f;
            //                //GC.KeepAlive( _ConcurrentFactory_RU );
            //            }
            //        }
            //    }
            //    return (f);
            //}
            //public static ConcurrentFactory GetConcurrentFactory_EN()
            //{
            //    var f = _ConcurrentFactory_EN;
            //    if ( f == null )
            //    {
            //        lock ( _SyncLock )
            //        {
            //            f = _ConcurrentFactory_EN;
            //            if ( f == null )
            //            {
            //                var model = new NativeTextMMFModelBinary( Config.Inst.GetBinaryModelConfig_EN() );

            //                f = new ConcurrentFactory( model, Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT );                            
            //                _ConcurrentFactory_EN = f;
            //                //GC.KeepAlive( _ConcurrentFactory_EN );
            //            }
            //        }
            //    }
            //    return (f);
            //}

            private static ConcurrentFactory _ConcurrentFactory;

            private static _UInitParam_[] LoadModels()
            {
                var ps = new List< _UInitParam_ >( Enum.GetValues( typeof(_ULanguage_) ).Length );
                try
                {
                    ps.Add( _UInitParam_.Create( new NativeTextMMFModelBinary( Config.Inst.GetBinaryModelConfig_RU() ), _ULanguage_.RU ) );
                    ps.Add( _UInitParam_.Create( new NativeTextMMFModelBinary( Config.Inst.GetBinaryModelConfig_EN() ), _ULanguage_.EN ) );
                    return (ps.ToArray());
                }
                catch ( Exception ex )
                {
                    Debug.WriteLine( ex );
                    ps.ForEach( p => p.Model.Dispose() );
                    throw;
                }
            }
            public static ConcurrentFactory GetConcurrentFactory()
            {
                var f = _ConcurrentFactory;
                if ( f == null )
                {
                    lock ( _SyncLock )
                    {
                        f = _ConcurrentFactory;
                        if ( f == null )
                        {
                            var ps = LoadModels();

                            f = new ConcurrentFactory( Config.Inst.CONCURRENT_FACTORY_INSTANCE_COUNT, ps );
                            _ConcurrentFactory = f;
                            //GC.KeepAlive( _ConcurrentFactory );
                        }
                    }
                }
                return (f);
            }
        }


        static RESTProcessHandler() => Environment.CurrentDirectory = HttpContext.Current.Server.MapPath( "~/" );

        public bool IsReusable => true;

        public void ProcessRequest( HttpContext context )
        {
            #region [.log.]
            if ( Log.ProcessViewCommand( context ) )
            {
                return;
            }
            #endregion

            var text = default(string);
            try
            {
                text = context.GetRequestStringParam( "text", Config.Inst.MAX_INPUTTEXT_LENGTH );
                var lang = context.TryGetRequestEnumParam< _ULanguage_ >( "lang" );

                var rows = from textRow in text.Split( new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries )
                           where (!string.IsNullOrWhiteSpace( textRow ))
                           select textRow;

                var termProbsByRows = default(ICollection< result.tuple_TermProbability_Offset >);
                if ( lang.HasValue )
                {
                    termProbsByRows = (from textRow in rows
                                       let r = processing_context.GetConcurrentFactory().Run_Offset( textRow, lang.Value )
                                       select result.tuple_TermProbability_Offset.Create( textRow, r )
                                      )
                                      .ToArray();
                }
                else
                {
                    termProbsByRows = (from textRow in rows
                                       let r = processing_context.GetConcurrentFactory().RunBest_Offset( textRow ) 
                                       select result.tuple_TermProbability_Offset.Create( textRow, r )
                                      )
                                      .ToArray();

                }

                Log.Info( context, text );
                context.Response.ToJson( termProbsByRows );
            }
            catch ( Exception ex )
            {
                Log.Error( context, text, ex );
                context.Response.ToJson( ex );
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        public static string GetRequestStringParam( this HttpContext context, string paramName, int maxLength )
        {
            var value = context.Request[ paramName ];
            if ( (value != null) && (maxLength < value.Length) && (0 < maxLength) )
            {
                return (value.Substring( 0, maxLength ));
            }
            return (value);
        }
        public static T GetRequestEnumParam< T >( this HttpContext context, string paramName ) 
            where T : struct => ((T) Enum.Parse( typeof(T), context.Request[ paramName ], true ));
        public static T? TryGetRequestEnumParam< T >( this HttpContext context, string paramName ) 
            where T : struct => (Enum.TryParse< T >( context.Request[ paramName ], true, out var t ) ? t : ((T?) null));

        public static void ToJson( this HttpResponse response, ICollection< RESTProcessHandler.result.tuple_TermProbability_Offset > termProbsByRows ) => 
            response.ToJson( new RESTProcessHandler.result( termProbsByRows ) );
        public static void ToJson( this HttpResponse response, Exception ex ) => 
            response.ToJson( new RESTProcessHandler.result( ex ) );
        public static void ToJson( this HttpResponse response, RESTProcessHandler.result result )
        {
            response.ContentType = "application/json";
            //---response.Headers.Add( "Access-Control-Allow-Origin", "*" );

            var json = JsonConvert.SerializeObject( result );
            response.Write( json );
        }
    }
}
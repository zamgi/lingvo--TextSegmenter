using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;

using _UInitParam_    = lingvo.ts.UnionTextSegmenter.InitParam_v1;
using _ULanguage_     = lingvo.ts.UnionTextSegmenter.LanguageEnum;
using _UResult_       = lingvo.ts.UnionTextSegmenter.Result;
using _UResultOffset_ = lingvo.ts.UnionTextSegmenter.Result_Offset;

namespace lingvo.ts.TestApp
{
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config
    {
        private Config()
        {
            RU_TEXT_MODEL_FILENAME = ConfigurationManager.AppSettings[ "RU_TEXT_MODEL_FILENAME" ];

            _RU_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "RU" );
            _RU_BINARY_MODEL_FILENAMES     = Get_BINARY_MODEL_FILENAMES  ( "RU" );

            _EN_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "EN" );
            _EN_BINARY_MODEL_FILENAMES     = Get_BINARY_MODEL_FILENAMES  ( "EN" );
        }

        private static int      Get_MODEL_DICTIONARY_CAPACITY( string lang ) => int.Parse( ConfigurationManager.AppSettings[ $"{lang}_MODEL_DICTIONARY_CAPACITY" ] );
        private static string   Get_BINARY_MODEL_FILENAME    ( string lang ) => ConfigurationManager.AppSettings[ $"{lang}_BINARY_MODEL_FILENAME" ];
        private static string[] Get_BINARY_MODEL_FILENAMES   ( string lang )
        {
            var binaryModelDirectory = ConfigurationManager.AppSettings[ $"{lang}_BINARY_MODEL_DIRECTORY" ] ?? string.Empty;
            var bmfns = ConfigurationManager.AppSettings[ $"{lang}_BINARY_MODEL_FILENAMES" ] ?? string.Empty;
            var binaryModelFilenames = (from fn in bmfns.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                                        let fileName = fn.Trim()
                                        where ( !string.IsNullOrEmpty( fileName ) )
                                        select Path.Combine( binaryModelDirectory, fileName )
                                       ).ToArray();
            return (binaryModelFilenames);
        }

        private static Config _Inst;
        public static Config Inst
        {
            get
            {
                if ( _Inst == null )
                {
                    lock ( typeof(Config) )
                    {
                        if ( _Inst == null )
                        {
                            _Inst = new Config();
                        }
                    }
                }
                return (_Inst);
            }
        }

        public  string   RU_TEXT_MODEL_FILENAME { get; private set; }
        private string[] _RU_BINARY_MODEL_FILENAMES;
        private int      _RU_MODEL_DICTIONARY_CAPACITY;
        private string[] _EN_BINARY_MODEL_FILENAMES;
        private int      _EN_MODEL_DICTIONARY_CAPACITY;

        public TextModelConfig GetTextModelConfig_RU() => new TextModelConfig( RU_TEXT_MODEL_FILENAME, _RU_MODEL_DICTIONARY_CAPACITY );

        public BinaryModelConfig GetBinaryModelConfig_RU() => new BinaryModelConfig( _RU_BINARY_MODEL_FILENAMES ) { ModelDictionaryCapacity = _RU_MODEL_DICTIONARY_CAPACITY };
        public BinaryModelConfig GetBinaryModelConfig_EN() => new BinaryModelConfig( _EN_BINARY_MODEL_FILENAMES ) { ModelDictionaryCapacity = _EN_MODEL_DICTIONARY_CAPACITY };
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            #region [.GC.]
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
            {
                GCSettings.LatencyMode = GCLatencyMode.Batch;
            }
            #endregion

            Test__UnionTextSegmenter();
            //---Test__NativeTextMMFModelBinary_RU();
            //---Test__ManagedTextModel_RU();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine( "\r\n[.....finita.....]" );
            Console.ReadLine();
        }

        private static void Test__ManagedTextModel_RU()
        {
            Console.Write( "load -text- language model..." );

            var sw = Stopwatch.StartNew();            
            using ( var model = new ManagedTextModel( Config.Inst.GetTextModelConfig_RU() ) )
            {
                var count = model.RecordCount;
                sw.Stop();

                    GCCollect();
                Console.WriteLine( $"=> elapsed: {sw.Elapsed}, record-count: {count}\r\n" );

                //*
                Test__TextSegmenter( model );
                //* /

                Console.Write( "\r\ndisposing language model..." );                    
            }
            GCCollect();
            Console.WriteLine( "=> end\r\n" );
        }
        private static void Test__NativeTextMMFModelBinary_RU()
        {
            Console.Write( "load -binary- language model..." );

            var sw = Stopwatch.StartNew();
            using ( var model = new NativeTextMMFModelBinary( Config.Inst.GetBinaryModelConfig_RU() ) )
            {
                var count = model.RecordCount;
                sw.Stop();

                    GCCollect();
                Console.WriteLine( $"=> elapsed: {sw.Elapsed}, record-count: {count}\r\n" );

                //*
                Test__TextSegmenter( model );
                //* /

                Console.Write( "\r\ndisposing language model..." );
            }
            GCCollect();
            Console.WriteLine( "=> end\r\n" );
        }

        private static void Test__UnionTextSegmenter()
        {
            Console.Write( "load -binary- language model's..." );

            var sw = Stopwatch.StartNew();
            using ( var uts = new UnionTextSegmenter( _UInitParam_.Create( Config.Inst.GetBinaryModelConfig_RU(), _ULanguage_.RU ),
                                                      _UInitParam_.Create( Config.Inst.GetBinaryModelConfig_EN(), _ULanguage_.EN ) )
                  )
            {
                sw.Stop();

                GCCollect();
                Console.WriteLine( $"=> elapsed: {sw.Elapsed}\r\n" );

                //*
                Test__UnionTextSegmenter( uts );
                //* /

                Console.Write( "\r\ndisposing language model's..." );
            }
            GCCollect();
            Console.WriteLine( "=> end\r\n" );
        }

        private static void Test__TextSegmenter( IModel model )
        {
            using ( var ts = new TextSegmenter( model ) )
            {
                ts.RunToConsole( "бабушкакозликаоченьлюбила" );
                ts.RunToConsole( "баб_ушкакозликаоченьлюбила" );
                ts.RunToConsole( "вротебатьебатькопать" );
                ts.RunToConsole( "волокно" );
                ts.RunToConsole( "полоса" );
                ts.RunToConsole( "барсук" );
                ts.RunToConsole( "карамель" );
                ts.RunToConsole( "ебеммозгибезучетаконтекста" );

                //EN
                ts.RunToConsole( "Itiseasytoreadwordswithoutspaces" ); // => "It is easy to read words without spaces"
            }
        }
        private static void Test__UnionTextSegmenter( UnionTextSegmenter uts )
        {
            uts.RunToConsole( "Textsegmentation" );
            uts.RunToConsole( "азатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыков" );

            uts.RunToConsole( "бабушкакозликаоченьлюбила" );
            uts.RunToConsole( "баб_ушкакозликаоченьлюбила" );
            uts.RunToConsole( "вротебатьебатькопать" );
            uts.RunToConsole( "волокно" );
            uts.RunToConsole( "полоса" );
            uts.RunToConsole( "барсук" );
            uts.RunToConsole( "карамель" );
            uts.RunToConsole( "ебеммозгибезучетаконтекста" );

            //EN
            uts.RunToConsole( "Itiseasytoreadwordswithoutspaces" ); // => "It is easy to read words without spaces"
        }

        //------------------------------------------------------------//
        private static void GCCollect()
        {
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
            GC.WaitForPendingFinalizers();
            GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
        }

        private static void RunToConsole( this ITextSegmenter ts, string text )
        {
            Console.Write( $"'{text}' => " );
            //ts.Run( text ).ToConsole();
            ts.Run_Offset( text ).ToConsole( text );
        }
        private static void ToConsole( this IReadOnlyCollection< TermProbability > tps )
        {
            if ( tps != null && tps.Any() )
            {
                Console.WriteLine( '\'' + string.Join( " ", tps.Select( t => t.Term ) ) + '\'' );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }
        private static void ToConsole( this IReadOnlyCollection< TermProbability_Offset > tps, string text )
        {
            if ( tps != null && tps.Any() )
            {
                Console.WriteLine( '\'' + string.Join( " ", tps.Select( t => t.GetTerm( text ) ) ) + '\'' );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }


        private static void RunToConsole( this UnionTextSegmenter uts, string text )
        {
            Console.Write( $"'{text}' => " );
            uts.RunBest_Offset( text ).ToConsole( text );
        }
        private static void ToConsole( this _UResult_ r )
        {
            if ( r.TPS != null && r.TPS.Any() )
            {
                Console.WriteLine( '\'' + string.Join( " ", r.TPS.Select( t => t.Term ) ) + $"' ({r.Language})" );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }
        private static void ToConsole( this _UResultOffset_ r, string text )
        {
            if ( r.TPS != null && r.TPS.Any() )
            {
                Console.WriteLine( '\'' + string.Join( " ", r.TPS.Select( t => t.GetTerm( text ) ) ) + $"' ({r.Language})" );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }
    }
}

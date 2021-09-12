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
            _RU_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES   ( "RU" );

            _EN_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "EN" );
            _EN_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES   ( "EN" );

            _DE_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "DE" );
            _DE_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES   ( "DE" );
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
        private string[] _DE_BINARY_MODEL_FILENAMES;
        private int      _DE_MODEL_DICTIONARY_CAPACITY;

        public TextModelConfig GetTextModelConfig_RU() => new TextModelConfig( RU_TEXT_MODEL_FILENAME, _RU_MODEL_DICTIONARY_CAPACITY );

        public BinaryModelConfig GetBinaryModelConfig_RU() => new BinaryModelConfig( _RU_BINARY_MODEL_FILENAMES, _RU_MODEL_DICTIONARY_CAPACITY );
        public BinaryModelConfig GetBinaryModelConfig_EN() => new BinaryModelConfig( _EN_BINARY_MODEL_FILENAMES, _EN_MODEL_DICTIONARY_CAPACITY );
        public BinaryModelConfig GetBinaryModelConfig_DE() => new BinaryModelConfig( _DE_BINARY_MODEL_FILENAMES, _DE_MODEL_DICTIONARY_CAPACITY );
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main()
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
            Console.Write( "load \"-text-\" language model..." );

            var sw = Stopwatch.StartNew();            
            using ( var model = new ManagedTextModel( Config.Inst.GetTextModelConfig_RU() ) )
            {
                var count = model.RecordCount;

                Console.WriteLine( $"=> elapsed: {sw.StopAndElapsed()}, record-count: {count}\r\n" );

                Test__TextSegmenter( model );

                Console.Write( "\r\ndisposing language model..." );                    
            }
            Console.WriteLine( "=> end\r\n" );
        }
        private static void Test__NativeTextMMFModelBinary_RU()
        {
            Console.Write( "load \"-binary-\" language model..." );

            var sw = Stopwatch.StartNew();
            using ( var model = new NativeTextMMFModelBinary( Config.Inst.GetBinaryModelConfig_RU() ) )
            {
                var count = model.RecordCount;

                Console.WriteLine( $"=> elapsed: {sw.StopAndElapsed()}, record-count: {count}\r\n" );

                Test__TextSegmenter( model );

                Console.Write( "\r\ndisposing language model..." );
            }
            Console.WriteLine( "=> end\r\n" );
        }

        private static void Test__UnionTextSegmenter()
        {
            Console.Write( "load \"-binary-\" language model's..." );

            var sw = Stopwatch.StartNew();
            using ( var uts = new UnionTextSegmenter( _UInitParam_.Create( Config.Inst.GetBinaryModelConfig_RU(), _ULanguage_.RU ),
                                                      _UInitParam_.Create( Config.Inst.GetBinaryModelConfig_EN(), _ULanguage_.EN ),
                                                      _UInitParam_.Create( Config.Inst.GetBinaryModelConfig_DE(), _ULanguage_.DE ) )
                  )
            {
                Console.WriteLine( $"=> elapsed: {sw.StopAndElapsed()}\r\n" );

                Test__UnionTextSegmenter( uts );

                Console.Write( "\r\ndisposing language model's..." );
            }
            Console.WriteLine( "=> end\r\n" );
        }

        private static void Test__TextSegmenter( IModel model )
        {
            using ( var ts = new TextSegmenter( model ) )
            {
                ts.Run_ToConsole( "бабушкакозликаоченьлюбила" );
                ts.Run_ToConsole( "баб_ушкакозликаоченьлюбила" );
                ts.Run_ToConsole( "вротебатьебатькопать" );
                ts.Run_ToConsole( "волокно" );
                ts.Run_ToConsole( "полоса" );
                ts.Run_ToConsole( "барсук" );
                ts.Run_ToConsole( "карамель" );
                ts.Run_ToConsole( "ебеммозгибезучетаконтекста" );

                //EN
                ts.Run_ToConsole( "Itiseasytoreadwordswithoutspaces" ); // => "It is easy to read words without spaces"
            }
        }
        private static void Test__UnionTextSegmenter( UnionTextSegmenter uts )
        {
            uts.Run4All( "бабушкакозликаоченьлюбила" ).ToConsole();
            uts.Run4All( "Textsegmentation" ).ToConsole();
            uts.Run4All( "Itiseasytoreadwordswithoutspaces" ).ToConsole();
            uts.Run4All( "The western yellow robin is a species of bird in the Australasian robin family native to Australia".NoWhiteSpace() ).ToConsole();
            uts.Run4All( "EsisteinfachWörterohneLeerzeichenzulesen" ).ToConsole();
            uts.Run4All( "In seinen Anfangsjahren trat er mit wirtschafts und siedlungsgeschichtlichen Arbeiten hervor".NoWhiteSpace() ).ToConsole();
            uts.Run4All( "MayersZielwardieErarbeitungeineseuropäischenGeschichtsbildes,dasvorallemvonderdeutschenGeschichtswissenschaftausbestimmtwird" ).ToConsole();
            uts.Run4All( "dasistfantastisch" ).ToConsole();
            uts.Run4All( "Глокая куздра штеко будланула бокра и курдячит бокрёнка".NoWhiteSpace() ).ToConsole();

            uts.RunBest_Debug_ToConsole( "Глокая куздра штеко будланула бокра и курдячит бокрёнка".NoWhiteSpace() );
            uts.Run_Debug_ToConsole( "на поле он о вскомконом говорил".NoWhiteSpace() );
            uts.RunBest_Debug_ToConsole( "наполеонсеяллен" );
            uts.RunBest_ToConsole( "азатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыков" );
            uts.RunBest_ToConsole( "бабушкакозликаоченьлюбила" );
            uts.RunBest_ToConsole( "баб_ушкакозликаоченьлюбила" );
            uts.RunBest_ToConsole( "вротебатьебатькопать" );
            uts.RunBest_ToConsole( "волокно" );
            uts.RunBest_ToConsole( "полоса" );
            uts.RunBest_ToConsole( "барсук" );
            uts.RunBest_ToConsole( "карамель" );
            uts.RunBest_ToConsole( "ебеммозгибезучетаконтекста" );
            uts.RunBest_ToConsole( "наполеон" );
            uts.RunBest_ToConsole( "Варкалось Хливкие шорьки Пырялись по наве И хрюкотали зелюки Как мюмзики в мове".NoWhiteSpace() );

            //EN
            uts.RunBest_ToConsole( "Textsegmentation" );
            uts.RunBest_ToConsole( "Itiseasytoreadwordswithoutspaces" ); // => "It is easy to read words without spaces"
            uts.RunBest_ToConsole( "The western yellow robin is a species of bird in the Australasian robin family native to Australia".NoWhiteSpace() );

            //DE
            uts.RunBest_Debug_ToConsole( "EsisteinfachWörterohneLeerzeichenzulesen" );
            uts.RunBest_Offset_ToConsole( "In seinen Anfangsjahren trat er mit wirtschafts und siedlungsgeschichtlichen Arbeiten hervor".NoWhiteSpace() );
            uts.RunBest_Offset_ToConsole( "MayersZielwardieErarbeitungeineseuropäischenGeschichtsbildes,dasvorallemvonderdeutschenGeschichtswissenschaftausbestimmtwird" );
            uts.RunBest_Offset_ToConsole( "dasistfantastisch" );

            /*
            const string TXT = "анаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеон" +
                               "анаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеоннаполеон";

            uts.Run4All( TXT ).ToConsole();
            uts.Run4All( "наполеонсеяллен" ).ToConsole();
            uts.Run4All( "Textsegmentation" ).ToConsole();
            //*/
        }

        //------------------------------------------------------------//
        private static void Run_ToConsole( this ITextSegmenter ts, string text )
        {
            Console.Write( $"'{text}' => " );
            ts.Run( text ).ToConsole();
        }
        private static void Run_Debug_ToConsole( this ITextSegmenter ts, string text )
        {
            Console.Write( $"'{text}' => " );
            ts.Run_Debug( text ).ToConsole();
        }
        private static void Run_Offset_ToConsole( this ITextSegmenter ts, string text )
        {
            Console.Write( $"'{text}' => " );
            ts.Run_Offset( text ).ToConsole( text );
        }
        private static void ToConsole( this IReadOnlyCollection< TermProbability > tps )
        {
            if ( tps.AnyEx() )
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
            if ( tps.AnyEx() )
            {
                Console.WriteLine( '\'' + string.Join( " ", tps.Select( t => t.GetTerm( text ) ) ) + '\'' );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }


        private static void RunBest_ToConsole( this UnionTextSegmenter uts, string text )
        {
            Console.Write( $"'{text}' => " );
            uts.RunBest( text ).ToConsole();
        }
        private static void RunBest_Debug_ToConsole( this UnionTextSegmenter uts, string text )
        {
            Console.Write( $"'{text}' => " );
            uts.RunBest_Debug( text ).ToConsole();
        }
        private static void RunBest_Offset_ToConsole( this UnionTextSegmenter uts, string text )
        {
            Console.Write( $"'{text}' => " );
            uts.RunBest_Offset( text ).ToConsole( text );
        }
        private static void ToConsole( this _UResult_ r )//, string text )
        {
            //Console.Write( $"'{text}' => " );
            if ( r.TPS.AnyEx() )
            {
                Console.WriteLine( '\'' + string.Join( " ", r.TPS.Select( t => t.Term ) ) + $"' ({r.Language})" );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }
        private static void ToConsole( this IReadOnlyCollection< (double prop, _UResult_ r) > res )
        {
            if ( res.AnyEx() )
            {
                Console.WriteLine( "-------------------------------------" );
                foreach ( var t in res.OrderByDescending( t => t.prop ) )
                {
                    Console.WriteLine( $"({t.r.Language})  {t.prop * 100:N2}%  '" + string.Join( " ", t.r.TPS.Select( tp => tp.Term ) ) + '\'' );
                }
                Console.WriteLine( "-------------------------------------" );
            }
            else
            {
                Console.WriteLine( "EMPTY" );
            }
        }
        private static void ToConsole( this _UResultOffset_ r, string text )
        {
            if ( r.TPS.AnyEx() )
            {
                Console.WriteLine( '\'' + string.Join( " ", r.TPS.Select( t => t.GetTerm( text ) ) ) + $"' ({r.Language})" );
            }
            else
            {
                Console.WriteLine( "EMPTY" ); 
            }
        }


        private static bool AnyEx< T >( this IEnumerable< T > seq ) => ((seq != null) && seq.Any());
        private static TimeSpan StopAndElapsed( this Stopwatch sw )
        {
            sw.Stop();
            return (sw.Elapsed);
        }
        private static string NoWhiteSpace( this string s ) => s.Replace( " ", "" );
    }
}

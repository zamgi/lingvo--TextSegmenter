using System;
using System.Collections.Generic;
using System.Runtime;

namespace lingvo.ts.modelconverter
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                #region [.GC.]
                GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                if ( GCSettings.LatencyMode != GCLatencyMode.LowLatency )
                {
                    GCSettings.LatencyMode = GCLatencyMode.Batch;
                }
                #endregion

                #region [.print to console config.]
                Console.WriteLine( $"{Environment.NewLine}----------------------------------------------" );
                Console.WriteLine( $"      TEXT_MODEL_FILENAME: '{Config.Inst.TEXT_MODEL_FILENAME}'" );
                Console.WriteLine( $"MODEL_DICTIONARY_CAPACITY: '{Config.Inst.MODEL_DICTIONARY_CAPACITY}'" );
                Console.WriteLine( $"          OUTPUT_FILENAME: '{Config.Inst.OUTPUT_FILENAME}'" );
                if ( Config.Inst.OUTPUT_FILE_SIZE_IN_BYTES != 0 )
                {
                Console.WriteLine( $"OUTPUT_FILE_SIZE_IN_BYTES: '{Config.Inst.OUTPUT_FILE_SIZE_IN_BYTES}'" );
                }
				Console.WriteLine( $"----------------------------------------------{Environment.NewLine}" );
                #endregion

                #region [.main routine.]
                var modelFilenames = ConvertFromTxt2Bin();
#if DEBUG
                Comare_ModelBinaryNative_And_ModelClassic( modelFilenames ); 
#endif
                Console.WriteLine( $"[{Environment.NewLine}.....finita fusking comedy.....]" );
                Console.ReadLine();
                #endregion
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();

                Console.WriteLine( $"{Environment.NewLine}[.....finita fusking comedy (push ENTER 4 exit).....]" );
                Console.ReadLine();
            }
        }

        private static IEnumerable< string > ConvertFromTxt2Bin()
        {
            var tmcfg = Config.Inst.GetTextModelConfig();
            using ( var model = new ManagedTextModel( tmcfg ) )
            {
                var config = new Txt2BinModelConverterConfig()
                {
                    Model                 = model,
                    OutputFileName        = Config.Inst.OUTPUT_FILENAME,
                    OutputFileSizeInBytes = Config.Inst.OUTPUT_FILE_SIZE_IN_BYTES,
                };
                var outputFileNames = Txt2BinModelConverter.Run( config );

                Console.WriteLine( $"{Environment.NewLine} output-files: " );
                Console.WriteLine( " --------------" );
                for ( var i = 0; i < outputFileNames.Count; i++ )
                {
                    Console.WriteLine( $" {(i + 1).ToString()}). '{outputFileNames[ i ]}'" );
                }
                Console.WriteLine( " --------------\r\n" );
                return (outputFileNames);
            }
        }

        private static void Comare_ModelBinaryNative_And_ModelClassic( IEnumerable< string > modelFilenames )
        {
#if DEBUG
            var bmcfg = new BinaryModelConfig( modelFilenames ) { ModelDictionaryCapacity = Config.Inst.MODEL_DICTIONARY_CAPACITY };

            using ( var model_1 = new NativeTextMMFModelBinary( bmcfg ) )
            using ( var model_2 = new ManagedTextModel( Config.Inst.GetTextModelConfig() ) )
            {
                ModelComparer.Compare( model_1, model_2 );
            } 
#else
            throw (new NotImplementedException( "Allowed only in DEBUG mode" ));
#endif
        }
    }
}

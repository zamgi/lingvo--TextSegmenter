using System;
using System.Diagnostics;

using lingvo.urls;

namespace lingvo.ts.modelbuilder
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            var wasErrors = false;
            try
            {
                #region [.print to console config.]
                Console.WriteLine( $"{Environment.NewLine}----------------------------------------------");
                Console.WriteLine( $"USE_HIGH_PRIORITY        : '{Config.Inst.USE_HIGH_PRIORITY}'" );
                Console.WriteLine( $"NGARMS                   : '{Config.Inst.NGARMS}'" );
                Console.WriteLine( $"CUT_PERCENT              : '{Config.Inst.CUT_PERCENT.GetValueOrDefault()}%'" );
                #region comm
                //---Console.WriteLine( $"BUILD_MODE               : '{Config.Inst.BUILD_MODE}'" );
                /*---switch ( Config.Inst.BUILD_MODE )
                {
                    case BuildModeEnum.single_model:
                Console.WriteLine( $"NGARMS                   : '{Config.Inst.NGARMS}'" );
                Console.WriteLine( $"CUT_PERCENT              : '{Config.Inst.CUT_PERCENT.GetValueOrDefault()}%'" );
                //Console.WriteLine( $"CUT_THRESHOLD            : '{Config.Inst.CUT_THRESHOLD}'" );
                    break;
                }*/ 
                #endregion
                Console.WriteLine( $"INPUT_DIRECTORY          : '{Config.Inst.INPUT_DIRECTORY}'" );
                Console.WriteLine( $"INPUT_ENCODING           : '{Config.Inst.INPUT_ENCODING.WebName}'" );
                Console.WriteLine( $"CLEAR_CYRILLICS_CHARS    : '{Config.Inst.CLEAR_CYRILLICS_CHARS}'" );
                Console.WriteLine( $"CLEAR_DIGITS_CHARS       : '{Config.Inst.CLEAR_DIGITS_CHARS}'" );
                Console.WriteLine( $"SINGLE_WORD_MAX_LENGTH   : '{Config.Inst.SINGLE_WORD_MAX_LENGTH}'" );
                Console.WriteLine( $"OUTPUT_DIRECTORY         : '{Config.Inst.OUTPUT_DIRECTORY}'" );
                Console.WriteLine( $"OUTPUT_ENCODING          : '{Config.Inst.OUTPUT_ENCODING.WebName}'" );
                Console.WriteLine( $"USE_PORTION              : '{Config.Inst.USE_PORTION}'" );
                if ( Config.Inst.USE_PORTION )
                Console.WriteLine( $"MAX_PORTION_SIZE         : '{Config.Inst.MAX_PORTION_SIZE}'" );
                Console.WriteLine( $"----------------------------------------------{Environment.NewLine}");
                #endregion

                #region [.use high priority.]
                if ( Config.Inst.USE_HIGH_PRIORITY )
                {
                    Extensions.SetCurrentProcessHighPriority();
                }
                #endregion

                #region [.url-detector.]
                var urlDetectorModel = new UrlDetectorModel( Config.Inst.URL_DETECTOR_RESOURCES_XML_FILENAME );
                #endregion

                #region [.build model's.]
                var bp = new BuildParams_t()
                {
                    UrlDetectorModel    = urlDetectorModel,
                    InputDirectory      = Config.Inst.INPUT_DIRECTORY,
                    Ngrams              = Config.Inst.NGARMS,
                    CutPercent          = Config.Inst.CUT_PERCENT,
                    OutputDirectory     = Config.Inst.OUTPUT_DIRECTORY,
                    MaxPortionSize      = Config.Inst.MAX_PORTION_SIZE,
                    ClearCyrillicsChars = Config.Inst.CLEAR_CYRILLICS_CHARS,
                    ClearDigitsChars    = Config.Inst.CLEAR_DIGITS_CHARS,
                    SingleWordMaxLength = Config.Inst.SINGLE_WORD_MAX_LENGTH,
                };
                var sw = Stopwatch.StartNew();
                if ( Config.Inst.USE_PORTION )
                {
                    ModelBuilder.Build_UsePortion( bp );
                }
                else
                {
                    ModelBuilder.Build( bp );
                }
                sw.Stop();

                Console.WriteLine( $"'{Config.Inst.NGARMS}; cut_{Config.Inst.CUT_PERCENT.GetValueOrDefault()}%' - success, elapsed: {sw.Elapsed}{Environment.NewLine}" );

                #region comm
                /*--if ( Config.Inst.BUILD_MODE == BuildModeEnum.single_model )
                {
                    var bp = new BuildParams_t()
                    {
                        UrlDetectorModel    = urlDetectorModel,
                        InputDirectory      = Config.Inst.INPUT_DIRECTORY,
                        Ngrams              = Config.Inst.NGARMS,
                        CutPercent          = Config.Inst.CUT_PERCENT,
                        OutputDirectory     = Config.Inst.OUTPUT_DIRECTORY,
                        MaxPortionSize      = Config.Inst.MAX_PORTION_SIZE,
                        ClearCyrillicsChars = Config.Inst.CLEAR_CYRILLICS_CHARS,
                        SingleWordMaxLength = Config.Inst.SINGLE_WORD_MAX_LENGTH,
                    };
                    var sw = Stopwatch.StartNew();
                    if ( Config.Inst.USE_PORTION )
                    {
                        ModelBuilder.Build_UsePortion( bp );
                    }
                    else
                    {
                        ModelBuilder.Build( bp );
                    }
                    sw.Stop();

                    Console.WriteLine( $"'{Config.Inst.NGARMS}; cut_{Config.Inst.CUT_PERCENT.GetValueOrDefault()}%' - success, elapsed: {sw.Elapsed}{Environment.NewLine}" );
                }
                else
                {
                    #region [.build model's.]
                    var sw_total = Stopwatch.StartNew();
                    foreach ( var t in Extensions.GetProcessParams() )
                    {
                        var bp = new BuildParams_t()
                        {
                            UrlDetectorModel    = urlDetectorModel,
                            InputDirectory      = Config.Inst.INPUT_DIRECTORY,
                            Ngrams              = t.Item1,
                            CutPercent          = TFProcessor.GetCutPercent( t.Item2 ),
                            OutputDirectory     = Config.Inst.OUTPUT_DIRECTORY,
                            MaxPortionSize      = Config.Inst.MAX_PORTION_SIZE,
                            ClearCyrillicsChars = Config.Inst.CLEAR_CYRILLICS_CHARS,
                            SingleWordMaxLength = Config.Inst.SINGLE_WORD_MAX_LENGTH,
                        };
                        try
                        {
                            var sw = Stopwatch.StartNew();
                            if ( Config.Inst.USE_PORTION )
                            {
                                ModelBuilder.Build_UsePortion( bp );
                            }
                            else
                            {
                                ModelBuilder.Build( bp );
                            }
                            sw.Stop();

                            Console.WriteLine( $"'{bp.Ngrams}; cut_{bp.CutPercent.GetValueOrDefault()}%' - success, elapsed: {sw.Elapsed}{Environment.NewLine}" );
                        }
                        catch ( Exception ex )
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine( $"'{bp.Ngrams}; cut_{bp.CutPercent.GetValueOrDefault()}%' - {ex.GetType()}: {ex.Message}" );
                            Console.ResetColor();
                            wasErrors = true;
                        }
                    }
                    sw_total.Stop();

                    Console.WriteLine( $"total elapsed: {sw_total.Elapsed}" );
                    #endregion
                }*/
                #endregion
                #endregion
            }
            catch ( Exception ex )
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine( Environment.NewLine + ex + Environment.NewLine );
                Console.ResetColor();
                wasErrors = true;
            }

            if ( wasErrors )
            {
                Console.WriteLine( $"{Environment.NewLine}[.....finita fusking comedy (push ENTER 4 exit).....]" );
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine( $"{Environment.NewLine}[.....finita fusking comedy.....]" );
            }
        }
    }
}

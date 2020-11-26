using System;
using System.Configuration;
using System.Globalization;
using System.Text;

using lingvo.core.algorithm;

namespace lingvo
{    
    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config
    {
        private const int DEFAULT_SINGLE_WORD_MAX_LENGTH = 100;

        private Config()
        {
            USE_HIGH_PRIORITY = bool.Parse( ConfigurationManager.AppSettings[ "USE_HIGH_PRIORITY" ] );

            URL_DETECTOR_RESOURCES_XML_FILENAME = ConfigurationManager.AppSettings[ "URL_DETECTOR_RESOURCES_XML_FILENAME" ];

            NGARMS = NGramsEnum.ngram_1;
            var v = ConfigurationManager.AppSettings[ "CUT_PERCENT" ];
            if ( !string.IsNullOrWhiteSpace( v ) )
            {
                CUT_PERCENT = float.Parse( v, NumberStyles.AllowDecimalPoint, new NumberFormatInfo() { NumberDecimalSeparator = "." } );
                CUT_PERCENT = Math.Max( 0, Math.Min( 100, CUT_PERCENT.Value ) );
            }

            SINGLE_WORD_MAX_LENGTH = int.TryParse( ConfigurationManager.AppSettings[ "SINGLE_WORD_MAX_LENGTH" ], out var i ) ? i : DEFAULT_SINGLE_WORD_MAX_LENGTH;

            CLEAR_CYRILLICS_CHARS = bool.Parse( ConfigurationManager.AppSettings[ "CLEAR_CYRILLICS_CHARS" ] );
            CLEAR_DIGITS_CHARS    = bool.Parse( ConfigurationManager.AppSettings[ "CLEAR_DIGITS_CHARS" ] );

            INPUT_DIRECTORY = ConfigurationManager.AppSettings[ "INPUT_DIRECTORY" ];
            INPUT_ENCODING  = Encoding.GetEncoding( (ConfigurationManager.AppSettings[ "INPUT_ENCODING" ] ?? "utf-8") );

            OUTPUT_DIRECTORY = ConfigurationManager.AppSettings[ "OUTPUT_DIRECTORY" ];
            OUTPUT_ENCODING  = Encoding.GetEncoding( (ConfigurationManager.AppSettings[ "OUTPUT_ENCODING" ] ?? "utf-8") );

            USE_PORTION      = bool.Parse( ConfigurationManager.AppSettings[ "USE_PORTION" ] );
            MAX_PORTION_SIZE = int .Parse( ConfigurationManager.AppSettings[ "MAX_PORTION_SIZE" ] );
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

        public bool       USE_HIGH_PRIORITY { get; }
        public NGramsEnum NGARMS            { get; }
        public float?     CUT_PERCENT       { get; }
        public string     URL_DETECTOR_RESOURCES_XML_FILENAME { get; }

        public string   INPUT_DIRECTORY        { get; }
        public Encoding INPUT_ENCODING         { get; }
        public bool     CLEAR_CYRILLICS_CHARS  { get; }
        public bool     CLEAR_DIGITS_CHARS     { get; }        
        public int      SINGLE_WORD_MAX_LENGTH { get; }

        public string   OUTPUT_DIRECTORY { get; }
        public Encoding OUTPUT_ENCODING  { get; }

        public bool USE_PORTION      { get; }
        public int  MAX_PORTION_SIZE { get; }
    }
}

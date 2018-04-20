using System.Configuration;

namespace lingvo.ts.modelconverter
{
    /// <summary>
    /// 
    /// </summary>
    internal interface IConfig
    {
        TextModelConfig GetTextModelConfig();
        string          TEXT_MODEL_FILENAME       { get; }
        int             MODEL_DICTIONARY_CAPACITY { get; }
        string          OUTPUT_FILENAME           { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config : IConfig
    {
        private Config()
        {
            TEXT_MODEL_FILENAME       = ConfigurationManager.AppSettings[ "TEXT_MODEL_FILENAME" ];
            MODEL_DICTIONARY_CAPACITY = int.Parse( ConfigurationManager.AppSettings[ "MODEL_DICTIONARY_CAPACITY" ] );
            OUTPUT_FILENAME           = ConfigurationManager.AppSettings[ "OUTPUT_FILENAME" ];
            OUTPUT_FILE_SIZE_IN_BYTES = int.TryParse( ConfigurationManager.AppSettings[ "OUTPUT_FILE_SIZE_IN_BYTES" ], out var n ) ? n : 0;
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

        public string TEXT_MODEL_FILENAME       { get; private set; }
        public int    MODEL_DICTIONARY_CAPACITY { get; private set; }
        public string OUTPUT_FILENAME           { get; private set; }
        public int    OUTPUT_FILE_SIZE_IN_BYTES { get; private set; }

        public TextModelConfig GetTextModelConfig() => new TextModelConfig( TEXT_MODEL_FILENAME, MODEL_DICTIONARY_CAPACITY );
    }
}

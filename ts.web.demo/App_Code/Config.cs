using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Config
    {        
        private Config()
        {
            MAX_INPUTTEXT_LENGTH              = int.Parse( ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ] );
            CONCURRENT_FACTORY_INSTANCE_COUNT = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );

            _RU_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "RU" );
            _RU_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES   ( "RU" );

            _EN_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "EN" );
            _EN_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES   ( "EN" );

            _DE_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "DE" );
            _DE_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES   ( "DE" );
        }

        private static int      Get_MODEL_DICTIONARY_CAPACITY( string lang ) { return (int.Parse( ConfigurationManager.AppSettings[ lang + "_MODEL_DICTIONARY_CAPACITY" ] )); }
        //private static string   Get_BINARY_MODEL_FILENAME    ( string lang ) { return (ConfigurationManager.AppSettings[ lang + "_BINARY_MODEL_FILENAME" ]); }
        private static string[] Get_BINARY_MODEL_FILENAMES   ( string lang )
        {
            var binaryModelDirectory = ConfigurationManager.AppSettings[ lang + "_BINARY_MODEL_DIRECTORY" ] ?? string.Empty;
            var bmfns = ConfigurationManager.AppSettings[ lang + "_BINARY_MODEL_FILENAMES" ] ?? string.Empty;
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

        private string[] _RU_BINARY_MODEL_FILENAMES;
        private int      _RU_MODEL_DICTIONARY_CAPACITY;
        private string[] _EN_BINARY_MODEL_FILENAMES;
        private int      _EN_MODEL_DICTIONARY_CAPACITY;
        private string[] _DE_BINARY_MODEL_FILENAMES;
        private int      _DE_MODEL_DICTIONARY_CAPACITY;

        public int MAX_INPUTTEXT_LENGTH              { get; private set; }
        public int CONCURRENT_FACTORY_INSTANCE_COUNT { get; private set; }

        #region [.ModelConfig.]
        public BinaryModelConfig GetBinaryModelConfig_RU() { return (new BinaryModelConfig( _RU_BINARY_MODEL_FILENAMES, _RU_MODEL_DICTIONARY_CAPACITY )); }
        public BinaryModelConfig GetBinaryModelConfig_EN() { return (new BinaryModelConfig( _EN_BINARY_MODEL_FILENAMES, _EN_MODEL_DICTIONARY_CAPACITY )); }
        public BinaryModelConfig GetBinaryModelConfig_DE() { return (new BinaryModelConfig( _DE_BINARY_MODEL_FILENAMES, _DE_MODEL_DICTIONARY_CAPACITY )); }
        #endregion
    }
}
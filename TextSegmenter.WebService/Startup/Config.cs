using System;
using System.Linq;
using System.Configuration;
using System.IO;

namespace lingvo.ts.webService
{
    /// <summary>
    /// 
    /// </summary>
    public interface IConfig
    {
        int CONCURRENT_FACTORY_INSTANCE_COUNT { get; }
        int MAX_INPUTTEXT_LENGTH { get; }

        BinaryModelConfig GetBinaryModelConfig_DE();
        BinaryModelConfig GetBinaryModelConfig_EN();
        BinaryModelConfig GetBinaryModelConfig_RU();
    }

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Config : IConfig
    {
        public Config()
        {
            MAX_INPUTTEXT_LENGTH              = int.Parse( ConfigurationManager.AppSettings[ "MAX_INPUTTEXT_LENGTH" ] );
            CONCURRENT_FACTORY_INSTANCE_COUNT = int.Parse( ConfigurationManager.AppSettings[ "CONCURRENT_FACTORY_INSTANCE_COUNT" ] );

            _RU_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "RU" );
            _RU_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES( "RU" );

            _EN_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "EN" );
            _EN_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES( "EN" );

            _DE_MODEL_DICTIONARY_CAPACITY = Get_MODEL_DICTIONARY_CAPACITY( "DE" );
            _DE_BINARY_MODEL_FILENAMES    = Get_BINARY_MODEL_FILENAMES( "DE" );
        }

        private int Get_MODEL_DICTIONARY_CAPACITY( string lang ) => int.Parse( ConfigurationManager.AppSettings[ lang + "_MODEL_DICTIONARY_CAPACITY" ] );
        //private static string Get_BINARY_MODEL_FILENAME ( string lang ) => ConfigurationManager.AppSettings[ lang + "_BINARY_MODEL_FILENAME" ];
        private string[] Get_BINARY_MODEL_FILENAMES( string lang )
        {
            var binaryModelDirectory = ConfigurationManager.AppSettings[ lang + "_BINARY_MODEL_DIRECTORY" ] ?? string.Empty;
            var bmfns = ConfigurationManager.AppSettings[ lang + "_BINARY_MODEL_FILENAMES" ] ?? string.Empty;
            var binaryModelFilenames = (from fn in bmfns.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries )
                                        let fileName = fn.Trim()
                                        where (!string.IsNullOrEmpty( fileName ))
                                        select Path.Combine( binaryModelDirectory, fileName )
                                       ).ToArray();
            return (binaryModelFilenames);
        }

        private string[] _RU_BINARY_MODEL_FILENAMES;
        private int _RU_MODEL_DICTIONARY_CAPACITY;
        private string[] _EN_BINARY_MODEL_FILENAMES;
        private int _EN_MODEL_DICTIONARY_CAPACITY;
        private string[] _DE_BINARY_MODEL_FILENAMES;
        private int _DE_MODEL_DICTIONARY_CAPACITY;

        public int MAX_INPUTTEXT_LENGTH              { get; }
        public int CONCURRENT_FACTORY_INSTANCE_COUNT { get; }

        #region [.ModelConfig.]
        public BinaryModelConfig GetBinaryModelConfig_RU() => new BinaryModelConfig( _RU_BINARY_MODEL_FILENAMES, _RU_MODEL_DICTIONARY_CAPACITY );
        public BinaryModelConfig GetBinaryModelConfig_EN() => new BinaryModelConfig( _EN_BINARY_MODEL_FILENAMES, _EN_MODEL_DICTIONARY_CAPACITY );
        public BinaryModelConfig GetBinaryModelConfig_DE() => new BinaryModelConfig( _DE_BINARY_MODEL_FILENAMES, _DE_MODEL_DICTIONARY_CAPACITY );
        #endregion
    }
}

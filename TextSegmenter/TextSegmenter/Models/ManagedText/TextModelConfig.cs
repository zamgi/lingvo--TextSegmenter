using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public class TextModelConfig
    {
        protected const string INVALIDDATAEXCEPTION_FORMAT_MESSAGE = "Wrong format of model-filename (file-name: '{0}', line# {1}, line-value: '{2}')";
        protected const           NumberStyles     NS  = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
        protected static readonly NumberFormatInfo NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        public TextModelConfig( string modelFilename, int modelDictionaryCapacity = 0 )
        {
            modelFilename.ThrowIfNullOrWhiteSpace( nameof(modelFilename) );

            ModelFilename = modelFilename;
            if ( !File.Exists( ModelFilename ) )
            {
                throw (new FileNotFoundException( $"File not found: '{ModelFilename}'", ModelFilename ));
            }
            ModelDictionaryCapacity = modelDictionaryCapacity;
        }

        public string ModelFilename           { get; }
        public int    ModelDictionaryCapacity { get; set; }

        public IEnumerable< KeyValuePair< string, float > > GetModelFileContent()
        {
            var SPLIT_CHARS = new[] { '\t' };

            using ( var sr = new StreamReader( ModelFilename ) )
            {
                var lineCount = 0;
                var line      = default(string);

                for ( line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                {
                    if ( !line.StartsWith( "#" ) )
                    {
                        break;
                    }
                }

                for ( ; line != null; line = sr.ReadLine() )
                {
                    lineCount++;

                    var a = line.Split( SPLIT_CHARS, StringSplitOptions.RemoveEmptyEntries );
                    if ( a.Length != 2 )
                        throw (new InvalidDataException(string.Format(INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, line)));

                    var text = a[ 0 ].Trim();                    
                    if ( text.IsNullOrWhiteSpace() )
                        throw (new InvalidDataException(string.Format(INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, line)));
                    
                    if ( !float.TryParse( a[ 1 ].Trim(), NS, NFI, out var weight ) )
                        throw (new InvalidDataException(string.Format(INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, line)));

                    yield return (new KeyValuePair< string, float >( text, weight ));                    
                }
            }
        }
#if DEBUG
        public override string ToString() => $"'{ModelFilename}'";
#endif
    }
}

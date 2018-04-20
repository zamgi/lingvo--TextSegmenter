using System;
using System.Collections.Generic;
using System.Linq;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class BinaryModelConfig
    {
        private HashSet< string > _ModelFilenames;

        public BinaryModelConfig()
        {
            _ModelFilenames = new HashSet< string >( StringComparer.InvariantCultureIgnoreCase );
        }
        public BinaryModelConfig( IEnumerable< string > modelFilenames, int modelDictionaryCapacity = 0 )
        {
            modelFilenames.ThrowIfNullOrWhiteSpaceAnyElement( nameof(modelFilenames) );

            _ModelFilenames = new HashSet< string >( modelFilenames, StringComparer.InvariantCultureIgnoreCase );
        }
        public BinaryModelConfig( string modelFilename, int modelDictionaryCapacity = 0 )
            : this( Enumerable.Repeat( modelFilename, 1 ), modelDictionaryCapacity )
        {
        }

        public void AddModelFilename( string modelFilename )
        {
            modelFilename.ThrowIfNullOrWhiteSpace( nameof(modelFilename) );

            _ModelFilenames.Add( modelFilename );
        }

        public IReadOnlyCollection< string > ModelFilenames => _ModelFilenames;
        public int ModelDictionaryCapacity { get; set; }
    }
}

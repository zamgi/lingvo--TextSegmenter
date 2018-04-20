using System;
using System.Collections.Generic;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class ManagedTextModel : IModel
    {
#if DEBUG
        /// <summary>
        /// 
        /// </summary>
        public static class FuskingTraitor
        {
            public static Dictionary< string, double > GetDictionary( ManagedTextModel model ) => model._Dictionary;
        } 
#endif
        private Dictionary< string, double > _Dictionary;

        public ManagedTextModel( TextModelConfig config )
        {
            config.ThrowIfNull( nameof(config) );

            _Dictionary = (0 < config.ModelDictionaryCapacity) ? new Dictionary< string, double >( config.ModelDictionaryCapacity ) 
                                                               : new Dictionary< string, double >();

            foreach ( var p in config.GetModelFileContent() )
            {
                _Dictionary.Add( p.Key, p.Value );
            }
        }
        public void Dispose()
        {
            if ( _Dictionary != null )
            {
                _Dictionary.Clear();
                _Dictionary = null;
            }
        }

        public IEnumerable< ModelRecord > GetAllRecords()
        {
            foreach ( var p in _Dictionary )
            {
                yield return (new ModelRecord() { Ngram = p.Key, Probability = p.Value });
            }
        }
        public int RecordCount => _Dictionary.Count;

        public bool TryGetProbability( string ngram, out double probability ) => _Dictionary.TryGetValue( ngram, out probability );
        public bool TryGetProbability( ref NativeOffset no, out double probability ) => throw new NotImplementedException();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

using lingvo.core;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NativeTextMMFModelBinary : NativeTextMMFModelBase, IModel
    {
#if DEBUG
        /// <summary>
        /// 
        /// </summary>
        public static class FuskingTraitor
        {
            public static SetNative GetSet( NativeTextMMFModelBinary model ) => model._Set;
        } 
#endif
        #region [.private field's.]
        private SetNative _Set;
        #endregion        

        #region [.ctor().]
        public NativeTextMMFModelBinary( BinaryModelConfig config ) => _Set = LoadBinaryModel( config );
        ~NativeTextMMFModelBinary() => DisposeNativeResources();

        public void Dispose()
        {
            DisposeNativeResources();

            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            if ( _Set != null )
            {
                foreach ( var ptr in _Set )
                {
                    Marshal.FreeHGlobal( ptr );
                }
                _Set = null;
            }
        } 
        #endregion

        #region [.model-dictionary loading.]
        private static SetNative LoadBinaryModel( BinaryModelConfig config )
        {
            var set = new SetNative( config.ModelDictionaryCapacity );

            foreach ( var modelFilename in config.ModelFilenames )
            {
                LoadFromBinFile( modelFilename, set );
            }

            return (set);
        }
        unsafe private static void LoadFromBinFile( string modelFilename, SetNative set )
        {
            const int BUFFER_SIZE = 0x2000;

            using ( var fs = new FileStream( modelFilename, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan ) )
            using ( var mmf = MemoryMappedFile.CreateFromFile( fs, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true ) )
            using ( var accessor = mmf.CreateViewAccessor( 0L, 0L, MemoryMappedFileAccess.Read ) )
            {
                byte* buffer = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer( ref buffer );

                for ( byte* endBuffer = buffer + fs.Length; buffer < endBuffer; )
                {
                    #region [.read 'textPtr' as C#-chars (with double-byte-zero '\0').]
                    var bufferCharPtr = (char*) buffer;
                    for ( var idx = 0; ; idx++ )
                    {
                        if ( BUFFER_SIZE < idx )
                        {
                            throw (new InvalidDataException( "WTF?!?!: [BUFFER_SIZE < idx]" ));
                        }
                        if ( bufferCharPtr[ idx ] == '\0' )
                        {
                            #region [.alloc term-&-probability and copy term.]
                            //alloc with include zero-'\0' end-of-string
                            var source       = bufferCharPtr;
                            var sourceLength = idx;
                            var recordSize   = ((sourceLength + 1) * sizeof(char)) + sizeof(double);
                            var destPtr = Marshal.AllocHGlobal( recordSize );
                            var destination = (char*) destPtr;
                            for ( ; 0 < sourceLength; sourceLength-- )
                            {
                                *(destination++) = *(source++);
                            }
                            *destination = '\0';
                            #endregion

                            #region [.read probability.]
                            *(double*) (destination + 1) = *(double*) (source + 1);
                            #endregion
#if DEBUG
                            var m = ToModelRecord( destPtr );
                            var prob = *(double*) (destination + 1);
                            Debug.Assert( m.Probability == prob );
#endif
                            set.Add( destPtr );

                            #region [.move to next record.]
                            buffer += recordSize;
                            #endregion

                            break;
                        }
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region [.IModel.]
        [M(O.AggressiveInlining)]
        unsafe private static double ToProbability( IntPtr baseIntPtr, int offset )
        {
            var probabilityPtr = ((byte*) baseIntPtr) + sizeof(char) * offset;
            var probability    = *(double*) probabilityPtr;            
            return (probability);
        }

        [M(O.AggressiveInlining)]
        private static void ToModelRecord( IntPtr baseIntPtr, out ModelRecord m )
        {
            var s = StringsHelper.ToString( baseIntPtr );
            m = new ModelRecord() { Ngram = s, Probability = ToProbability( baseIntPtr, s.Length + 1 ) };
        }
#if DEBUG
        public static ModelRecord ToModelRecord( IntPtr baseIntPtr )
        {
            var s = StringsHelper.ToString( baseIntPtr );
            return (new ModelRecord() { Ngram = s, Probability = ToProbability( baseIntPtr, s.Length + 1 ) });
        }
#endif
        public IEnumerable< ModelRecord > GetAllRecords()
        {
            foreach ( var p in _Set )
            {
                //var s = StringsHelper.ToString( p );
                //yield return (new ModelRecord() { Ngram = s, Probability = ToProbability( p, s.Length + 1 ) });

                ToModelRecord( p, out var m );
                yield return (m); 
            }
        }

        public int RecordCount => _Set.Count;

        unsafe public bool TryGetProbability( string ngram, out double probability )
        {
            fixed ( char* ngramPtr = ngram )
            {
                if ( _Set.TryGetValue( (IntPtr) ngramPtr, out var existsValue ) )
                {
#if DEBUG
                    var len = StringsHelper.GetLength( existsValue );
                    Debug.Assert( len == ngram.Length );
#endif
                    probability = ToProbability( existsValue, ngram.Length + 1 );
                    return (true);
                }
            }
            probability = default(double);
            return (false);
        }
        public bool TryGetProbability( in NativeOffset no, out double probability )
        {
            if ( _Set.TryGetValue( in no, out var existsValue ) )
            {
#if DEBUG
                var len = StringsHelper.GetLength( existsValue );
                Debug.Assert( len == no.Length );
#endif
                probability = ToProbability( existsValue, no.Length + 1 ); //--- len + 1 );
                return (true);
            }
            probability = default( double );
            return (false);
        }
        #endregion
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    unsafe public abstract class MMFModelBase
    {
        /// <summary>
        /// 
        /// </summary>
        protected struct NativeString
        {
            public static NativeString EMPTY = new NativeString() { Length = -1, Start = null };

            public char* Start;
            public int   Length;
#if DEBUG
            public override string ToString() => StringsHelper.ToString( Start, Length );
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        protected sealed class EnumeratorMMF : IEnumerator< NativeString >
        {
            private const int  BUFFER_SIZE = 0x400;
            private const byte NEW_LINE    = (byte) '\n';

            private char[]   _CharBuffer;
            private int      _CharBufferLength;
            private char*    _CharBufferBase;
            private GCHandle _CharBufferGCHandle;
            private Encoding _Encoding;

            private FileStream               _FS;
            private MemoryMappedFile         _MMF;
            private MemoryMappedViewAccessor _Accessor;
            private byte* _Buffer;
            private byte* _EndBuffer;
            private NativeString _NativeString;

            private EnumeratorMMF( string fileName )
            {                    
                _Encoding           = Encoding.UTF8;
                _CharBufferLength   = BUFFER_SIZE;
                _CharBuffer         = new char[ _CharBufferLength ];
                _CharBufferGCHandle = GCHandle.Alloc( _CharBuffer, GCHandleType.Pinned );
                _CharBufferBase     = (char*) _CharBufferGCHandle.AddrOfPinnedObject().ToPointer();
                _NativeString       = NativeString.EMPTY;
                
                _FS  = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, FileOptions.SequentialScan );
                _MMF = MemoryMappedFile.CreateFromFile( _FS, null, 0L, MemoryMappedFileAccess.Read, new MemoryMappedFileSecurity(), HandleInheritability.None, true );
                _Accessor = _MMF.CreateViewAccessor( 0L, 0L, MemoryMappedFileAccess.Read );

                _Accessor.SafeMemoryMappedViewHandle.AcquirePointer( ref _Buffer );

                var length = _FS.Length;
                //try skip The UTF-8 representation of the BOM is the byte sequence [0xEF, 0xBB, 0xBF]
                if ( 3 <= length && _Buffer[ 0 ] == 0xEF && _Buffer[ 1 ] == 0xBB && _Buffer[ 2 ] == 0xBF )
                {
                    _Buffer += 3;
                    length  -= 3;
                }
                _EndBuffer = _Buffer + length;
            }
            ~EnumeratorMMF()
            {
                DisposeNativeResources();
            }

            public void Dispose()
            {
                DisposeNativeResources();

                GC.SuppressFinalize( this );
            }
            private void DisposeNativeResources()
            {
                if ( _CharBufferBase != null )
                {
                    _CharBufferGCHandle.Free();
                    _CharBufferBase = null;
                }

                if ( _Accessor != null )
                {
                    _Accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    _Accessor.Dispose();
                    _Accessor = null;
                }
                if ( _MMF != null )
                {
                    _MMF.Dispose();
                    _MMF = null;
                }
                if ( _FS != null )
                {
                    _FS.Dispose();
                    _FS = null;
                }
            }

            public NativeString Current => _NativeString;
            public bool MoveNext()
            {
                var start = _Buffer;
                for ( long currentIndex = 0, endIndex = _EndBuffer - start; currentIndex <= endIndex; currentIndex++ )
                {
                    if ( start [ currentIndex ] == NEW_LINE )
                    {
                        #region [.line.]
                        int len = (int) currentIndex;

                        _Buffer = start + currentIndex + 1; //force move forward over 'NEW_LINE' char

                        if ( 0 < len )
                        {
                            char* startChar = _CharBufferBase;

                            int realLen = _Encoding.GetChars( start, len, startChar, _CharBufferLength );

                            int startIndex  = 0;
                            int finishIndex = realLen - 1;
                            //skip starts white-spaces
                            for ( ; ; )
                            {
                                if ( ((_CTM[ startChar[ startIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                     (finishIndex <= ++startIndex)
                                   )
                                {
                                    break;
                                }
                            }
                            //skip ends white-spaces
                            for ( ; ; )
                            {
                                if ( ((_CTM[ startChar[ finishIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                     (--finishIndex <= startIndex)
                                   )
                                {
                                    break;
                                }
                            }

                            realLen = (finishIndex - startIndex) + 1;

                            if ( 0 < realLen )
                            {
                                _NativeString.Start  = startChar + startIndex;
                                _NativeString.Length = realLen;
                                return (true);
                            }
                        }
                        #endregion
                    }
                }

                #region [.last-line.]
                {
                    int len = (int) (_EndBuffer - start);
                    if ( 0 < len )
                    {
                        _Buffer = _EndBuffer + 1; //force move forward over '_EndBuffer'


                        var startChar = _CharBufferBase;

                        int realLen = _Encoding.GetChars( start, len, startChar, _CharBufferLength );
                            
                        int startIndex  = 0;
                        int finishIndex = realLen - 1;
                        //skip starts white-spaces
                        for ( ; ; )
                        {
                            if ( ((_CTM[ startChar[ startIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (finishIndex <= ++startIndex)
                               )
                            {
                                break;
                            }
                        }
                        //skip ends white-spaces
                        for ( ; ; )
                        {
                            if ( ((_CTM[ startChar[ finishIndex ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (--finishIndex <= startIndex)
                               )
                            {
                                break;
                            }
                        }

                        realLen = (finishIndex - startIndex) + 1;

                        if ( 0 < realLen )
                        {
                            _NativeString.Start  = startChar + startIndex;
                            _NativeString.Length = realLen;
                            return (true);
                        }
                    }
                } 
                #endregion

                _NativeString = NativeString.EMPTY;
                return (false);
            }

            object IEnumerator.Current => Current;
            public void Reset() => new NotSupportedException();

            public static EnumeratorMMF Create( string fileName ) => new EnumeratorMMF( fileName );
        }


        protected static CharType* _CTM;

        static MMFModelBase()
        {
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe internal struct IntPtrEqualityComparer : IEqualityComparer< IntPtr >
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals( IntPtr x, IntPtr y )
        {
            //--practically does not happen--// if ( x == y ) return (true);

            for ( char* x_ptr = (char*) x,
                        y_ptr = (char*) y; ; x_ptr++, y_ptr++ )
            {
                var x_ch = *x_ptr;
                if ( x_ch != *y_ptr )
                    return (false);
                if ( x_ch == '\0' )
                    return (true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode( IntPtr obj )
        {
            char* ptr = (char*) obj;
            int n1 = 5381;
            int n2 = 5381;
            int n3;
            while ( (n3 = (int) (*(ushort*) ptr)) != 0 )
            {
                n1 = ((n1 << 5) + n1 ^ n3);
                n2 = ((n2 << 5) + n2 ^ n3);
                ptr++;
            }
            return (n1 + n2 * 1566083941);
        }            


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals( char* x, char* y, char* y_end )
        {
            //--practically does not happen--// if ( x == y ) return (true);

            for ( ; ; x++ )
            {
                var x_ch = *x;
                if ( x_ch != *y )
                    return (false);
                if ( (x_ch == '\0') || (++y == y_end) )
                    return (true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode( ref NativeOffset no )
        {
            char* ptr    = no.BasePtr + no.StartIndex;
            char* endPtr = ptr + no.Length;
            int n1 = 5381;
            int n2 = 5381;
            for( ; ; )
            {
                var n3 = (int) (*(ushort*) ptr);
                n1 = ((n1 << 5) + n1 ^ n3);
                n2 = ((n2 << 5) + n2 ^ n3);
                ptr++;

                if ( ptr == endPtr )
                {
                    break;
                }
            }
            return (n1 + n2 * 1566083941);
        }            
    }
}

﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using lingvo.core;
using lingvo.urls;
using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal sealed class mld_tokenizer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        unsafe private sealed class UnsafeConst
        {
            #region [.static & xlat table's.]
            public static readonly char*   MAX_PTR = (char*) (0xffffffffFFFFFFFF);
            #endregion

            public readonly bool* _INTERPRETE_AS_WHITESPACE;
            public readonly bool* _DIGIT_WORD_CHARS;

            private UnsafeConst()
            {
                #region [.defines.]
                var INCLUDE_INTERPRETE_AS_WHITESPACE = "¥©¤¦§®¶€™<>";
                var EXCLUDE_INTERPRETE_AS_WHITESPACE = new char[] { '\u0026', /* 0x26   , 38   , '&' */
                                                                    '\u0027', /* 0x27   , 39   , ''' */
                                                                    '\u002D', /* 0x2D   , 45   , '-' */
                                                                    '\u002E', /* 0x2E   , 46   , '.' */
                                                                    '\u005F', /* 0x5F   , 95   , '_' */
                                                                    '\u00AD', /* 0xAD   , 173  , '­' */
                                                                    '\u055A', /* 0x55A  , 1370 , '՚' */
                                                                    '\u055B', /* 0x55B  , 1371 , '՛' */
                                                                    '\u055D', /* 0x55D  , 1373 , '՝' */
                                                                    '\u2012', /* 0x2012 , 8210 , '‒' */
                                                                    '\u2013', /* 0x2013 , 8211 , '–' */
                                                                    '\u2014', /* 0x2014 , 8212 , '—' */
                                                                    '\u2015', /* 0x2015 , 8213 , '―' */
                                                                    '\u2018', /* 0x2018 , 8216 , '‘' */
                                                                    '\u2019', /* 0x2019 , 8217 , '’' */
                                                                    '\u201B', /* 0x201B , 8219 , '‛' */
                                                                   };
                var INCLUDE_DIGIT_WORD_CHARS = ";,:./\\- –〃´°";
                #endregion

                var INTERPRETE_AS_WHITESPACE = new bool[ char.MaxValue - char.MinValue + 1 ];
                var DIGIT_WORD_CHARS         = new bool[ char.MaxValue - char.MinValue + 1 ];

                fixed ( bool* iaw_base = INTERPRETE_AS_WHITESPACE )
                fixed ( bool* dwc_base = DIGIT_WORD_CHARS )
                {
                    for ( var c = char.MinValue; ; c++ )
                    {
                        iaw_base[ c ] = /*char.IsWhiteSpace( c ) ||*/ char.IsPunctuation( c );
                        dwc_base[ c ] = char.IsDigit( c );

                        if ( c == char.MaxValue )
                        {
                            break;
                        }
                    }

                    foreach ( var c in INCLUDE_INTERPRETE_AS_WHITESPACE )
                    {
                        iaw_base[ c ] = true;
                    }
                    foreach ( var c in EXCLUDE_INTERPRETE_AS_WHITESPACE )
                    {
                        iaw_base[ c ] = false;
                    }

                    foreach ( var c in INCLUDE_DIGIT_WORD_CHARS )
                    {
                        dwc_base[ c ] = true;
                    }
                }

                var INTERPRETE_AS_WHITESPACE_GCHandle = GCHandle.Alloc( INTERPRETE_AS_WHITESPACE, GCHandleType.Pinned );
                _INTERPRETE_AS_WHITESPACE = (bool*) INTERPRETE_AS_WHITESPACE_GCHandle.AddrOfPinnedObject().ToPointer();

                var DIGIT_WORD_CHARS_GCHandle = GCHandle.Alloc( DIGIT_WORD_CHARS, GCHandleType.Pinned );
                _DIGIT_WORD_CHARS = (bool*) DIGIT_WORD_CHARS_GCHandle.AddrOfPinnedObject().ToPointer();
            }

            public static UnsafeConst Inst { [M(O.AggressiveInlining)] get; } = new UnsafeConst();
        }

        #region [.cctor().]
        private static readonly CharType* _CTM;
        private static readonly char*     _UIM;
        private static readonly bool*     _DWC;
        private static readonly bool*     _IAW;
        static mld_tokenizer()
        {
            _UIM = xlat_Unsafe.Inst._UPPER_INVARIANT_MAP;
            _CTM = xlat_Unsafe.Inst._CHARTYPE_MAP;
            _IAW = UnsafeConst.Inst._INTERPRETE_AS_WHITESPACE;
            _DWC = UnsafeConst.Inst._DIGIT_WORD_CHARS;
        }
        #endregion

        #region [.private field's.]
        private const int DEFAULT_WORDCAPACITY      = 1000;
        private const int DEFAULT_WORDTOUPPERBUFFER = 100;

        private readonly UrlDetector    _UrlDetector;
        private readonly List< string > _Words;
        private readonly StringBuilder  _NgramsSB;
        private char*                   _BASE;
        private char*                   _Ptr;
        private int                     _StartIndex;
        private int                     _Length;
        //private char[]                  _WordToUpperBuffer;
        private int                     _WordToUpperBufferSize;
        private GCHandle                _WordToUpperBufferGCHandle;
        private char*                   _WordToUpperBufferPtrBase;
        private Action< string >        _AddWordToListAction;
        #endregion

        #region [.ctor().]
        public mld_tokenizer( UrlDetectorModel urlModel ) : this( urlModel, DEFAULT_WORDCAPACITY ) { }
        public mld_tokenizer( UrlDetectorModel urlModel, int wordCapacity )
        {
            _UrlDetector = new UrlDetector( new UrlDetectorConfig( urlModel, UrlDetector.UrlExtractModeEnum.Position ) );
            _Words       = new List< string >( Math.Max( DEFAULT_WORDCAPACITY, wordCapacity ) );
            _NgramsSB    = new StringBuilder();
            _AddWordToListAction = new Action< string >( AddWordToList );

            ReAllocWordToUpperBuffer( DEFAULT_WORDTOUPPERBUFFER );
        }

        private void ReAllocWordToUpperBuffer( int newBufferSize )
        {
            FreeWordToUpperBuffer();

            _WordToUpperBufferSize = newBufferSize;
            var wordToUpperBuffer  = new char[ _WordToUpperBufferSize ];
            _WordToUpperBufferGCHandle = GCHandle.Alloc( wordToUpperBuffer, GCHandleType.Pinned );
            _WordToUpperBufferPtrBase  = (char*) _WordToUpperBufferGCHandle.AddrOfPinnedObject().ToPointer();
        }
        private void FreeWordToUpperBuffer()
        {
            if ( _WordToUpperBufferPtrBase != null )
            {
                _WordToUpperBufferGCHandle.Free();
                _WordToUpperBufferPtrBase = null;
            }
        }

        ~mld_tokenizer() => DisposeNativeResources();
        public void Dispose()
        {
            DisposeNativeResources();
            GC.SuppressFinalize( this );
        }
        private void DisposeNativeResources()
        {
            FreeWordToUpperBuffer();
            _UrlDetector.Dispose();
        }
        #endregion

        unsafe public IList< string > Run( string text )
        {
            _Words.Clear();
            Run( text, _AddWordToListAction );
            return (_Words);
        }
        private void AddWordToList( string word ) => _Words.Add( word );

        unsafe public void Run( string text, Action< string > processWordAction )
        {
            _StartIndex = 0;
            _Length     = 0;

            var word = default(string);

            fixed ( char* _base = text )
            {
                _BASE = _base;

                var urls = _UrlDetector.AllocateUrls( _base );
                var urlIndex = 0;
                var startUrlPtr = (urlIndex < urls.Count) ? urls[ urlIndex ].startPtr : UnsafeConst.MAX_PTR;

                #region [.main.]
                for ( _Ptr = _base; *_Ptr != '\0'; _Ptr++ )
                {
                    #region [.skip allocated url's.]
                    if ( startUrlPtr <= _Ptr )
                    {
                        if ( _Length != 0 )
                        {
                            //word
                            if ( !IsSkipedDigit() )
                            {
                                //word
                                if ( (word = CreateWord()) != null )
                                    processWordAction( word );
                            }
                        }

                        _Ptr = startUrlPtr + urls[ urlIndex ].length - 1;
                        urlIndex++;
                        startUrlPtr = (urlIndex < urls.Count) ? urls[ urlIndex ].startPtr : UnsafeConst.MAX_PTR;

                        _StartIndex = (int) (_Ptr - _BASE + 1);
                        _Length     = 0;
                        continue;
                    }
                    #endregion

                    var ct = *(_CTM + *_Ptr); //*(ctm + *(_base + i)); //
                    if ( (ct & CharType.IsWhiteSpace) == CharType.IsWhiteSpace )
                    {
                        if ( _Length != 0 )
                        {
                            //word
                            if ( !IsSkipedDigit() )
                            {
                                if ( (word = CreateWord()) != null )
                                    processWordAction( word );
                            }

                            _StartIndex += _Length;
                            _Length      = 0;
                        }

                        _StartIndex++;
                    }
                    else
                    if ( *(_IAW + *_Ptr) )
                    {
                        if ( _Length != 0 )
                        {
                            //if ( SkipIfItUrl() )
                                //continue;

                            //word
                            if ( (word = CreateWord()) != null )
                                processWordAction( word );

                            _StartIndex += _Length;
                        }

                        if ( IsLetterPrevAndNextChar() )
                        {
                            _Length = 0;
                            _StartIndex++;
                        }
                        else
                        {
                            #region [.fusking punctuation.]
                            _Length = 1;
                            //merge punctuation (with white-space's)
                            _Ptr++;
                            for ( ; *_Ptr != '\0'; _Ptr++ ) 
                            {
                                ct = *(_CTM + *_Ptr);
                                if ( (ct & CharType.IsPunctuation) != CharType.IsPunctuation &&
                                     (ct & CharType.IsWhiteSpace ) != CharType.IsWhiteSpace )
                                {
                                    break;
                                }
                                _Length++;

                                if (*_Ptr == '\0')
                                    break;
                            }
                            if ( *_Ptr == '\0' )
                            {
                                if (_Length == 1)
                                    _Length = 0;
                                break;
                            }
                            _Ptr--;
                            #endregion

                            //skip punctuation
                            #region commented
                            /*
                            if ( !IsSkipedPunctuation() )
                            {
                                //word
                                processTermAction( CreateWord() );
                            }
                            */
                            #endregion

                            _StartIndex += _Length;
                            _Length      = 0;
                        }
                    }
                    else
                    {
                        _Length++;
                    }
                }

                //last word
                if ( _Length != 0 )
                {
                    if ( !IsSkipedDigit() )
                    {
                        //word
                        if ( (word = CreateWord()) != null )
                            processWordAction( word );
                    }
                }
                #endregion
            }            
        }

        unsafe private bool IsLetterPrevAndNextChar()
        {
            if ( _Ptr == _BASE )
                return (false);

            var ch = *(_Ptr - 1);
            var ct = *(_CTM + ch);
            if ( (ct & CharType.IsLetter) != CharType.IsLetter )
                return (false);

            ch = *(_Ptr + 1);
            if ( ch == 0 )
                return (false);
            ct = *(_CTM + ch);
            if ( (ct & CharType.IsLetter) != CharType.IsLetter )
                return (false);

            return (true);
        }

        private void CreateWordAndPutToList()
        {
            var w = CreateWord();
            if ( w != null )
            {
                _Words.Add( w );
            }
        }
        unsafe private string CreateWord()
        {
            var len_minus_1 = _Length - 1;

            char* ptr = _BASE + _StartIndex;
            var start = 0;
            for ( ; start <= len_minus_1; start++ )
            {
                var ct = *(_CTM + *(ptr + start));
                if ( (ct & CharType.IsLetter) == CharType.IsLetter ||
                     (ct & CharType.IsDigit ) == CharType.IsDigit )
                    break;
            }

            var end = len_minus_1;
            for ( ; start < end; end-- )
            {
                var ct = *(_CTM + *(ptr + end));
                if ( (ct & CharType.IsLetter) == CharType.IsLetter ||
                     (ct & CharType.IsDigit ) == CharType.IsDigit )
                    break; 
            }

            if ( start != 0 || end != len_minus_1 )
            {
                if ( end <= start )
                {
                    return (null);
                }

#if NOT_USE_UPPER_INVARIANT_CONVERTION
                var w = new string( ptr, start, end - start + 1 );
                return (w);
#else
                var len = end - start + 1;
                if ( _WordToUpperBufferSize < len )
                {
                    ReAllocWordToUpperBuffer( len );
                }
                for ( int i = 0;  i < len; i++ )
                {
                    *(_WordToUpperBufferPtrBase + i) = *(_UIM + *(ptr + start + i));
                }
                var w = new string( _WordToUpperBufferPtrBase, 0, len );
                return (w);
#endif
            }
            else
            {
#if NOT_USE_UPPER_INVARIANT_CONVERTION
                var w = new string( ptr, 0, _Length );
                return (w);
#else
                if ( _WordToUpperBufferSize < _Length )
                {
                    ReAllocWordToUpperBuffer( _Length );
                }
                for ( int i = 0; i < _Length; i++ )
                {
                    *(_WordToUpperBufferPtrBase + i) = *(_UIM + *(ptr + i));
                }
                var w = new string( _WordToUpperBufferPtrBase, 0, _Length );
                return (w);
#endif
            }            
        }
        unsafe private bool IsSkipedDigit()
        {
            for ( int i = _StartIndex, len = _StartIndex + _Length; i <= len; i++ )
            {
                if ( !(*(_DWC + *(_BASE + i))) )
                {
                    return (false);
                }
            }
            return (true);
        }
    }
}

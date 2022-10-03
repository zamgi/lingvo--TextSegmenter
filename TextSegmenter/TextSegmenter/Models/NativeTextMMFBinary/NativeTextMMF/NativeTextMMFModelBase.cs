using System;
using System.IO;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    unsafe public abstract class NativeTextMMFModelBase : MMFModelBase
    {
        /// <summary>
        /// 
        /// </summary>
        internal struct Pair
        {
            public IntPtr TextPtr;
            public int    TextLength;
            public double Probability;
#if DEBUG
            public override string ToString() => ($"'{StringsHelper.ToString( TextPtr )}': {Probability}");
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        internal delegate void LoadModelFileContentCallback( ref Pair p );

        /// <summary>
        /// 
        /// </summary>
        unsafe protected sealed class TextModelFileReaderMMF : TextModelConfig
        {
            private TextModelFileReaderMMF( TextModelConfig config ) : base( config.ModelFilename, config.ModelDictionaryCapacity ) { }

            private void LoadModelFilenameContent( LoadModelFileContentCallback callbackAction )
            {
                using ( var emmf = EnumeratorMMF.Create( ModelFilename ) )
                {
                    var lineCount   = 0;
                    var text        = default(string);
                    var probability = default(double);
                    var pair        = new Pair();

                    #region [.read first line.]
                    if ( !emmf.MoveNext() )
                    {
                        return;
                    } 
                    #endregion

                    #region [.skip beginning comments.]
                    for ( ; ; )
                    {
                        #region [.check on comment.]
                        if ( *emmf.Current.Start != '#' )
                        {
                            break;
                        } 
                        #endregion

                        #region [.move to next line.]
                        if ( !emmf.MoveNext() )
                        {
                            return;
                        }
                        #endregion
                    } 
                    #endregion

                    #region [.read all lines.]
                    for ( ; ; )
                    {
                        lineCount++;

                        var ns = emmf.Current;

                        #region [.first-value in string.]
                        int startIndex_1  = 0;
                        int finishIndex_2 = ns.Length - 1;

                        //search '\t'
                        int startIndex_2  = 0;
                        int finishIndex_1 = 0;
                        for ( ; ; )
                        {
                            if ( ns.Start[ finishIndex_1 ] == '\t' )
                            {
                                startIndex_2 = finishIndex_1 + 1;
                                finishIndex_1--;
                                break;
                            }
                            //not found '\t'
                            if ( finishIndex_2 <= ++finishIndex_1 )
                            {
                                throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                            }
                        }
                        //skip ends white-spaces
                        for ( ; ; )
                        {
                            if ( ((_CTM[ ns.Start[ finishIndex_1 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (--finishIndex_1 <= startIndex_1)
                               )
                            {
                                break;
                            }
                        }

                        if ( finishIndex_1 < startIndex_1 )
                        {
                            throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                        }
                        #endregion

                        #region [.second-value in string.]
                        //skip starts white-spaces
                        for ( ; ; )
                        {
                            if ( ((_CTM[ ns.Start[ startIndex_2 ] ] & CharType.IsWhiteSpace) != CharType.IsWhiteSpace) ||
                                 (finishIndex_2 <= ++startIndex_2)
                               )
                            {
                                break;
                            }
                        }
                        #endregion

                        #region [.fill 'Pair_v2' & calling 'callbackAction()'.]
                        var len = (finishIndex_2 - startIndex_2) + 1;
                        text = StringsHelper.ToString( ns.Start + startIndex_2, len );

                        if ( !double.TryParse( text, NS, NFI, out probability ) )
                        {
                            throw (new InvalidDataException( string.Format( INVALIDDATAEXCEPTION_FORMAT_MESSAGE, ModelFilename, lineCount, ns.ToString() ) ));
                        }

                        pair.TextLength = (finishIndex_1 - startIndex_1) + 1;
                        var textPtr = ns.Start + startIndex_1;
                        textPtr[ pair.TextLength ] = '\0';
                        StringsHelper.ToUpperInvariantInPlace( textPtr, pair.TextLength );

                        pair.TextPtr = (IntPtr) textPtr;
                        pair.Probability = probability;

                        callbackAction( ref pair ); 
                        #endregion

                        #region [.move to next line.]
                        if ( !emmf.MoveNext() )
                        {
                            break;
                        }
                        #endregion
                    }
                    #endregion
                }
            }

            internal static void Read( TextModelConfig config, LoadModelFileContentCallback callbackAction )
            {
                var _this = new TextModelFileReaderMMF( config );
                _this.LoadModelFilenameContent( callbackAction );
            }
        }
    }
}

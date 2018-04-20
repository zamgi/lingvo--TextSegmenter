using System;
using System.Collections.Generic;
using System.Linq;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class TextSegmenter : ITextSegmenter, IDisposable
    {
        #region [.private field's.]
        private static readonly TermProbability[]        TERMPROBABILITY_EMPTY        = new TermProbability[ 0 ];
        private static readonly TermProbability_Offset[] TERMPROBABILITY_OFFSET_EMPTY = new TermProbability_Offset[ 0 ];

        private ViterbiTextSegmentation        _VTS;
        private ViterbiTextSegmentation_Offset _VTS_Offset;
        #endregion

        #region [.ctor().]
        public TextSegmenter( IModel model )
        {
            model.ThrowIfNull( nameof(model) );

            _VTS        = new ViterbiTextSegmentation( model );
            _VTS_Offset = new ViterbiTextSegmentation_Offset( model );
        }

        public void Dispose()
        {
            _VTS        = null;
            _VTS_Offset = null;
        }
        #endregion

        #region [.ITextSegmenter.]
        public IReadOnlyList< TermProbability > Run( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
            {
                return (TERMPROBABILITY_EMPTY);
            }
            //---------------------------------------------------------//

            var text_upper = StringsHelper.ToUpperInvariant( text ); //---StringsHelper.ToUpperInvariantInPlace( text );

            var tuples = _VTS.Run( text_upper );
            return (tuples);
        }
        unsafe public IReadOnlyList< TermProbability_Offset > Run_Offset( string text )
        {
            var textAsUpper = StringsHelper.ToUpperInvariant( text, out var isNullOrWhiteSpace );
            if ( isNullOrWhiteSpace )
            {
                return (TERMPROBABILITY_OFFSET_EMPTY);
            }
            //---------------------------------------------------------//

            fixed ( char* textAsUpper_ptr = textAsUpper )
            {
                var tuples = _VTS_Offset.Run( textAsUpper_ptr, textAsUpper.Length );
                return (tuples);
            }

            #region comm. in-place to-upper
            /*
            fixed ( char* text_ptr = text )
            {
                StringsHelper.ToUpperInvariantInPlace( text_ptr, out var isNullOrWhiteSpace );
                if ( isNullOrWhiteSpace )
                {
                    return (TERMPROBABILITY_OFFSET_EMPTY);
                }
                //---------------------------------------------------------//

                var tuples = _VTS_Offset.Run( text_ptr, text.Length );
                return (tuples);
            }
            */ 
            #endregion
        }
        #endregion

        #region [.helper's.]
        unsafe public static bool HasCyrillicLetters( string text, int cyrillicLettersPercent, out int resultCyrillicLettersPercent )
        {
            resultCyrillicLettersPercent = 100;
            if ( cyrillicLettersPercent <= 0 )
            {
                return (true);
            }                

            var len  = text.Length;
            var rate = (cyrillicLettersPercent / 100.0f);
            var cyrillicLettersThreshold = (int) (rate * len); //Convert.ToInt32( rate * len );
            var cyrillicLettersCount = 0;
            var nonLettersCount = 0;
            fixed ( char* _base = text )
            fixed ( CharType* ctm = xlat.CHARTYPE_MAP )
            {                
                for ( int i = 0; i < len; i++ )
                {
                    var ch = *(_base + i);
                    if ( 'А' <= ch && ch <= 'я') // кирилический символ
                    {
                        if ( cyrillicLettersThreshold <= ++cyrillicLettersCount )
                        {
                			resultCyrillicLettersPercent = (int)((1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0); //Convert.ToInt32( (1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0 );
                            return (true);
                        }
                    }
                    else if ( (*(ctm + ch) & CharType.IsLetter) != CharType.IsLetter )
                    {
                        nonLettersCount++;
                    }
                }

                cyrillicLettersThreshold = (int)(rate * (len - nonLettersCount)); //Convert.ToInt32( rate * (len - nonLettersCount) );
                resultCyrillicLettersPercent = (int)((1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0); //Convert.ToInt32( (1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0 );
                return (cyrillicLettersThreshold <= cyrillicLettersCount);
            }
        }
        unsafe public static bool HasCyrillicLetters( string text, int cyrillicLettersPercent )
        {
            if ( cyrillicLettersPercent <= 0 )
            {
                return (true);
            }                

            var len  = text.Length;
            var rate = (cyrillicLettersPercent / 100.0f);
            var cyrillicLettersThreshold = (int) (rate * len); //Convert.ToInt32( rate * len );
            var cyrillicLettersCount = 0;
            var nonLettersCount = 0;
            fixed ( char* _base = text )
            fixed ( CharType* ctm = xlat.CHARTYPE_MAP )
            {                
                for ( int i = 0; i < len; i++ )
                {
                    var ch = *(_base + i); //*ptr;
                    if ( 'А' <= ch && ch <= 'я') // кирилический символ
                    {
                        if ( cyrillicLettersThreshold <= ++cyrillicLettersCount )
                        {
                            return (true);
                        }
                    }
                    else if ( (*(ctm + ch) & CharType.IsLetter) != CharType.IsLetter )
                    {
                        nonLettersCount++;
                    }
                }

                cyrillicLettersThreshold = (int)(rate * (len - nonLettersCount)); //Convert.ToInt32( rate * (len - nonLettersCount) );
                return (cyrillicLettersThreshold <= cyrillicLettersCount);
            }
        }
        #endregion
    }
}

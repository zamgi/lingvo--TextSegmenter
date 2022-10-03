using System;

using lingvo.core;

namespace lingvo.tokenizing
{
    /// <summary>
    /// 
    /// </summary>
    internal static class rld_tokenizer
    {
        unsafe public static string[] ParseText( string text )
        {
            text = ReplaceInvisibleCharacters( text );

            var terms = text.Split( xlat.WHITESPACE_CHARS, StringSplitOptions.RemoveEmptyEntries );

            fixed ( CharType* ctm = xlat.CHARTYPE_MAP        )
            fixed ( char*     uim = xlat.UPPER_INVARIANT_MAP )
            {
                for ( int i = 0, len = terms.Length; i < len; i++ )
                {
                    terms[ i ] = NormalizeTerm( ctm, uim, terms[ i ] );
                }
            }
            return (terms);
        }
        
        unsafe private static string NormalizeTerm( CharType* ctm, char* uim, string term )
        {
            var len_minus_1 = term.Length - 1;
            fixed ( char* ptr = term )
            {
                var start = 0;
                for ( ; start <= len_minus_1; start++ )
                {
                    //if ( char.IsLetter( *(ptr + start) ) )
                    //if ( (xlat.CHARTYPE_MAP[ *(ptr + start) ] & CharType.IsLetter) == CharType.IsLetter )
                    if ( (*(ctm + *(ptr + start)) & CharType.IsLetter) == CharType.IsLetter )
                        break;                
                }

                var end = len_minus_1;
                for ( ; start < end; end-- )
                {
                    //if ( char.IsLetter( *(ptr + end) ) )
                    //if ( (xlat.CHARTYPE_MAP[ *(ptr + end) ] & CharType.IsLetter) == CharType.IsLetter )
                    if ( (*(ctm + *(ptr + end)) & CharType.IsLetter) == CharType.IsLetter )
                        break; 
                }

                if ( start != 0 || end != len_minus_1 )
                {
                    if ( end <= start )
                    {
                        return (null);    
                    }

                    for ( var i = start; i <= end; i++ )
                    {
                        //*(ptr + i) = char.ToUpperInvariant( *(ptr + i) );
                        //*(ptr + i) = xlat.UPPER_INVARIANT_MAP[ *(ptr + i) ];
                        *(ptr + i) = *(uim + *(ptr + i));
                    }

                    var normTerm = new string( ptr, start, end - start + 1 );
                    return (normTerm);
                }
                else
                {
                    for ( var i = 0; i <= len_minus_1; i++ )
                    {
                        //*(ptr + i) = char.ToUpperInvariant( *(ptr + i) );
                        //*(ptr + i) = xlat.UPPER_INVARIANT_MAP[ *(ptr + i) ];
                        *(ptr + i) = *(uim + *(ptr + i));
                    }
                    return (term);
                }
            }
        }

        /// <summary>
        /// заменить на пробелы все невидимые символы (0-30 в Dec);
        /// </summary>
        unsafe private static string ReplaceInvisibleCharacters( string text )
        {
            fixed ( char* _base = text )
            {
                for ( int i = 0, len = text.Length; i < len; i++ )
                {
                    if ( *(_base + i) <= '\u001e' )
                    {
                        *(_base + i) = '\u0020';
                    }
                }
                return (text);
            }

            #region comm.
            /*
            fixed ( char* _base = text )
            {
                for ( var ptr = _base; *ptr != '\u0000'; ptr++ )
                {
                    if ( *ptr <= '\u001e' )
                    {
                        *ptr = '\u0020';
                    }
                }
                return (text);
            }
            */
            #endregion
        }
        
#if DEBUG
        unsafe public static bool HasCyrillicLetters( string text, int cyrillicLettersPercent, out int resultCyrillicLettersPercent )
#else
        unsafe public static bool HasCyrillicLetters( string text, int cyrillicLettersPercent )
#endif
        {
#if DEBUG
            resultCyrillicLettersPercent = 100;
#endif            
            if ( cyrillicLettersPercent <= 0 )
            {
                return (true);
            }                

            var len = text.Length;
            var rate = (cyrillicLettersPercent / 100.0f);
            var cyrillicLettersThreshold = (int) (rate * len); //Convert.ToInt32( rate * len );
            var cyrillicLettersCount = 0;
            var nonLettersCount = 0;
            fixed ( char* _base = text )
            fixed ( CharType* ctm = xlat.CHARTYPE_MAP )
            {                
                for ( int i = 0; i < len; i++ ) //for ( var ptr = _base; *ptr != 0; ptr++ )
                {
                    var ch = *(_base + i); //*ptr;
                    if ( 'А' <= ch && ch <= 'я') // кирилический символ
                    {
                        if ( cyrillicLettersThreshold <= ++cyrillicLettersCount )
                        {
#if DEBUG
                			resultCyrillicLettersPercent = (int)((1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0); //Convert.ToInt32( (1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0 );
#endif
                            return (true);
                        }
                    }
                    else if ( (*(ctm + ch) & CharType.IsLetter) != CharType.IsLetter )
                       //if ( !char.IsLetter( ch ) ) // char.IsWhiteSpace( ch ) || char.IsDigit( ch ) || char.IsPunctuation( ch ) )
                    {
                        nonLettersCount++;
                    }
                }

                cyrillicLettersThreshold = (int)(rate * (len - nonLettersCount)); //Convert.ToInt32( rate * (len - nonLettersCount) );
#if DEBUG
                resultCyrillicLettersPercent = (int)((1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0); //Convert.ToInt32( (1.0 * cyrillicLettersCount / (len - nonLettersCount)) * 100.0 );
#endif
                return (cyrillicLettersThreshold <= cyrillicLettersCount);
            }
        }
    }
}

using System;
using System.Collections.Generic;

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
        public IReadOnlyList< TermProbability > Run_Debug( string text )
        {
            if ( text.IsNullOrWhiteSpace() )
            {
                return (TERMPROBABILITY_EMPTY);
            }
            //---------------------------------------------------------//

            var text_upper = StringsHelper.ToUpperInvariant( text ); //---StringsHelper.ToUpperInvariantInPlace( text );

            var tuples = _VTS.Run_Debug( text_upper );
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
    }
}

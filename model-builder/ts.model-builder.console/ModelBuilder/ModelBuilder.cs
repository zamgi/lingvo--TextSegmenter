using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using lingvo.core.algorithm;
using lingvo.tokenizing;
using lingvo.urls;

namespace lingvo.ts.modelbuilder
{
    /// <summary>
    /// 
    /// </summary>
    internal struct BuildParams_t
    {
        public UrlDetectorModel UrlDetectorModel;
        public string           InputDirectory;
        public NGramsEnum       Ngrams;
        public float?           CutPercent;
        public string           OutputDirectory;
        public int              MaxPortionSize;
        public bool             ClearCyrillicsChars;
        public bool             ClearDigitsChars;
        public int              SingleWordMaxLength;

        public IEnumerable< FileInfo > EnumerateFilesFromInputFolder( string searchPattern = "*.txt", SearchOption searchOption = SearchOption.TopDirectoryOnly )
        {
            var fis = from fileName in Directory.EnumerateFiles( this.InputDirectory, searchPattern, searchOption )
                        let fi = new FileInfo( fileName )
                        orderby fi.Length descending
                        select fi;
            return (fis);
        }

        public override string ToString() => $"{Ngrams}-cut_{CutPercent.GetValueOrDefault()}%";
    }    

    /// <summary>
    /// 
    /// </summary>
    internal sealed class Portioner
    {
        /// <summary>
        /// 
        /// </summary>
        private sealed class ngram_filereader : IDisposable
        {
            private static readonly char[] SPLIT_CHAR = new[] { '\t' };
            private readonly StreamReader _Sr;
            private string _Line;

            public ngram_filereader( string fileName )
            {
                _Sr   = new StreamReader( fileName, Config.Inst.INPUT_ENCODING );
                _Line = _Sr.ReadLine();
            }

            public void Dispose()
            {
                _Sr.Dispose();
            }

            public TermFrequency ReadNext()
            {
                if ( _Line != null )
                {
                    var a = _Line.Split( SPLIT_CHAR, StringSplitOptions.RemoveEmptyEntries );
                    _Line = _Sr.ReadLine();
                    var word = new TermFrequency() 
                    { 
                        Term = a[ 0 ], 
                        Frequency = int.Parse( a[ 1 ] ) 
                    };
                    return (word);
                }
                return (null);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class tuple : IDisposable
        {
            public tuple( TermFrequency _tf, ngram_filereader _ngram_filereader )
            {
                tf             = _tf;
                ngram_filereader = _ngram_filereader;
            }

            public TermFrequency tf
            {
                get;
                set;
            }
            public ngram_filereader ngram_filereader
            {
                get;
                private set;
            }

            public void Dispose()
            {
                ngram_filereader.Dispose();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class tuple_tf_term_Comparer : IComparer< tuple >
        {
            public static readonly tuple_tf_term_Comparer Instance = new tuple_tf_term_Comparer();
            private tuple_tf_term_Comparer() { }

            #region [.IComparer< tuple_t >.]
            public int Compare( tuple x, tuple y ) => string.CompareOrdinal( x.tf.Term, y.tf.Term );
            #endregion
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class tuple_tf_frequency_Comparer : IComparer< tuple >
        {
            public static readonly tuple_tf_frequency_Comparer Instance = new tuple_tf_frequency_Comparer();
            private tuple_tf_frequency_Comparer() { }

            #region [.IComparer< tuple_t >.]
            public int Compare( tuple x, tuple y )
            {
                var d = y.tf.Frequency - x.tf.Frequency;            
                if ( d != 0 )
                    return (d);

                return (string.CompareOrdinal( y.tf.Term, x.tf.Term ));
            }
            #endregion
        }
        /// <summary>
        /// 
        /// </summary>
        private sealed class stringComparer : IComparer< string >, IEqualityComparer< string >
        {
            public static readonly stringComparer Instance = new stringComparer();
            private stringComparer() { }

            #region [.IComparer< string >.]
            public int Compare( string x, string y ) => string.CompareOrdinal( x, y );
            #endregion

            #region [.IEqualityComparer< string >.]
            public bool Equals( string x, string y ) => (string.CompareOrdinal( x, y ) == 0);
            public int GetHashCode( string obj ) => obj.GetHashCode();
            #endregion
        }

        #region [.field's.]        
        private BuildParams_t             _Bp;
        private FileInfo                  _Fi;
        private int                       _DocumentWordCount;
        private Dictionary< string, int > _DocumentNgrams_1;
        private Dictionary< string, int > _DocumentNgrams_2;
        private Dictionary< string, int > _DocumentNgrams_3;
        private Dictionary< string, int > _DocumentNgrams_4;
        private string                    _Word_prev1;
        private string                    _Word_prev2;
        private string                    _Word_prev3;
        private StringBuilder             _Sb;
        private TFProcessor               _TFProcessor;
        private int                       _CurrentProcesId;
        private Dictionary< NGramsEnum, List< string > > _OutputFilenames;
#if DEBUG
        private int _SkipWordCount; 
#endif
        #endregion

        #region [.ctor().]
        public Portioner( BuildParams_t bp, FileInfo fi, TFProcessor tfProcessor )
        {
            _Bp = bp;
            _Fi = fi;

            _OutputFilenames  = new Dictionary< NGramsEnum, List< string > >();
            _OutputFilenames.Add( NGramsEnum.ngram_1, new List< string >() );
            _DocumentNgrams_1 = new Dictionary< string, int >( _Bp.MaxPortionSize, stringComparer.Instance );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    _OutputFilenames.Add( NGramsEnum.ngram_4, new List< string >() );
                    _DocumentNgrams_4 = new Dictionary< string, int >( _Bp.MaxPortionSize, stringComparer.Instance );
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    _OutputFilenames.Add( NGramsEnum.ngram_3, new List< string >() );
                    _DocumentNgrams_3 = new Dictionary< string, int >( _Bp.MaxPortionSize, stringComparer.Instance );
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    _OutputFilenames.Add( NGramsEnum.ngram_2, new List< string >() );
                    _DocumentNgrams_2 = new Dictionary< string, int >( _Bp.MaxPortionSize, stringComparer.Instance );
                break;
            }
               
            _Sb          = new StringBuilder();
            _TFProcessor = tfProcessor;
            using ( var p = Process.GetCurrentProcess() )
            {
                _CurrentProcesId = p.Id;
            }
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWordAction( string word )
        {
            if ( _Bp.SingleWordMaxLength < word.Length )
            {
#if DEBUG
                Console.Write( $"\r\nskip-by-len #{(++_SkipWordCount)}: '{word}'" );
#endif
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }

            CheckPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

            _DocumentWordCount++;

            _DocumentNgrams_1.AddOrUpdate( word );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    CheckPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                    if ( _Word_prev3 != null )
                    {
                        _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev3 ).Append( ' ' )
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev3 = _Word_prev2;
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    CheckPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                    if ( _Word_prev2 != null )
                    {
                        _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev2 = _Word_prev1;
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    CheckPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                    if ( _Word_prev1 != null )
                    {
                        _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev1 = word;
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWordActionClearDigitsChars( string word )
        {
            if ( _Bp.SingleWordMaxLength < word.Length )
            {
#if DEBUG
                Console.Write( $"\r\nskip-by-len #{(++_SkipWordCount)}: '{word}'" );
#endif
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }
            if ( word.HasDigitsChars() )
            {
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }

            CheckPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

            _DocumentWordCount++;

            _DocumentNgrams_1.AddOrUpdate( word );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    CheckPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                    if ( _Word_prev3 != null )
                    {
                        _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev3 ).Append( ' ' )
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev3 = _Word_prev2;
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    CheckPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                    if ( _Word_prev2 != null )
                    {
                        _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev2 = _Word_prev1;
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    CheckPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                    if ( _Word_prev1 != null )
                    {
                        _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev1 = word;
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWordActionClearCyrillicsChars( string word )
        {
            if ( word.HasCyrillicsChars() )
            {
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }
            if ( _Bp.SingleWordMaxLength < word.Length )
            {
#if DEBUG
                Console.Write( $"\r\nskip-by-len #{(++_SkipWordCount)}: '{word}'" );
#endif
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }

            CheckPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

            _DocumentWordCount++;

            _DocumentNgrams_1.AddOrUpdate( word );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    CheckPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                    if ( _Word_prev3 != null )
                    {
                        _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev3 ).Append( ' ' )
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev3 = _Word_prev2;
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    CheckPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                    if ( _Word_prev2 != null )
                    {
                        _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev2 = _Word_prev1;
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    CheckPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                    if ( _Word_prev1 != null )
                    {
                        _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev1 = word;
                break;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessWordActionClearCyrillicsAndDigitsChars( string word )
        {
            if ( word.HasCyrillicsOrDigitsChars() )
            {
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }
            if ( _Bp.SingleWordMaxLength < word.Length )
            {
#if DEBUG
                Console.Write( $"\r\nskip-by-len #{(++_SkipWordCount)}: '{word}'" );
#endif
                _Word_prev3 = _Word_prev2 = _Word_prev1 = null;
                return;
            }

            CheckPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

            _DocumentWordCount++;

            _DocumentNgrams_1.AddOrUpdate( word );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    CheckPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                    if ( _Word_prev3 != null )
                    {
                        _DocumentNgrams_4.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev3 ).Append( ' ' )
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev3 = _Word_prev2;
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    CheckPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                    if ( _Word_prev2 != null )
                    {
                        _DocumentNgrams_3.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev2 ).Append( ' ' )
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev2 = _Word_prev1;
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    CheckPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                    if ( _Word_prev1 != null )
                    {
                        _DocumentNgrams_2.AddOrUpdate( _Sb.Clear()
                                                            .Append( _Word_prev1 ).Append( ' ' )
                                                            .Append( word )
                                                            .ToString() 
                                                        );                        
                    }
                    _Word_prev1 = word;
                break;
            }
        }
        private void ProcessLastAction()
        {
            CheckLastPortion( _DocumentNgrams_1, NGramsEnum.ngram_1 );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    CheckLastPortion( _DocumentNgrams_4, NGramsEnum.ngram_4 );
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    CheckLastPortion( _DocumentNgrams_3, NGramsEnum.ngram_3 );
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    CheckLastPortion( _DocumentNgrams_2, NGramsEnum.ngram_2 );
                break;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckPortion( Dictionary< string, int > dict, NGramsEnum ngram )
        {
            if ( _Bp.MaxPortionSize <= dict.Count )
            {
                var lst = _OutputFilenames[ ngram ];
                var outputFilename = Write2File( _Fi, lst.Count, dict, ngram, _CurrentProcesId );
                lst.Add( outputFilename );
                dict.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckLastPortion( Dictionary< string, int > dict, NGramsEnum ngram )
        {
            if ( dict.Count != 0 )
            {
                var lst = _OutputFilenames[ ngram ];
                if ( 0 < lst.Count )
                {
                    var outputFilename = Write2File( _Fi, lst.Count, dict, ngram, _CurrentProcesId );
                    lst.Add( outputFilename );
                    dict.Clear();
                } 
            }
        }


        private void ProcessNgrams()
        {
            _TFProcessor.BeginAddDocumentTerms();

            //-ngran_1-
            ProcessNgrams_Routine( NGramsEnum.ngram_1 );

            switch ( _Bp.Ngrams )
            {
                case NGramsEnum.ngram_4:
                    ProcessNgrams_Routine( NGramsEnum.ngram_4 );
                goto case NGramsEnum.ngram_3;

                case NGramsEnum.ngram_3:
                    ProcessNgrams_Routine( NGramsEnum.ngram_3 );
                goto case NGramsEnum.ngram_2;

                case NGramsEnum.ngram_2:
                    ProcessNgrams_Routine( NGramsEnum.ngram_2 );
                break;
            }

            _TFProcessor.EndAddDocumentTerms( _DocumentWordCount );
        }
        private void ProcessNgrams_Routine( NGramsEnum ngram )
        {
            var outputFilenames = _OutputFilenames[ ngram ];
            if ( outputFilenames.Count == 0 )
            {
                var dict = default(Dictionary< string, int >);
                switch ( ngram )
                {
                    //case NGramsEnum.ngram_1: dict = _DocumentNgrams_1; break;
                    case NGramsEnum.ngram_2: dict = _DocumentNgrams_2; break;
                    case NGramsEnum.ngram_3: dict = _DocumentNgrams_3; break;
                    case NGramsEnum.ngram_4: dict = _DocumentNgrams_4; break;
                    default:                 dict = _DocumentNgrams_1; break;
                }

                _TFProcessor.AddDocumentTerms( dict );
            }
            else
            {
                Console.WriteLine();

                //-1-
                _OutputFilenames[ ngram ] = new List< string >();
                var ss  = new SortedSet< TermFrequency >( TermFrequencyComparer.Instance );
                foreach ( var tf in GroupByMerging_1( outputFilenames ) )
                {
                    ss.Add( tf );
                    if ( _Bp.MaxPortionSize <= ss.Count )
                    {
                        var lst = _OutputFilenames[ ngram ];
                        var outputFilename = Write2File( _Fi, lst.Count, ss, ngram, _CurrentProcesId );
                        lst.Add( outputFilename );
                        ss.Clear();
                    }
                }
                if ( ss.Count != 0 )
                {
                    var lst = _OutputFilenames[ ngram ];
                    var outputFilename = Write2File( _Fi, lst.Count, ss, ngram, _CurrentProcesId );
                    lst.Add( outputFilename );
                    ss.Clear();
                }

                //-2-
                outputFilenames.ForEach( outputFilename => File.Delete( outputFilename ) );
                outputFilenames = _OutputFilenames[ ngram ];
                var tuples = CreateTuples4Merging( outputFilenames );
                ss = TFProcessor.CreateSortedSetAndCutIfNeed( GroupByMerging_2( tuples ) );

                //-3-
                tuples.ForEach( t => t.Dispose() );
                outputFilenames.ForEach( outputFilename => File.Delete( outputFilename ) );
                _TFProcessor.AddDocumentTerms( ss );
            }
        }


        public void BuildTFMatrix_UsePortion()
        {
            using ( var tokenizer = new mld_tokenizer( _Bp.UrlDetectorModel ) )
            {
                //-1-
                var processWordAction = default(Action< string >);
                if ( _Bp.ClearCyrillicsChars )
                {
                    if ( _Bp.ClearDigitsChars )
                    {
                        processWordAction = ProcessWordActionClearCyrillicsAndDigitsChars;
                    }
                    else
                    {
                        processWordAction = ProcessWordActionClearCyrillicsChars;
                    }
                }
                else
                {
                    if ( _Bp.ClearDigitsChars )
                    {
                        processWordAction = ProcessWordActionClearDigitsChars;
                    }
                    else
                    {
                        processWordAction = ProcessWordAction;
                    }
                }

                var totalSentenceCount = 0;
                using ( var sr = new StreamReader( _Fi.FullName, Config.Inst.INPUT_ENCODING ) )
                {
                    for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                    {
                        tokenizer.Run( line, processWordAction );
                        totalSentenceCount++;

                        #region [.print sentence-count.]
                        if ( (totalSentenceCount % 100_000) == 0 )
                        {
                            Console.Write( '.' );
                            if ( (totalSentenceCount % 1_000_000) == 0 )
                            {
                                Console.WriteLine( $"sentence-count: {totalSentenceCount}, ngrams_1-count: {_DocumentNgrams_1.Count}" );
                            }
                        } 
                        #endregion
                    }
                    #region [.print sentence-count.]
                    Console.WriteLine( $"total-sentence-count: {totalSentenceCount}" );
                    #endregion

                    ProcessLastAction();

                    if ( (_OutputFilenames[ NGramsEnum.ngram_1 ].Count == 0) && (_DocumentNgrams_1.Count == 0) )
                    {
                        throw (new InvalidDataException( $"input text is-null-or-white-space, filename: '{_Fi.FullName}'" ));
                    }
                }

                //-2-
                ProcessNgrams();
            }
        }


        private static IEnumerable< TermFrequency > GroupByMerging_1    ( List< string > fileNames )
        {
            var current_tuples = new List< tuple >( fileNames.Count );
            for ( var i = 0; i < fileNames.Count; i++ )
            {
                var nfr = new ngram_filereader( fileNames[ i ] );
                var tf  = nfr.ReadNext();

                current_tuples.Add( new tuple( tf, nfr ) );
            }

            for ( ; current_tuples.Count != 0; )
            {
                current_tuples.Sort( tuple_tf_term_Comparer.Instance );

                var tuple = current_tuples[ 0 ];
                var tf    = tuple.tf;
                
                for ( var i = 1; i < current_tuples.Count; i++ )
                {
                    var t = current_tuples[ i ];
                    if ( tf.Term == t.tf.Term )
                    {
                        tf.Frequency += t.tf.Frequency;

                        t.tf = t.ngram_filereader.ReadNext();
                        if ( t.tf == null )
                        {
                            t.Dispose();
                            current_tuples.RemoveAt( i );
                            i--;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                tuple.tf = tuple.ngram_filereader.ReadNext();
                if ( tuple.tf == null )
                {
                    tuple.Dispose();
                    current_tuples.RemoveAt( 0 );
                }

                yield return (tf);
            }

        }
        private static List< tuple >                CreateTuples4Merging( List< string > fileNames )
        {
            var tuples = new List< tuple >( fileNames.Count );
            for ( var i = 0; i < fileNames.Count; i++ )
            {
                var nfr = new ngram_filereader( fileNames[ i ] );
                var tf  = nfr.ReadNext();

                tuples.Add( new tuple( tf, nfr ) );
            }
            return (tuples);
        }
        private static IEnumerable< TermFrequency > GroupByMerging_2    ( List< tuple >  tuples )
        {
            for ( ; tuples.Count != 0; )
            {
                tuples.Sort( tuple_tf_frequency_Comparer.Instance );

                var tuple = tuples[ 0 ];
                var tf    = tuple.tf;

                tuple.tf = tuple.ngram_filereader.ReadNext();
                if ( tuple.tf == null )
                {
                    tuple.Dispose();
                    tuples.RemoveAt( 0 );
                }

                yield return (tf);
            }
        }

        private static string Write2File( FileInfo fi, int portionNumber, Dictionary< string, int > tf_matrix, NGramsEnum tf_matrix_type, int currentProcesId )
        {
            var outputFilename = Path.Combine( fi.DirectoryName, "temp", $"pid_{currentProcesId}, {fi.Name}.{tf_matrix_type}.{portionNumber}" );
            Console.Write( $"start write portion-file: '{outputFilename}'..." );

            var ss = new SortedDictionary< string, int >( stringComparer.Instance );
            foreach ( var p in tf_matrix )
            {
                ss.Add( p.Key, p.Value );
            }
            tf_matrix.Clear();

            var ofi = new FileInfo( outputFilename );
            if ( !ofi.Directory.Exists )
            {
                ofi.Directory.Create();
            }
            using ( var sw = new StreamWriter( outputFilename, false, Config.Inst.OUTPUT_ENCODING ) )
            {
                foreach ( var p in ss )
                {
                    sw.Write( p.Key );
                    sw.Write( '\t' );
                    sw.WriteLine( p.Value );
                }
            }
            ss.Clear();
            ss = null;
            GC.Collect();

            Console.WriteLine( " => end write portion-file" );

            return (outputFilename);
        }
        private static string Write2File( FileInfo fi, int portionNumber, SortedSet< TermFrequency > ss, NGramsEnum ss_type, int currentProcesId )
        {
            var outputFilename = Path.Combine( fi.DirectoryName, "temp", $"pid_{currentProcesId}, {fi.Name}.ss.{ss_type}.{portionNumber}" );
            Console.Write( $"start write portion-file: '{outputFilename}'..." );

            var ofi = new FileInfo( outputFilename );
            if ( !ofi.Directory.Exists )
            {
                ofi.Directory.Create();
            }
            using ( var sw = new StreamWriter( outputFilename, false, Config.Inst.OUTPUT_ENCODING ) )
            {
                foreach ( var tf in ss )
                {
                    sw.Write( tf.Term );
                    sw.Write( '\t' );
                    sw.WriteLine( tf.Frequency );
                }
            }
            ss.Clear();
            ss = null;
            GC.Collect();

            Console.WriteLine( " => end write portion-file" );

            return (outputFilename);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ModelBuilder
    {
        public const int TFPROCESSOR_DICTIONARY_CAPACITY = 4_500_000;

        public static void Build( BuildParams_t bp, int tfProcessorDictionaryCapacity = TFPROCESSOR_DICTIONARY_CAPACITY )
        {
            #region [.-0-.]
            Console.WriteLine( $"start process folder: '{bp.InputDirectory}'..." );

            var tokenizer   = new mld_tokenizer( bp.UrlDetectorModel );
            var tfProcessor = TFProcessor.Create( tfProcessorDictionaryCapacity );

#if DEBUG
            var skipWordCount = 0; 
#endif
            var processWordAction = default(Action< string >);
            if ( bp.ClearCyrillicsChars )
            {
                if ( bp.ClearDigitsChars )
                {
                    processWordAction = (word) =>
                    {
                        if ( !word.HasCyrillicsOrDigitsChars() )
                        {
                            if ( word.Length <= bp.SingleWordMaxLength )
                            {
                                tfProcessor.AddTerm( word );
                            }
#if DEBUG
                            else
                            {
                                Console.Write( $"\r\nskip-by-len #{(++skipWordCount)}: '{word}'" );
                            } 
#endif
                        }
                    };
                }
                else
                {
                    processWordAction = (word) =>
                    {
                        if ( !word.HasCyrillicsChars() )
                        {
                            if ( word.Length <= bp.SingleWordMaxLength )
                            {
                                tfProcessor.AddTerm( word );
                            }
#if DEBUG
                            else
                            {
                                Console.Write( $"\r\nskip-by-len #{(++skipWordCount)}: '{word}'" );
                            } 
#endif
                        }
                    };
                }
            }
            else
            {
                if ( bp.ClearDigitsChars )
                {
                    processWordAction = (word) =>
                    {
                        if ( word.Length <= bp.SingleWordMaxLength )
                        {
                            if ( !word.HasDigitsChars() )
                            {
                                tfProcessor.AddTerm( word );
                            }
                        }
#if DEBUG
                        else
                        {
                            Console.Write( $"\r\nskip-by-len #{(++skipWordCount)}: '{word}'" );
                        }
#endif
                    };
                }
                else
                {
                    processWordAction = (word) =>
                    {
                        if ( word.Length <= bp.SingleWordMaxLength )
                        {
                            tfProcessor.AddTerm( word );
                        }
#if DEBUG
                        else
                        {
                            Console.Write( $"\r\nskip-by-len #{(++skipWordCount)}: '{word}'" );
                        }
#endif
                    };
                }
            }
            #endregion

            #region [.-1-.]
            var totalSentenceCount = 0;
            var first_fi = default(FileInfo);
            var fis      = bp.EnumerateFilesFromInputFolder();
            var fi_num   = 0;
            foreach ( var fi in fis )
            {
                if ( first_fi == null)
                {
                    first_fi = fi;
                }

                Console.WriteLine( $"{(++fi_num)}). start process file: '{fi.Name}' [{fi.DisplaySize()}]..." );

                using ( var sr = new StreamReader( fi.FullName, Config.Inst.INPUT_ENCODING ) )
                {
                    for ( var line = sr.ReadLine(); line != null; line = sr.ReadLine() )
                    {
                        tokenizer.Run( line, processWordAction );

                        #region [.print-2-console.]
                        if ( (++totalSentenceCount % 100_000) == 0 )
                        {
                            Console.Write( '.' );
                            if ( (totalSentenceCount % 1_000_000) == 0 )
                            {
                                Console.WriteLine( $"sentence-count: {totalSentenceCount}, ngrams_1-count: {tfProcessor.DictionarySize}" );
                            }
                        }
                        #endregion
                    }
                    #region [.print-2-console.]
                    Console.WriteLine( $"total-sentence-count: {totalSentenceCount}" );
                    #endregion
                }
                GC.Collect();
                Console.WriteLine( "end process file" );
            }

            if ( first_fi == null )
            {
                throw (new InvalidDataException( $"No .txt-files found by path: '{bp.InputDirectory}'" ));
            }
            #endregion
            
            #region [.-2-.]
            Console.Write( "start calc probability..." );
            var probabilityResult = tfProcessor.CalcProbabilityOrdered( Config.Inst.CUT_PERCENT );
            tfProcessor = default(TFProcessor);
            GC.Collect();
            Console.WriteLine( "end calc probability" );
            #endregion

            #region [.-3-.]
            Console.Write( "start write result..." );
            if ( !Directory.Exists( bp.OutputDirectory ) )
            {
                Directory.CreateDirectory( bp.OutputDirectory );
            }

            var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };

            var outputFile = Path.Combine( bp.OutputDirectory, Path.GetFileNameWithoutExtension( first_fi.Name ) + $"-({bp.Ngrams}-cut_{bp.CutPercent.GetValueOrDefault()}%){first_fi.Extension}" );

            using ( var sw = new StreamWriter( outputFile, false, Config.Inst.OUTPUT_ENCODING ) )
            {
                sw.WriteLine( $"#\t'{first_fi.Name}' ({bp.Ngrams}-cut_{bp.CutPercent.GetValueOrDefault()}%)" );

                foreach ( var tp in probabilityResult )
                {
                    if ( tp.Probability != 0 )
                    {
                        sw.Write( tp.Term );
                        sw.Write( '\t' );
                        sw.WriteLine( tp.Probability.ToString( nfi ) );
                    }
                }
            }
            
            Console.WriteLine( $"end write result{Environment.NewLine}" );
            #endregion
        }

        public static void Build_UsePortion( BuildParams_t bp, int tfProcessorDictionaryCapacity = TFPROCESSOR_DICTIONARY_CAPACITY )
        {
            #region [.-0-.]
            Console.WriteLine( $"start process folder: '{bp.InputDirectory}'..." );

            var tfProcessor = TFProcessor.Create( tfProcessorDictionaryCapacity );
            #endregion

            #region [.-1-.]
            var first_fi = default(FileInfo);
            var fis      = bp.EnumerateFilesFromInputFolder();
            var fi_num   = 0;
            foreach ( var fi in fis )
            {
                if ( first_fi == null )
                {
                    first_fi = fi;
                }

                Console.WriteLine( $"{(++fi_num)}). start process file: '{fi.Name}' [{fi.DisplaySize()}]..." );

                BuildTFMatrix_UsePortion( bp, fi, tfProcessor );

                Console.WriteLine( $"end process file{Environment.NewLine}" );
            }

            if ( first_fi == null )
            {
                throw (new InvalidDataException( $"No .txt-files found by path: '{bp.InputDirectory}'" ));
            }
            #endregion

            #region [.-2-.]
            Console.Write( "start calc probability..." );
            var probabilityResult = tfProcessor.CalcProbabilityOrdered( Config.Inst.CUT_PERCENT );
            tfProcessor = default(TFProcessor);
            GC.Collect();
            Console.WriteLine( "end calc probability" );
            #endregion

            #region [.-3-.]
            Console.Write( "start write result..." );
            if ( !Directory.Exists( bp.OutputDirectory ) )
            {
                Directory.CreateDirectory( bp.OutputDirectory );
            }

            var nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };

            var outputFile = Path.Combine( bp.OutputDirectory, Path.GetFileNameWithoutExtension( first_fi.Name ) + $"-({bp.Ngrams}-cut_{bp.CutPercent.GetValueOrDefault()}%){first_fi.Extension}" );

            using ( var sw = new StreamWriter( outputFile, false, Config.Inst.OUTPUT_ENCODING ) )
            {
                sw.WriteLine( $"#\t'{first_fi.Name}' ({bp.Ngrams}-cut_{bp.CutPercent.GetValueOrDefault()}%)" );

                foreach ( var tp in probabilityResult )
                {
                    if ( tp.Probability != 0 )
                    {
                        sw.Write( tp.Term );
                        sw.Write( '\t' );
                        sw.WriteLine( tp.Probability.ToString( nfi ) );
                    }
                }
            }

            var tempOutputFolder = Path.Combine( bp.InputDirectory, "temp" );
            if ( Directory.Exists( tempOutputFolder ) && !Directory.EnumerateFiles( tempOutputFolder, "*", SearchOption.TopDirectoryOnly ).Any() )
            {
                Directory.Delete( tempOutputFolder, true );
            }

            Console.WriteLine( $"end write result{Environment.NewLine}" );            
            #endregion
        }
        private static void BuildTFMatrix_UsePortion( BuildParams_t bp, FileInfo fi, TFProcessor tfProcessor )
        {
            var portioner = new Portioner( bp, fi, tfProcessor );

            portioner.BuildTFMatrix_UsePortion();

            portioner = null;
            GC.Collect();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static bool HasCyrillicsChars( this string value )
        {
            fixed ( char* _base = value )
            {
                for ( var ptr = _base; ; ptr++ )
                {
                    var ch = *ptr;
                    switch ( ch )
                    {
                        case '\0':
                            return (false);
                        case 'Ё':
                        case 'ё':
                            return (true);
                        default:
                            if ( 'А' <= ch && ch <= 'я' )
                                return (true);
                        break;    
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static bool HasDigitsChars( this string value )
        {
            fixed ( char* _base = value )
            {
                for ( var ptr = _base; ; ptr++ )
                {
                    var ch = *ptr;
                    switch ( ch )
                    {
                        case '\0':
                            return (false);
                        default:
                            if ( '0' <= ch && ch <= '9' )
                                return (true);
                        break;    
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static bool HasCyrillicsOrDigitsChars( this string value )
        {
            fixed ( char* _base = value )
            {
                for ( var ptr = _base; ; ptr++ )
                {
                    var ch = *ptr;
                    switch ( ch )
                    {
                        case '\0':
                            return (false);
                        case 'Ё':
                        case 'ё':
                            return (true);
                        default:
                            if ( 'А' <= ch && ch <= 'я' )
                                return (true);
                            if ( '0' <= ch && ch <= '9' )
                                return (true);
                        break;    
                    }
                }
            }
        }

        public static string DisplaySize( this FileInfo fileInfo )
        {
            const float KILOBYTE = 1024;
            const float MEGABYTE = KILOBYTE * KILOBYTE;
            const float GIGABYTE = MEGABYTE * KILOBYTE;

            if ( fileInfo == null )
                return ("NULL");

            if ( GIGABYTE < fileInfo.Length )
                return ( (fileInfo.Length / GIGABYTE).ToString("N2") + " GB");
            if ( MEGABYTE < fileInfo.Length )
                return ( (fileInfo.Length / MEGABYTE).ToString("N2") + " MB");
            if ( KILOBYTE < fileInfo.Length )
                return ( (fileInfo.Length / KILOBYTE).ToString("N2") + " KB");
            return (fileInfo.Length.ToString("N0") + " bytes");
        }

        public static void SetCurrentProcessHighPriority()
        {
            using ( var pr = Process.GetCurrentProcess() )
            {
                pr.PriorityClass              = ProcessPriorityClass.High;
                pr.PriorityBoostEnabled       = true;
                Thread.CurrentThread.Priority = ThreadPriority.Highest;
            }
        }
    }
}

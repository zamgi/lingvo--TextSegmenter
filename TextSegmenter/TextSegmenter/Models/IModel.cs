using System;
using System.Collections.Generic;

using lingvo.core;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public struct ModelRecord
    {
        public string Ngram;
        public double Probability;
#if DEBUG
        public override string ToString() => $"'{Ngram}': {Probability}";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    unsafe public struct NativeOffset
    {
        public char* BasePtr;
        public int   StartIndex;
        public int   Length;
#if DEBUG
        public override string ToString() => $"'{StringsHelper.ToString( BasePtr + StartIndex, Length )}'";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IModel : IDisposable
    {
        bool TryGetProbability( string ngram, out double probability );
        bool TryGetProbability( in NativeOffset no, out double probability );

        IEnumerable< ModelRecord > GetAllRecords();
        int RecordCount { get; }
    }
}

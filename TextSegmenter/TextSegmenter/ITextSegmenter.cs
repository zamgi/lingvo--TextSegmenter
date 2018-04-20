using System.Collections.Generic;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
    public struct TermProbability
    {
        public string Term;
        public double Probability;
#if DEBUG
        public override string ToString() => $"'{Term}': {Probability}";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public struct TermProbability_Offset
    {
        public int    StartIndex;
        public int    Length;
        public double Probability;
        public string GetTerm( string text ) => text.Substring( StartIndex, Length );
#if DEBUG
        public string ToString( string text ) => $"'{GetTerm( text )}': {Probability}";
#endif
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ITextSegmenter
    {
        IReadOnlyList< TermProbability > Run( string text );
        IReadOnlyList< TermProbability_Offset > Run_Offset( string text );
    }
}

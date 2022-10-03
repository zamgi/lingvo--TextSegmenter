using System;
using System.Collections;
using System.Collections.Generic;

using M = System.Runtime.CompilerServices.MethodImplAttribute;
using O = System.Runtime.CompilerServices.MethodImplOptions;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
#if DEBUG
    public
#else
    internal
#endif
        sealed class SetOfIntPtr : IEnumerable< IntPtr >
    {
        /// <summary>
        /// 
        /// </summary>
        internal struct Slot
        {
            internal int    HashCode;
            internal int    Next;
            internal IntPtr Value;
        }

        /// <summary>
        /// 
        /// </summary>
        //unsafe internal sealed class IntPtrEqualityComparer : IEqualityComparer< IntPtr >
        unsafe private static class IntPtrEqualityComparer
        {
            [M(O.AggressiveInlining)] public static bool Equals( IntPtr x, IntPtr y )
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
            [M(O.AggressiveInlining)] public static int GetHashCode( IntPtr obj )
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

            [M(O.AggressiveInlining)] public static bool Equals( char* x, char* y, char* y_end )
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
            [M(O.AggressiveInlining)] public static int GetHashCode( in NativeOffset no )
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

        private const int DEFAULT_CAPACITY = 7;

        private int[]  _Buckets;
        private Slot[] _Slots;
        private int    _Count;
        private int    _FreeList;
        //private IntPtrEqualityComparer _Comparer;

        public int Count { [M(O.AggressiveInlining)] get => _Count; }

        public SetOfIntPtr() : this( DEFAULT_CAPACITY ) { }
        public SetOfIntPtr( int capacity ) 
        {
            var capacityPrime = PrimeHelper.GetPrime_v2( capacity );

            _Buckets  = new int [ capacityPrime ];
            _Slots    = new Slot[ capacityPrime ];
            _FreeList = -1;
        }

        [M(O.AggressiveInlining)] public bool Add( IntPtr value ) => (!Find( value, true ));
        [M(O.AggressiveInlining)] public bool Contains( IntPtr value ) => Find( value, false );

        public bool Remove( IntPtr value )
        {
            int hash   = InternalGetHashCode( value );
            int bucket = hash % _Buckets.Length;
            int last   = -1;
            for ( int i = _Buckets[ bucket ] - 1; 0 <= i; )
            {
                ref readonly var slot = ref _Slots[ i ];
                if ( (slot.HashCode == hash) && IntPtrEqualityComparer.Equals( slot.Value, value ) )
                {
                    if ( last < 0 )
                    {
                        _Buckets[ bucket ] = slot.Next + 1; 
                    }
                    else
                    {
                        _Slots[ last ].Next = slot.Next; 
                    }
                    _Slots[ i ] = new Slot()
                    {
                        HashCode = -1,
                        Value    = IntPtr.Zero,
                        Next     = _FreeList,
                    };
                    _FreeList = i;
                    _Count--;
                    return (true);
                }
                last = i;
                i = slot.Next;
            }
            return (false);
        }

        [M(O.AggressiveInlining)]
        public bool TryGetValue( IntPtr value, out IntPtr existsValue )
        {
            int hash = InternalGetHashCode( value );
            for ( int i = _Buckets[ hash % _Buckets.Length ] - 1; 0 <= i; )
            {
                ref readonly var slot = ref _Slots[ i ];
                if ( (slot.HashCode == hash) && IntPtrEqualityComparer.Equals( slot.Value, value ) )
                {
                    existsValue = slot.Value;
                    return (true);
                }
                i = slot.Next;
            }
            existsValue = IntPtr.Zero;
            return (false);
        }

        [M(O.AggressiveInlining)]
        unsafe public bool TryGetValue( in NativeOffset no, out IntPtr existsValue )
        {
            int hash = (IntPtrEqualityComparer.GetHashCode( in no ) & 0x7FFFFFFF);
            var start_ptr = no.BasePtr + no.StartIndex;
            var end_ptr   = start_ptr + no.Length;
            for ( int i = _Buckets[ hash % _Buckets.Length ] - 1; 0 <= i; )
            {
                ref readonly var slot = ref _Slots[ i ];
                if ( (slot.HashCode == hash) && IntPtrEqualityComparer.Equals( (char*) slot.Value, start_ptr, end_ptr ) )
                {
                    existsValue = slot.Value;
                    return (true);
                }
                i = slot.Next;
            }
            existsValue = IntPtr.Zero;
            return (false);
        }

        [M(O.AggressiveInlining)]
        private bool Find( IntPtr value, bool add )
        {
            int hash = InternalGetHashCode( value );
            for ( int i = _Buckets[ hash % _Buckets.Length ] - 1; 0 <= i; )
            {
                ref readonly var slot = ref _Slots[ i ];
                if ( (slot.HashCode == hash) && IntPtrEqualityComparer.Equals( slot.Value, value ) )
                {
                    return (true);
                }
                i = slot.Next;
            }

            if ( add )
            {
                int index;
                if ( 0 <= _FreeList )
                {
                    index = _FreeList;
                    ref readonly var slot = ref _Slots[ index ];
                    _FreeList = slot.Next;
                }
                else
                {
                    if ( _Count == _Slots.Length )
                    {
                        Resize();
                    }
                    index = _Count;
                    _Count++;
                }
                int bucket = hash % _Buckets.Length;
                _Slots[ index ] = new Slot() 
                {
                    HashCode = hash,
                    Value    = value,
                    Next     = _Buckets[ bucket ] - 1,
                };
                _Buckets[ bucket ] = index + 1;
            }

            return (false);
        }

        [M(O.AggressiveInlining)]
        private void Resize()
        {
            var newSize    = PrimeHelper.ExpandPrime4Size( _Count );
            var newBuckets = new int[ newSize ];
            var newSlots   = new Slot[ newSize ];
            Array.Copy( _Slots, 0, newSlots, 0, _Count );
            for ( int i = 0; i < _Count; i++ )
            {
                ref var slot = ref newSlots[ i ];
                int bucket = slot.HashCode % newSize;
                slot.Next = newBuckets[ bucket ] - 1;
                newBuckets[ bucket ] = i + 1;
            }
            _Buckets = newBuckets;
            _Slots   = newSlots;
        }

        [M(O.AggressiveInlining)] private static int InternalGetHashCode( IntPtr value ) => (IntPtrEqualityComparer.GetHashCode( value ) & 0x7FFFFFFF);
        //[M(O.AggressiveInlining)] private int InternalGetHashCode( IntPtr value ) => (_Comparer.GetHashCode( value ) & 0x7FFFFFFF);

        public IEnumerator< IntPtr > GetEnumerator() => new Enumerator( this );
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator( this );

        /// <summary>
        /// 
        /// </summary>
        public struct Enumerator : IEnumerator< IntPtr >
        {
            private SetOfIntPtr _Set;
            private int       _Index;
            private IntPtr    _Current;

            internal Enumerator( SetOfIntPtr set )
            {
                _Set     = set;
                _Index   = 0;
                _Current = IntPtr.Zero;
            }
            public void Dispose() { }

            public bool MoveNext()
            {
                // Use unsigned comparison since we set index to set.count+1 when the enumeration ends.
                // set.count+1 could be negative if dictionary.count is Int32.MaxValue
                while ( (uint) _Index < (uint) _Set._Count )
                {
                    ref readonly var slot = ref _Set._Slots[ _Index ];
                    if ( 0 <= slot.HashCode )
                    {
                        _Current = slot.Value;
                        _Index++;
                        return (true);
                    }
                    _Index++;
                }

                _Index = _Set._Count + 1;
                _Current = IntPtr.Zero;
                return (false);
            }
            public IntPtr Current => _Current;

            object IEnumerator.Current
            {
                get
                {
#if DEBUG
                    if ( _Index == 0 || (_Index == _Set._Count + 1) )
                    {
                        throw (new InvalidOperationException());
                    } 
#endif
                    return (_Current);
                }
            }
            void IEnumerator.Reset()
            {
                _Index   = 0;
                _Current = IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class PrimeHelper
    {
        private static readonly int[] _Primes = new[]
        {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
        };

        [M(O.AggressiveInlining)] public static int ExpandPrime4Size( int oldSize )
        {
            int newSize = oldSize << 1;
            if ( ((uint) newSize > (uint) 0x7feffffd) && ((int) 0x7feffffd > oldSize) )
            {
                return ((int) 0x7feffffd);
            }
            return (GetPrime( newSize ));
        }

        [M(O.AggressiveInlining)] public static int GetPrime( int min )
        {
            if ( min < 0 ) throw (new ArgumentException( null, nameof(min) ));

            for ( int i = 0; i < _Primes.Length; i++ )
            {
                int p = _Primes[ i ];
                if ( min <= p )
                {
                    return (p);
                }
            }
            for ( int j = min | 1; j < int.MaxValue; j += 2 )
            {
                if ( IsPrime( j ) && ((j - 1) % 101 != 0) )
                {
                    return (j);
                }
            }
            return (min);
        }
        [M(O.AggressiveInlining)] public static int GetPrime_v2( int min )
        {
            if ( min < 0 ) throw (new ArgumentException( null, nameof(min) ));

            /*for ( int i = 0; i < _Primes.Length; i++ )
            {
                int p = _Primes[ i ];
                if ( min <= p )
                {
                    return (p);
                }
            }*/
            for ( int j = min | 1; j < int.MaxValue; j += 2 )
            {
                if ( IsPrime( j ) && ((j - 1) % 101 != 0) )
                {
                    return (j);
                }
            }
            return (min);
        }

        [M(O.AggressiveInlining)] public static bool IsPrime( int candidate )
        {
            if ( (candidate & 1) != 0 )
            {
                int n = (int) Math.Sqrt( (double) candidate );
                for ( int i = 3; i <= n; i += 2 )
                {
                    if ( (candidate % i) == 0 )
                    {
                        return (false);
                    }
                }
                return (true);
            }
            return (candidate == 2);
        }
    }
}

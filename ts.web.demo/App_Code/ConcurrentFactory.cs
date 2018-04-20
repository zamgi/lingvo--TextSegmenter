using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

using _UInitParam_    = lingvo.ts.UnionTextSegmenter.InitParam_v2;
using _ULanguage_     = lingvo.ts.UnionTextSegmenter.LanguageEnum;
using _UResultOffset_ = lingvo.ts.UnionTextSegmenter.Result_Offset;

namespace lingvo.ts
{
    /// <summary>
    /// 
    /// </summary>
	internal sealed class ConcurrentFactory
    {
		private readonly int                          _InstanceCount;		
		private Semaphore                             _Semaphore;
        private ConcurrentStack< UnionTextSegmenter > _Stack;

        public ConcurrentFactory( int instanceCount, params _UInitParam_[] ps )
		{
            if ( ps == null || !ps.Any() ) throw (new ArgumentNullException( "ps" ));
            if ( instanceCount <= 0 )      throw (new ArgumentException( "instanceCount" ));

            _InstanceCount = instanceCount;
            _Semaphore     = new Semaphore( instanceCount, instanceCount );
            _Stack         = new ConcurrentStack< UnionTextSegmenter >();
			for ( int i = 0; i < instanceCount; i++ )
			{
                _Stack.Push( new UnionTextSegmenter( ps ) );
			}
		}

        public _UResultOffset_ RunBest_Offset( string text )
		{
            _Semaphore.WaitOne();
			var worker = default(UnionTextSegmenter);
			var result = default(_UResultOffset_);
			try
			{
                worker = _Stack.Pop();
                result = worker.RunBest_Offset( text );
			}
			finally
			{
                if ( !worker.Equals( default(UnionTextSegmenter) ) )
				{
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}

        public _UResultOffset_ Run_Offset( string text, _ULanguage_ lang )
		{
            _Semaphore.WaitOne();
			var worker = default(UnionTextSegmenter);
			var result = default(_UResultOffset_);
			try
			{
                worker  = _Stack.Pop();
                var tps = worker.Run_Offset( text, lang );
                result  = _UResultOffset_.Create( tps, lang );
            }
			finally
			{
                if ( !worker.Equals( default(UnionTextSegmenter) ) )
				{
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}
    }

    /// <summary>
    /// 
    /// </summary>
    internal static class ConcurrentFactoryExtensions
    {
        public static T Pop< T >( this ConcurrentStack< T > stack )
        {
            T t;
            if ( stack.TryPop( out t ) )
            {
                return (t);
            }
            return (default(T));
        }
    }
}

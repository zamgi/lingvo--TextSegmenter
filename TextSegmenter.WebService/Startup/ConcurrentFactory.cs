using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using _UInitParam_    = lingvo.ts.UnionTextSegmenter.InitParam_v2;
using _ULanguage_     = lingvo.ts.UnionTextSegmenter.LanguageEnum;
using _UResultOffset_ = lingvo.ts.UnionTextSegmenter.Result_Offset;

namespace lingvo.ts.webService
{
    /// <summary>
    /// 
    /// </summary>
	public sealed class ConcurrentFactory : IDisposable
	{
		private readonly SemaphoreSlim                         _Semaphore;
        private readonly ConcurrentStack< UnionTextSegmenter > _Stack;

        public ConcurrentFactory( int instanceCount, params _UInitParam_[] ps )
        {
            if ( !ps.AnyEx() )        throw (new ArgumentNullException( nameof(ps) ));
            if ( instanceCount <= 0 ) throw (new ArgumentException( nameof(instanceCount) ));

            _Semaphore = new SemaphoreSlim( instanceCount, instanceCount );
            _Stack = new ConcurrentStack< UnionTextSegmenter >();
            for ( int i = 0; i < instanceCount; i++ )
            {
                _Stack.Push( new UnionTextSegmenter( ps ) );
            }
        }
        public void Dispose()
        {
            foreach ( var worker in _Stack )
			{
                worker.Dispose();
			}
			_Semaphore.Dispose();
        }

        public Task< _UResultOffset_ > Run( string text, _ULanguage_? lang )
        {
            if ( lang.HasValue )
            {
                return (Run_Offset( text, lang.Value ));
            }
            return (RunBest_Offset( text ));
        }

        public async Task< _UResultOffset_ > RunBest_Offset( string text )
		{
            await _Semaphore.WaitAsync().CAX();
            var worker = default(UnionTextSegmenter);
			var result = default(_UResultOffset_);
			try
			{
                worker = Pop( _Stack );
                result = worker.RunBest_Offset( text );
			}
			finally
			{
                if ( worker != null )
				{
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}
        public async Task< _UResultOffset_ > Run_Offset( string text, _ULanguage_ lang )
		{
            await _Semaphore.WaitAsync().CAX();
            var worker = default(UnionTextSegmenter);
			var result = default(_UResultOffset_);
			try
			{
                worker  = Pop( _Stack );
                var tps = worker.Run_Offset( text, lang );
                result  = _UResultOffset_.Create( tps, lang );
            }
			finally
			{
                if ( worker != null )
                {
                    _Stack.Push( worker );
				}
				_Semaphore.Release();
			}
			return (result);
		}

        private static T Pop< T >( ConcurrentStack< T > stack ) => stack.TryPop( out var t ) ? t : default;
	}
}

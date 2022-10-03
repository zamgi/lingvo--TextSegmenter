using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace lingvo.ts.webService.Controllers
{
    public sealed class ProcessController : Controller
    {
        #region [.ctor().]
        private readonly ConcurrentFactory _ConcurrentFactory;
#if DEBUG
        private readonly ILogger< ProcessController > _Logger;
#endif
#if DEBUG
        public ProcessController( ConcurrentFactory concurrentFactory, ILogger< ProcessController > logger )
        {
            _ConcurrentFactory = concurrentFactory;
            _Logger            = logger;
        }
#else
        public ProcessController( ConcurrentFactory concurrentFactory ) => _ConcurrentFactory = concurrentFactory;
#endif
        #endregion

        [HttpPost] public async Task< IActionResult > Run( [FromBody] InitParamsVM m )
        {
            try
            {
#if DEBUG
                _Logger.LogInformation( $"start process: '{m.Text}'..." );
#endif
                if ( m.Text.IsNullOrWhiteSpace() ) throw (new ArgumentNullException( nameof(m.Text) ));

                var rows = from textRow in m.Text.Split( new[] { '\r', '\n', ' ' }, StringSplitOptions.RemoveEmptyEntries )
                           where (!textRow.IsNullOrWhiteSpace())
                           select textRow;

                var termProbsByRows = new List< ResultVM.tuple_TermProbability_Offset >();
                var lang = m.GetLang();
                await rows.ForEachAsync( async (textRow, _) =>
                {
                    var res = await _ConcurrentFactory.Run( textRow, lang ).CAX();
                    var t = ResultVM.tuple_TermProbability_Offset.Create( textRow, res );

                    lock ( termProbsByRows )
                    {
                        termProbsByRows.Add( t );
                    }
                });

                var result = new ResultVM( m, termProbsByRows );
#if DEBUG
                _Logger.LogInformation( $"end process: '{m.Text}'." );
#endif
                return Ok( result );
            }
            catch ( Exception ex )
            {
#if DEBUG
                _Logger.LogError( $"Error while process: '{m.Text}' => {ex}" );
#endif
                return Ok( new ResultVM( m, ex ) );
            }
        }
    }
}

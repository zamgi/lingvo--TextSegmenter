using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

using _UInitParam_ = lingvo.ts.UnionTextSegmenter.InitParam_v2;
using _ULanguage_  = lingvo.ts.UnionTextSegmenter.LanguageEnum;

namespace lingvo.ts.webService
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Program
    {
        public const string SERVICE_NAME = "TextSegmenter.WebService";

        private static _UInitParam_[] LoadModels( IConfig cfg )
        {
            var ps = new List< _UInitParam_ >( Enum.GetValues( typeof(_ULanguage_) ).Length );
            try
            {
                Console.Write( "load models..." );
                var sw = Stopwatch.StartNew();

                var funcs = new Func< _UInitParam_ >[]
                {
                    () => _UInitParam_.Create( new NativeTextMMFModelBinary( cfg.GetBinaryModelConfig_RU() ), _ULanguage_.RU ),
                    () => _UInitParam_.Create( new NativeTextMMFModelBinary( cfg.GetBinaryModelConfig_EN() ), _ULanguage_.EN ),
                    () => _UInitParam_.Create( new NativeTextMMFModelBinary( cfg.GetBinaryModelConfig_DE() ), _ULanguage_.DE )
                };
                Parallel.ForEach( funcs, func =>
                {
                    var res = func();
                    lock ( ps )
                    {
                        ps.Add( res );
                    }
                });

                Console.WriteLine( $"elapsed: {sw.StopElapsed()}\r\n" );
                return (ps.ToArray());
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( ex );
                ps.ForEach( p => p.Model.Dispose() );
                throw;
            }
        }
        private static async Task Main( string[] args )
        {
            var hostApplicationLifetime = default(IHostApplicationLifetime);
            var logger                  = default(ILogger);
            try
            {
                //---------------------------------------------------------------//
                var opts = new Config();
                var ps = LoadModels( opts );
                using var concurrentFactory = new ConcurrentFactory( opts.CONCURRENT_FACTORY_INSTANCE_COUNT, ps );
                //---------------------------------------------------------------//

                var host = Host.CreateDefaultBuilder( args )
                               .ConfigureLogging( loggingBuilder => loggingBuilder.ClearProviders().AddDebug().AddConsole().AddEventSourceLogger()
                                                              .AddEventLog( new EventLogSettings() { LogName = SERVICE_NAME, SourceName = SERVICE_NAME } ) )
                               //---.UseWindowsService()
                               .ConfigureServices( (hostContext, services) => services.AddSingleton( concurrentFactory ) )
                               .ConfigureWebHostDefaults( webBuilder => webBuilder.UseStartup< Startup >() )
                               .Build();
                hostApplicationLifetime = host.Services.GetService< IHostApplicationLifetime >();
                logger                  = host.Services.GetService< ILoggerFactory >()?.CreateLogger( SERVICE_NAME );
                await host.RunAsync();
            }
            catch ( OperationCanceledException ex ) when ((hostApplicationLifetime?.ApplicationStopping.IsCancellationRequested).GetValueOrDefault())
            {
                Debug.WriteLine( ex ); //suppress
            }
            catch ( Exception ex ) when (logger != null)
            {
                logger.LogCritical( ex, "Global exception handler" );
            }
        }

        private static TimeSpan StopElapsed( this Stopwatch sw )
        {
            sw.Stop();
            return (sw.Elapsed);
        }
    }
}

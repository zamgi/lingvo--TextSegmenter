using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace lingvo.ts.webService
{
    internal static class Extensions
    {
        public static bool IsNullOrWhiteSpace( this string s ) => string.IsNullOrWhiteSpace( s );
        public static bool AnyEx< T >( this ICollection< T > seq ) => (seq != null) && (seq.Count != 0);

        public static ConfiguredTaskAwaitable CAX( this Task task ) => task.ConfigureAwait( false );
        public static ConfiguredTaskAwaitable< T > CAX< T >( this Task< T > task ) => task.ConfigureAwait( false );
    }
}

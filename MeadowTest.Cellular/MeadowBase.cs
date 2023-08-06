using Meadow;
using Meadow.Logging;

namespace MeadowTest.Cellular
{
    internal abstract class MeadowBase
    {
        protected TestAppSettings AppSettings { get; } = (TestAppSettings)Resolver.Services.Get(typeof(TestAppSettings));
        protected Logger Log { get; } = Resolver.Log;

        public MeadowBase() { }
    }
}

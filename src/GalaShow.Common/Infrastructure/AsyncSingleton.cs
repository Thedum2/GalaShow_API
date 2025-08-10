namespace GalaShow.Common.Infrastructure
{
    public abstract class AsyncSingleton<T> : IDisposable where T : class
    {
        private static readonly Lazy<T> _lazy =
            new(() => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!);

        public static T Instance => _lazy.Value;

        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public async Task InitializeAsync()
        {
            if (_initialized) return;
            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;
                await InitializeCoreAsync();
                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        protected abstract Task InitializeCoreAsync();

        public virtual void Dispose() => _initLock.Dispose();
    }
}
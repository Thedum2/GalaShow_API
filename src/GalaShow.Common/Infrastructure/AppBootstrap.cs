using GalaShow.Common.Service;

namespace GalaShow.Common.Infrastructure
{
    public static class AppBootstrap
    {
        private static readonly Lazy<Task> LazyInit = new(InitializeCoreAsync);

        public static Task InitAsync() => LazyInit.Value;

        private static async Task InitializeCoreAsync()
        {
            await SecretsService.Instance.InitializeAsync();
            await DatabaseService.Instance.InitializeAsync();
            await JwtService.Instance.InitializeAsync();
            await TokenService.Instance.InitializeAsync();
            //====================================================
            await BackgroundService.Instance.InitializeAsync();
            await BannerService.Instance.InitializeAsync();
            await PolicyService.Instance.InitializeAsync();
            await SnsService.Instance.InitializeAsync();
        }
    }
}
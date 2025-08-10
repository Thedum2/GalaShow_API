using GalaShow.Common.Service;

namespace GalaShow.Common.Infrastructure
{
    public static class AppBootstrap
    {
        private static readonly Lazy<Task> LazyInit = new(InitializeCoreAsync);

        public static Task InitAsync() => LazyInit.Value;

        private static async Task InitializeCoreAsync()
        {
            //모든 싱글톤 여기서 초기화ㅋ
            await SecretsService.Instance.InitializeAsync();
            await JwtService.Instance.InitializeAsync();
            await TokenService.Instance.InitializeAsync();
            await DatabaseService.Instance.InitializeAsync();
            await BannerService.Instance.InitializeAsync();

            StageResolver.Resolve();
        }
    }
}
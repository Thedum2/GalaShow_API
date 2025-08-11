using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Response.Background;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class BackgroundService : AsyncSingleton<BackgroundService>
    {
        private readonly BackGroundRepository _repo = new();

        private BackgroundService() { }

        protected override Task InitializeCoreAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<List<BackgroundResponse>> GetAllBackgroundAsync()
        {
            var background = await _repo.GetAllAsync();
            return background.Select(b => new BackgroundResponse
            {
                Id = b.Id,
                Title = b.Title,
                Type = b.Type,
                Url = b.Url
            }).ToList();
        }

        public Task<int> UpdateBannerAsync(int id, string title, string type, string fileUrl)
            => _repo.UpdateAsync(id, title, type, fileUrl);
    }
}
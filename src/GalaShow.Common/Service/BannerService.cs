using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Response.Banner;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class BannerService : AsyncSingleton<BannerService>
    {
        private readonly BannerRepository _repo = new();

        private BannerService() { }

        protected override Task InitializeCoreAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<List<BannerResponse>> GetAllBannersAsync()
        {
            var banners = await _repo.GetAllAsync();
            return banners.Select(b => new BannerResponse
            {
                Id     = b.Id,
                Message= b.Message,
                Order  = b.Order
            }).ToList();
        }

        public Task<int> UpdateBannerAsync(int id, string message)
            => _repo.UpdateAsync(id, message);
    }
}
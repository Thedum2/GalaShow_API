using GalaShow.Common.Models.Response;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service;

public class BannerService
{
    private readonly BannerRepository _bannerRepository;

    public BannerService(BannerRepository bannerRepository)
    {
        _bannerRepository = bannerRepository;
    }

    public async Task<List<BannerResponse>> GetAllBannersAsync()
    {
        var banners = await _bannerRepository.GetAllAsync();

        return banners.Select(banner => new BannerResponse
        {
            Id = banner.Id,
            Message = banner.Message,
            Order = banner.Order
        }).ToList();
    }

    public async Task<int> UpdateBannerAsync(int id, string message)
    {
        return await _bannerRepository.UpdateAsync(id, message);
    }
}
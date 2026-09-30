using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Request.Minigame;
using GalaShow.Common.Models.Response.Minigame;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class ViewerAvatarService : AsyncSingleton<ViewerAvatarService>
    {
        /// <summary>시청자 아바타는 최대 8개까지 등록한다.</summary>
        public const int MaxAvatars = 8;

        public static bool ExceedsLimit(IReadOnlyCollection<ViewerAvatarDto> avatars) => avatars.Count > MaxAvatars;

        private readonly ViewerAvatarRepository _repo = new();

        private ViewerAvatarService() { }

        protected override Task InitializeCoreAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<List<ViewerAvatarResponse>> GetAllAvatarsAsync()
        {
            var avatars = await _repo.GetAllAsync();
            return avatars.Select(a => new ViewerAvatarResponse
            {
                Id = a.Id,
                Order = a.Order,
                Name = a.Name,
                GifUrl = a.GifUrl
            }).ToList();
        }

        public Task<bool> HasDuplicateOrdersAsync(List<ViewerAvatarDto> avatars)
        {
            return _repo.HasDuplicateOrdersAsync(avatars);
        }

        public Task UpdateAllAvatarsAsync(List<ViewerAvatarDto> avatars)
        {
            return _repo.UpdateAllAsync(avatars);
        }
    }
}

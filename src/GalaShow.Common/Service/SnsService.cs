using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Request.Sns;
using GalaShow.Common.Models.Response.Sns;
using GalaShow.Common.Repositories;
using GalaShow.Common.Data.Entities;

namespace GalaShow.Common.Service
{
    public sealed class SnsService : AsyncSingleton<SnsService>
    {
        private SnsService() { }
        protected override Task InitializeCoreAsync() => Task.CompletedTask;

        public async Task<List<SnsLinkResponse>> GetAllAsync()
        {
            var repo = new SnsLinkRepository(DatabaseService.Instance);
            var rows = await repo.GetAllAsync();
            return rows.Select(x => new SnsLinkResponse
            {
                Title = x.Title,
                Url   = x.Url,
                IconUrl  = x.IconUrl,
                Order = x.Order
            }).ToList();
        }

        public async Task<int> ReplaceAllAsync(UpdateSnsLinksRequest req)
        {
            if (req?.Data is null || req.Data.Count == 0)
                throw new ArgumentException("data is required");

            var normalized = req.Data
                .Select(d => new SnsLink
                {
                    Title = (d.Title ?? "").Trim(),
                    Url   = (d.Url   ?? "").Trim(),
                    IconUrl  = (d.IconUrl  ?? "").Trim(),
                    Order = d.Order
                })
                .OrderBy(d => d.Order)
                .ToList();

            if (normalized.Any(n => string.IsNullOrWhiteSpace(n.Title) || string.IsNullOrWhiteSpace(n.Url)))
                throw new ArgumentException("title and url are required for all items");

            var repo = new SnsLinkRepository(DatabaseService.Instance);
            return await repo.ReplaceAllAsync(normalized);
        }
    }
}
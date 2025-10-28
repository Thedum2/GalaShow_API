using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Response.Policy;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class PolicyService : AsyncSingleton<PolicyService>
    {
        private readonly PolicyRepository _repo = new();

        private PolicyService() { }

        protected override Task InitializeCoreAsync() => Task.CompletedTask;

        public async Task<PolicyResponse> GetAsync()
        {
            var row = await _repo.GetLatestAsync();
            return new PolicyResponse
            {
                TermsOfService = row?.TermsOfServiceUrl ?? string.Empty,
                PrivacyPolicy  = row?.PrivacyPolicyUrl  ?? string.Empty
            };
        }

        public Task<int> UpdateAsync(string tosUrl, string ppUrl)
            => _repo.UpsertSingletonAsync(tosUrl, ppUrl);
    }
}
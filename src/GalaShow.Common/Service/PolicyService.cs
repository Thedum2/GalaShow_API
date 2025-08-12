using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Response.Policy;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class PolicyService : AsyncSingleton<PolicyService>
    {
        private PolicyService() { }

        protected override Task InitializeCoreAsync() => Task.CompletedTask;

        public async Task<PolicyResponse> GetAsync()
        {
            var repo = new PolicyRepository(DatabaseService.Instance);
            var row = await repo.GetLatestAsync();
            return new PolicyResponse
            {
                TermsOfService = row?.TermsOfServiceUrl ?? string.Empty,
                PrivacyPolicy  = row?.PrivacyPolicyUrl  ?? string.Empty
            };
        }

        public async Task<int> UpdateAsync(string tosUrl, string ppUrl)
        {
            var repo = new PolicyRepository(DatabaseService.Instance);
            return await repo.UpsertSingletonAsync(tosUrl, ppUrl);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Response.Question;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class QuestionCategoryService : AsyncSingleton<QuestionCategoryService>
    {
        private readonly QuestionCategoryRepository _repo = new();

        private QuestionCategoryService() { }

        protected override Task InitializeCoreAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<List<QuestionCategoryResponse>> GetCategoriesAsync()
        {
            var categories = await _repo.GetAllAsync();
            return categories.Select(c => new QuestionCategoryResponse
            {
                Id = c.Id,
                Name = c.Name
            }).ToList();
        }

        public async Task<QuestionCategoryResponse?> CreateCategoryAsync(string name)
        {
            var newCategoryId = await _repo.CreateAsync(name);
            if (newCategoryId == 0) return null;

            var newCategory = await _repo.GetByIdAsync(newCategoryId);
            if (newCategory == null) return null;

            return new QuestionCategoryResponse
            {
                Id = newCategory.Id,
                Name = newCategory.Name
            };
        }

        public Task<int> UpdateCategoryAsync(int id, string name)
        {
            return _repo.UpdateAsync(id, name);
        }

        public Task<int> DeleteCategoryAsync(int id)
        {
            return _repo.DeleteAsync(id);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaShow.Common.Data.Entities;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models.Request.Question;
using GalaShow.Common.Models.Response.Question;
using GalaShow.Common.Repositories;

namespace GalaShow.Common.Service
{
    public sealed class QuestionService : AsyncSingleton<QuestionService>
    {
        private readonly QuestionRepository _repo = new();

        private QuestionService() { }

        protected override Task InitializeCoreAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<List<QuestionResponse>> GetQuestionsByCategoryAsync(int categoryId, int limit, bool shuffle)
        {
            var questions = await _repo.GetByCategoryAsync(categoryId, limit, shuffle);
            var response = new List<QuestionResponse>();
            foreach (var q in questions)
            {
                var choices = await _repo.GetChoicesByQuestionIdAsync(q.Id);
                response.Add(new QuestionResponse
                {
                    Id = q.Id,
                    CategoryId = q.CategoryId,
                    Title = q.Title,
                    Choices = choices.Select(c => new ChoiceResponse { Text = c.Text, ImageUrl = c.ImageUrl }).ToList()
                });
            }
            return response;
        }

        public async Task<QuestionResponse?> GetQuestionAsync(int id)
        {
            var q = await _repo.GetByIdAsync(id);
            if (q == null) return null;

            var choices = await _repo.GetChoicesByQuestionIdAsync(q.Id);
            return new QuestionResponse
            {
                Id = q.Id,
                CategoryId = q.CategoryId,
                Title = q.Title,
                Choices = choices.Select(c => new ChoiceResponse { Text = c.Text, ImageUrl = c.ImageUrl }).ToList()
            };
        }

        public async Task<List<SimpleQuestionResponse>> GetRandomQuestionsAsync(int count)
        {
            var questions = await _repo.GetRandomAsync(count);
            return questions.Select(q => new SimpleQuestionResponse
            {
                Id = q.Id,
                Title = q.Title
            }).ToList();
        }

        public async Task<QuestionResponse?> CreateQuestionAsync(CreateQuestionRequest req)
        {
            var question = new Question { CategoryId = req.CategoryId, Title = req.Title };
            var choices = req.Choices.Select(c => new Choice { Text = c.Text, ImageUrl = c.ImageUrl }).ToList();
            
            var newQuestionId = await _repo.CreateQuestionAsync(question, choices);
            if (newQuestionId == 0) return null;

            return await GetQuestionAsync(newQuestionId);
        }

        public async Task<QuestionResponse?> UpdateQuestionAsync(int questionId, UpdateQuestionRequest req)
        {
            var question = new Question { Id = questionId, CategoryId = req.CategoryId, Title = req.Title };
            var choices = req.Choices.Select(c => new Choice { Text = c.Text, ImageUrl = c.ImageUrl }).ToList();

            var affectedRows = await _repo.UpdateQuestionAsync(question, choices);
            if (affectedRows == 0) return null; // Or handle as not found

            return await GetQuestionAsync(questionId);
        }

        public Task<int> DeleteQuestionAsync(int id)
        {
            return _repo.DeleteAsync(id);
        }
    }
}
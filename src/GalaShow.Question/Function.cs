#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GalaShow.Common.Errors;
using GalaShow.Common.Infrastructure;
using GalaShow.Common.Models;
using GalaShow.Common.Models.Request.Question;
using GalaShow.Common.Service;
using Microsoft.IdentityModel.Tokens;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GalaShow.Question
{
    public class Function
    {
        public async Task<APIGatewayProxyResponse?> FunctionHandler(APIGatewayProxyRequest req, ILambdaContext context)
        {
            StageResolver.Resolve(req);
            await AppBootstrap.InitAsync();

            try
            {
                return (req.HttpMethod, req.Path) switch
                {
                    // Question routes
                    ("GET", "/questions/random") => await TokenService.Instance.RequireAuthThen(req, _ => GetRandomQuestions(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("GET", var p) when p.StartsWith("/questions/") => await TokenService.Instance.RequireAuthThen(req, _ => GetQuestion(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("POST", "/questions") => await TokenService.Instance.RequireAuthThen(req, _ => CreateQuestion(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("PUT", var p) when p.StartsWith("/questions/") => await TokenService.Instance.RequireAuthThen(req, _ => UpdateQuestion(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("DELETE", var p) when p.StartsWith("/questions/") => await TokenService.Instance.RequireAuthThen(req, _ => DeleteQuestion(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),

                    // Question Category routes
                    ("GET", "/question-categories") => await TokenService.Instance.RequireAuthThen(req, _ => GetCategories(), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("POST", "/question-categories") => await TokenService.Instance.RequireAuthThen(req, _ => CreateCategory(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("PUT", var p) when p.StartsWith("/question-categories/") => await TokenService.Instance.RequireAuthThen(req, _ => UpdateCategory(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("DELETE", var p) when p.StartsWith("/question-categories/") => await TokenService.Instance.RequireAuthThen(req, _ => DeleteCategory(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),
                    ("GET", var p) when p.StartsWith("/question-categories/") && p.Length > "/question-categories".Length + 1 => await TokenService.Instance.RequireAuthThen(req, _ => GetQuestionsByCategory(req), () => ErrorResults.Json(ErrorCode.AuthTokenExpired), () => ErrorResults.Json(ErrorCode.Unauthorized)),

                    _ => ErrorResults.Json(ErrorCode.PathNotFound)
                };
            }
            catch (SecurityTokenException ste)
            {
                return ErrorResults.Json(ErrorCode.Unauthorized);
            }
            catch (Exception ex)
            {
                return ErrorResults.Json(ErrorCode.Internal);
            }
        }

        // Question methods (from GalaShow.Question/Function.cs)
        private async Task<APIGatewayProxyResponse?> GetRandomQuestions(APIGatewayProxyRequest request)
        {
            var count = request.QueryStringParameters?.TryGetValue("count", out var countStr) == true && int.TryParse(countStr, out var c) ? c : 1;
            var questions = await QuestionService.Instance.GetRandomQuestionsAsync(count);
            return Success(200, questions);
        }

        private async Task<APIGatewayProxyResponse?> GetQuestion(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("questionId", out var idStr) || !int.TryParse(idStr, out var questionId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid question ID");
            }

            var question = await QuestionService.Instance.GetQuestionAsync(questionId);
            if (question == null)
            {
                return ErrorResults.Json(ErrorCode.QuestionNotFound);
            }
            return Success(200, question);
        }

        private async Task<APIGatewayProxyResponse?> CreateQuestion(APIGatewayProxyRequest request)
        {
            var dto = JsonSerializer.Deserialize<CreateQuestionRequest>(request.Body);
            if (dto == null)
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var newQuestion = await QuestionService.Instance.CreateQuestionAsync(dto);
            if (newQuestion == null)
            {
                return ErrorResults.Json(ErrorCode.QuestionCreateFailed);
            }

            return Success(201, newQuestion);
        }

        private async Task<APIGatewayProxyResponse?> UpdateQuestion(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("questionId", out var idStr) || !int.TryParse(idStr, out var questionId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid question ID");
            }

            var dto = JsonSerializer.Deserialize<UpdateQuestionRequest>(request.Body);
            if (dto == null)
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var updatedQuestion = await QuestionService.Instance.UpdateQuestionAsync(questionId, dto);
            if (updatedQuestion == null)
            {
                return ErrorResults.Json(ErrorCode.QuestionUpdateFailed);
            }

            return Success(200, updatedQuestion);
        }

        private async Task<APIGatewayProxyResponse?> DeleteQuestion(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("questionId", out var idStr) || !int.TryParse(idStr, out var questionId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid question ID");
            }

            var deleted = await QuestionService.Instance.DeleteQuestionAsync(questionId);
            if (deleted == 0)
            {
                return ErrorResults.Json(ErrorCode.QuestionDeleteFailed);
            }

            return new APIGatewayProxyResponse { StatusCode = 204 };
        }

        // Question Category methods (from GalaShow.QuestionCategory/Function.cs)
        private async Task<APIGatewayProxyResponse?> GetCategories()
        {
            var categories = await QuestionCategoryService.Instance.GetCategoriesAsync();
            return Success(200, categories);
        }

        private async Task<APIGatewayProxyResponse?> CreateCategory(APIGatewayProxyRequest request)
        {
            var dto = JsonSerializer.Deserialize<CreateQuestionCategoryRequest>(request.Body);
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var newCategory = await QuestionCategoryService.Instance.CreateCategoryAsync(dto.Name);
            if (newCategory == null)
            {
                return ErrorResults.Json(ErrorCode.QuestionCategoryCreateFailed);
            }

            return Success(201, newCategory);
        }

        private async Task<APIGatewayProxyResponse?> UpdateCategory(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("categoryId", out var idStr) || !int.TryParse(idStr, out var categoryId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid category ID");
            }

            var dto = JsonSerializer.Deserialize<UpdateQuestionCategoryRequest>(request.Body);
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid request body");
            }

            var updated = await QuestionCategoryService.Instance.UpdateCategoryAsync(categoryId, dto.Name);
            if (updated == 0)
            {
                return ErrorResults.Json(ErrorCode.QuestionCategoryUpdateFailed);
            }

            return Success<object>(200, null);
        }

        private async Task<APIGatewayProxyResponse?> DeleteCategory(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("categoryId", out var idStr) || !int.TryParse(idStr, out var categoryId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid category ID");
            }

            var deleted = await QuestionCategoryService.Instance.DeleteCategoryAsync(categoryId);
            if (deleted == 0)
            {
                return ErrorResults.Json(ErrorCode.QuestionCategoryDeleteFailed);
            }

            return new APIGatewayProxyResponse { StatusCode = 204 };
        }

        private async Task<APIGatewayProxyResponse?> GetQuestionsByCategory(APIGatewayProxyRequest request)
        {
            if (request.PathParameters == null || !request.PathParameters.TryGetValue("categoryId", out var idStr) || !int.TryParse(idStr, out var categoryId))
            {
                return ErrorResults.Json(ErrorCode.BadRequest, "Invalid category ID");
            }

            var limit = request.QueryStringParameters?.TryGetValue("limit", out var limitStr) == true && int.TryParse(limitStr, out var l) ? l : 10;
            var shuffle = request.QueryStringParameters?.TryGetValue("shuffle", out var shuffleStr) == true && bool.TryParse(shuffleStr, out var s) && s;

            var questions = await QuestionService.Instance.GetQuestionsByCategoryAsync(categoryId, limit, shuffle);
            return Success(200, questions);
        }

        // Common helper methods (kept from GalaShow.Question/Function.cs)
        private static Dictionary<string, string> JsonHeaders() => new()
        {
            ["Content-Type"] = "application/json; charset=utf-8"
        };

        private static APIGatewayProxyResponse Success<T>(int statusCode, T? body) => new()
        {
            StatusCode = statusCode,
            Headers = JsonHeaders(),
            Body = body != null ? JsonSerializer.Serialize(ApiResponse<T>.Success(body, statusCode)) : JsonSerializer.Serialize(ApiResponse<object>.Success(statusCode))
        };
    }
}
using FindActivity.Application.Dtos;

namespace FindActivity.Application.Services;

public interface IReviewService
{
    Task<bool> CreateReviewAsync(ReviewCreateDto dto, string reviewerUserId, CancellationToken cancellationToken = default);
}

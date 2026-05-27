using AutoMapper;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class NewsBoardService : INewsBoardService
{
    private readonly IUnitOfWork             _uow;
    private readonly IMapper                 _mapper;
    private readonly ILogger<NewsBoardService> _logger;

    public NewsBoardService(IUnitOfWork uow, IMapper mapper, ILogger<NewsBoardService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── GetPublished ─────────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<NewsBoardDto>>> GetPublishedAsync(
        int schoolId, int? classId, CancellationToken ct = default)
    {
        var news = await _uow.NewsBoards.FindAsync(
            n => n.SchoolId == schoolId &&
                 n.IsPublished &&
                 (!classId.HasValue ||
                  n.TargetClassId == null ||
                  n.TargetClassId == classId.Value),
            include: q => q.Include(n => n.PublishedBy),
            ct: ct);

        var dtos = _mapper.Map<List<NewsBoardDto>>(
            news.OrderByDescending(n => n.PublishedAt));

        return Result<IReadOnlyList<NewsBoardDto>>.Success(dtos);
    }

    // ─── Create ───────────────────────────────────────────────────────────────

    public async Task<Result<NewsBoardDto>> CreateAsync(
        int schoolId, int publishedByUserId, CreateNewsRequest request, CancellationToken ct = default)
    {
        var post = new NewsBoard
        {
            SchoolId          = schoolId,
            Title             = request.Title.Trim(),
            ContentHtml       = request.ContentHtml.Trim(),
            CoverImageUrl     = request.CoverImageUrl?.Trim(),
            Scope             = request.Scope,
            TargetClassId     = request.TargetClassId,
            TargetGradeLevelId = request.TargetGradeLevelId,
            IsPinned          = request.IsPinned,
            IsPublished       = request.PublishNow,
            PublishedAt       = request.PublishNow ? DateTime.UtcNow : null,
            PublishedByUserId = publishedByUserId
        };

        await _uow.NewsBoards.AddAsync(post, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "NewsBoard created: Id={Id}, School={SchoolId}, By={UserId}",
            post.Id, schoolId, publishedByUserId);

        var dto = _mapper.Map<NewsBoardDto>(post);
        return Result<NewsBoardDto>.Success(dto);
    }

    // ─── Publish ──────────────────────────────────────────────────────────────

    public async Task<Result> PublishAsync(
        int newsId, int publishedByUserId, CancellationToken ct = default)
    {
        var post = await _uow.NewsBoards.GetByIdAsync(newsId, ct);
        if (post is null)
            return Result.Failure("NOT_FOUND", "Bài đăng không tồn tại.");

        if (post.IsPublished)
            return Result.Failure("ALREADY_PUBLISHED", "Bài đăng đã được công bố.");

        post.IsPublished       = true;
        post.PublishedAt       = DateTime.UtcNow;
        post.PublishedByUserId = publishedByUserId;

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("NewsBoard {Id} published by user {UserId}", newsId, publishedByUserId);
        return Result.Success();
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    public async Task<Result> DeleteAsync(
        int newsId, int requestedByUserId, CancellationToken ct = default)
    {
        var post = await _uow.NewsBoards.GetByIdAsync(newsId, ct);
        if (post is null)
            return Result.Failure("NOT_FOUND", "Bài đăng không tồn tại.");

        if (post.PublishedByUserId != requestedByUserId)
            return Result.Failure("FORBIDDEN", "Bạn không có quyền xóa bài đăng này.");

        _uow.NewsBoards.Remove(post);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("NewsBoard {Id} deleted by user {UserId}", newsId, requestedByUserId);
        return Result.Success();
    }
}

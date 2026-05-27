using AutoMapper;
using LuminaTutors.Application.DTOs.Communication;
using LuminaTutors.Application.Interfaces.Services;
using LuminaTutors.Domain.Common;
using LuminaTutors.Domain.Entities.Communication;
using LuminaTutors.Domain.Enums;
using LuminaTutors.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LuminaTutors.Application.Services;

public sealed class MessageService : IMessageService
{
    private readonly IUnitOfWork           _uow;
    private readonly IMapper               _mapper;
    private readonly ILogger<MessageService> _logger;

    public MessageService(IUnitOfWork uow, IMapper mapper, ILogger<MessageService> logger)
    {
        _uow    = uow;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── GetConversations ─────────────────────────────────────────────────────

    public async Task<Result<IReadOnlyList<ConversationDto>>> GetConversationsAsync(
        int userId, CancellationToken ct = default)
    {
        var conversations = await _uow.Conversations.FindAsync(
            c => c.Participants.Any(p => p.UserId == userId),
            include: q => q
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1)),
            ct: ct);

        var dtos = conversations
            .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAt) ?? c.CreatedAt)
            .Select(c => _mapper.Map<ConversationDto>(c))
            .ToList();

        return Result<IReadOnlyList<ConversationDto>>.Success(dtos);
    }

    // ─── GetMessages ──────────────────────────────────────────────────────────

    public async Task<Result<PagedResult<MessageDto>>> GetMessagesAsync(
        int conversationId, int currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var participation = await _uow.ConversationParticipants.FindAsync(
            p => p.ConversationId == conversationId && p.UserId == currentUserId,
            ct: ct);

        if (!participation.Any())
            return Result<PagedResult<MessageDto>>.Failure("FORBIDDEN", "Bạn không có quyền truy cập cuộc trò chuyện này.");

        var paged = await _uow.Messages.GetPagedAsync(
            pageNumber: page,
            pageSize:   pageSize,
            filter:  m => m.ConversationId == conversationId && !m.IsDeleted,
            orderBy: q => q.OrderByDescending(m => m.SentAt),
            include: q => q.Include(m => m.Sender),
            ct: ct);

        var dtos = paged.Items.Select(m =>
        {
            var dto = _mapper.Map<MessageDto>(m);
            return dto with { IsMine = m.SenderId == currentUserId };
        }).ToList();

        var result = new PagedResult<MessageDto>(dtos, paged.TotalCount, page, pageSize);
        return Result<PagedResult<MessageDto>>.Success(result);
    }

    // ─── SendMessage ──────────────────────────────────────────────────────────

    public async Task<Result<MessageDto>> SendMessageAsync(
        int senderId, SendMessageRequest request, CancellationToken ct = default)
    {
        var participation = await _uow.ConversationParticipants.FindAsync(
            p => p.ConversationId == request.ConversationId && p.UserId == senderId,
            ct: ct);

        if (!participation.Any())
            return Result<MessageDto>.Failure("FORBIDDEN", "Bạn không thuộc cuộc trò chuyện này.");

        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId       = senderId,
            MessageText    = request.MessageText?.Trim(),
            AttachmentUrl  = request.AttachmentUrl?.Trim(),
            AttachmentType = request.AttachmentType?.Trim(),
            SentAt         = DateTime.UtcNow,
            IsDeleted      = false
        };

        await _uow.Messages.AddAsync(message, ct);
        await _uow.SaveChangesAsync(ct);

        var messages = await _uow.Messages.FindAsync(
            m => m.Id == message.Id,
            include: q => q.Include(m => m.Sender),
            ct: ct);

        var dto = _mapper.Map<MessageDto>(messages.First()) with { IsMine = true };
        return Result<MessageDto>.Success(dto);
    }

    // ─── StartConversation ────────────────────────────────────────────────────

    public async Task<Result<ConversationDto>> StartConversationAsync(
        int initiatorId, int schoolId, StartConversationRequest request, CancellationToken ct = default)
    {
        // Check if a Direct conversation already exists between the two users
        var existing = await _uow.Conversations.FindAsync(
            c => c.ConversationType == ConversationType.Direct &&
                 c.Participants.Any(p => p.UserId == initiatorId) &&
                 c.Participants.Any(p => p.UserId == request.RecipientUserId),
            include: q => q
                .Include(c => c.Participants).ThenInclude(p => p.User)
                .Include(c => c.Messages.Take(1)),
            ct: ct);

        if (existing.Any())
            return Result<ConversationDto>.Success(_mapper.Map<ConversationDto>(existing.First()));

        await _uow.BeginTransactionAsync(ct);
        try
        {
            var conversation = new Conversation
            {
                SchoolId         = schoolId,
                ConversationType = ConversationType.Direct,
                CreatedByUserId  = initiatorId
            };

            await _uow.Conversations.AddAsync(conversation, ct);
            await _uow.SaveChangesAsync(ct);

            var participants = new[]
            {
                new ConversationParticipant { ConversationId = conversation.Id, UserId = initiatorId,              JoinedAt = DateTime.UtcNow },
                new ConversationParticipant { ConversationId = conversation.Id, UserId = request.RecipientUserId, JoinedAt = DateTime.UtcNow }
            };

            await _uow.ConversationParticipants.AddRangeAsync(participants, ct);
            await _uow.SaveChangesAsync(ct);

            // Send initial message if provided
            if (!string.IsNullOrWhiteSpace(request.InitialMessage))
            {
                var initialMsg = new Message
                {
                    ConversationId = conversation.Id,
                    SenderId       = initiatorId,
                    MessageText    = request.InitialMessage.Trim(),
                    SentAt         = DateTime.UtcNow,
                    IsDeleted      = false
                };
                await _uow.Messages.AddAsync(initialMsg, ct);
                await _uow.SaveChangesAsync(ct);
            }

            await _uow.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Conversation {Id} started by user {InitiatorId} with user {RecipientId}",
                conversation.Id, initiatorId, request.RecipientUserId);

            var convs = await _uow.Conversations.FindAsync(
                c => c.Id == conversation.Id,
                include: q => q.Include(c => c.Participants).ThenInclude(p => p.User),
                ct: ct);

            return Result<ConversationDto>.Success(_mapper.Map<ConversationDto>(convs.First()));
        }
        catch (Exception ex)
        {
            await _uow.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "StartConversation failed");
            return Result<ConversationDto>.Failure("INTERNAL_ERROR", "Có lỗi khi tạo cuộc trò chuyện.");
        }
    }

    // ─── DeleteMessage ────────────────────────────────────────────────────────

    public async Task<Result> DeleteMessageAsync(
        int messageId, int requestedByUserId, CancellationToken ct = default)
    {
        var message = await _uow.Messages.GetByIdAsync(messageId, ct);
        if (message is null)
            return Result.Failure("NOT_FOUND", "Tin nhắn không tồn tại.");

        if (message.SenderId != requestedByUserId)
            return Result.Failure("FORBIDDEN", "Bạn chỉ có thể xóa tin nhắn của chính mình.");

        message.IsDeleted   = true;
        message.DeletedAt   = DateTime.UtcNow;
        message.MessageText = "[Tin nhắn đã bị xóa]";

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

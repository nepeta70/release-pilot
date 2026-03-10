using MediatR;
using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports.External;
using ReleasePilot.Application.Ports.Messaging;
using ReleasePilot.Application.Ports.Output;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Exceptions;
using System.Data;

namespace ReleasePilot.Application.UseCases.Promotions.Commands;

public class PromotionCommandHandler :
    IRequestHandler<RequestPromotionCommand, Guid>,
    IRequestHandler<ApprovePromotionCommand>,
    IRequestHandler<StartDeploymentCommand>,
    IRequestHandler<CompletePromotionCommand>,
    IRequestHandler<RollbackPromotionCommand>,
    IRequestHandler<CancelPromotionCommand>
{
    private readonly IUserContext _userContext;
    private readonly IPromotionWriteRepository _repository;
    private readonly IPromotionReadRepository _readRepository;
    private readonly IDbConnection _dbConnection;
    private readonly IDeploymentPort _deploymentPort;
    private readonly INotificationPort _notificationPort;
    private readonly IEventOutbox _outbox;
    private readonly ILogger _logger;

    public PromotionCommandHandler(
        IUserContext userContext,
        IPromotionWriteRepository repository,
        IPromotionReadRepository readRepository,
        IDbConnection dbConnection,
        IDeploymentPort deploymentPort,
        INotificationPort notificationPort,
        IEventOutbox outbox,
        ILoggerFactory loggerFactory)
    {
        _userContext = userContext;
        _repository = repository;
        _readRepository = readRepository;
        _dbConnection = dbConnection;
        _deploymentPort = deploymentPort;
        _notificationPort = notificationPort;
        _outbox = outbox;
        _logger = loggerFactory.CreateLogger<PromotionCommandHandler>();
    }

    public async Task<Guid> Handle(RequestPromotionCommand request, CancellationToken ct)
    {
        var targetEnv = Enum.Parse<DeploymentEnvironment>(request.TargetEnv, ignoreCase: true);

        var currentStatus = await _readRepository.GetStatusByAppAsync(request.AppName, ct);
        var existingPromotion = currentStatus.LastOrDefault(s => s.Environment == targetEnv);
        if (existingPromotion is not null)
        {
            var sourceEnv = Enum.Parse<DeploymentEnvironment>(request.TargetEnv, ignoreCase: true);
            var requiredPrevious = targetEnv.GetRequiredPrevious();
            var previousIsCompleted = currentStatus.Any(s => s.Environment == requiredPrevious && s.Status == PromotionStatus.Completed);
            if (targetEnv != DeploymentEnvironment.Dev && !previousIsCompleted)
            {
                _logger.LogWarning("Promotion request for {AppName} to {TargetEnv} blocked due to incomplete promotion in {RequiredPrevious}.", request.AppName, targetEnv, requiredPrevious);
                throw new DomainException($"You must release {requiredPrevious} before {targetEnv}.");
            }
        }

        using var transaction = StartTransaction();
        var id = Guid.NewGuid();

        var promotion = Promotion.Request(
            id,
            request.AppName,
            request.Version,
            targetEnv,
            request.WorkItemIds,
            _userContext.GetCurrent().Name);

        await _repository.InsertAsync(promotion, _userContext.GetCurrent().Name, transaction, ct);
        await PersistDomainEvents(promotion, transaction, ct);
        transaction.Commit();
        _logger.LogInformation("Promotion requested for {AppName} version {Version} to {TargetEnv} by {User}.", request.AppName, request.Version, targetEnv, _userContext.GetCurrent().Name);
        return id;
    }

    public async Task Handle(ApprovePromotionCommand request, CancellationToken ct)
    {
        using var transaction = StartTransaction();
        var promotion = await LoadPromotion(request.PromotionId, transaction, ct);

        var locked = await _repository.HasInProgressAsync(
            promotion.ApplicationName,
            promotion.TargetEnvironment,
            excludePromotionId: promotion.Id,
            transaction, ct);

        var user = _userContext.GetCurrent();
        promotion.Approve(user.Role, user.Name, locked);

        await PersistAndSendEvents(transaction, promotion, ct);
        _logger.LogInformation("Promotion {PromotionId} for {AppName} to {TargetEnv} approved by {User}.", promotion.Id, promotion.ApplicationName, promotion.TargetEnvironment, user.Name);
        transaction.Commit();
    }

    public async Task Handle(StartDeploymentCommand request, CancellationToken ct)
    {
        using var transaction = StartTransaction();
        var promotion = await LoadPromotion(request.PromotionId, transaction, ct);

        var locked = await _repository.HasInProgressAsync(
            promotion.ApplicationName,
            promotion.TargetEnvironment,
            excludePromotionId: promotion.Id,
            transaction, ct);

        var user = _userContext.GetCurrent();
        promotion.StartDeployment(locked, user.Name);

        // Use the enum's string value for the external port
        await _deploymentPort.InitiateDeploymentAsync(
            promotion.ApplicationName,
            promotion.Version,
            promotion.TargetEnvironment.ToString());

        await PersistAndSendEvents(transaction, promotion, ct);
        _logger.LogInformation("Deployment started for promotion {PromotionId} of {AppName} to {TargetEnv} by {User}.", promotion.Id, promotion.ApplicationName, promotion.TargetEnvironment, user.Name);
        transaction.Commit();
    }

    public async Task Handle(CompletePromotionCommand request, CancellationToken ct)
    {
        using var transaction = StartTransaction();
        var promotion = await LoadPromotion(request.PromotionId, transaction, ct);

        var user = _userContext.GetCurrent();
        promotion.Complete(user.Name);

        await PersistAndSendEvents(transaction, promotion, ct);
        transaction.Commit();
        await _notificationPort.NotifyStatusChangeAsync(promotion.Id, promotion.ApplicationName, "Completed");
    }

    public async Task Handle(RollbackPromotionCommand request, CancellationToken ct)
    {
        using var transaction = StartTransaction();
        var promotion = await LoadPromotion(request.PromotionId, transaction, ct);

        var user = _userContext.GetCurrent();
        promotion.Rollback(request.Reason, user.Name);

        await PersistAndSendEvents(transaction, promotion, ct);
        transaction.Commit();
        await _notificationPort.NotifyStatusChangeAsync(promotion.Id, promotion.ApplicationName, "RolledBack");
    }

    public async Task Handle(CancelPromotionCommand request, CancellationToken ct)
    {
        using var transaction = StartTransaction();
        var promotion = await LoadPromotion(request.PromotionId, transaction, ct);

        var user = _userContext.GetCurrent();
        promotion.Cancel(user.Name);

        await PersistAndSendEvents(transaction, promotion, ct);
        _logger.LogInformation("Promotion {PromotionId} for {AppName} to {TargetEnv} cancelled by {User}.", promotion.Id, promotion.ApplicationName, promotion.TargetEnvironment, user.Name);
        transaction.Commit();
    }

    private IDbTransaction StartTransaction()
    {
        if (_dbConnection.State != ConnectionState.Open) _dbConnection.Open();
        return _dbConnection.BeginTransaction();
    }

    private async Task<Promotion> LoadPromotion(Guid id, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var promotion = await _repository.GetByIdAsync(id, transaction, cancellationToken);
        return promotion is null ? throw new DomainException(PromotionErrors.PromotionNotFound) : promotion;
    }

    private async Task PersistDomainEvents(Promotion promotion, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        foreach (var @event in promotion.DomainEvents)
        {
            await _outbox.SaveEventAsync(@event, promotion.Id, transaction, cancellationToken);
        }
        promotion.ClearEvents();
    }
    private async Task PersistAndSendEvents(IDbTransaction transaction, Promotion promotion, CancellationToken ct)
    {
        var user = _userContext.GetCurrent();
        await _repository.UpdateAsync(promotion, user.Name, transaction, ct);
        await PersistDomainEvents(promotion, transaction, ct);
    }
}
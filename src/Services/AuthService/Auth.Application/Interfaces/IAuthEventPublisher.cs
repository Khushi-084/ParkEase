namespace Auth.Application.Interfaces;

public interface IAuthEventPublisher
{
    Task PublishLotManagerApprovedAsync(Guid userId, string email, string managerName);
}

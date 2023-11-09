using Gatherly.Domain.Entities;

namespace Gatherly.Application.Abstractions;

internal interface IEmailService
{
    Task SendInvitationAcceptedEmailAsync(Gathering gathering, CancellationToken cancellationToken = default);
    Task SendInvitationSentEmailAsync(Member member, Gathering gathering, CancellationToken cancellationToken = default);
}

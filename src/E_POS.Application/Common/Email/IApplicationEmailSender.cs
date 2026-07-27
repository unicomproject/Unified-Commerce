using E_POS.Application.Common.Models;

namespace E_POS.Application.Common.Email;

public interface IApplicationEmailSender
{
    /// <summary>
    /// True when the email provider has enough configuration to attempt a send.
    /// </summary>
    bool IsConfigured { get; }

    Task<ApplicationResult<ApplicationEmailSendResult>> SendAsync(
        ApplicationEmailMessage message,
        CancellationToken cancellationToken);
}

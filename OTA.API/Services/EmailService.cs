using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using OTA.API.Models.Settings;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>
    /// SMTP-based email service. Reads configuration from the EmailSettings section
    /// (appsettings.json → EmailSettings). All sends are fire-and-forget safe;
    /// failures are logged but never thrown to the caller.
    /// </summary>
    public sealed class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger   = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Core send ─────────────────────────────────────────────────────────

        public async Task SendAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.Enabled)
            {
                _logger.LogDebug("Email disabled — skipping send to {Email} (subject: {Subject}).", toEmail, subject);
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.Host))
            {
                _logger.LogWarning("EmailSettings.Host is not configured — cannot send email.");
                return;
            }

            try
            {
                using var client = BuildSmtpClient();
                using var message = BuildMessage(toEmail, toName, subject, htmlBody);

                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Email sent to {Email} — subject: {Subject}.", toEmail, subject);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Email send to {Email} was cancelled.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email} (subject: {Subject}).", toEmail, subject);
                throw; // re-throw so callers (e.g. test endpoint) can surface the error
            }
        }

        // ── Pre-built templates ───────────────────────────────────────────────

        public Task SendFirmwareApprovalNotificationAsync(
            string toEmail, string approverName, string firmwareVersion, string projectName,
            CancellationToken cancellationToken = default)
        {
            var subject = $"[OTA Platform] Firmware {firmwareVersion} Approved";
            var body = EmailTemplate($@"
                <h2 style='color:#16a34a;'>Firmware Approved ✔</h2>
                <p>Hi <strong>{Escape(approverName)}</strong>,</p>
                <p>Firmware version <strong>{Escape(firmwareVersion)}</strong> for project
                   <strong>{Escape(projectName)}</strong> has been <strong style='color:#16a34a;'>approved</strong>
                   and is now eligible for rollout.</p>
                <p>Log in to the OTA Platform to start a rollout.</p>");
            return SendAsync(toEmail, approverName, subject, body, cancellationToken);
        }

        public Task SendFirmwareRejectionNotificationAsync(
            string toEmail, string submitterName, string firmwareVersion, string reason,
            CancellationToken cancellationToken = default)
        {
            var subject = $"[OTA Platform] Firmware {firmwareVersion} Rejected";
            var body = EmailTemplate($@"
                <h2 style='color:#dc2626;'>Firmware Rejected ✖</h2>
                <p>Hi <strong>{Escape(submitterName)}</strong>,</p>
                <p>Firmware version <strong>{Escape(firmwareVersion)}</strong> has been
                   <strong style='color:#dc2626;'>rejected</strong>.</p>
                <p><strong>Reason:</strong> {Escape(reason)}</p>
                <p>Please address the feedback and resubmit.</p>");
            return SendAsync(toEmail, submitterName, subject, body, cancellationToken);
        }

        public Task SendRolloutCompletionNotificationAsync(
            string toEmail, string recipientName, string rolloutName,
            int totalDevices, int succeeded, int failed,
            CancellationToken cancellationToken = default)
        {
            var statusColor = failed == 0 ? "#16a34a" : "#dc2626";
            var statusText  = failed == 0 ? "Completed Successfully" : "Completed with Failures";
            var subject = $"[OTA Platform] Rollout '{rolloutName}' {statusText}";
            var body = EmailTemplate($@"
                <h2 style='color:{statusColor};'>Rollout {Escape(statusText)}</h2>
                <p>Hi <strong>{Escape(recipientName)}</strong>,</p>
                <p>OTA rollout <strong>{Escape(rolloutName)}</strong> has finished.</p>
                <table style='border-collapse:collapse;margin:16px 0;'>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Total Devices</td>
                        <td style='padding:6px 0;font-weight:600;'>{totalDevices}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Succeeded</td>
                        <td style='padding:6px 0;font-weight:600;color:#16a34a;'>{succeeded}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Failed</td>
                        <td style='padding:6px 0;font-weight:600;color:#dc2626;'>{failed}</td></tr>
                </table>
                <p>Log in to the OTA Platform for a detailed report.</p>");
            return SendAsync(toEmail, recipientName, subject, body, cancellationToken);
        }

        public Task SendWelcomeEmailAsync(
            string toEmail, string userName, string temporaryPassword,
            CancellationToken cancellationToken = default)
        {
            var subject = "[OTA Platform] Welcome — Your Account is Ready";
            var body = EmailTemplate($@"
                <h2 style='color:#2563eb;'>Welcome to OTA Platform</h2>
                <p>Hi <strong>{Escape(userName)}</strong>,</p>
                <p>Your account has been created. Use the credentials below to log in:</p>
                <table style='border-collapse:collapse;margin:16px 0;background:#f8fafc;border-radius:8px;padding:12px;'>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Email</td>
                        <td style='padding:6px 0;font-weight:600;font-family:monospace;'>{Escape(toEmail)}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Password</td>
                        <td style='padding:6px 0;font-weight:600;font-family:monospace;'>{Escape(temporaryPassword)}</td></tr>
                </table>
                <p style='color:#dc2626;'><strong>Please change your password after first login.</strong></p>");
            return SendAsync(toEmail, userName, subject, body, cancellationToken);
        }

        public Task SendCrudNotificationAsync(
            string toEmail, string recipientName, string action, string entityType, string entityName,
            CancellationToken cancellationToken = default)
        {
            var actionColor = action.ToLowerInvariant() switch
            {
                "created" => "#16a34a",
                "updated" => "#2563eb",
                "deleted" => "#dc2626",
                _         => "#475569"
            };
            var subject = $"[OTA Platform] {entityType} {Escape(entityName)} {action}";
            var body = EmailTemplate($@"
                <h2 style='color:{actionColor};'>{Escape(entityType)} {Escape(action)} ✔</h2>
                <p>Hi <strong>{Escape(recipientName)}</strong>,</p>
                <p>The following action was performed on the OTA Platform:</p>
                <table style='border-collapse:collapse;margin:16px 0;background:#f8fafc;border-radius:8px;padding:12px;'>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Action</td>
                        <td style='padding:6px 0;font-weight:600;color:{actionColor};'>{Escape(action)}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Entity Type</td>
                        <td style='padding:6px 0;font-weight:600;'>{Escape(entityType)}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Name / ID</td>
                        <td style='padding:6px 0;font-weight:600;font-family:monospace;'>{Escape(entityName)}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Performed by</td>
                        <td style='padding:6px 0;font-weight:600;'>{Escape(recipientName)}</td></tr>
                    <tr><td style='padding:6px 16px 6px 0;color:#64748b;'>Timestamp</td>
                        <td style='padding:6px 0;'>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
                </table>
                <p>Log in to the OTA Platform to review the change.</p>");
            return SendAsync(toEmail, recipientName, subject, body, cancellationToken);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private SmtpClient BuildSmtpClient()
        {
            var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl   = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 30_000
            };

            // Office365 / Exchange use STARTTLS on port 587 with EnableSsl = false
            // but the socket is upgraded via STARTTLS — SmtpClient handles this automatically
            // when EnableSsl = false and the server advertises STARTTLS.
            return client;
        }

        private MailMessage BuildMessage(string toEmail, string toName, string subject, string htmlBody)
        {
            var senderName = string.IsNullOrWhiteSpace(_settings.SenderName) ? "OTA Platform" : _settings.SenderName;
            var from = new MailAddress(_settings.SenderEmail, senderName);
            var to   = new MailAddress(toEmail, toName);

            var msg = new MailMessage(from, to)
            {
                Subject    = subject,
                Body       = htmlBody,
                IsBodyHtml = true
            };
            return msg;
        }

        private static string EmailTemplate(string contentHtml) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='margin:0;padding:0;background:#f1f5f9;font-family:Arial,sans-serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#f1f5f9;padding:32px 0;'>
    <tr><td align='center'>
      <table width='600' cellpadding='0' cellspacing='0' style='background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08);'>
        <!-- Header -->
        <tr><td style='background:linear-gradient(135deg,#1e3a5f,#2563eb);padding:24px 32px;'>
          <h1 style='color:#ffffff;margin:0;font-size:20px;font-weight:700;letter-spacing:.5px;'>OTA Platform</h1>
          <p style='color:#93c5fd;margin:4px 0 0;font-size:12px;'>Firmware Management System</p>
        </td></tr>
        <!-- Body -->
        <tr><td style='padding:32px;color:#1e293b;font-size:15px;line-height:1.6;'>
          {contentHtml}
        </td></tr>
        <!-- Footer -->
        <tr><td style='background:#f8fafc;border-top:1px solid #e2e8f0;padding:16px 32px;text-align:center;'>
          <p style='color:#94a3b8;font-size:12px;margin:0;'>
            This is an automated message from OTA Platform. Please do not reply to this email.
          </p>
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";

        private static string Escape(string? input) =>
            System.Net.WebUtility.HtmlEncode(input ?? string.Empty);
    }
}

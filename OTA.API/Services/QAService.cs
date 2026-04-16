using Microsoft.AspNetCore.Http;
using OTA.API.Models.DTOs;
using OTA.API.Models.Entities;
using OTA.API.Models.Enums;
using OTA.API.Repositories.Interfaces;
using OTA.API.Services.Interfaces;

namespace OTA.API.Services
{
    /// <summary>Service implementation for managing QA testing sessions for firmware versions.</summary>
    public class QAService : IQAService
    {
        private readonly IQASessionRepository _qaRepo;
        private readonly IFirmwareRepository _firmwareRepo;
        private readonly INotificationService _notificationService;
        private readonly ILogger<QAService> _logger;

        public QAService(
            IQASessionRepository qaRepo,
            IFirmwareRepository firmwareRepo,
            INotificationService notificationService,
            ILogger<QAService> logger)
        {
            _qaRepo              = qaRepo;
            _firmwareRepo        = firmwareRepo;
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger              = logger;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public async Task<QASessionDto?> GetSessionAsync(string firmwareId, CancellationToken cancellationToken = default)
        {
            var session = await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken);
            return session is null ? null : ToDto(session);
        }

        public async Task<QASessionDto> StartSessionAsync(string firmwareId, string userId, string? userName = null, CancellationToken cancellationToken = default)
        {
            var existing = await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken);
            if (existing is not null)
                throw new InvalidOperationException($"A QA session already exists for firmware '{firmwareId}'.");

            var firmware = await _firmwareRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"Firmware '{firmwareId}' was not found.");

            var now = DateTime.UtcNow;
            var session = new QASessionEntity
            {
                FirmwareId        = firmwareId,
                Status            = QASessionStatus.NotStarted,
                StartedAt         = now,
                StartedByUserId   = userId,
                StartedByName     = !string.IsNullOrWhiteSpace(userName) ? userName : null,
                CreatedByUserId   = userId,
                CreatedAt         = now,
                UpdatedAt         = now,
                EventLog          =
                [
                    new QAEventLogItem
                    {
                        EventType   = "SessionStarted",
                        Description = $"QA session started for firmware {firmware.Version}.",
                        UserId      = userId,
                        Timestamp   = now,
                        Metadata    = new Dictionary<string, string> { ["firmwareVersion"] = firmware.Version }
                    }
                ]
            };

            await _qaRepo.InsertAsync(session, cancellationToken);
            _logger.LogInformation("QA session started for firmware {FirmwareId} by {UserId}.", firmwareId, userId);

            _ = _notificationService.NotifyAsync(
                "QA Session Started",
                $"QA session started for firmware '{firmware.Version}' (ID: {firmwareId}).",
                new Dictionary<string, string> { ["type"] = "qa_session_started", ["firmwareId"] = firmwareId, ["version"] = firmware.Version },
                cancellationToken: CancellationToken.None);

            return ToDto(session);
        }

        public async Task<QASessionDto> UpdateStatusAsync(string firmwareId, UpdateQAStatusRequest request, string userId, CancellationToken cancellationToken = default)
        {
            var session = await RequireSession(firmwareId, cancellationToken);

            await _qaRepo.UpdateStatusAsync(session.Id, request.Status, request.Remarks, cancellationToken);

            await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
            {
                EventType   = "StatusChanged",
                Description = $"QA status changed from {session.Status} to {request.Status}.",
                UserId      = userId,
                Metadata    = new Dictionary<string, string>
                {
                    ["from"] = session.Status.ToString(),
                    ["to"]   = request.Status.ToString()
                }
            }, cancellationToken);

            return ToDto(await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken) ?? session);
        }

        public async Task<UploadQADocumentResponse> UploadDocumentAsync(
            string firmwareId, IFormFile file, string docType, string baseUrl,
            string userId, CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
                throw new ArgumentException("No file provided.");

            var session = await RequireSession(firmwareId, cancellationToken);

            var ext        = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".bin";
            var stored     = $"{Guid.NewGuid():N}{ext}";
            var uploadDir  = Path.Combine(Directory.GetCurrentDirectory(), "qa-docs");
            Directory.CreateDirectory(uploadDir);
            var filePath   = Path.Combine(uploadDir, stored);

            await using (var fs = File.Create(filePath))
            await using (var src = file.OpenReadStream())
                await src.CopyToAsync(fs, cancellationToken);

            var downloadUrl = $"{baseUrl}/qa-docs/{stored}";
            var now         = DateTime.UtcNow;

            var doc = new QADocumentItem
            {
                DocumentId        = Guid.NewGuid().ToString("N"),
                Name              = file.FileName,
                StoredFileName    = stored,
                DownloadUrl       = downloadUrl,
                FileSizeBytes     = file.Length,
                UploadedAt        = now,
                UploadedByUserId  = userId,
            };

            if (docType.Equals("testResult", StringComparison.OrdinalIgnoreCase))
                await _qaRepo.PushTestResultDocumentAsync(session.Id, doc, cancellationToken);
            else
                await _qaRepo.PushTestCaseDocumentAsync(session.Id, doc, cancellationToken);

            await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
            {
                EventType   = "DocumentUploaded",
                Description = $"{(docType.Equals("testResult", StringComparison.OrdinalIgnoreCase) ? "Test result" : "Test case")} document '{file.FileName}' uploaded.",
                UserId      = userId,
                Metadata    = new Dictionary<string, string>
                {
                    ["docType"]  = docType,
                    ["fileName"] = file.FileName,
                    ["size"]     = file.Length.ToString()
                }
            }, cancellationToken);

            return new UploadQADocumentResponse
            {
                DocumentId    = doc.DocumentId,
                Name          = doc.Name,
                StoredFileName = doc.StoredFileName,
                DownloadUrl   = doc.DownloadUrl,
                FileSizeBytes = doc.FileSizeBytes,
                UploadedAt    = doc.UploadedAt,
            };
        }

        public async Task<bool> RemoveDocumentAsync(string firmwareId, string documentId, string userId, CancellationToken cancellationToken = default)
        {
            var session = await RequireSession(firmwareId, cancellationToken);
            await _qaRepo.PullDocumentAsync(session.Id, documentId, cancellationToken);

            await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
            {
                EventType   = "DocumentRemoved",
                Description = $"Document '{documentId}' removed.",
                UserId      = userId,
                Metadata    = new Dictionary<string, string> { ["documentId"] = documentId }
            }, cancellationToken);

            return true;
        }

        public async Task<QASessionDto> AddBugAsync(string firmwareId, AddBugRequest request, string userId, CancellationToken cancellationToken = default)
        {
            var session = await RequireSession(firmwareId, cancellationToken);

            var bug = new QABugItem
            {
                BugId             = Guid.NewGuid().ToString("N"),
                Title             = request.Title,
                Description       = request.Description,
                Severity          = request.Severity,
                BugStatus         = BugStatus.Open,
                ReportedAt        = DateTime.UtcNow,
                ReportedByUserId  = userId,
            };

            await _qaRepo.PushBugAsync(session.Id, bug, cancellationToken);

            // Auto-transition status to BugListRaised if currently InProgress or NotStarted
            if (session.Status is QASessionStatus.InProgress or QASessionStatus.NotStarted)
            {
                await _qaRepo.UpdateStatusAsync(session.Id, QASessionStatus.BugListRaised, null, cancellationToken);
                await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
                {
                    EventType   = "StatusChanged",
                    Description = $"QA status auto-advanced to BugListRaised after bug was raised.",
                    UserId      = userId,
                    Metadata    = new Dictionary<string, string> { ["from"] = session.Status.ToString(), ["to"] = "BugListRaised" }
                }, cancellationToken);
            }

            await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
            {
                EventType   = "BugRaised",
                Description = $"Bug raised: [{bug.Severity}] {bug.Title}",
                UserId      = userId,
                Metadata    = new Dictionary<string, string>
                {
                    ["bugId"]    = bug.BugId,
                    ["severity"] = bug.Severity.ToString(),
                    ["title"]    = bug.Title
                }
            }, cancellationToken);

            _ = _notificationService.NotifyAsync(
                "QA Bug Raised",
                $"[{bug.Severity}] Bug raised: {bug.Title}",
                new Dictionary<string, string> { ["type"] = "qa_bug_raised", ["firmwareId"] = firmwareId, ["bugId"] = bug.BugId, ["severity"] = bug.Severity.ToString() },
                cancellationToken: CancellationToken.None);

            return ToDto(await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken) ?? session);
        }

        public async Task<QASessionDto> UpdateBugAsync(string firmwareId, string bugId, UpdateBugRequest request, string userId, CancellationToken cancellationToken = default)
        {
            var session = await RequireSession(firmwareId, cancellationToken);
            var existing = session.Bugs.FirstOrDefault(b => b.BugId == bugId)
                ?? throw new KeyNotFoundException($"Bug '{bugId}' not found in QA session for firmware '{firmwareId}'.");

            var updated = new QABugItem
            {
                BugId            = existing.BugId,
                Title            = request.Title ?? existing.Title,
                Description      = request.Description ?? existing.Description,
                Severity         = request.Severity ?? existing.Severity,
                BugStatus        = request.BugStatus ?? existing.BugStatus,
                ReportedAt       = existing.ReportedAt,
                ReportedByUserId = existing.ReportedByUserId,
                Resolution       = request.Resolution ?? existing.Resolution,
                ResolvedAt       = (request.BugStatus is BugStatus.Resolved or BugStatus.WontFix)
                                    ? DateTime.UtcNow
                                    : existing.ResolvedAt,
            };

            await _qaRepo.UpdateBugAsync(session.Id, bugId, updated, cancellationToken);

            await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
            {
                EventType   = "BugUpdated",
                Description = $"Bug '{existing.Title}' updated to status {updated.BugStatus}.",
                UserId      = userId,
                Metadata    = new Dictionary<string, string>
                {
                    ["bugId"]      = bugId,
                    ["newStatus"]  = updated.BugStatus.ToString(),
                    ["resolution"] = updated.Resolution ?? string.Empty
                }
            }, cancellationToken);

            return ToDto(await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken) ?? session);
        }

        public async Task<QASessionDto> CompleteSessionAsync(string firmwareId, CompleteQARequest request, string userId, CancellationToken cancellationToken = default)
        {
            if (request.FinalStatus is not (QASessionStatus.Complete or QASessionStatus.Fail))
                throw new InvalidOperationException("FinalStatus must be either 'Complete' or 'Fail'.");

            var session = await RequireSession(firmwareId, cancellationToken);

            await _qaRepo.SetCompletedAsync(session.Id, request.FinalStatus, request.Remarks, cancellationToken);

            await _qaRepo.PushEventLogAsync(session.Id, new QAEventLogItem
            {
                EventType   = "SessionCompleted",
                Description = $"QA session finalized with status {request.FinalStatus}.",
                UserId      = userId,
                Metadata    = new Dictionary<string, string>
                {
                    ["finalStatus"] = request.FinalStatus.ToString(),
                    ["remarks"]     = request.Remarks ?? string.Empty
                }
            }, cancellationToken);

            _ = _notificationService.NotifyAsync(
                "QA Session Completed",
                $"QA session for firmware '{firmwareId}' finalized with status: {request.FinalStatus}.",
                new Dictionary<string, string> { ["type"] = "qa_session_completed", ["firmwareId"] = firmwareId, ["status"] = request.FinalStatus.ToString() },
                cancellationToken: CancellationToken.None);

            return ToDto(await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken) ?? session);
        }

        public async Task<List<QAEventLogItemDto>> GetEventLogAsync(string firmwareId, CancellationToken cancellationToken = default)
        {
            var session = await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken);
            if (session is null) return new List<QAEventLogItemDto>();

            return session.EventLog
                .OrderByDescending(e => e.Timestamp)
                .Select(e => new QAEventLogItemDto
                {
                    EventId     = e.EventId,
                    EventType   = e.EventType,
                    Description = e.Description,
                    UserId      = e.UserId,
                    Timestamp   = e.Timestamp,
                    Metadata    = e.Metadata,
                }).ToList();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private async Task<QASessionEntity> RequireSession(string firmwareId, CancellationToken cancellationToken)
        {
            return await _qaRepo.GetByFirmwareIdAsync(firmwareId, cancellationToken)
                ?? throw new KeyNotFoundException($"No QA session found for firmware '{firmwareId}'. Start a session first.");
        }

        private static QASessionDto ToDto(QASessionEntity s) => new()
        {
            SessionId          = s.Id,
            FirmwareId         = s.FirmwareId,
            Status             = s.Status.ToString(),
            TestCaseDocuments  = s.TestCaseDocuments.Select(DocToDto).ToList(),
            TestResultDocuments= s.TestResultDocuments.Select(DocToDto).ToList(),
            Bugs               = s.Bugs.OrderByDescending(b => b.ReportedAt).Select(BugToDto).ToList(),
            EventLog           = s.EventLog.OrderByDescending(e => e.Timestamp).Select(EventToDto).ToList(),
            StartedAt          = s.StartedAt,
            StartedByUserId    = s.StartedByUserId,
            StartedByName      = s.StartedByName,
            CompletedAt        = s.CompletedAt,
            Remarks            = s.Remarks,
            CreatedAt          = s.CreatedAt,
            UpdatedAt          = s.UpdatedAt,
        };

        private static QADocumentItemDto DocToDto(QADocumentItem d) => new()
        {
            DocumentId       = d.DocumentId,
            Name             = d.Name,
            StoredFileName   = d.StoredFileName,
            DownloadUrl      = d.DownloadUrl,
            FileSizeBytes    = d.FileSizeBytes,
            UploadedAt       = d.UploadedAt,
            UploadedByUserId = d.UploadedByUserId,
        };

        private static QABugItemDto BugToDto(QABugItem b) => new()
        {
            BugId            = b.BugId,
            Title            = b.Title,
            Description      = b.Description,
            Severity         = b.Severity.ToString(),
            BugStatus        = b.BugStatus.ToString(),
            ReportedAt       = b.ReportedAt,
            ReportedByUserId = b.ReportedByUserId,
            ResolvedAt       = b.ResolvedAt,
            Resolution       = b.Resolution,
        };

        private static QAEventLogItemDto EventToDto(QAEventLogItem e) => new()
        {
            EventId     = e.EventId,
            EventType   = e.EventType,
            Description = e.Description,
            UserId      = e.UserId,
            Timestamp   = e.Timestamp,
            Metadata    = e.Metadata,
        };
    }
}

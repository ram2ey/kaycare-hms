using KayCare.Core.Constants;
using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KayCare.Infrastructure.Services;

public class NursingNoteService : INursingNoteService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditService       _audit;
    private readonly ILogger<NursingNoteService> _logger;

    public NursingNoteService(AppDbContext db, ICurrentUserService currentUser, IAuditService audit, ILogger<NursingNoteService> logger)
    {
        _db          = db;
        _currentUser = currentUser;
        _audit       = audit;
        _logger      = logger;
    }

    public async Task<NursingNoteResponse> AddAsync(Guid patientId, AddNursingNoteRequest req, CancellationToken ct = default)
    {
        var note = new NursingNote
        {
            NursingNoteId = Guid.NewGuid(),
            PatientId     = patientId,
            AdmissionId   = req.AdmissionId,
            AuthorId      = _currentUser.UserId,
            NoteType      = req.NoteType,
            Note          = req.Note,
        };
        _db.NursingNotes.Add(note);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.NursingNoteCreate, nameof(NursingNote), note.NursingNoteId, note.PatientId,
            details: $"NoteType={note.NoteType}", ct: ct);
        _logger.LogInformation("Nursing note {NursingNoteId} added for patient {PatientId} ({NoteType})",
            note.NursingNoteId, note.PatientId, note.NoteType);

        var author = await _db.Users.AsNoTracking().FirstAsync(u => u.UserId == note.AuthorId, ct);
        return ToResponse(note, $"{author.FirstName} {author.LastName}");
    }

    public async Task<List<NursingNoteResponse>> GetForPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        var list = await _db.NursingNotes
            .Include(n => n.Author)
            .AsNoTracking()
            .Where(n => n.PatientId == patientId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return list.Select(n => ToResponse(n, $"{n.Author.FirstName} {n.Author.LastName}")).ToList();
    }

    public async Task<List<NursingNoteResponse>> GetForAdmissionAsync(Guid admissionId, CancellationToken ct = default)
    {
        var list = await _db.NursingNotes
            .Include(n => n.Author)
            .AsNoTracking()
            .Where(n => n.AdmissionId == admissionId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

        return list.Select(n => ToResponse(n, $"{n.Author.FirstName} {n.Author.LastName}")).ToList();
    }

    public async Task DeleteAsync(Guid noteId, CancellationToken ct = default)
    {
        var note = await _db.NursingNotes.FirstOrDefaultAsync(n => n.NursingNoteId == noteId, ct)
            ?? throw new NotFoundException("NursingNote", noteId);

        if (note.AuthorId != _currentUser.UserId)
            throw new AppException("You can only delete your own nursing notes.", 403);

        _db.NursingNotes.Remove(note);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditActions.NursingNoteDelete, nameof(NursingNote), noteId, note.PatientId, ct: ct);
        _logger.LogWarning("Nursing note {NursingNoteId} deleted by author {AuthorId}", noteId, note.AuthorId);
    }

    private static NursingNoteResponse ToResponse(NursingNote n, string authorName) =>
        new(n.NursingNoteId, n.PatientId, n.AdmissionId, authorName, n.NoteType, n.Note, n.CreatedAt);
}

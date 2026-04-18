using KayCare.Core.DTOs.Nursing;
using KayCare.Core.Entities;
using KayCare.Core.Exceptions;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KayCare.Infrastructure.Services;

public class NursingNoteService : INursingNoteService
{
    private readonly AppDbContext        _db;
    private readonly ICurrentUserService _currentUser;

    public NursingNoteService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
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
    }

    private static NursingNoteResponse ToResponse(NursingNote n, string authorName) =>
        new(n.NursingNoteId, n.PatientId, n.AdmissionId, authorName, n.NoteType, n.Note, n.CreatedAt);
}

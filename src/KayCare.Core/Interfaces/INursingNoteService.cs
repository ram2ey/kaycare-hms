using KayCare.Core.DTOs.Nursing;

namespace KayCare.Core.Interfaces;

public interface INursingNoteService
{
    Task<NursingNoteResponse>       AddAsync(Guid patientId, AddNursingNoteRequest request, CancellationToken ct = default);
    Task<List<NursingNoteResponse>> GetForPatientAsync(Guid patientId, CancellationToken ct = default);
    Task<List<NursingNoteResponse>> GetForAdmissionAsync(Guid admissionId, CancellationToken ct = default);
    Task                            DeleteAsync(Guid noteId, CancellationToken ct = default);
}

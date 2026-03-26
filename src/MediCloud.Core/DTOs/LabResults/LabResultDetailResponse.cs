namespace MediCloud.Core.DTOs.LabResults;

public class LabResultDetailResponse : LabResultResponse
{
    public IReadOnlyList<LabObservationResponse> Observations { get; set; } = [];
}

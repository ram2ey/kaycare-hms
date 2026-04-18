using KayCare.Core.DTOs.Pharmacy;

namespace KayCare.Core.Interfaces;

public interface ICSRegisterService
{
    Task<List<CSRegisterDrugEntry>> GetRegisterAsync(DateOnly? from = null, DateOnly? to = null, CancellationToken ct = default);
}

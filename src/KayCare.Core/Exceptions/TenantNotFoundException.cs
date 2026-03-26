namespace KayCare.Core.Exceptions;

public class TenantNotFoundException : AppException
{
    public TenantNotFoundException(string identifier)
        : base($"Tenant '{identifier}' not found.", 404) { }
}

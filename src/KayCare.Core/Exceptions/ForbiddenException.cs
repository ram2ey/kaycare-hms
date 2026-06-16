namespace KayCare.Core.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Access denied.") : base(message, 403) { }
}

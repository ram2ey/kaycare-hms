namespace KayCare.Core.Constants;

public static class DocumentConstants
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB

    public static readonly string[] AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    ];
}

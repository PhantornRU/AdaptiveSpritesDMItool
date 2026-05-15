namespace AdaptiveSpritesDmiTool.Application.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

public static class Errors
{
    public static Error Validation(string message) => new("validation", message);

    public static Error NotFound(string message) => new("not-found", message);

    public static Error Conflict(string message) => new("conflict", message);

    public static Error Unexpected(string message) => new("unexpected", message);

    public static Error Cancelled(string message) => new("cancelled", message);
}
namespace StrayThoughts_BE.Utilities;

public static class ProjectClock
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(8);

    public static DateTime Now => DateTime.SpecifyKind(DateTime.UtcNow + Offset, DateTimeKind.Unspecified);
}

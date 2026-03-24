using FullBenchmark.Contracts.Domain.Enums;

namespace FullBenchmark.Contracts.Domain.Entities;

public class UserNote
{
    public Guid           Id        { get; set; }
    public Guid           SessionId { get; set; }

    public string  Content   { get; set; } = string.Empty;
    public TagType TagType   { get; set; } = TagType.Manual;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public BenchmarkSession Session { get; set; } = null!;
}

namespace LoafNCatting.Tests;

internal sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset _utcNow = utcNow.ToUniversalTime();

    public override DateTimeOffset GetUtcNow() => _utcNow;

    internal void SetUtcNow(DateTimeOffset value)
        => _utcNow = value.ToUniversalTime();
}

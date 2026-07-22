namespace LoafNCatting.Infrastructure.Repositories;

internal static class SqlServerErrorClassifier
{
    internal static bool IsUniqueConstraintViolation(int number)
        => number is 2601 or 2627;
}

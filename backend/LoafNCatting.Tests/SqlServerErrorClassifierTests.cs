using LoafNCatting.Infrastructure.Repositories;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class SqlServerErrorClassifierTests
{
    [DataRow(2601)]
    [DataRow(2627)]
    [TestMethod]
    public void IsUniqueConstraintViolation_RecognizesSqlServerDuplicateErrors(int number)
    {
        Assert.IsTrue(SqlServerErrorClassifier.IsUniqueConstraintViolation(number));
    }

    [TestMethod]
    public void IsUniqueConstraintViolation_RejectsOtherDatabaseErrors()
    {
        Assert.IsFalse(SqlServerErrorClassifier.IsUniqueConstraintViolation(547));
    }
}

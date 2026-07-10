using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Infrastructure.Context;

public class DbFactoryContext
{
    private readonly Func<LoafNcattingPrn232Context> _instanceFunc;
    private DbContext? _dbContext;

    public DbContext DbContext => _dbContext ??= _instanceFunc.Invoke();

    public DbFactoryContext(Func<LoafNcattingPrn232Context> dbContextFactory)
    {
        _instanceFunc = dbContextFactory;
    }
}

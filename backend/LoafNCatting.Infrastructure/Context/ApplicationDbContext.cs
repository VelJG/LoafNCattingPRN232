using LoafNCatting.Application.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Infrastructure.Context;

public class ApplicationDbContext : IApplicationDbContext
{
    private readonly DbFactoryContext _dbFactoryContext;

    public ApplicationDbContext(DbFactoryContext dbFactoryContext)
    {
        _dbFactoryContext = dbFactoryContext;
    }

    public DbContext DbContext => _dbFactoryContext.DbContext;
}

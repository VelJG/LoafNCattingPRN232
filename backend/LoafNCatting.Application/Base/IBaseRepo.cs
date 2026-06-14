using LoafNCatting.Application.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Application.Base;

public interface IBaseRepo<T> where T : class
{
    DbSet<T> Entities { get; }

    IApplicationDbContext ApplicationDbContext { get; }
}

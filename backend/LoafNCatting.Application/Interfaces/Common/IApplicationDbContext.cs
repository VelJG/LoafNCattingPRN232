using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Application.Interfaces.Common;

public interface IApplicationDbContext
{
    DbContext DbContext { get; }
}

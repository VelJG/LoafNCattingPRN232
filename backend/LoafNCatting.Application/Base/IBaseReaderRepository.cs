namespace LoafNCatting.Application.Base;

public interface IBaseReaderRepository<T> : IBaseRepo<T> where T : class
{
    Task<IList<T>> GetAllAsync();

    T? Find(params object[] keyValues);

    Task<T?> FindAsync(params object[] keyValues);
}

using Common.BaseDto;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Repository.Base;
using San.MDbLogging.Models;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace San.SqlLogging;

public class LogServiceSql<T, TContext> : RepositoryBase<T, TContext>, ILogServiceSql<T,TContext>
                                            where T : BaseSqlModel
                                            where TContext : LogDbContext<T>
{

    public async Task<int> AddAllLog(IEnumerable<T> entities)
    {
        return await AddAllAsync(entities);
    }

    public async Task<int> AddLog(T entity)
    {
        return await AddAsync(entity);
    }

    public async Task<List<T>> AddRangeLog(IEnumerable<T> entities)
    {
        return await AddRangeLog(entities);
    }
}
public interface ILogServiceSql<T, TContext> : IRepositoryBase<T, TContext>
                                                where T : BaseSqlModel 
                                                where TContext : LogDbContext<T>
{
    Task<int> AddLog(T entity);
    Task<int> AddAllLog(IEnumerable<T> entities);
    Task<List<T>> AddRangeLog(IEnumerable<T> entities);
}

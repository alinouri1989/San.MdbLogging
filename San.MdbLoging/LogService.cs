using Microsoft.Extensions.Options;
using MongoDB.Driver;
using San.MdbLogging.Models;
using System.Diagnostics;

namespace San.MdbLogging;

public class LogService<T> where T : BaseMongoModel
{
    private readonly IMongoCollection<T> _logs;

    public LogService(IOptions<LogDatabaseSettings> settings, string dbColName = null)
    {
        try
        {
            _logs = new MongoClient(settings.Value.ConnectionString).GetDatabase(settings.Value.DatabaseName).GetCollection<T>(dbColName ?? settings.Value.LogsCollectionName);
        }
        catch (Exception ex)
        {
            Debug.Fail(ex.ToString());
        }
    }

    public async Task<List<T>> Get()
    {
        return (await _logs.FindAsync((T book) => true)).ToList();
    }

    public async Task<T> Get(string id)
    {
        string id2 = id;
        return (await _logs.FindAsync((T t) => t.Id == id2)).FirstOrDefault();
    }

    public async Task<T> Create(T log)
    {
        await _logs.InsertOneAsync(log);
        return log;
    }

    public async Task Create(IEnumerable<T> logs)
    {
        await _logs.InsertManyAsync(logs);
    }

    public void Update(string id, T log)
    {
        string id2 = id;
        _logs.ReplaceOne((T t) => t.Id == id2, log);
    }

    public void Remove(T log)
    {
        T log2 = log;
        _logs.DeleteOne((T t) => t.Id == log2.Id);
    }

    public void Remove(string id)
    {
        string id2 = id;
        _logs.DeleteOne((T t) => t.Id == id2);
    }
}
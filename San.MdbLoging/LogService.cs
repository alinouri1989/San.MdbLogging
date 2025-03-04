using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoLogger.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace MongoLogger
{
    public class LogService<T> : ILogService<T> where T : BaseMongoModel
    {
        private readonly IMongoCollection<T> _logs;

        public LogService(IOptions<LogDatabaseSettings> settings, string dbColName = null)
        {
            try
            {
                var client = new MongoClient(settings.Value.ConnectionString);
                var database = client.GetDatabase(settings.Value.DatabaseName);

                _logs = database.GetCollection<T>(dbColName ?? GetCollectionName(settings));

                CreateIndexes();
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.ToString());
            }
        }

        public int GetIso8601WeekOfYear(DateTime time)
        {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
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
            try
            {
                await _logs.InsertOneAsync(log);
                return log;
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message, Newtonsoft.Json.JsonConvert.SerializeObject(ex.Message));
                throw;
            }
        }

        public async Task Create(IEnumerable<T> logs)
        {
            try
            {
                await _logs.InsertManyAsync(logs);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message, Newtonsoft.Json.JsonConvert.SerializeObject(ex.Message));
                throw;
            }
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

        private void CreateIndexes()
        {
            var indexKeysDefinition = Builders<T>.IndexKeys.Ascending(h => h.TraceCode);
            var indexKeysDefinition2 = Builders<T>.IndexKeys.Ascending(h => h.ReferenceNo);
            var indexKeysDefinition3 = Builders<T>.IndexKeys.Ascending(h => h.ExternalRefrenceCode);
            var indexKeysDefinition4 = Builders<T>.IndexKeys.Ascending(h => h.BusinessDate);

            _logs.Indexes.CreateOne(new CreateIndexModel<T>(indexKeysDefinition));
            _logs.Indexes.CreateOne(new CreateIndexModel<T>(indexKeysDefinition2));
            _logs.Indexes.CreateOne(new CreateIndexModel<T>(indexKeysDefinition3));
            _logs.Indexes.CreateOne(new CreateIndexModel<T>(indexKeysDefinition4));
        }
        private string GetCollectionName(IOptions<LogDatabaseSettings> settings)
        {
            string collectionName = settings.Value.LogsCollectionName;

            return settings.Value.RollingInterval switch
            {
                RollingInterval.none => collectionName,
                RollingInterval.Minutely => $"{collectionName}-{GetMinuteNameTemplate()}",
                RollingInterval.Hourly => $"{collectionName}-{GeHourNameTemplate()}",
                RollingInterval.Daily => $"{collectionName}-{GetDayNameTemplate()}",
                RollingInterval.Weekly => $"{collectionName}-{GetWeekNameTemplate()}",
                RollingInterval.Yearly => $"{collectionName}-{GetYearNameTemplate()}",
                RollingInterval.Monthly => $"{collectionName}-{GetMonthNameTemplate()}",
                _ => collectionName,
            };
        }
        private string GetMonthNameTemplate()
        {
            var now = DateTime.Now;
            return $"{now.Year:D4}-{now.Month:D2}";
        }
        private string GetYearNameTemplate()
        {
            var now = DateTime.Now;
            return $"{now.Year:D4}";
        }
        private string GetWeekNameTemplate()
        {
            var now = DateTime.Now;
            int weekNumber = GetIso8601WeekOfYear(now);
            return $"{now.Year:D4}-W{weekNumber:D2}";
        }
        private string GetDayNameTemplate()
        {
            var now = DateTime.Now;
            return $"{now:yyyy-MM-dd}";
        }
        private string GetMinuteNameTemplate()
        {
            var now = DateTime.Now;
            return $"{now:yyyy-MM-dd-HH-mm}";
        }
        private string GeHourNameTemplate()
        {
            var now = DateTime.Now;
            return $"{now:yyyy-MM-dd-HH}";
        }
    }

    public interface ILogService<T>
    {
        Task<List<T>> Get();
        Task<T> Get(string id);
        Task<T> Create(T log);
        Task Create(IEnumerable<T> logs);
        void Update(string id, T log);
        void Remove(T log);
        void Remove(string id);
    }
}
using System.Collections.Generic;

namespace MongoLogger.Models
{
    #region Single DataBase Log Setting
    public class LogDatabaseSettings : ILogDatabaseSettings
    {
        public string LogsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string SqlConnectionString { get; set; }
        public string SqlScriptString { get; set; }
        public string DatabaseName { get; set; }
        public string FileLogPath { get; set; }
        public bool ActiveLogFile { get; set; }
        public int BatchSize { get; set; }
        public RollingInterval RollingInterval { get; set; }
    }

    public interface ILogDatabaseSettings
    {
        public string LogsCollectionName { get; set; }
        public string SqlScriptString { get; set; }
        public string ConnectionString { get; set; }
        public string SqlConnectionString { get; set; }
        string DatabaseName { get; set; }
        public string FileLogPath { get; set; }
        public bool ActiveLogFile { get; set; }

        int BatchSize { get; set; }
        RollingInterval RollingInterval { get; set; }
    }
    #endregion

    #region MultipleDataBase Log Setting
    public class LogMultipleDatabaseSettings : ILogMultipleDatabaseSettings
    {
        public Dictionary<string, string> LogsCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public List<string> DatabaseName { get; set; }
        public int BatchSize { get; set; }
        RollingInterval RollingInterval { get; set; }
    }

    public interface ILogMultipleDatabaseSettings
    {
        Dictionary<string, string> LogsCollectionName { get; set; }
        string ConnectionString { get; set; }
        List<string> DatabaseName { get; set; }
        int BatchSize { get; set; }
    }
    public enum RollingInterval
    {
        none,
        Minutely,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Hourly
    }
    #endregion
}

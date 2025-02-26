namespace San.MdbLogging.Models;

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
    public int BatchSize { get; set; }
}

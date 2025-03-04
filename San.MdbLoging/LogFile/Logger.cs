using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using San.MdbLogging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace San.MdbLogging.LogFile
{
    internal class Logger : ILogger
    {
        public Logger(LoggerProvider Provider, string Category)
        {
            this.Provider = Provider;
            this.Category = Category;
        }

        IDisposable ILogger.BeginScope<TState>(TState state)
        {
            return Provider.ScopeProvider.Push(state);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return Provider.IsEnabled(logLevel);
        }

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if ((this as ILogger).IsEnabled(logLevel))
            {
                LogEntry Info = new LogEntry();
                Info.Category = this.Category;
                Info.Level = logLevel;
                                                                                Info.Text = exception?.Message ?? state.ToString();                 Info.Exception = exception;
                Info.EventId = eventId;
                Info.State = state;

                                if (state is string)
                {
                    Info.StateText = state.ToString();
                }
                                                                                                                else if (state is IEnumerable<KeyValuePair<string, object>> Properties)
                {
                    Info.StateProperties = new Dictionary<string, object>();

                    foreach (KeyValuePair<string, object> item in Properties)
                    {
                        Info.StateProperties[item.Key] = item.Value;
                    }
                }

                                if (Provider.ScopeProvider != null)
                {
                    Provider.ScopeProvider.ForEachScope((value, loggingProps) =>
                    {
                        if (Info.Scopes == null)
                            Info.Scopes = new List<LogScopeInfo>();

                        LogScopeInfo Scope = new LogScopeInfo();
                        Info.Scopes.Add(Scope);

                        if (value is string)
                        {
                            Scope.Text = value.ToString();
                        }
                        else if (value is IEnumerable<KeyValuePair<string, object>> props)
                        {
                            if (Scope.Properties == null)
                                Scope.Properties = new Dictionary<string, object>();

                            foreach (var pair in props)
                            {
                                Scope.Properties[pair.Key] = pair.Value;
                            }
                        }
                    },
                    state);
                }

                Provider.WriteLog(Info);
            }
        }

        public LoggerProvider Provider { get; private set; }
        public string Category { get; private set; }
    }
}

using DnsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using San.MDbLogging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace San.MdbLogging.LogFile
{
    public class FileLoggerOptions
    {
        string fFolder;
        int fMaxFileSizeInMB;
        int fRetainPolicyFileCount;

        public FileLoggerOptions()
        {
        }

        public LogLevel LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Information;

        public string Folder
        {
            get
            {
                return !string.IsNullOrWhiteSpace(fFolder) ?
                  System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location) + "\\" + fFolder : System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location);
            }
            set { fFolder = value; }
        }

        public int MaxFileSizeInMB
        {
            get { return fMaxFileSizeInMB > 0 ? fMaxFileSizeInMB : 2; }
            set { fMaxFileSizeInMB = value; }
        }

        public int RetainPolicyFileCount
        {
            get { return fRetainPolicyFileCount < 5 ? 5 : fRetainPolicyFileCount; }
            set { fRetainPolicyFileCount = value; }
        }
    }
}

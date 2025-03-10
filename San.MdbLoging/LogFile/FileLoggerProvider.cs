﻿using DnsClient;
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
    [Microsoft.Extensions.Logging.ProviderAlias("File")]
    public class FileLoggerProvider : LoggerProvider
    {
        bool Terminated;
        int Counter = 0;
        string FilePath;
        Dictionary<string, int> Lengths = new Dictionary<string, int>();

        ConcurrentQueue<LogEntry> InfoQueue = new ConcurrentQueue<LogEntry>();

        void ApplyRetainPolicy()
        {
            FileInfo FI;
            try
            {
                List<FileInfo> FileList = new DirectoryInfo(Settings.Folder)
                .GetFiles("*.log", SearchOption.TopDirectoryOnly)
                .OrderBy(fi => fi.CreationTime)
                .ToList();

                while (FileList.Count >= Settings.RetainPolicyFileCount)
                {
                    FI = FileList.First();
                    FI.Delete();
                    FileList.Remove(FI);
                }
            }
            catch
            {
            }
        }

        void WriteLine(string Text)
        {
                        Counter++;
            if (Counter % 100 == 0)
            {
                FileInfo FI = new FileInfo(FilePath);
                if (FI.Length > (1024 * 1024 * Settings.MaxFileSizeInMB))
                {
                    BeginFile();
                }
            }

            File.AppendAllText(FilePath, Text);
        }

        string Pad(string Text, int MaxLength)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return "".PadRight(MaxLength);

            if (Text.Length > MaxLength)
                return Text.Substring(0, MaxLength);

            return Text.PadRight(MaxLength);
        }

        void PrepareLengths()
        {
                        Lengths["Time"] = 24;
            Lengths["Host"] = 16;
            Lengths["User"] = 16;
            Lengths["Level"] = 14;
            Lengths["EventId"] = 32;
            Lengths["Category"] = 92;
            Lengths["Scope"] = 64;
        }

        void BeginFile()
        {
            Directory.CreateDirectory(Settings.Folder);
            FilePath = Path.Combine(Settings.Folder, LogEntry.StaticHostName +
                       "-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".log");

                        StringBuilder SB = new StringBuilder();
            SB.Append(Pad("Time", Lengths["Time"]));
            SB.Append(Pad("Host", Lengths["Host"]));
            SB.Append(Pad("User", Lengths["User"]));
            SB.Append(Pad("Level", Lengths["Level"]));
            SB.Append(Pad("EventId", Lengths["EventId"]));
            SB.Append(Pad("Category", Lengths["Category"]));
            SB.Append(Pad("Scope", Lengths["Scope"]));
            SB.AppendLine("Text");

            File.WriteAllText(FilePath, SB.ToString());

            ApplyRetainPolicy();
        }

        void WriteLogLine()
        {
            LogEntry Info = null;
            if (InfoQueue.TryDequeue(out Info))
            {
                string S;
                StringBuilder SB = new StringBuilder();
                SB.Append(Pad(Info.TimeStampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.ff"),
                          Lengths["Time"]));
                SB.Append(Pad(Info.HostName, Lengths["Host"]));
                SB.Append(Pad(Info.UserName, Lengths["User"]));
                SB.Append(Pad(Info.Level.ToString(), Lengths["Level"]));
                SB.Append(Pad(Info.EventId != null ? Info.EventId.ToString() : "",
                          Lengths["EventId"]));
                SB.Append(Pad(Info.Category, Lengths["Category"]));

                S = "";
                if (Info.Scopes != null && Info.Scopes.Count > 0)
                {
                    LogScopeInfo SI = Info.Scopes.Last();
                    if (!string.IsNullOrWhiteSpace(SI.Text))
                    {
                        S = SI.Text;
                    }
                    else
                    {
                    }
                }
                SB.Append(Pad(S, Lengths["Scope"]));

                string Text = Info.Text;

                /* writing properties is too much for a text file logger
                if (Info.StateProperties != null && Info.StateProperties.Count > 0)
                {
                    Text = Text + " Properties = " + 
                           Newtonsoft.Json.JsonConvert.SerializeObject(Info.StateProperties);
                }                
                    */

                if (!string.IsNullOrWhiteSpace(Text))
                {
                    SB.Append(Text.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " "));
                }

                SB.AppendLine();
                WriteLine(SB.ToString());
            }
        }
        void ThreadProc()
        {
            Task.Run(() =>
            {

                while (!Terminated)
                {
                    try
                    {
                        WriteLogLine();
                        System.Threading.Thread.Sleep(100);
                    }
                    catch                     {
                    }
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            Terminated = true;
            base.Dispose(disposing);
        }


        public FileLoggerProvider(IOptionsMonitor<FileLoggerOptions> Settings)
            : this(Settings.CurrentValue)
        {
                        SettingsChangeToken = Settings.OnChange(settings =>
            {
                this.Settings = settings;
            });
        }

        public FileLoggerProvider(FileLoggerOptions Settings)
        {
            PrepareLengths();
            this.Settings = Settings;

                        BeginFile();

            ThreadProc();
        }

        public override bool IsEnabled(LogLevel logLevel)
        {
            bool Result = logLevel != LogLevel.None
                && this.Settings.LogLevel != LogLevel.None
                && Convert.ToInt32(logLevel) >= Convert.ToInt32(this.Settings.LogLevel);

            return Result;
        }

        public override void WriteLog(LogEntry Info)
        {
            InfoQueue.Enqueue(Info);
        }

        internal FileLoggerOptions Settings { get; private set; }
    }
}

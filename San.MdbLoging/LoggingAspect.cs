
using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoLogger.Attributes;
using MongoLogger.BgTasks;
using MongoLogger.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MongoLogger
{
    public class LoggingAspect : OnMethodBoundaryAspect
    {
        IBackgroundTaskQueue<LogModel> _logWorker;
        LogManager<LogModel> _logManager;
        static IServiceProvider _provider;
        private HttpContext _httpContext;
        static IHttpContextAccessor _httpContextAccessor;
        static string _traceCode;

        public LoggingAspect(bool enableOutput = true, bool enableInput = true)
        {
            try
            {
                EnableInput = enableInput;
                EnableOutput = enableOutput;
                _logManager = _provider.GetRequiredService<LogManager<LogModel>>();
                _logWorker = (IBackgroundTaskQueue<LogModel>)_provider.GetService(typeof(IBackgroundTaskQueue<LogModel>));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }
        public bool EnableOutput { get; set; }
        public bool EnableInput { get; set; }
        internal static void SetServiceProvider(IServiceProvider provider)
        {
            _provider = provider;
        }
        private bool FindLastLaFrame(out int index)
        {
            index = -1;
            MethodBase method;
            List<LoggingAspect> alAttrs = new List<LoggingAspect>();
            var st = new StackTrace();
            for (int i = 1; i <= st.FrameCount; ++i)
            {
                var mt = st.GetFrame(i).GetMethod();
                var alAttr = (LoggingAspect)mt.GetCustomAttribute(typeof(LoggingAspect));

                if (alAttr != null)
                {
                    alAttrs.Add(alAttr);
                    index = i;
                }
            }
            return alAttrs.Any();
        }
        private string GetUserContextTraceCode()
        {
            var st = new StackTrace();
            if (st.GetFrame(3).GetILOffset() == StackFrame.OFFSET_UNKNOWN)
            {
                _traceCode = Guid.NewGuid().ToString();

            }

            return _traceCode;
        }
        public override void OnEntry(MethodExecutionArgs args)
        {
            try
            {
                var currentDateTime = DateTime.Now;
                string guid = null;

                setHttpContext(args.Instance);
                if (_httpContext != null)
                {
                    guid = (string)_httpContext.Items["LogGuid"];
                    if (string.IsNullOrWhiteSpace(guid))
                    {
                        guid = Guid.NewGuid().ToString();
                        _httpContext.Items.Add("LogGuid", guid);
                    }
                }

                var cEditor = new CardNoMaskEditor();
                var list = new List<object>();
                long refId = -1;
                foreach (var paramInfo in args.Method.GetParameters())
                {
                    if (paramInfo.CustomAttributes.Any(p => p.AttributeType == typeof(ReferenceIdAttribute)))
                    {
                        for (int x = 0; x < args.Arguments.Count(); x++)
                        {
                            if (args.Method.GetParameters()[x].Name == paramInfo.Name && args.Arguments.GetValue(x) != null)
                                long.TryParse(args.Arguments.GetValue(x).ToString(), out refId);
                        }
                    }
                }
                foreach (var item in args.Arguments)
                {
                    var propsWithRefAttr = item?.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(ReferenceIdAttribute))).FirstOrDefault();
                    if (propsWithRefAttr != null)
                    {
                        if (!long.TryParse(propsWithRefAttr.GetValue(item).ToString(), out refId))
                        {
                            Debug.WriteLine("ReferenceId not in correct format");
                        }
                    }
                    object editedObj = null;
                    editedObj = item.Clone();
                    list.Add(editedObj);
                }
                var argsStr = EnableInput ? JsonConvert.SerializeObject(new MessageModel { Type = "start", ClassName = args.Instance.GetType().FullName, MethodName = args.Method.Name, Input = list }, new JsonSerializerSettings { }) : "N/A";

                var Item = new LogModel
                {
                    BusinessDate = currentDateTime.Date.ToString("yyyy/MM/dd"),
                    CreateDate = currentDateTime,
                    Level = "ASPECT-ENTER",
                    Logger = nameof(LoggingAspect),
                    Message = $"🡲 {args.Method.Name} action in {args.Instance.GetType().FullName}",
                    Data = EnableInput ? argsStr : null,
                    TraceCode = guid,
                    ReferenceNo = refId
                };


                _logWorker.QueueBackgroundWorkItem(Item, async (log, ct) =>
                {
                    await _logManager.LogInternal(log);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public override void OnExit(MethodExecutionArgs args)
        {
            try
            {
                var currentDateTime = DateTime.Now;

                string guid = null;

                if (_httpContext != null)
                    guid = (string)_httpContext.Items["LogGuid"];
                if (args.ReturnValue is Task t)
                {
                    var tObj = Convert(t);
                    tObj.ContinueWith((task, s) =>
                    {
                        object retObj = null;
                        var argsC = (MethodExecutionArgs)s;
                        string jsonObj = string.Empty;
                        if (!tObj.IsFaulted && tObj.Result != null)
                        {
                            retObj = task.Result;
                            var retVal = ((ObjectResult)retObj).Value;
                            jsonObj = EnableOutput ? JsonConvert.SerializeObject(new MessageModel { Type = "End", ClassName = args.Instance.GetType().FullName, MethodName = args.Method.Name, Output = retVal }) : "N/A";
                        }
                        else
                            jsonObj = JsonConvert.SerializeObject("void");

                        long refId = -1;
                        var propsWithRefAttr = retObj?.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(ReferenceIdAttribute))).FirstOrDefault();
                        if (propsWithRefAttr != null)
                        {
                            if (!long.TryParse(propsWithRefAttr.GetValue(retObj).ToString(), out refId))
                            {
                                Debug.WriteLine("ReferenceId not in correct format");
                            }
                        }
                        var Item =
                            new LogModel
                            {
                                BusinessDate = currentDateTime.Date.ToString("yyyy/MM/dd"),
                                CreateDate = currentDateTime,
                                Level = "ASPECT-EXIT",
                                Logger = nameof(LoggingAspect),
                                Message = $"🡰 {args.Method.Name} action in {args.Instance.GetType().FullName}",
                                Data = EnableOutput ? jsonObj : null,
                                TraceCode = guid,
                                ReferenceNo = refId
                            };

                        _logWorker.QueueBackgroundWorkItem(Item, async (item, ct) =>
                        {
                            await _logManager.LogInternal(item);
                        });

                    }, args);
                }
                else
                {
                    long refId = -1;
                    var retVal = ((ObjectResult)args.ReturnValue).Value;
                    var propsWithRefAttr = args.ReturnValue?.GetType().GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(ReferenceIdAttribute))).FirstOrDefault();
                    if (propsWithRefAttr != null)
                    {
                        if (!long.TryParse(propsWithRefAttr.GetValue(args.ReturnValue).ToString(), out refId))
                        {
                            Debug.WriteLine("ReferenceId not in correct format");
                        }
                    }

                    string jsonObj = string.Empty;
                    if (retVal != null)
                    {
                        jsonObj = EnableOutput ? JsonConvert.SerializeObject(new MessageModel { Type = "End", ClassName = args.Instance.GetType().FullName, MethodName = args.Method.Name, Output = retVal }) : "N/A";
                    }
                    else
                    {
                        jsonObj = JsonConvert.SerializeObject("void");
                    }

                    var Item =
                            new LogModel
                            {
                                BusinessDate = currentDateTime.Date.ToString("yyyy/MM/dd"),
                                CreateDate = currentDateTime,
                                Level = "ASPECT-EXIT",
                                Logger = nameof(LoggingAspect),
                                Message = $"🡰 {args.Method.Name} action in {args.Instance.GetType().FullName}",
                                Data = EnableOutput ? jsonObj : null,
                                TraceCode = guid,
                                ReferenceNo = refId
                            };

                    _logWorker.QueueBackgroundWorkItem(Item, async (item, ct) =>
                    {
                        await _logManager.LogInternal(item);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public override void OnException(MethodExecutionArgs args)
        {
            try
            {

                string guid = null;
                if (_httpContext != null)
                    guid = (string)_httpContext.Items["LogGuid"];
                var currentDateTime = DateTime.Now;

                var exStr = JsonConvert.SerializeObject(args.Exception);

                var Item =
                        new LogModel
                        {
                            BusinessDate = currentDateTime.Date.ToString("yyyy/MM/dd"),
                            CreateDate = currentDateTime,
                            Level = "ASPECT-ERROR",
                            Logger = nameof(LoggingAspect),
                            Message = $"⚡ Exception on {args.Method.Name} action in {args.Instance.GetType().FullName}",
                            Data = JsonConvert.SerializeObject(new MessageModel { Type = "Exception", ClassName = args.Instance.GetType().FullName, MethodName = args.Method.Name }),
                            Exception = args.Exception,
                            TraceCode = guid,
                            ExStr = args.Exception == null ? string.Empty : JsonConvert.SerializeObject(args.Exception)
                        };

                _logWorker.QueueBackgroundWorkItem(Item, async (item, ct) =>
                {
                    await _logManager.LogInternal(item);
                });

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        private async Task<object> Convert(Task task)
        {
            await task;
            var voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
            if (voidTaskType.IsAssignableFrom(task.GetType()))
                return null;

            var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return null;

            return property.GetValue(task);
        }
        private void setHttpContext(object instance)
        {
            var hcon = instance.GetType().GetProperty("HttpContext");
            if (hcon != null && _httpContext == null)
            {
                _httpContext = (HttpContext)hcon.GetValue(instance);
            }
            else if (_httpContextAccessor != null)
                _httpContext = _httpContextAccessor.HttpContext;
        }
        internal static void SetHttpAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
    }
}

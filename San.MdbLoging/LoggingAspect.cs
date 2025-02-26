using MethodBoundaryAspect.Fody.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using San.MdbLogging.Attributes;
using San.MdbLogging.BgTasks;
using San.MdbLogging.Models;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;

namespace San.MdbLogging;

public class LoggingAspect : OnMethodBoundaryAspect
{
    private IBackgroundTaskQueue<LogModel> _logWorker;

    private LogManager<LogModel> _logManager;

    private static IServiceProvider _provider;

    private HttpContext _httpContext;

    private static IHttpContextAccessor _httpContextAccessor;

    private static string _traceCode;

    public bool EnableOutput { get; set; }

    public bool EnableInput { get; set; }

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

    internal static void SetServiceProvider(IServiceProvider provider)
    {
        _provider = provider;
    }

    private bool FindLastLaFrame(out int index)
    {
        index = -1;
        List<LoggingAspect> list = new List<LoggingAspect>();
        StackTrace stackTrace = new StackTrace();
        for (int i = 1; i <= stackTrace.FrameCount; i++)
        {
            LoggingAspect loggingAspect = (LoggingAspect)stackTrace.GetFrame(i).GetMethod().GetCustomAttribute(typeof(LoggingAspect));
            if (loggingAspect != null)
            {
                list.Add(loggingAspect);
                index = i;
            }
        }

        return list.Any();
    }

    private string GetUserContextTraceCode()
    {
        if (new StackTrace().GetFrame(3).GetILOffset() == -1)
        {
            _traceCode = Guid.NewGuid().ToString();
        }

        return _traceCode;
    }

    public override void OnEntry(MethodExecutionArgs args)
    {
        try
        {
            DateTime now = DateTime.Now;
            string text = null;
            Claim claim = null;
            setHttpContext(args.Instance);
            if (_httpContext != null)
            {
                claim = _httpContext.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                text = (string)_httpContext.Items["LogGuid"];
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = Guid.NewGuid().ToString();
                    _httpContext.Items.Add("LogGuid", text);
                }
            }

            CardNoMaskEditor cardNoMaskEditor = new CardNoMaskEditor();
            List<object> list = new List<object>();
            long result = -1L;
            ParameterInfo[] parameters = args.Method.GetParameters();
            foreach (ParameterInfo parameterInfo in parameters)
            {
                if (!parameterInfo.CustomAttributes.Any((CustomAttributeData p) => p.AttributeType == typeof(ReferenceIdAttribute)))
                {
                    continue;
                }

                for (int j = 0; j < args.Arguments.Count(); j++)
                {
                    if (args.Method.GetParameters()[j].Name == parameterInfo.Name && args.Arguments.GetValue(j) != null)
                    {
                        long.TryParse(args.Arguments.GetValue(j).ToString(), out result);
                    }
                }
            }

            object[] arguments = args.Arguments;
            foreach (object obj in arguments)
            {
                PropertyInfo propertyInfo = (from pi in obj?.GetType().GetProperties()
                                             where Attribute.IsDefined(pi, typeof(ReferenceIdAttribute))
                                             select pi).FirstOrDefault();
                if (propertyInfo != null && !long.TryParse(propertyInfo.GetValue(obj).ToString(), out result))
                {
                    Debug.WriteLine("ReferenceId not in correct format");
                }

                object item2 = obj.Clone();
                list.Add(item2);
            }

            string text2 = (EnableInput ? JsonConvert.SerializeObject(new MessageModel
            {
                Type = "start",
                ClassName = args.Instance.GetType().FullName,
                MethodName = args.Method.Name,
                Input = list
            }, new JsonSerializerSettings()) : "N/A");
            string text3 = text2;
            LogModel logModel = new LogModel();
            logModel.User = claim?.Value;
            logModel.BusinessDate = now.Date.ToString("yyyy/MM/dd");
            logModel.CreateDate = now;
            logModel.Level = "ASPECT-ENTER";
            logModel.Logger = "LoggingAspect";
            logModel.Message = "\ud83e\udc72 " + args.Method.Name + " action in " + args.Instance.GetType().FullName;
            logModel.Data = (EnableInput ? text3 : null);
            logModel.TraceCode = text;
            logModel.ReferenceNo = result;
            _logWorker.QueueBackgroundWorkItem(logModel, async (log, ct) =>
            {
                await _logManager.LogInternal(log);
            });

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public override void OnExit(MethodExecutionArgs args)
    {
        MethodExecutionArgs args2 = args;
        Console.WriteLine("Pigi Logger: OnExit!");
        try
        {
            DateTime currentDateTime = DateTime.Now;
            string guid = null;
            Claim claim = null;
            if (_httpContext != null)
            {
                claim = (claim = _httpContext.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));
                guid = (string)_httpContext.Items["LogGuid"];
            }

            if (args2.ReturnValue is Task task2)
            {
                Task<object> tObj = Convert(task2);
                tObj.ContinueWith(delegate (Task<object> task, object? s)
                {
                    string empty2 = string.Empty;
                    string text4;
                    if (!tObj.IsFaulted && tObj.Result != null)
                    {
                        object result2 = task.Result;
                        string text3 = (EnableOutput ? JsonConvert.SerializeObject(new MessageModel
                        {
                            Type = "End",
                            ClassName = args2.Instance.GetType().FullName,
                            MethodName = args2.Method.Name,
                            Output = result2
                        }) : "N/A");
                        text4 = text3;
                    }
                    else
                    {
                        text4 = JsonConvert.SerializeObject("void");
                    }

                    long result3 = -1L;
                    ParameterInfo[] parameters2 = args2.Method.GetParameters();
                    foreach (ParameterInfo parameterInfo2 in parameters2)
                    {
                        if (parameterInfo2.CustomAttributes.Any((CustomAttributeData p) => p.AttributeType == typeof(ReferenceIdAttribute)))
                        {
                            for (int m = 0; m < args2.Arguments.Count(); m++)
                            {
                                if (args2.Method.GetParameters()[m].Name == parameterInfo2.Name && args2.Arguments.GetValue(m) != null)
                                {
                                    long.TryParse(args2.Arguments.GetValue(m).ToString(), out result3);
                                }
                            }
                        }
                    }

                    object[] arguments2 = args2.Arguments;
                    foreach (object obj2 in arguments2)
                    {
                        PropertyInfo propertyInfo2 = (from pi in obj2?.GetType().GetProperties()
                                                      where Attribute.IsDefined(pi, typeof(ReferenceIdAttribute))
                                                      select pi).FirstOrDefault();
                        if (propertyInfo2 != null && !long.TryParse(propertyInfo2.GetValue(obj2).ToString(), out result3))
                        {
                            Debug.WriteLine("ReferenceId not in correct format");
                        }
                    }
                    var logModel = new LogModel
                    {
                        BusinessDate = currentDateTime.Date.ToString("yyyy/MM/dd"),
                        CreateDate = currentDateTime,
                        Level = "ASPECT-EXIT",
                        Logger = "LoggingAspect",
                        Message = "\ud83e\udc70 " + args2.Method.Name + " action in " + args2.Instance.GetType().FullName,
                        Data = (EnableOutput ? text4 : null),
                        TraceCode = guid,
                        ReferenceNo = result3
                    };

                    _logWorker.QueueBackgroundWorkItem(logModel, async (log, ct) =>
                    {
                        await _logManager.LogInternal(log);
                    });
                }, args2);
                return;
            }

            long result = -1L;
            object returnValue = args2.ReturnValue;
            ParameterInfo[] parameters = args2.Method.GetParameters();
            foreach (ParameterInfo parameterInfo in parameters)
            {
                if (!parameterInfo.CustomAttributes.Any((CustomAttributeData p) => p.AttributeType == typeof(ReferenceIdAttribute)))
                {
                    continue;
                }

                for (int j = 0; j < args2.Arguments.Count(); j++)
                {
                    if (args2.Method.GetParameters()[j].Name == parameterInfo.Name && args2.Arguments.GetValue(j) != null)
                    {
                        long.TryParse(args2.Arguments.GetValue(j).ToString(), out result);
                    }
                }
            }

            object[] arguments = args2.Arguments;
            foreach (object obj in arguments)
            {
                PropertyInfo propertyInfo = (from pi in obj?.GetType().GetProperties()
                                             where Attribute.IsDefined(pi, typeof(ReferenceIdAttribute))
                                             select pi).FirstOrDefault();
                if (propertyInfo != null && !long.TryParse(propertyInfo.GetValue(obj).ToString(), out result))
                {
                    Debug.WriteLine("ReferenceId not in correct format");
                }
            }

            string empty = string.Empty;
            string text2;
            if (returnValue != null)
            {
                string text = (EnableOutput ? JsonConvert.SerializeObject(new MessageModel
                {
                    Type = "End",
                    ClassName = args2.Instance.GetType().FullName,
                    MethodName = args2.Method.Name,
                    Output = returnValue
                }) : "N/A");
                text2 = text;
            }
            else
            {
                text2 = JsonConvert.SerializeObject("void");
            }

            LogModel logModel = new LogModel();
            logModel.User = claim?.Value;
            logModel.BusinessDate = currentDateTime.Date.ToString("yyyy/MM/dd");
            logModel.CreateDate = currentDateTime;
            logModel.Level = "ASPECT-EXIT";
            logModel.Logger = "LoggingAspect";
            logModel.Message = "\ud83e\udc70 " + args2.Method.Name + " action in " + args2.Instance.GetType().FullName;
            logModel.Data = (EnableOutput ? text2 : null);
            logModel.TraceCode = guid;
            logModel.ReferenceNo = result;

            _logWorker.QueueBackgroundWorkItem(logModel, async (log, ct) =>
            {
                await _logManager.LogInternal(log);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public override void OnException(MethodExecutionArgs args)
    {
        try
        {
            string traceCode = null;
            Claim claim = null;
            if (_httpContext != null)
            {
                claim = (claim = _httpContext.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"));
                traceCode = (string)_httpContext.Items["LogGuid"];
            }

            DateTime now = DateTime.Now;
            JsonConvert.SerializeObject(args.Exception);
            LogModel logModel = new LogModel();
            logModel.User = claim?.Value;
            logModel.BusinessDate = now.Date.ToString("yyyy/MM/dd");
            logModel.CreateDate = now;
            logModel.Level = "ASPECT-ERROR";
            logModel.Logger = "LoggingAspect";
            logModel.Message = "⚡ Exception on " + args.Method.Name + " action in " + args.Instance.GetType().FullName;
            logModel.Data = JsonConvert.SerializeObject(new MessageModel
            {
                Type = "Exception",
                ClassName = args.Instance.GetType().FullName,
                MethodName = args.Method.Name
            });
            logModel.Exception = args.Exception;
            logModel.TraceCode = traceCode;
            logModel.ExStr = ((args.Exception == null) ? string.Empty : JsonConvert.SerializeObject(args.Exception));

            _logWorker.QueueBackgroundWorkItem(logModel, async (log, ct) =>
            {
                await _logManager.LogInternal(log);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task<object> Convert(Task task)
    {
        await task;
        Type voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
        if (voidTaskType.IsAssignableFrom(task.GetType()))
        {
            return null;
        }

        PropertyInfo property = task.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public);
        return (!(property == null)) ? property.GetValue(task) : null;
    }

    private void setHttpContext(object instance)
    {
        PropertyInfo property = instance.GetType().GetProperty("HttpContext");
        if (property != null && _httpContext == null)
        {
            _httpContext = (HttpContext)property.GetValue(instance);
        }
        else if (_httpContextAccessor != null)
        {
            _httpContext = _httpContextAccessor.HttpContext;
        }
    }

    internal static void SetHttpAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
}

using log4net.Appender;
using log4net.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LinhPDTestService.log4net
{
    public class RollingFilePoolAppender : RollingFileAppender
    {
        private readonly SemaphoreSlim _signal = new(0, 1);
        private readonly ConcurrentQueue<LoggingEvent> _loggingEvents = new();
        public RollingFilePoolAppender()
        {
            new Thread(async () =>
            {
                try
                {
                    while (true)
                    {
                        try
                        {
                            if (_loggingEvents.IsEmpty) await _signal.WaitAsync();
                            while (!_loggingEvents.IsEmpty)
                            {
                                if (_loggingEvents.TryDequeue(out LoggingEvent loggingEvent))
                                {
                                    base.Append(loggingEvent);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogUtil.CreateLogger().LogError(ex, "");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.CreateLogger().LogError(ex, "");
                }
            }).Start();
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (LoggingEvent loggingEvent in loggingEvents)
                Append(loggingEvent);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            _ = loggingEvent.LocationInformation.ClassName;
            _loggingEvents.Enqueue(loggingEvent);
            if (_signal.CurrentCount == 0) _signal.Release();
        }
    }
}

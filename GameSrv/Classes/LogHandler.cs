using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;

namespace RandM.GameSrv {
    class LogHandler : IDisposable {
        private List<string> _Log = new List<string>();
        private object _LogLock = new object();
        private Timer _LogTimer = new Timer();
        private string _TimeFormat = "";

        public LogHandler(string timeFormat) {
            this._TimeFormat = timeFormat;

            // Ensure the log directory exists
            Directory.CreateDirectory(StringUtils.PathCombine(ProcessUtils.StartupPath, "logs"));

            _LogTimer.Interval = 60000; // 1 minute
            _LogTimer.Elapsed += LogTimer_Elapsed;
            _LogTimer.Start();

            if (Helpers.Debug) {
                RMLog.Level = LogLevel.Debug;
            }
            RMLog.Handler += RMLog_Handler;
        }

        private void AddToLog(string logMessage) {
            lock (_LogLock) {
                _Log.Add(DateTime.Now.ToString(_TimeFormat) + "  " + logMessage);
            }
        }

        private void FlushLog() {
            lock (_LogLock) {
                // Flush log to disk
                if (_Log.Count > 0) {
                    try {
                        FileUtils.FileAppendAllText(StringUtils.PathCombine(ProcessUtils.StartupPath, "logs", "gamesrv.log"), string.Join(Environment.NewLine, _Log.ToArray()) + Environment.NewLine);
                        _Log.Clear();
                    } catch (Exception ex) {
                        RMLog.Exception(ex, "Unable to update gamesrv.log");
                    }
                }
            }
        }

        void LogTimer_Elapsed(object sender, ElapsedEventArgs e) {
            _LogTimer.Stop();
            FlushLog();
            _LogTimer.Start();
        }

        private void RMLog_Handler(object sender, RMLogEventArgs e) {
            AddToLog($"[{e.Level.ToString()}] {e.Message}");
        }

        #region IDisposable Support
        private bool _Disposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // Dispose managed state (managed objects).
                    if (_LogTimer != null) {
                        _LogTimer.Stop();
                        _LogTimer.Dispose();
                    }
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                _Disposed = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~LogHandler() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}

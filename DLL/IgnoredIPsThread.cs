using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RandM.GameSrv
{
    class IgnoredIPsThread : RMThread, IDisposable
    {
        private bool _Disposed = false;

        public event EventHandler<ExceptionEventArgs> ExceptionEvent = null;

        ~IgnoredIPsThread()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_Disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                _Disposed = true;
            }
        }
        
        protected override void Execute()
        {
            while (!_Stop)
            {
                string IgnoredIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips.txt");
                string CombinedFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-combined.txt");
                string StatusCakeFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-statuscake.txt");
                string UptimeRobotFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-uptimerobot.txt");

                // Get the list of servers from StatusCake
                try
                {
                    string IPs = WebUtils.HttpGet("https://www.statuscake.com/API/Locations/txt");
                    IPs = IPs.Replace("\r\n", "CRLF").Replace("\n", "\r\n").Replace("CRLF", "\r\n");
                    FileUtils.FileWriteAllText(StatusCakeFileName, IPs);
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Unable to download https://www.statuscake.com/API/Locations/txt", ex);
                }
                
                // Get the list of servers from UptimeRobot
                try
                {
                    string Locations = WebUtils.HttpGet("http://uptimerobot.com/locations");
                    var Matches = Regex.Matches(Locations, @"[<]li[>](\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");
                    
                    List<string> IPs = new List<string>();
                    foreach (Match M in Matches) {
                        IPs.Add(M.Groups[1].Value);
                    }

                    FileUtils.FileWriteAllText(UptimeRobotFileName, string.Join("\r\n", IPs.ToArray()));
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Unable to download https://www.statuscake.com/API/Locations/txt", ex);
                }

                // Combine the lists
                try
                {
                    FileUtils.FileWriteAllText(CombinedFileName, "");
                    if (File.Exists(IgnoredIPsFileName)) FileUtils.FileAppendAllText(CombinedFileName, FileUtils.FileReadAllText(IgnoredIPsFileName));
                    if (File.Exists(StatusCakeFileName)) FileUtils.FileAppendAllText(CombinedFileName, FileUtils.FileReadAllText(StatusCakeFileName));
                    if (File.Exists(UptimeRobotFileName)) FileUtils.FileAppendAllText(CombinedFileName, FileUtils.FileReadAllText(UptimeRobotFileName));
                }
                catch (Exception ex)
                {
                    RaiseExceptionEvent("Unable to combine Ignored IPs lists", ex);
                }

                // Get the user
                _StopEvent.WaitOne(3600000); // Wait for one hour before updating again
            }
        }

        private void RaiseExceptionEvent(string message, Exception exception)
        {
            EventHandler<ExceptionEventArgs> Handler = ExceptionEvent;
            if (Handler != null) Handler(this, new ExceptionEventArgs(message, exception));
        }
    }
}

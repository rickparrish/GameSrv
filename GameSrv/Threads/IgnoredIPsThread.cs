using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RandM.GameSrv {
    class IgnoredIPsThread : RMThread {
        private static IgnoredIPsThread _IgnoredIPsThread = null;

        protected override void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects)
                // set large fields to null.

                // Call the base dispose
                base.Dispose(disposing);
            }
        }

        protected override void Execute() {
            while (!_Stop) {
                string IgnoredIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips.txt");
                string CombinedFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-combined.txt");
                string StatusCakeFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-statuscake.txt");
                string UptimeRobotFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-uptimerobot.txt");

                // Get the list of servers from StatusCake
                try {
                    string IPs = WebUtils.HttpGet("https://www.statuscake.com/API/Locations/txt");
                    IPs = IPs.Replace("\r\n", "CRLF").Replace("\n", "\r\n").Replace("CRLF", "\r\n");
                    FileUtils.FileWriteAllText(StatusCakeFileName, IPs);
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Unable to download https://www.statuscake.com/API/Locations/txt");
                }

                // Get the list of servers from UptimeRobot
                try {
                    string Locations = WebUtils.HttpGet("http://uptimerobot.com/locations");
                    var Matches = Regex.Matches(Locations, @"[<]li[>](\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})");

                    List<string> IPs = new List<string>();
                    foreach (Match M in Matches) {
                        IPs.Add(M.Groups[1].Value);
                    }

                    FileUtils.FileWriteAllText(UptimeRobotFileName, string.Join("\r\n", IPs.ToArray()));
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Unable to download https://www.statuscake.com/API/Locations/txt");
                }

                // Combine the lists
                try {
                    FileUtils.FileWriteAllText(CombinedFileName, "");
                    if (File.Exists(IgnoredIPsFileName)) FileUtils.FileAppendAllText(CombinedFileName, FileUtils.FileReadAllText(IgnoredIPsFileName));
                    if (File.Exists(StatusCakeFileName)) FileUtils.FileAppendAllText(CombinedFileName, FileUtils.FileReadAllText(StatusCakeFileName));
                    if (File.Exists(UptimeRobotFileName)) FileUtils.FileAppendAllText(CombinedFileName, FileUtils.FileReadAllText(UptimeRobotFileName));
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Unable to combine Ignored IPs lists");
                }

                // Wait for one hour before updating again
                if (!_Stop && (_StopEvent != null)) _StopEvent.WaitOne(3600000);
            }
        }

        public static bool StartThread() {
            RMLog.Info("Starting Ignored IPs Thread");

            try {
                // Create Ignored IPs Thread and Thread objects
                _IgnoredIPsThread = new IgnoredIPsThread();
                _IgnoredIPsThread.Start();
                return true;
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in GameSrv::StartIgnoredIPsThread()");
                return false;
            }
        }

        public static bool StopThread() {
            if (_IgnoredIPsThread != null) {
                RMLog.Info("Stopping Ignored IPs Thread");

                try {
                    _IgnoredIPsThread.Stop();
                    _IgnoredIPsThread.Dispose();
                    _IgnoredIPsThread = null;

                    return true;
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in GameSrv::StopIgnoredIPsThread()");
                    return false;
                }
            } else {
                return true;
            }
        }
    }
}

using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RandM.GameSrv {
    class TimedEventsThread : RMThread {
        private int _ActiveOfflineEvents = 0;
        private static TimedEventsThread _TimedEventsThread = null;
        private static List<TimedEvent> _TimedEvents = new List<TimedEvent>();

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
            // Load the events into memory
            var EventNames = TimedEvent.GetEventNames();
            foreach (var EventName in EventNames) {
                _TimedEvents.Add(new TimedEvent(EventName));
            }

            while (!_Stop) {
                // Get the current day and time, which we'll compare to the list of events in memory
                string CurrentDay = DateTime.Now.DayOfWeek.ToString();
                string CurrentTime = DateTime.Now.ToString("HH:mm");

                // Get matching events
                var EventsToRun = _TimedEvents.Where(x => x.Days.Contains(CurrentDay) && x.Time == CurrentTime);
                foreach (var EventToRun in EventsToRun) {
                    // Check if we need to go offline for this event
                    if (EventToRun.GoOffline) {
                        RMLog.Info("Going offline to run event '" + EventToRun.Name + "'");
                        _ActiveOfflineEvents += 1;
                        // TODOX Raise event to take GameSrv offline
                    } else {
                        RMLog.Info("Running event '" + EventToRun.Name + "'");
                    }

                    // Execute the event
                    ProcessStartInfo PSI = new ProcessStartInfo(EventToRun.Command) {
                        WindowStyle = EventToRun.WindowStyle,
                        WorkingDirectory = ProcessUtils.StartupPath,
                    };
                    var P = RMProcess.Start(PSI);

                    // TODOX Need to get notification of the event completing so we can go back online
                }

                // Wait until the next minute rolls around to try again
                if (!_Stop && (_StopEvent != null)) _StopEvent.WaitOne((61 - DateTime.Now.Second) * 1000);
            }
        }

        public static int MinutesToNextEvent() {
            // TODOX Should check how many minutes until the next event
            // TODOX When a user logs on, this method should be called so their time can be reduced accordingly
            // TODOX   ie if next event is in 35 minutes, user should only be given 34 minutes or something like that
            return Int32.MaxValue;
        }

        public static void StartThread() {
            RMLog.Info("Starting Timed Events Thread");

            try {
                // Create Ignored IPs Thread and Thread objects
                _TimedEventsThread = new TimedEventsThread();
                _TimedEventsThread.Start();
            } catch (Exception ex) {
                RMLog.Exception(ex, "Error in TimedEventsThread::StartThread()");
            }
        }

        public static void StopThread() {
            RMLog.Info("Stopping Timed Events Thread");

            if (_TimedEventsThread != null) {
                try {
                    _TimedEventsThread.Stop();
                    _TimedEventsThread.Dispose();
                    _TimedEventsThread = null;
                } catch (Exception ex) {
                    RMLog.Exception(ex, "Error in TimedEventsThread::StartThread()");
                }
            }
        }
    }
}

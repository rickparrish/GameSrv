using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    public class TimedEvent : ConfigHelper {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Days { get; set; }
        public string Time { get; set; }
        public bool GoOffline { get; set; }
        public ProcessWindowStyle WindowStyle { get; set; }

        private TimedEvent() {
            // Don't let the user instantiate this without a constructor
        }

        public TimedEvent(string eventName)
            : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "timed-events.ini")) {
            Name = "";
            Command = "";
            Days = "";
            Time = "";
            GoOffline = false;
            WindowStyle = ProcessWindowStyle.Normal;

            Load(eventName);
        }

        public static string[] GetEventNames() {
            using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("config", "timed-events.ini")))) {
                // Return all the sections in timed-events.ini
                return Ini.ReadSections().ToArray();
            }
        }
    }
}

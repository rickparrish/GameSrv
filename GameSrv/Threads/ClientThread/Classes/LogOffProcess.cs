using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    public class LogOffProcess : ConfigHelper {
        public string Name { get; set; }
        public Action Action { get; set; }
        public string Parameters { get; set; }
        public int RequiredAccess { get; set; }

        private LogOffProcess() {
            // Don't let the user instantiate this without a constructor
        }

        public LogOffProcess(string section)
            : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "logoffprocess.ini")) {
            Name = "";
            Action = Action.None;
            Parameters = "";
            RequiredAccess = 0;

            Load(section);
        }

        public static string[] GetProcesses() {
            using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "logoffprocess.ini"))) {
                return Ini.ReadSections();
            }
        }

        public static void Run(ClientThread clientThread) {
            if (clientThread == null) {
                throw new ArgumentNullException("clientThread");
            }

            // Loop through the options, and run the ones we allow here
            bool ExitFor = false;
            string[] Processes = GetProcesses();
            for (int i = 0; i < Processes.Length; i++) {
                try {
                    LogOffProcess LP = new LogOffProcess(Processes[i]);
                    if ((LP.Loaded) && (!clientThread.QuitThread())) {
                        switch (LP.Action) {
                            case Action.Disconnect:
                            case Action.DisplayFile:
                            case Action.DisplayFileMore:
                            case Action.DisplayFilePause:
                            case Action.Pause:
                            case Action.RunDoor:
                                MenuOption MO = new MenuOption("", '\0');
                                MO.Action = LP.Action;
                                MO.Name = LP.Name;
                                MO.Parameters = LP.Parameters;
                                MO.RequiredAccess = LP.RequiredAccess;
                                ExitFor = clientThread.HandleMenuOption(MO);
                                break;
                        }
                        if (ExitFor) {
                            break;
                        }
                    }
                } catch (ArgumentException aex) {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RMLog.Exception(aex, "Error during logoff process '" + Processes[i] + "'");
                }
            }
        }
    }

}

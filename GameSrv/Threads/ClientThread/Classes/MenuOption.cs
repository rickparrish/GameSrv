using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    public class MenuOption : ConfigHelper {
        public string Name { get; set; }
        public Action Action { get; set; }
        public string Parameters { get; set; }
        public int RequiredAccess { get; set; }

        private MenuOption() {
            // Don't let the user instantiate this without a constructor
        }

        public MenuOption(string menu, char hotkey)
            : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("menus", menu.ToLower() + ".ini")) {
            Name = "";
            Action = Action.None;
            Parameters = "";
            RequiredAccess = 0;

            Load(hotkey.ToString());
        }

        public static string[] GetHotkeys(string menu) {
            using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("menus", menu.ToLower() + ".ini")))) {
                return Ini.ReadSections();
            }
        }
    }
}

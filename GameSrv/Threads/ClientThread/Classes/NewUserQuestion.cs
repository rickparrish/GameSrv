using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RandM.GameSrv {
    public class NewUserQuestion : ConfigHelper {
        public bool Confirm { get; set; }
        public bool Required { get; set; }
        public ValidationType Validate { get; set; }

        private NewUserQuestion() {
            // Don't let the user instantiate this without a constructor
        }

        public NewUserQuestion(string question)
            : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "newuser.ini")) {
            Confirm = false;
            Required = false;
            Validate = ValidationType.None;

            Load(question);
        }

        public static string[] GetQuestions() {
            using (IniFile Ini = new IniFile(StringUtils.PathCombine(ProcessUtils.StartupPath, StringUtils.PathCombine("config", "newuser.ini")))) {
                // Return all the sections in newuser.ini, except for [alias] and [password] since they're reserved
                return Ini.ReadSections().Where(x => (x.ToLower() != "alias") && (x.ToLower() != "password")).ToArray();
            }
        }
    }
}

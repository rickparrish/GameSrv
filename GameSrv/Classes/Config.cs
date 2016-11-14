/*
  GameSrv: A BBS Door Game Server
  Copyright (C) 2002-2014  Rick Parrish, R&M Software

  This file is part of GameSrv.

  GameSrv is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  any later version.

  GameSrv is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with GameSrv.  If not, see <http://www.gnu.org/licenses/>.
*/
using System.Collections.Generic;
using System.Globalization;
using RandM.RMLib;
using System;
using System.Diagnostics;

namespace RandM.GameSrv {
    // TODOX Create a Config.Default singleton?
    public class Config : ConfigHelper {
        public string BBSName { get; set; }
        public int FirstNode { get; set; }
        public int LastNode { get; set; }
        public int NextUserId { get; set; }
        public string NewUserPassword { get; set; }
        public string PasswordPepper { get; set; }
        public bool RLoginPromptForCredentialsOnFailedLogin { get; set; }
        public string RLoginServerIP { get; set; }
        public int RLoginServerPort { get; set; }
        public bool RLoginSkipNewUserPrompts { get; set; }
        public bool RLoginValidatePassword { get; set; }
        public string SysopEmail { get; set; }
        public string SysopFirstName { get; set; }
        public string SysopLastName { get; set; }
        public string TelnetServerIP { get; set; }
        public int TelnetServerPort { get; set; }
        public TerminalType TerminalType { get; set; }
        public string TimeFormatLog { get; set; }
        public string TimeFormatUI { get; set; }
        public int TimePerCall { get; set; }
        public string UnixUser { get; set; }
        public string WebSocketServerIP { get; set; }
        public int WebSocketServerPort { get; set; }

        public Config()
            : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("config", "gamesrv.ini")) {
            BBSName = "New GameSrv BBS";
            FirstNode = 1;
            LastNode = 5;
            NextUserId = 1;
            NewUserPassword = "";
            PasswordPepper = Debugger.IsAttached ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : StringUtils.RandomString(100);
            RLoginPromptForCredentialsOnFailedLogin = false;
            RLoginServerIP = "0.0.0.0";
            RLoginServerPort = 513;
            RLoginSkipNewUserPrompts = true;
            RLoginValidatePassword = false;
            SysopEmail = "root@localhost";
            SysopFirstName = "New";
            SysopLastName = "Sysop";
            TelnetServerIP = "0.0.0.0";
            TelnetServerPort = 23;
            TerminalType = TerminalType.ANSI;
            TimeFormatLog = "G";
            TimeFormatUI = "T";
            TimePerCall = 60;
            UnixUser = "gamesrv";
            WebSocketServerIP = "0.0.0.0";
            WebSocketServerPort = 1123;

            Load();

            if (base.Loaded) {
                // Check for blank pepper (means it was the first time the config was loaded, and there's no value yet)
                if (string.IsNullOrEmpty(PasswordPepper)) {
                    PasswordPepper = Debugger.IsAttached ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : StringUtils.RandomString(100);
                    Save();
                }
            }
        }

        public string ServerPorts {
            get {
                List<string> Result = new List<string>();
                if (RLoginServerPort > 0) Result.Add(RLoginServerPort.ToString());
                if (TelnetServerPort > 0) Result.Add(TelnetServerPort.ToString());
                if (WebSocketServerPort > 0) Result.Add(WebSocketServerPort.ToString());
                return string.Join(",", Result.ToArray());
            }
        }

        public new void Save() {
            base.Save();
        }
    }
}

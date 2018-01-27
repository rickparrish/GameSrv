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

 
  This file also contains portions of the Synchronet BBS software
  that have been ported from the original C code to C#.  Those portions are
  Copyright Rob Swindell - http://www.synchro.net/copyright.html
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using RandM.RMLib;
using Unix;
using System.Net.Sockets;
using System.Timers;
using System.Linq;
using System.Text.RegularExpressions;

namespace RandM.GameSrv {
    public class ClientThread : RMThread {
        private Config _Config = new Config();
        private string _CurrentMenu;
        private Dictionary<char, MenuOption> _CurrentMenuOptions = new Dictionary<char, MenuOption>();
        private string _LastDisplayFile = "";
        private NodeInfo _NodeInfo = new NodeInfo();
        private Random _R = new Random();
        private string _Status = "";

        // TODOZ Add a Disconnect event of some sort to allow a sysop to disconnect another node
        public event EventHandler<NodeEventArgs> NodeEvent = null;
        public event EventHandler<WhoIsOnlineEventArgs> WhoIsOnlineEvent = null; // TODOX Gotta be a better way to get

        public ClientThread(TcpConnection connection, ConnectionType connectionType, TerminalType terminalType) {
            _NodeInfo.Connection = connection;
            _NodeInfo.ConnectionType = connectionType;
            _NodeInfo.TerminalType = terminalType;
        }

        protected override void Dispose(bool disposing) {
            if (!_Disposed) {
                if (disposing) {
                    // dispose managed state (managed objects).
                    if (_NodeInfo.Connection != null) _NodeInfo.Connection.Dispose();
                }

                // free unmanaged resources (unmanaged objects)
                // set large fields to null.

                // Call the base dispose
                base.Dispose(disposing);
            }
        }

        public string Alias {
            get { return (_NodeInfo.User.Alias == null) ? "" : _NodeInfo.User.Alias; }
        }

        private bool AuthenticateRLogin() {
            string UserName = ((RLoginConnection)_NodeInfo.Connection).ServerUserName;
            string Password = ((RLoginConnection)_NodeInfo.Connection).ClientUserName;
            string TerminalType = ((RLoginConnection)_NodeInfo.Connection).TerminalType;

            if (string.IsNullOrEmpty(UserName) || string.IsNullOrEmpty(Password)) {
                // RLogin requires both fields
                if (_Config.RLoginPromptForCredentialsOnFailedLogOn) {
                    return AuthenticateTelnet();
                } else {
                    DisplayAnsi("RLOGIN_INVALID");
                    return false;
                }
            } else {
                // Check if we're requesting a door
                if (TerminalType.ToLower().StartsWith("xtrn=")) {
                    _NodeInfo.Door = new DoorInfo(TerminalType.Substring(5)); // 5 = strip off leading xtrn=
                    if (!_NodeInfo.Door.Loaded) {
                        // Requested door was not found
                        DisplayAnsi("RLOGIN_INVALID_XTRN");
                        return false;
                    }
                }

                // Check if the username is valid
                _NodeInfo.User = new UserInfo(UserName);
                if (_NodeInfo.User.Loaded) {
                    // Yep, so validate the password
                    if (_Config.RLoginValidatePassword) {
                        if (!_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper)) {
                            // Password is bad
                            if (_Config.RLoginPromptForCredentialsOnFailedLogOn) {
                                return AuthenticateTelnet();
                            } else {
                                DisplayAnsi("RLOGIN_INVALID_PASSWORD");
                                return false;
                            }
                        }
                    }
                } else if (_Config.RLoginPromptForCredentialsOnFailedLogOn) {
                    // Nope, and we want to prompt for credentials when the alias isn't found
                    return AuthenticateTelnet();
                } else {
                    // Nope, so perform the new user process with given username and password
                    if (_Config.RLoginSkipNewUserPrompts) {
                        // We're going to just directly register the user since sysop wants to skip the prompts
                        if (IsBannedUser(UserName)) {
                            RMLog.Warning("RLogin user not allowed due to banned alias: '" + UserName + "'");
                            return false;
                        } else {
                            lock (Helpers.RegistrationLock) {
                                if (_NodeInfo.User.StartRegistration(Alias)) {
                                    _NodeInfo.User.SetPassword(Password, _Config.PasswordPepper);
                                    Config C = new Config();
                                    _NodeInfo.User.UserId = C.NextUserId++;
                                    _NodeInfo.User.SaveRegistration();
                                    C.Save();
                                } else {
                                    // TODOZ This user lost the race (_NodeInfo.User.Loaded returned false above, so the user should have been free to register, but between then and .StartRegistration the alias was taken)
                                    //       Not sure what to do in this case, aside from log that it happened
                                    RMLog.Warning("RLogin user lost a race condition and couldn't register as '" + UserName + "'");
                                }
                            }
                        }
                    } else {
                        return Register(UserName, Password);
                    }
                }
            }

            // If we get here, logon is ok
            return true;
        }

        private bool AuthenticateTelnet() {
            DisplayAnsi("LOGON_HEADER");

            int FailedAttempts = 0;
            while (FailedAttempts++ < 3) {
                // Get alias
                UpdateStatus("Entering Alias");
                DisplayAnsi("LOGON_ENTER_ALIAS");
                string Alias = ReadLn().Trim();

                // Make sure we should still proceed
                if (QuitThread()) return false;

                if (Alias.ToUpper() == "NEW") {
                    bool CanRegister = false;

                    if (string.IsNullOrEmpty(_Config.NewUserPassword)) {
                        CanRegister = true;
                    } else {
                        // Get new user password
                        UpdateStatus("Entering New User Password");
                        DisplayAnsi("NEWUSER_ENTER_NEWUSER_PASSWORD");
                        string NewUserPassword = ReadLn('*').Trim();
                        _NodeInfo.Connection.WriteLn();

                        CanRegister = NewUserPassword == _Config.NewUserPassword;
                    }

                    // Make sure we should still proceed
                    if (QuitThread()) return false;

                    if (CanRegister) {
                        // Trying to register as a new user
                        UpdateStatus("Registering as new user");
                        return Register();
                    } else {
                        UpdateStatus("Entered invalid newuser password");
                    }
                } else if (IsBannedUser(Alias)) {
                    //List<string> BannedIPs = new List<string>();

                    // Load existing banned ips, if file exists
                    //string BannedIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-ips.txt");
                    //if (File.Exists(BannedIPsFileName)) BannedIPs.AddRange(FileUtils.FileReadAllLines(BannedIPsFileName));

                    // Add new banned ip
                    //BannedIPs.Add(_NodeInfo.Connection.GetRemoteIP());

                    // Save updated banned ip list
                    //FileUtils.FileWriteAllLines(BannedIPsFileName, BannedIPs.ToArray());

                    // Add to temp ban list
                    Helpers.AddTempIgnoredIP(_NodeInfo.Connection.GetRemoteIP());

                    RMLog.Warning("IP banned for trying to log in as " + Alias);
                    DisplayAnsi("USER_BANNED");
                    return false;
                } else if (!string.IsNullOrEmpty(Alias)) {
                    UpdateStatus("Logging on as " + Alias);

                    // Get password
                    UpdateStatus("Entering Password");
                    DisplayAnsi("LOGON_ENTER_PASSWORD");
                    string Password = ReadLn('*').Trim();

                    // Make sure we should still proceed
                    if (QuitThread()) return false;

                    UpdateStatus("Validating Credentials");
                    _NodeInfo.User = new UserInfo(Alias);
                    if (_NodeInfo.User.Loaded && (_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper))) {
                        // Successfully loaded user info
                        return true;
                    } else {
                        DisplayAnsi("LOGON_INVALID");
                    }
                }
            }

            // If we get here, we failed all 3 logon attempts
            DisplayAnsi("LOGON_FAILED");
            return false;
        }

        public void ClrScr() {
            switch (_NodeInfo.TerminalType) {
                case TerminalType.ANSI:
                    _NodeInfo.Connection.Write(Ansi.TextAttr(7) + Ansi.ClrScr() + Ansi.GotoXY(1, 1));
                    break;
                case TerminalType.ASCII:
                    _NodeInfo.Connection.Write("\r\n\x0C");
                    break;
                case TerminalType.RIP:
                    _NodeInfo.Connection.Write("\r\n!|*" + Ansi.TextAttr(7) + Ansi.ClrScr() + Ansi.GotoXY(1, 1));
                    break;
            }
        }

        public bool DisplayAnsi(string fileName) {
            return DisplayAnsi(fileName, false);
        }

        public bool DisplayAnsi(string fileName, bool pauseAtEnd) {
            if (string.IsNullOrEmpty(fileName)) {
                return false;
            } else {
                List<string> FilesToCheck = new List<string>();
                if (_NodeInfo.TerminalType == TerminalType.RIP) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".rip"));
                if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".ans"));
                FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "ansi", fileName.ToLower() + ".asc"));

                for (int i = 0; i < FilesToCheck.Count; i++) {
                    if (File.Exists(FilesToCheck[i])) return DisplayFile(FilesToCheck[i], false, pauseAtEnd, false);
                }
            }

            return false;
        }

        private void DisplayCurrentMenu() {
            // Generate list of possible menus to look for, prioritized by access level specific and terminal type specific
            List<string> FilesToCheck = new List<string>();

            // Access level specific
            if (_NodeInfo.TerminalType == TerminalType.RIP) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".rip"));
            if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".ans"));
            FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + _NodeInfo.User.AccessLevel.ToString() + ".asc"));

            // No specified access level
            if (_NodeInfo.TerminalType == TerminalType.RIP) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".rip"));
            if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".ans"));
            FilesToCheck.Add(StringUtils.PathCombine(ProcessUtils.StartupPath, "menus", _CurrentMenu.ToLower() + ".asc"));

            // Check if any of the above files exists
            for (int i = 0; i < FilesToCheck.Count; i++) {
                if (File.Exists(FilesToCheck[i])) {
                    DisplayFile(FilesToCheck[i], true, false, false);
                    return;
                }
            }

            // None of the files existed, displayed canned menu
            if (_NodeInfo.TerminalType == TerminalType.ASCII) {
                // Clear the screen
                ClrScr();

                // Display menu header
                _NodeInfo.Connection.WriteLn("ÄÄÄ´°±² " + _CurrentMenu.ToUpper() + " MENU ²±°ÃÄÄÄ");
                _NodeInfo.Connection.WriteLn("ÚÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄÂÄ¿");
                _NodeInfo.Connection.WriteLn("ÃÄÅÄÉÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ»ÄÅÄ´");

                // Display current menu options
                int Row = 1;
                _NodeInfo.Connection.Write((0 == Row % 2) ? "ÃÄÅÄº " : "ÃÄÅÄº ");
                _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenu.ToUpper() + " MENU OPTIONS", ' ', 30 * 2 + 8));
                _NodeInfo.Connection.WriteLn((0 == Row++ % 2) ? "ºÄÅÄ´" : "ºÄÅÄ´");
                Row = DisplayCurrentMenuOptions(Row);

                // Display menu footer
                if (0 == Row % 2) {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄÈÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼ÄÅÄ´");
                    _NodeInfo.Connection.WriteLn("ÀÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÙ");
                } else {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄÈÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼ÄÅÄ´");
                    _NodeInfo.Connection.WriteLn("ÀÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÁÄÙ");
                }

                // Still part of the footer
                _NodeInfo.Connection.Write("[" + StringUtils.SecToHMS(_NodeInfo.SecondsLeft) + "] Select: ");
            } else {
                // Clear the screen
                ClrScr();

                // Display menu header
                _NodeInfo.Connection.WriteLn("\x1B" + "[1;30mÄ" + "\x1B" + "[0mÄ" + "\x1B" + "[1mÄ´" + "\x1B" + "[0;34m°±²" + "\x1B" + "[1;44;37m " + _CurrentMenu.ToUpper() + " MENU " + "\x1B" + "[0;34m²±°" + "\x1B" + "[1;37mÃÄ" + "\x1B" + "[0mÄ" + "\x1B" + "[1;30mÄ");
                _NodeInfo.Connection.WriteLn("\x1B" + "[37mÚÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄÂÄ" + "\x1B" + "[0mÂ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m¿");
                _NodeInfo.Connection.WriteLn("\x1B" + "[1mÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mÉÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ" + "\x1B" + "[1;30m»Ä" + "\x1B" + "[37mÅÄ´");

                // Display current menu options
                int Row = 1;
                _NodeInfo.Connection.Write((0 == Row % 2) ? "ÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mº " : "\x1B" + "[1mÃÄÅÄ" + "\x1B" + "[0mº " + "\x1B" + "[1;37m");
                _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenu.ToUpper() + " MENU OPTIONS", ' ', 30 * 2 + 8));
                _NodeInfo.Connection.WriteLn((0 == Row++ % 2) ? "\x1B" + "[1;30mºÄ" + "\x1B" + "[37mÅÄ´" : "\x1B" + "[1;30mºÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                Row = DisplayCurrentMenuOptions(Row);

                // Display menu footer
                if (0 == Row % 2) {
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mÈ" + "\x1B" + "[1;30mÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼Ä" + "\x1B" + "[37mÅÄ´");
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÀ" + "\x1B" + "[0mÄ" + "\x1B" + "[1mÁÄ" + "\x1B" + "[30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[37mÄ" + "\x1B" + "[0mÙ");
                } else {
                    _NodeInfo.Connection.WriteLn("ÃÄÅÄ" + "\x1B" + "[0mÈ" + "\x1B" + "[1;30mÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍÍ¼Ä" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                    _NodeInfo.Connection.WriteLn("\x1B" + "[1mÀ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[0mÄÁÄ" + "\x1B" + "[1;30mÁ" + "\x1B" + "[37mÄÙ");
                }

                // Still part of the footer
                _NodeInfo.Connection.Write("\x1B" + "[1;30m[" + StringUtils.SecToHMS(_NodeInfo.SecondsLeft) + "]" + "\x1B" + "[37m Select: " + "\x1B" + "[0m");
            }
        }

        private int DisplayCurrentMenuOptions(int row) {
            ArrayList HotKeys = new ArrayList();
            foreach (KeyValuePair<char, MenuOption> KV in _CurrentMenuOptions) {
                HotKeys.Add(KV.Key);
            }
            HotKeys.Sort();

            int i = 0;
            while (i <= HotKeys.Count - 1) {
                if (_NodeInfo.TerminalType == TerminalType.ASCII) {
                    // Display left side
                    _NodeInfo.Connection.Write((0 == row % 2) ? "ÃÄÅÄº " : "ÃÄÅÄº ");

                    // Display left hotkey and description
                    _NodeInfo.Connection.Write(HotKeys[i].ToString() + "] ");
                    _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");

                    // Check if that was the last item, or if there is one more
                    if (i >= HotKeys.Count) {
                        // Display blank space (that was the last item)
                        _NodeInfo.Connection.Write(new string(' ', 30 + 4));
                    } else {
                        // Display right hotkey and description
                        _NodeInfo.Connection.Write(HotKeys[i].ToString() + "] ");
                        _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");
                    }

                    // Display right side
                    _NodeInfo.Connection.WriteLn((0 == row++ % 2) ? "ºÄÅÄ´" : "ºÄÅÄ´");
                } else {
                    // Display left side
                    _NodeInfo.Connection.Write((0 == row % 2) ? "ÃÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0mº " : "\x1B" + "[1mÃÄÅÄ" + "\x1B" + "[0mº ");

                    // Display left hotkey and description
                    _NodeInfo.Connection.Write("\x1B" + "[1;33m" + HotKeys[i].ToString() + "\x1B" + "[1;30m] " + "\x1B" + "[0;37m");
                    _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");

                    // Check if that was the last item, or if there is one more
                    if (i >= HotKeys.Count) {
                        // Display blank space (that was the last item)
                        _NodeInfo.Connection.Write(new string(' ', 30 + 4));
                    } else {
                        // Display right hotkey and description
                        _NodeInfo.Connection.Write("\x1B" + "[1;33m" + HotKeys[i].ToString() + "\x1B" + "[1;30m] " + "\x1B" + "[0;37m");
                        _NodeInfo.Connection.Write(StringUtils.PadRight(_CurrentMenuOptions[HotKeys[i++].ToString()[0]].Name, ' ', 30) + " ");
                    }

                    // Display right side
                    _NodeInfo.Connection.WriteLn((0 == row++ % 2) ? "\x1B" + "[1;30mºÄ" + "\x1B" + "[37mÅÄ´" : "\x1B" + "[1;30mºÄ" + "\x1B" + "[0mÅ" + "\x1B" + "[1mÄ" + "\x1B" + "[0m´");
                }
            }

            return row;
        }

        private bool DisplayFile(string fileName, bool clearAtBeginning, bool pauseAtEnd, bool pauseAfter24) {
            try {
                _LastDisplayFile = fileName;

                if (clearAtBeginning) {
                    // Clear the screen
                    ClrScr();
                }

                // Translate the slashes accordingly
                if (OSUtils.IsWindows) {
                    fileName = fileName.Replace("/", "\\");
                } else if (OSUtils.IsUnix) {
                    fileName = fileName.Replace("\\", "/");
                }

                // If file starts with @, then it's an index file and we want to choose a random row from it
                if (fileName.StartsWith("@")) {
                    // Strip @
                    fileName = fileName.Substring(1);

                    if (File.Exists(fileName)) {
                        // Read index file
                        string[] FileNames = FileUtils.FileReadAllLines(fileName, RMEncoding.Ansi);

                        // Pick random filename
                        fileName = FileNames[_R.Next(0, FileNames.Length)];
                        _LastDisplayFile = fileName;

                        // Translate the slashes accordingly
                        if (OSUtils.IsWindows) {
                            fileName = fileName.Replace("/", "\\");
                        } else if (OSUtils.IsUnix) {
                            fileName = fileName.Replace("\\", "/");
                        }
                    } else {
                        return false;
                    }
                }

                // Check if we need to pick an extension based on the user's terminal type (do this if the file doesn't exist, and doesn't have an extension)
                if (!File.Exists(fileName) && !Path.HasExtension(fileName)) {
                    List<string> FileNamesWithExtension = new List<string>();
                    if (_NodeInfo.TerminalType == TerminalType.RIP) {
                        FileNamesWithExtension.Add(fileName + ".rip");
                    }
                    if ((_NodeInfo.TerminalType == TerminalType.RIP) || (_NodeInfo.TerminalType == TerminalType.ANSI)) {
                        FileNamesWithExtension.Add(fileName + ".ans");
                    }
                    FileNamesWithExtension.Add(fileName + ".asc");

                    foreach (string FileNameWithExtension in FileNamesWithExtension) {
                        if (File.Exists(FileNameWithExtension)) {
                            fileName = FileNameWithExtension;
                            break;
                        }
                    }
                }

                if (File.Exists(fileName)) {
                    string TranslatedText = TranslateMCI(FileUtils.FileReadAllText(fileName, RMEncoding.Ansi), fileName);

                    // Check if we need to filter out a SAUCE record
                    if (TranslatedText.Contains("\x1A")) TranslatedText = TranslatedText.Substring(0, TranslatedText.IndexOf("\x1A"));

                    // Check if the file contains manual pauses
                    if (TranslatedText.Contains("{PAUSE}")) {
                        // When the file contains {PAUSE} statements then pauseAfter24 is ignored
                        string[] Pages = TranslatedText.Split(new string[] { "{PAUSE}" }, StringSplitOptions.None);
                        for (int i = 0; i < Pages.Length; i++) {
                            _NodeInfo.Connection.Write(Pages[i]);
                            if (i < (Pages.Length - 1)) ReadChar();
                        }
                    } else {
                        // Check if we want to pause every 24 lines
                        if (pauseAfter24) {
                            // Yep, so split the file on CRLF
                            string[] TranslatedLines = TranslatedText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                            if (TranslatedLines.Length <= 24) {
                                // But if the file is less than 24 lines, then just send it all at once
                                _NodeInfo.Connection.Write(TranslatedText);
                            } else {
                                // More than 24 lines, do it the hard way
                                for (int i = 0; i < TranslatedLines.Length; i++) {
                                    _NodeInfo.Connection.Write(TranslatedLines[i]);
                                    if (i < TranslatedLines.Length - 1) _NodeInfo.Connection.WriteLn();

                                    // TODOZ This doesn't work when a single line of output is spread across multiple lines of input
                                    //      Should somehow count the number of lines scrolled, and pause after 24
                                    if ((i % 24 == 23) && (i < TranslatedLines.Length - 1)) {
                                        _NodeInfo.Connection.Write("<more>");
                                        var Ch = ReadChar();
                                        _NodeInfo.Connection.Write("\b\b\b\b\b\b      \b\b\b\b\b\b");
                                        if (Ch.ToString().ToUpper() == "Q") return true;
                                    }
                                }
                            }
                        } else {
                            // Nope, so just send it as is.
                            _NodeInfo.Connection.Write(TranslatedText);
                        }
                    }

                    if (pauseAtEnd) {
                        ReadChar();
                        ClrScr();
                    }
                    return true;
                } else {
                    return false;
                }
            } catch (IOException ioex) {
                RMLog.Exception(ioex, "Unable to display '" + fileName + "'");
                return false;
            }
        }

        protected override void Execute() {
            bool ShouldRaiseLogOffEvent = false;

            try {
                // Make telnet connections convert CRLF to CR
                _NodeInfo.Connection.LineEnding = "\r";
                _NodeInfo.Connection.StripLF = true;

                // Check for an ignored IP
                if (IsIgnoredIP(_NodeInfo.Connection.GetRemoteIP())) {
                    // Do nothing for ignored IPs
                    RMLog.Debug("Ignored " + _NodeInfo.ConnectionType.ToString() + " connection from " + _NodeInfo.Connection.GetRemoteIP() + ":" + _NodeInfo.Connection.GetRemotePort());
                    return;
                }

                // Log the incoming connction
                RMLog.Info("Incoming " + _NodeInfo.ConnectionType.ToString() + " connection from " + _NodeInfo.Connection.GetRemoteIP() + ":" + _NodeInfo.Connection.GetRemotePort());

                // Get our terminal type, if necessary
                if (_NodeInfo.TerminalType == TerminalType.AUTODETECT) GetTerminalType();

                // Check for whitelist/blacklist type rejections
                if ((_NodeInfo.ConnectionType == ConnectionType.RLogin) && !IsRLoginIP(_NodeInfo.Connection.GetRemoteIP())) {
                    // Do nothing for non-whitelisted RLogin IPs
                    RMLog.Warning("IP " + _NodeInfo.Connection.GetRemoteIP() + " doesn't match RLogin IP whitelist");
                    return;
                } else if (IsBannedIP(_NodeInfo.Connection.GetRemoteIP())) {
                    RMLog.Warning("IP " + _NodeInfo.Connection.GetRemoteIP() + " matches banned IP filter");
                    DisplayAnsi("IP_BANNED");
                    Thread.Sleep(2500);
                    return;
                } else if (_Paused) {
                    DisplayAnsi("SERVER_PAUSED");
                    Thread.Sleep(2500);
                    return;
                } else if (!_NodeInfo.Connection.Connected) {
                    RMLog.Info("No carrier detected (probably a portscanner)");
                    return;
                }

                // Get our node number and bail if there are none available
                _NodeInfo.Node = NodeManager.GetFreeNode(this);
                if (_NodeInfo.Node == 0) {
                    DisplayAnsi("SERVER_BUSY");
                    Thread.Sleep(2500);
                    return;
                }

                // If we get here we can raise a logoff event at the end of the method
                ShouldRaiseLogOffEvent = true;

                // Check if we're doing RUNBBS mode
                _NodeInfo.Door = new DoorInfo("RUNBBS");
                if (_NodeInfo.Door.Loaded) {
                    _NodeInfo.User.Alias = "Anonymous";
                    NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, "Running RUNBBS.INI process", NodeEventType.LogOn));

                    _NodeInfo.SecondsThisSession = 86400; // RUNBBS.BAT can run for 24 hours
                    (new RunDoor(this)).Run();
                    return;
                }

                // Handle authentication based on the connection type
                bool Authed = (_NodeInfo.ConnectionType == ConnectionType.RLogin) ? AuthenticateRLogin() : AuthenticateTelnet();
                if (!Authed || QuitThread()) return;

                // Update the user's time remaining
                _NodeInfo.UserLoggedOn = true;
                _NodeInfo.SecondsThisSession = _Config.TimePerCall * 60;
                NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, "Logging on", NodeEventType.LogOn));

                // Check if RLogin is requesting to launch a door immediately via the xtrn= command
                if ((_NodeInfo.Door != null) && _NodeInfo.Door.Loaded) {
                    (new RunDoor(this)).Run();
                    Thread.Sleep(2500);
                    return;
                }

                // Do the logon process
                UpdateStatus("Running Logon Process");
                LogOnProcess.Run(this);

                // Make sure we should still proceed
                if (QuitThread()) return;

                // Do the logoff process
                UpdateStatus("Running Logoff Process");
                LogOffProcess.Run(this);

                // Make sure we should still proceed
                if (QuitThread()) return;

                DisplayAnsi("LOGOFF");
                Thread.Sleep(2500);
            } catch (Exception ex) {
                RMLog.Exception(ex, "Exception in ClientThread::Execute()");
            } finally {
                // Try to close the connection
                try { _NodeInfo.Connection.Close(); } catch (Exception ex) { RMLog.Debug($"Exception closing connection in client thread: {ex.ToString()}"); }

                // Try to free the node
                if (ShouldRaiseLogOffEvent) {
                    try { NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, "Logging off", NodeEventType.LogOff)); } catch { /* Ignore */ }
                }
            }
        }

        private bool FileContainsIP(string filename, string ip) {
            // TODOZ Handle IPv6
            string[] ConnectionOctets = ip.Split('.');
            if (ConnectionOctets.Length == 4) {
                string[] FileIPs = FileUtils.FileReadAllLines(filename);
                foreach (string FileIP in FileIPs) {
                    if (FileIP.StartsWith(";")) continue;

                    string[] FileOctets = FileIP.Split('.');
                    if (FileOctets.Length == 4) {
                        bool Match = true;
                        for (int i = 0; i < 4; i++) {
                            if ((FileOctets[i] == "*") || (FileOctets[i] == ConnectionOctets[i])) {
                                // We still have a match
                                continue;
                            } else {
                                // No longer have a match
                                Match = false;
                                break;
                            }
                        }

                        // If we still have a match after the loop, it's a banned IP
                        if (Match) return true;
                    }
                }
            }

            return false;
        }

        private void GetCurrentMenu() {
            // Clear the dictionary
            _CurrentMenuOptions.Clear();

            // Find out what hotkeys the menu has
            string[] HotKeys = MenuOption.GetHotKeys(_CurrentMenu);

            // Get the data for each hotkey
            for (int i = 0; i < HotKeys.Length; i++) {
                char HotKey = HotKeys[i].ToString().ToUpper()[0];

                try {
                    MenuOption MO = new MenuOption(_CurrentMenu, HotKey);
                    if ((MO.Loaded) && (_NodeInfo.User.AccessLevel >= MO.RequiredAccess)) {
                        bool CanAdd = true;

                        // If the option is a door, check if the door can be run on the current platform
                        if (MO.Action == Action.RunDoor) {
                            DoorInfo DI = new DoorInfo(MO.Parameters);
                            if (DI.Loaded) {
                                // Determine how to run the door
                                if (DI.Platform == OSUtils.Platform.DOS) {
                                    if (OSUtils.IsWindows) {
                                        if (ProcessUtils.Is64BitOperatingSystem) {
                                            // DOS doors are OK on 64bit Windows if DOSBox is installed
                                            CanAdd = Helpers.IsDOSBoxInstalled();
                                        } else {
                                            // DOS doors are OK on 32bit Windows
                                            CanAdd = true;
                                        }
                                    } else if (OSUtils.IsUnix) {
                                        // DOS doors are OK on Linux if DOSEMU is installed
                                        CanAdd = Helpers.IsDOSEMUInstalled();
                                    } else {
                                        // DOS doors are not OK on unknown platforms
                                        CanAdd = false;
                                    }
                                } else if (DI.Platform == OSUtils.Platform.Linux) {
                                    // Linux doors are OK on Linux
                                    CanAdd = OSUtils.IsUnix;
                                } else if (DI.Platform == OSUtils.Platform.Windows) {
                                    // Windows doors are OK on Windows
                                    CanAdd = OSUtils.IsWindows;
                                }
                            } else {
                                // Can't load door, so don't display it
                                CanAdd = false;
                            }
                        }

                        if (CanAdd) _CurrentMenuOptions.Add(HotKey, MO);
                    }
                } catch (ArgumentException aex) {
                    // If there's something wrong with the ini entry (Action is invalid for example), this will throw a System.ArgumentException error, so we just ignore that menu item
                    RMLog.Exception(aex, "Unable to load '" + _CurrentMenu + "' menu option for '" + HotKey + "'");
                }
            }
        }

        // Logic for this terminal type detection taken from Synchronet's ANSWER.CPP
        private void GetTerminalType() {
            try {
                /* Detect terminal type */
                Thread.Sleep(200);
                _NodeInfo.Connection.ReadString();		/* flush input buffer */
                _NodeInfo.Connection.Write("\r\n" +		/* locate cursor at column 1 */
                    "\x1b[s" +	                /* save cursor position (necessary for HyperTerm auto-ANSI) */
                    "\x1b[255B" +	            /* locate cursor as far down as possible */
                    "\x1b[255C" +	            /* locate cursor as far right as possible */
                    "\b_" +		                /* need a printable at this location to actually move cursor */
                    "\x1b[6n" +	                /* Get cursor position */
                    "\x1b[u" +	                /* restore cursor position */
                    "\x1b[!_" +	                /* RIP? */
                    "\x1b[0m_" +	            /* "Normal" colors */
                    "\x1b[2J" +	                /* clear screen */
                    "\x1b[H" +	                /* home cursor */
                    "\xC" +		                /* clear screen (in case not ANSI) */
                    "\r"		                /* Move cursor left (in case previous char printed) */
                );

                char? c = '\0';
                int i = 0;
                string str = "";
                while (i++ < 50) { 	/* wait up to 5 seconds for response */
                    c = _NodeInfo.Connection.ReadChar(100);
                    if (_NodeInfo.Connection.ReadTimedOut)
                        continue;
                    if (c == null)
                        continue;
                    c = (char)(c & 0x7f);
                    if (c == 0)
                        continue;
                    i = 0;
                    if (string.IsNullOrEmpty(str) && c != '\x1b')	// response must begin with escape char
                        continue;
                    str += c;
                    if (c == 'R') {   /* break immediately if ANSI response */
                        Thread.Sleep(500);
                        break;
                    }
                }

                while (_NodeInfo.Connection.CanRead(100)) {
                    str += _NodeInfo.Connection.ReadString();
                }

                if (str.ToUpper().Contains("RIPSCRIP")) {
                    _NodeInfo.TerminalType = TerminalType.RIP;
                } else if (Regex.IsMatch(str, "\\x1b[[]\\d{1,3};\\d{1,3}R")) {
                    _NodeInfo.TerminalType = TerminalType.ANSI;
                }
            } catch (Exception) {
                // Ignore, we'll just assume ASCII if something bad happens
            }

            _NodeInfo.TerminalType = TerminalType.ASCII;
        }

        public bool HandleMenuOption(MenuOption menuOption) {
            if (_NodeInfo.User.AccessLevel >= menuOption.RequiredAccess) {
                switch (menuOption.Action) {
                    case Action.ChangeMenu:
                        UpdateStatus("Changing to " + menuOption.Parameters.ToUpper() + " menu");
                        _CurrentMenu = menuOption.Parameters.ToUpper();
                        return false;
                    case Action.Disconnect:
                        UpdateStatus("Disconnecting");
                        _NodeInfo.Connection.Close();
                        return true;
                    case Action.DisplayFile:
                        UpdateStatus("Displaying " + menuOption.Parameters);
                        DisplayFile(menuOption.Parameters, true, false, false);
                        if (menuOption.Parameters != _LastDisplayFile) UpdateStatus(" displayed " + _LastDisplayFile);
                        return false;
                    case Action.DisplayFileMore:
                        UpdateStatus("Displaying " + menuOption.Parameters + " (with more)");
                        DisplayFile(menuOption.Parameters, true, true, true);
                        if (menuOption.Parameters != _LastDisplayFile) UpdateStatus(" displayed " + _LastDisplayFile + " (with more)");
                        return false;
                    case Action.DisplayFilePause:
                        UpdateStatus("Displaying " + menuOption.Parameters + " (with pause)");
                        DisplayFile(menuOption.Parameters, true, true, false);
                        if (menuOption.Parameters != _LastDisplayFile) UpdateStatus(" displayed " + _LastDisplayFile + " (with pause)");
                        return false;
                    case Action.LogOff:
                        UpdateStatus("Logging off");
                        return true;
                    case Action.MainMenu:
                        UpdateStatus("Changing to " + menuOption.Parameters.ToUpper() + " menu");
                        _CurrentMenu = menuOption.Parameters.ToUpper();
                        MainMenu();
                        return true;
                    case Action.Pause:
                        UpdateStatus("Pausing for " + menuOption.Parameters + " seconds");
                        Thread.Sleep(int.Parse(menuOption.Parameters));
                        return false;
                    case Action.RunDoor:
                        UpdateStatus("Running " + menuOption.Parameters);
                        (new RunDoor(this)).Run(menuOption.Parameters);
                        return false;
                    case Action.Telnet:
                        UpdateStatus("Telnetting to " + menuOption.Parameters);
                        Telnet(menuOption.Parameters);
                        return false;
                }
            }

            return false;
        }

        public string IPAddress {
            get { return _NodeInfo.Connection.GetRemoteIP(); }
        }

        private bool IsBannedIP(string ip) {
            try {
                string BannedIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-ips.txt");
                if (File.Exists(BannedIPsFileName)) {
                    return FileContainsIP(BannedIPsFileName, ip);
                } else {
                    // No file means not banned
                    return false;
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to validate client IP against banned-ips.txt");
                return false; // Give them the benefit of the doubt on error
            }
        }

        private bool IsBannedUser(string alias) {
            try {
                alias = alias.Trim().ToLower();
                if (string.IsNullOrEmpty(alias)) return false; // Don't ban for blank inputs

                string BannedUsersFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-users.txt");
                if (File.Exists(BannedUsersFileName)) {
                    string[] BannedUsers = FileUtils.FileReadAllLines(BannedUsersFileName);
                    foreach (string BannedUser in BannedUsers) {
                        if (BannedUser.StartsWith(";")) continue;

                        if (BannedUser.Trim().ToLower() == alias) return true;
                    }
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to validate alias against banned-users.txt");
            }

            // If we get here, it's an OK name
            return false;
        }

        private bool IsIgnoredIP(string ip) {
            try {
                if (Helpers.IsTempIgnoredIP(ip)) return true;

                string IgnoredIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "ignored-ips-combined.txt");
                if (File.Exists(IgnoredIPsFileName)) {
                    return FileContainsIP(IgnoredIPsFileName, ip);
                } else {
                    // No file means not ignored
                    return false;
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to validate client IP against ignored-ips.txt");
                return false; // Give them the benefit of the doubt on error
            }
        }

        private bool IsRLoginIP(string ip) {
            try {
                string RLoginIPsFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "rlogin-ips.txt");
                if (File.Exists(RLoginIPsFileName)) {
                    return FileContainsIP(RLoginIPsFileName, ip);
                } else {
                    // No file means any RLogin connection allowed
                    return true;
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to validate client IP against ignored-ips.txt");
                return true; // Give them the benefit of the doubt on error
            }
        }

        private void MainMenu() {
            bool ExitWhile = false;
            while ((!ExitWhile) && (!QuitThread())) {
                // Get current menu options
                GetCurrentMenu();

                // Show menu options
                DisplayCurrentMenu();

                // Send node event message
                UpdateStatus("At " + _CurrentMenu.ToUpper() + " menu");

                string HotKey = ReadChar().ToString().ToUpper();
                if (!string.IsNullOrEmpty(HotKey) && !QuitThread()) {
                    if (_CurrentMenuOptions.ContainsKey(HotKey[0])) {
                        MenuOption MO = _CurrentMenuOptions[HotKey[0]];
                        switch (MO.Action) {
                            case Action.ChangeMenu:
                            case Action.Disconnect:
                            case Action.DisplayFile:
                            case Action.DisplayFileMore:
                            case Action.DisplayFilePause:
                            case Action.LogOff:
                            case Action.RunDoor:
                            case Action.Telnet:
                                ExitWhile = HandleMenuOption(MO);
                                break;
                        }
                    }
                }
            }
        }

        public NodeInfo NodeInfo { get { return _NodeInfo; } }

        public void OnDoorWait(object sender, RMProcessStartAndWaitEventArgs e) {
            e.Stop = QuitThread();
        }

        public bool QuitThread() {
            if (_Stop) return true;
            if (!_NodeInfo.Connection.Connected) return true;
            if (_NodeInfo.Connection.ReadTimedOut) return true;
            if (_NodeInfo.SecondsLeft <= 0) return true;
            return false;
        }

        private char? ReadChar() {
            char? Result = null;

            Result = _NodeInfo.Connection.ReadChar(_NodeInfo.ReadTimeout);
            if (Result == null) {
                if ((!_Stop) && (_NodeInfo.Connection.Connected)) {
                    if (_NodeInfo.SecondsLeft > 0) {
                        // User has time left so they timed out
                        DisplayAnsi("EXCEEDED_IDLE_LIMIT");
                        Thread.Sleep(2500);
                        _NodeInfo.Connection.Close();
                    } else {
                        // User has no time left, so they exceeded the call limit
                        DisplayAnsi("EXCEEDED_CALL_LIMIT");
                        Thread.Sleep(2500);
                        _NodeInfo.Connection.Close();
                    }
                }
            }

            return Result;
        }

        private string ReadLn() {
            // Call the main SocketReadLn() indicating no password character
            return ReadLn('\0');
        }

        private string ReadLn(char passwordChar) {
            string Result = _NodeInfo.Connection.ReadLn(passwordChar, _NodeInfo.ReadTimeout);
            if ((_NodeInfo.Connection.ReadTimedOut) && (!_Stop) && (_NodeInfo.Connection.Connected)) {
                if (_NodeInfo.SecondsLeft > 0) {
                    // User has time left so they timed out
                    DisplayAnsi("EXCEEDED_IDLE_LIMIT");
                    Thread.Sleep(2500);
                    _NodeInfo.Connection.Close();
                } else {
                    // User has no time left, so they exceeded the call limit
                    DisplayAnsi("EXCEEDED_CALL_LIMIT");
                    Thread.Sleep(2500);
                    _NodeInfo.Connection.Close();
                }
            }

            return Result;
        }

        private bool Register() {
            return Register(null, null);
        }

        private bool Register(string defaultUserName, string defaultPassword) {
            bool Registered = false;

            try {
                DisplayAnsi("NEWUSER_HEADER");

                // Get an alias
                string Alias = "";
                if (string.IsNullOrEmpty(defaultUserName)) {
                    GetAlias:
                    Alias = "";
                    while ((string.IsNullOrEmpty(Alias)) || (Alias.ToUpper() == "NEW")) {
                        DisplayAnsi("NEWUSER_ENTER_ALIAS");
                        Alias = ReadLn().Trim();
                        if (QuitThread()) return false;
                    }

                    // StartRegistration will check if the alias already exists, and if not, reserve it so there's no race condition for two people registering at the same time and both wanting the same alias
                    if (IsBannedUser(Alias) || !_NodeInfo.User.StartRegistration(Alias)) {
                        // Alias has already been taken
                        DisplayAnsi("NEWUSER_ENTER_ALIAS_DUPLICATE");
                        goto GetAlias;
                    }
                } else {
                    Alias = defaultUserName;
                }

                // Get their password
                GetPassword:
                string Password = "";
                if (string.IsNullOrEmpty(defaultPassword)) {
                    while (string.IsNullOrEmpty(Password)) {
                        DisplayAnsi("NEWUSER_ENTER_PASSWORD");
                        Password = ReadLn('*').Trim();
                        if (QuitThread()) return false;
                    }
                    _NodeInfo.User.SetPassword(Password, _Config.PasswordPepper);

                    // Confirm their password
                    DisplayAnsi("NEWUSER_ENTER_PASSWORD_CONFIRM");
                    Password = ReadLn('*').Trim();
                    if (QuitThread()) return false;

                    if (!_NodeInfo.User.ValidatePassword(Password, _Config.PasswordPepper)) {
                        DisplayAnsi("NEWUSER_ENTER_PASSWORD_MISMATCH");
                        goto GetPassword;
                    }
                } else {
                    Password = defaultPassword;
                }

                // Loop through the questions
                string[] Questions = NewUserQuestion.GetQuestions();
                for (int i = 0; i < Questions.Length; i++) {
                    NewUserQuestion Question = new NewUserQuestion(Questions[i]);

                    GetAnswer:
                    // Display prompt
                    if (DisplayAnsi("NEWUSER_ENTER_" + Questions[i])) {
                        // Get answer
                        string Answer = ReadLn().Trim();
                        if (QuitThread()) return false;

                        // Check if answer is required
                        if ((Question.Required) && (string.IsNullOrEmpty(Answer))) goto GetAnswer;

                        // Check if answer requires validation
                        if (!string.IsNullOrEmpty(Answer)) {
                            bool Valid = true;
                            switch (Question.Validate) {
                                case ValidationType.Email:
                                    if (!StringUtils.IsValidEmailAddress(Answer)) Valid = false;
                                    break;
                                case ValidationType.Numeric:
                                    double Temp = 0;
                                    if (!double.TryParse(Answer, out Temp)) Valid = false;
                                    break;
                                case ValidationType.TwoWords:
                                    if (!Answer.Contains(" ")) Valid = false;
                                    break;
                            }
                            if (!Valid) {
                                if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_INVALID")) _NodeInfo.Connection.WriteLn("Input is not valid!");
                                goto GetAnswer;
                            }
                        }

                        // Check if answer requires confirmation
                        if (Question.Confirm) {
                            if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_CONFIRM")) _NodeInfo.Connection.Write("Please re-enter: ");

                            string Confirm = ReadLn().Trim();
                            if (QuitThread()) return false;

                            // Check if confirmation matches
                            if (Confirm != Answer) {
                                if (!DisplayAnsi("NEWUSER_ENTER_" + Questions[i] + "_MISMATCH")) _NodeInfo.Connection.WriteLn("Text does not match!");
                                goto GetAnswer;
                            }
                        }

                        // If we get here, the answer is valid, so save it
                        _NodeInfo.User.AdditionalInfo[Questions[i]] = Answer;
                    } else {
                        RMLog.Error("Unable to prompt new user for '" + Questions[i] + "' since ansi\\newuser_enter_" + Questions[i].ToLower() + ".ans is missing");
                    }
                }

                Registered = true;
                return Registered;
            } finally {
                if (Registered) {
                    lock (Helpers.RegistrationLock) {
                        Config C = new Config();
                        _NodeInfo.User.UserId = C.NextUserId++;
                        _NodeInfo.User.SaveRegistration();
                        C.Save();
                    }

                    DisplayAnsi("NEWUSER_SUCCESS", true);
                } else {
                    _NodeInfo.User.AbortRegistration();
                }
            }
        }

        public string Status {
            get { return _Status; }
        }

        public override void Stop() {
            // Close the socket so that any waits on ReadLn(), ReadChar(), etc, will not block
            _NodeInfo.Connection.Close();

            base.Stop();
        }

        private void Telnet(string hostname) {
            bool _RLogin = false;
            string _RLoginClientUserName = "TODO";
            string _RLoginServerUserName = "TODO";
            string _RLoginTerminalType = "TODO";

            ClrScr();
            _NodeInfo.Connection.WriteLn();
            _NodeInfo.Connection.Write(" Connecting to remote server...");

            TcpConnection RemoteServer = null;
            if (_RLogin) {
                RemoteServer = new RLoginConnection();
            } else {
                RemoteServer = new TelnetConnection();
            }

            // Sanity check on the port
            int Port = 23;
            WebUtils.ParseHostPort(hostname, ref hostname, ref Port);
            if ((Port < 1) || (Port > 65535)) {
                Port = (_RLogin) ? 513 : 23;
            }

            if (RemoteServer.Connect(hostname, Port)) {
                bool CanContinue = true;
                if (_RLogin) {
                    // Send rlogin header
                    RemoteServer.Write("\0" + _RLoginClientUserName + "\0" + _RLoginServerUserName + "\0" + _RLoginTerminalType + "\0");

                    // Wait up to 5 seconds for a response
                    char? Ch = RemoteServer.ReadChar(5000);
                    if ((Ch == null) || (Ch != '\0')) {
                        CanContinue = false;
                        _NodeInfo.Connection.WriteLn("failed!");
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn(" Looks like the remote server doesn't accept RLogin connections.");
                    }
                }

                if (CanContinue) {
                    _NodeInfo.Connection.WriteLn("connected!");

                    bool UserAborted = false;
                    while (!UserAborted && RemoteServer.Connected && !QuitThread()) {
                        bool Yield = true;

                        // See if the server sent anything to the client
                        if (RemoteServer.CanRead()) {
                            _NodeInfo.Connection.Write(RemoteServer.ReadString());
                            Yield = false;
                        }

                        // See if the client sent anything to the server
                        if (_NodeInfo.Connection.CanRead()) {
                            //string ToSend = "";
                            //while (_NodeInfo.Connection.KeyPressed())
                            //{
                            //    byte B = (byte)_NodeInfo.Connection.ReadByte();
                            //    if (B == 29)
                            //    {
                            //        // Ctrl-]
                            //        RemoteServer.Close();
                            //        UserAborted = true;
                            //        break;
                            //    }
                            //    else
                            //    {
                            //        ToSend += (char)B;
                            //    }
                            //}
                            //RemoteServer.Write(ToSend);

                            RemoteServer.Write(_NodeInfo.Connection.ReadString());
                            Yield = false;
                        }

                        // See if we need to yield
                        if (Yield) Crt.Delay(1);
                    }

                    if (UserAborted) {
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn(" User hit CTRL-] to disconnect from server.");
                        ReadChar();
                    } else if ((_NodeInfo.Connection.Connected) && (!RemoteServer.Connected)) {
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn();
                        _NodeInfo.Connection.WriteLn(" Remote server closed the connection.");
                        ReadChar();
                    }
                }
            } else {
                _NodeInfo.Connection.WriteLn("failed!");
                _NodeInfo.Connection.WriteLn();
                _NodeInfo.Connection.WriteLn(" Looks like the remote server isn't online, please try back later.");
            }
        }

        private string TranslateMCI(string text, string fileName) {
            StringDictionary MCI = new StringDictionary();
            MCI.Add("ACCESSLEVEL", _NodeInfo.User.AccessLevel.ToString());
            MCI.Add("ALIAS", _NodeInfo.User.Alias);
            MCI.Add("BBSNAME", _Config.BBSName);
            MCI.Add("DATE", DateTime.Now.ToShortDateString());
            MCI.Add("FILENAME", fileName.Replace(StringUtils.PathCombine(ProcessUtils.StartupPath, ""), ""));
            MCI.Add("GSDIR", StringUtils.PathCombine(ProcessUtils.StartupPath, ""));
            MCI.Add("MENUNAME", _CurrentMenu);
            MCI.Add("NODE", _NodeInfo.Node.ToString());
            MCI.Add("OPERATINGSYSTEM", OSUtils.GetNameAndVersion());
            MCI.Add("SYSOPEMAIL", _Config.SysopEmail);
            MCI.Add("SYSOPNAME", _Config.SysopFirstName + " " + _Config.SysopLastName);
            MCI.Add("TIME", DateTime.Now.ToShortTimeString());
            MCI.Add("TIMELEFT", StringUtils.SecToHMS(_NodeInfo.SecondsLeft));
            foreach (DictionaryEntry DE in _NodeInfo.User.AdditionalInfo) {
                MCI.Add(DE.Key.ToString(), DE.Value.ToString());
            }
            if (text.Contains("WHOSONLINE_")) {
                EventHandler<WhoIsOnlineEventArgs> Handler = WhoIsOnlineEvent;
                if (Handler != null) {
                    WhoIsOnlineEventArgs WOEA = new WhoIsOnlineEventArgs();
                    Handler(this, WOEA);
                    foreach (DictionaryEntry DE in WOEA.WhoIsOnline) {
                        MCI.Add(DE.Key.ToString(), DE.Value.ToString());
                    }
                }
            }

            // Perform MCI Translations
            foreach (DictionaryEntry DE in MCI) {
                if (DE.Value != null) {
                    text = text.Replace("{" + DE.Key.ToString().ToUpper() + "}", DE.Value.ToString());
                    for (int PadWidth = 1; PadWidth <= 80; PadWidth++) {
                        // Now translate anything that needs right padding
                        text = text.Replace("{" + DE.Key.ToString().ToUpper() + PadWidth.ToString() + "}", StringUtils.PadRight(DE.Value.ToString(), ' ', PadWidth));

                        // And now translate anything that needs left padding
                        text = text.Replace("{" + PadWidth.ToString() + DE.Key.ToString().ToUpper() + '}', StringUtils.PadLeft(DE.Value.ToString(), ' ', PadWidth));
                    }
                }
            }

            return text;
        }

        public void UpdateStatus(string newStatus) {
            _Status = newStatus;
            NodeEvent?.Invoke(this, new NodeEventArgs(_NodeInfo, newStatus, NodeEventType.StatusChange));
        }
    }
}

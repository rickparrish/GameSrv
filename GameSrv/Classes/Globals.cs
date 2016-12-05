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
using RandM.RMLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace RandM.GameSrv {
    public static class Globals {
        public static bool Debug { get; set; }
        public static Collection<string> Log = new Collection<string>();
        public static object PrivilegeLock = new object();
        public static object RegistrationLock = new object();
        public static bool StartedAsRoot { get; set; }
        public static Dictionary<string, DateTime> TempIgnoredIPs = new Dictionary<string, DateTime>();

        private static object _RootLock = new object();
        private static object _TempIgnoredIPsLock = new object();

        private static WindowsImpersonationContext _WIC = null;

        static Globals() {
            Debug = Debugger.IsAttached;
            foreach (string arg in Environment.GetCommandLineArgs()) {
                if (arg.ToUpper() == "DEBUG") {
                    Debug = true;
                    break;
                }
            }

            StartedAsRoot = ((OSUtils.IsUnix) && (WindowsIdentity.GetCurrent().Token == IntPtr.Zero));
        }

        public static void AddTempIgnoredIP(string ip) {
            lock (_TempIgnoredIPsLock) {
                if (TempIgnoredIPs.ContainsKey(ip)) {
                    // Key exists, so just update the time
                    TempIgnoredIPs[ip] = DateTime.Now;
                } else {
                    // Key does not exist, so add it
                    TempIgnoredIPs.Add(ip, DateTime.Now);
                }
            }
        }

        public static void DropRoot(string dropToUser) {
            if (!StartedAsRoot) return;

            lock (_RootLock) {
                // If we're on a Unix machine, and running as root, drop privilege
                if ((OSUtils.IsUnix) && (_WIC == null) && (WindowsIdentity.GetCurrent().Token == IntPtr.Zero)) {
                    using (WindowsIdentity Before = WindowsIdentity.GetCurrent()) {
                        using (WindowsIdentity DropTo = new WindowsIdentity(dropToUser)) {
                            _WIC = DropTo.Impersonate();
                            using (WindowsIdentity After = WindowsIdentity.GetCurrent()) {
                                if (After.Name != dropToUser) throw new ArgumentOutOfRangeException("dropToUser", "requested user account '" + dropToUser + "' does not exist");
                            }
                        }
                    }
                }
            }
        }

        public static string Copyright {
            get {
                string Result = "";
                Result += ProcessUtils.ProductNameOfCallingAssembly + " " + ProcessUtils.ProductVersionOfCallingAssembly + " is copyright Rick Parrish, R&M Software\r\n";
                Result += "\r\n";
                if (OSUtils.IsWinNT) {
                    Result += "dosxtrn.exe, sbbsexec.dll and code ported from Synchronet's xtrn.cpp are copyright Rob Swindell - http://www.synchro.net/copyright.html\r\n";
                    Result += "\r\n";
                } else if (OSUtils.IsUnix) {
                    Result += "dosemu integration code ported from Synchronet's xtrn.cpp is copyright Rob Swindell - http://www.synchro.net/copyright.html\r\n";
                    Result += "\r\n";
                    Result += "pty-sharp.dll is copyright Miguel de Icaza\r\n";
                    Result += "\r\n";
                    Result += "pty-sharp-1.0.zip src/pty.* is copyright Red Hat, Inc.\r\n";
                    Result += "\r\n";
                    Result += "pty-sharp-1.0.zip gnome-pty-helper is copyright Miguel de Icaza\r\n";
                    Result += "\r\n";
                }

                return Result;
            }
        }

        public static bool IsDOSBoxInstalled() {
            string ProgramFilesX86 = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string DOSBoxExe = StringUtils.PathCombine(ProgramFilesX86, @"DOSBox-0.73\dosbox.exe"); // TODOZ add configuration variable so this path is not hardcoded
            return File.Exists(DOSBoxExe);
        }

        public static bool IsDOSEMUInstalled() {
            return File.Exists("/usr/bin/dosemu.bin"); // TODOZ add configuration variable so this path is not hardcoded
        }

        public static bool IsTempIgnoredIP(string ip) {
            lock (_TempIgnoredIPsLock) {
                if (TempIgnoredIPs.ContainsKey(ip)) {
                    // Key exists, check if it has expired
                    if (DateTime.Now.Subtract(TempIgnoredIPs[ip]).TotalMinutes >= 10) {
                        // Expired, remove record
                        TempIgnoredIPs.Remove(ip);
                        return false;
                    } else {
                        // Not expired, still ignored
                        return true;
                    }
                } else {
                    // Not ignored
                    return false;
                }
            }
        }

        public static void NeedRoot() {
            if (!StartedAsRoot) return;

            lock (_RootLock) {
                // If we're on a Unix machine, raise back to root privilege
                if ((OSUtils.IsUnix) && (_WIC != null) && (WindowsIdentity.GetCurrent().Token != IntPtr.Zero)) {
                    WindowsIdentity Before = WindowsIdentity.GetCurrent();
                    _WIC.Undo();
                    _WIC = null;
                    WindowsIdentity After = WindowsIdentity.GetCurrent();
                }
            }
        }
    }
}

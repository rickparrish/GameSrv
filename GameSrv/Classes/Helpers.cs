/*
  GameSrv: A BBS Door Game Server
  Copyright (C) Rick Parrish, R&M Software

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
    public static class Helpers {
        public static bool Debug { get; } = Debugger.IsAttached;
        public static object PrivilegeLock { get; } = new object();
        public static object RegistrationLock { get; } = new object();
        public static bool StartedAsRoot { get; } = ((OSUtils.IsUnix) && (WindowsIdentity.GetCurrent().Token == IntPtr.Zero));
        public static Dictionary<string, DateTime> TempIgnoredIPs { get; } = new Dictionary<string, DateTime>();

        private static object _RootLock = new object();
        private static object _TempIgnoredIPsLock = new object();

        private static WindowsImpersonationContext _WIC = null;

        static Helpers() {
            foreach (string arg in Environment.GetCommandLineArgs()) {
                if (arg.ToUpper() == "DEBUG") {
                    Debug = true;
                    break;
                }
            }
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

        public static void CleanUpFiles() {
            if (OSUtils.IsWindows) {
                FileUtils.FileDelete("cpulimit.sh");
                FileUtils.FileDelete("dosutils.zip");
                FileUtils.FileDelete("install.sh");
                FileUtils.FileDelete("pty-sharp-1.0.zip");
                FileUtils.FileDelete("start.sh");
                if (OSUtils.IsWinNT) {
                    if (ProcessUtils.Is64BitOperatingSystem) {
                        FileUtils.FileDelete("dosxtrn.exe");
                        FileUtils.FileDelete("dosxtrn.pif");
                        FileUtils.FileDelete("sbbsexec.dll");
                        if (!Helpers.IsDOSBoxInstalled()) {
                            RMLog.Error("PLEASE INSTALL DOSBOX 0.73 IF YOU PLAN ON RUNNING DOS DOORS USING DOSBOX");
                        }
                    } else {
                        FileUtils.FileDelete("dosbox.conf");
                        if (!File.Exists(StringUtils.PathCombine(Environment.SystemDirectory, "sbbsexec.dll"))) {
                            RMLog.Error("PLEASE COPY SBBSEXEC.DLL TO " + StringUtils.PathCombine(Environment.SystemDirectory, "sbbsexec.dll").ToUpper() + " IF YOU PLAN ON RUNNING DOS DOORS USING THE EMBEDDED SYNCHRONET FOSSIL");
                        }
                    }
                }
            } else if (OSUtils.IsUnix) {
                FileUtils.FileDelete("dosbox.conf");
                FileUtils.FileDelete("dosxtrn.exe");
                FileUtils.FileDelete("dosxtrn.pif");
                FileUtils.FileDelete("install.cmd");
                FileUtils.FileDelete("sbbsexec.dll");
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

        public static void DropRoot(string dropToUser) {
            if (!StartedAsRoot)
                return;

            lock (_RootLock) {
                // If we're on a Unix machine, and running as root, drop privilege
                if ((OSUtils.IsUnix) && (_WIC == null) && (WindowsIdentity.GetCurrent().Token == IntPtr.Zero)) {
                    using (WindowsIdentity Before = WindowsIdentity.GetCurrent()) {
                        using (WindowsIdentity DropTo = new WindowsIdentity(dropToUser)) {
                            _WIC = DropTo.Impersonate();
                            using (WindowsIdentity After = WindowsIdentity.GetCurrent()) {
                                if (After.Name != dropToUser)
                                    throw new ArgumentOutOfRangeException("dropToUser", "requested user account '" + dropToUser + "' does not exist");
                            }
                        }
                    }
                }
            }
        }

        public static bool FileContainsIP(string fileName, string ip) {
            if (string.IsNullOrEmpty(fileName)) {
                throw new ArgumentNullException("fileName");
            } else if (string.IsNullOrEmpty(ip)) {
                throw new ArgumentNullException("ip");
            }

            // TODOZ Handle IPv6
            string[] ConnectionOctets = ip.Split('.');
            if (ConnectionOctets.Length == 4) {
                string[] FileIPs = FileUtils.FileReadAllLines(fileName);
                foreach (string FileIP in FileIPs) {
                    if (FileIP.StartsWith(";"))
                        continue;

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
                        if (Match)
                            return true;
                    }
                }
            }

            return false;
        }

        public static bool IsBannedIP(string ip) {
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

        public static bool IsBannedUser(string alias) {
            if (string.IsNullOrEmpty(alias)) {
                throw new ArgumentNullException("alias");
            }

            try {
                alias = alias.Trim().ToLower();
                if (string.IsNullOrEmpty(alias))
                    return false; // Don't ban for blank inputs

                string BannedUsersFileName = StringUtils.PathCombine(ProcessUtils.StartupPath, "config", "banned-users.txt");
                if (File.Exists(BannedUsersFileName)) {
                    string[] BannedUsers = FileUtils.FileReadAllLines(BannedUsersFileName);
                    foreach (string BannedUser in BannedUsers) {
                        if (BannedUser.StartsWith(";"))
                            continue;

                        if (BannedUser.Trim().ToLower() == alias)
                            return true;
                    }
                }
            } catch (Exception ex) {
                RMLog.Exception(ex, "Unable to validate alias against banned-users.txt");
            }

            // If we get here, it's an OK name
            return false;
        }

        public static bool IsDOSBoxInstalled() {
            string ProgramFilesX86 = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string DOSBoxExe = StringUtils.PathCombine(ProgramFilesX86, @"DOSBox-0.73\dosbox.exe"); // TODOZ add configuration variable so this path is not hardcoded
            return File.Exists(DOSBoxExe);
        }

        public static bool IsDOSEMUInstalled() {
            return File.Exists("/usr/bin/dosemu.bin"); // TODOZ add configuration variable so this path is not hardcoded
        }

        public static bool IsIgnoredIP(string ip) {
            try {
                if (Helpers.IsTempIgnoredIP(ip))
                    return true;

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

        public static bool IsRLoginIP(string ip) {
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
            if (!StartedAsRoot)
                return;

            lock (_RootLock) {
                // If we're on a Unix machine, raise back to root privilege
                if ((OSUtils.IsUnix) && (_WIC != null) && (WindowsIdentity.GetCurrent().Token != IntPtr.Zero)) {
                    _WIC.Undo();
                    _WIC = null;
                }
            }
        }
    }
}

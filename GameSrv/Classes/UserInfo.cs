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
using System;
using System.Collections.Generic;
using System.Text;
using RandM.RMLib;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.IO;
using System.Globalization;

namespace RandM.GameSrv {
    public class UserInfo : ConfigHelper {
        public int AccessLevel { get; set; }
        public StringDictionary AdditionalInfo { get; set; }
        public string Alias { get; set; }
        public bool AllowMultipleConnections { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public int UserId { get; set; }

        internal UserInfo(string alias)
            : base(ConfigSaveLocation.Relative, StringUtils.PathCombine("users", UserInfo.SafeAlias(alias.ToLower()) + ".ini")) {
            AccessLevel = 10;
            AdditionalInfo = new StringDictionary();
            Alias = alias;
            AllowMultipleConnections = false;
            PasswordHash = "";
            PasswordSalt = StringUtils.RandomString(100);
            UserId = 0;

            Load("USER");
        }

        public void AbortRegistration() {
            if ((!string.IsNullOrEmpty(Alias)) && (Alias.ToUpper() != "NEW")) {
                lock (Helpers.RegistrationLock) {
                    FileUtils.FileDelete(FileName);
                }
            }
        }

        public static string GetPasswordHash(string password, string salt, string pepper) {
            if (pepper.ToUpper().Trim() == "DISABLE") {
                return password;
            } else {
                // Build the array of bytes to hash.  This is made up of the concatenation of:
                //   salt:     A random string.  This string should be unique per user, and can be stored in the user record along with the rest of the users information
                //   password: The plain text password
                //   pepper:   Another random string.  This string should be unique per BBS, and can be stored in the bbs config file
                // The purpose of the salt is to ensure that if the password hashes are stolen, a single rainbow table cannot be used to crack all the passwords.  For example, 2 users both using "password" as their password will have different hashes, since different salts will be applied.
                // The purpose of the pepper is to ensure that even if the password hashes and per user salt values are stolen, and the attacker has the CPU power required to create per-user rainbow tables, the tables will still be incomplete without the secret pepper value that is stored separately.  (Probably not far enough away in this case, but it doesn't hurt to haev this)
                using (SHA512Managed SHA = new SHA512Managed()) {
                    byte[] InBytes = Encoding.ASCII.GetBytes(salt + password + pepper);
                    byte[] OutBytes = SHA.ComputeHash(InBytes);

                    // Loop 1024 times -- this creates no noticeable delay on our server, but it may mean an attacker can now only try 1,000 per second instead of 1,000,000 per second
                    for (int i = 0; i < 1024; i++) {
                        OutBytes = SHA.ComputeHash(OutBytes);
                    }

                    return Convert.ToBase64String(OutBytes);
                }
            }
        }

        public static string SafeAlias(string alias) {
            // TODOX This differs between win and linux.  Maybe filter to just alphanumeric?
            char[] InvalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < InvalidChars.Length; i++) {
                // Pick a new character based on the old character that's either in the A-Z or a-z range
                string NewText = "_" + ((int)InvalidChars[i]).ToString() + "_";
                alias = alias.Replace(InvalidChars[i].ToString(), NewText);
            }
            return alias;
        }

        public void SaveRegistration() {
            lock (Helpers.PrivilegeLock) {
                base.Save();
            }
        }

        public void SetPassword(string password, string pepper) {
            PasswordSalt = StringUtils.RandomString(100);
            PasswordHash = GetPasswordHash(password, PasswordSalt, pepper);
        }

        public bool StartRegistration(string alias) {
            lock (Helpers.RegistrationLock) {
                // Check for existence of alias
                UserInfo U = new UserInfo(alias);
                if (U.Load()) {
                    // Alias exists, can't start registration
                    return false;
                } else {
                    // Alias is unique, save a file to start the registration process
                    base.FileName = U.FileName;
                    lock (Helpers.PrivilegeLock) {
                        this.Alias = alias;
                        Save();
                    }
                    return true;
                }
            }
        }

        public bool ValidatePassword(string password, string pepper) {
            return (PasswordHash == GetPasswordHash(password, PasswordSalt, pepper));
        }
    }

}

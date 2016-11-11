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
using System;
using System.Collections.Generic;
using System.Text;
using RandM.RMLib;

namespace RandM.GameSrv {
    public class NodeInfo {
        public TcpConnection Connection { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public DoorInfo Door { get; set; }
        public int Node { get; set; }
        public int SecondsThisSession { get; set; }
        public TerminalType TerminalType { get; set; }
        public DateTime TimeOn { get; set; }
        public UserInfo User { get; set; }
        public bool UserLoggedOn { get; set; }

        public NodeInfo() {
            Connection = null;
            ConnectionType = ConnectionType.None;
            Door = new DoorInfo("");
            Node = -1;
            SecondsThisSession = 300; // Default to 5 minutes during authentication, will be set accordingly at successful logon
            TerminalType = TerminalType.ANSI;
            TimeOn = DateTime.Now;
            User = new UserInfo("");
            UserLoggedOn = false;
        }
    }
}

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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace RandM.GameSrv
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "svc")]
    public partial class svcMain : ServiceBase
    {
        private GameSrv _GameSrv = new GameSrv();

        public svcMain()
        {
            InitializeComponent();
        }

        protected override void OnContinue()
        {
            base.OnContinue();
            _GameSrv.Start();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            _GameSrv.Start();
        }

        protected override void OnPause()
        {
            base.OnPause();
            _GameSrv.Pause();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            _GameSrv.Stop();
        }

        protected override void OnStop()
        {
            base.OnStop();
            _GameSrv.Stop();
        }
    }
}

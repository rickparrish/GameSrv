/*
  GameSrv: A BBS Door Game Server
  Copyright (C) 2002-2013  Rick Parrish, R&M Software

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
using RandM.RMLibUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace RandM.GameSrv
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

            DoorInfo Door = new DoorInfo("RUNBBS");
            chkRUNBBS.Checked = Door.Loaded;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void chkRUNBBS_CheckedChanged(object sender, EventArgs e)
        {
            DoorInfo Door = new DoorInfo("RUNBBS");
            string EnabledFileName = Door.FileName;
            string DisabledFileName = StringUtils.PathCombine(Path.GetDirectoryName(Door.FileName), "_" + Path.GetFileName(Door.FileName));

            if (chkRUNBBS.Checked)
            {
                // If we don't have a RUNBBS.INI but we have a _RUNBBS.INI, rename it to RUNBBS.INI
                if ((!File.Exists(EnabledFileName)) && (File.Exists(DisabledFileName)))
                {
                    FileUtils.FileMove(DisabledFileName, EnabledFileName);
                }
            }
            else
            {
                // If we have a RUNBBS.INI file, rename it to _RUNBBS.INI
                if (File.Exists(EnabledFileName))
                {
                    FileUtils.FileDelete(DisabledFileName);
                    FileUtils.FileMove(EnabledFileName, DisabledFileName);
                }
            }
        }

        private void cmdDoorGames_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }

        private void cmdLogOffProcess_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }

        private void cmdLogOnProcess_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }

        private void cmdMenuEditor_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }

        private void cmdNewUserQuestions_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }

        private void cmdRunBBSSettings_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }

        private void cmdServerSettings1_Click(object sender, EventArgs e)
        {
            using (frmServerSettings F = new frmServerSettings())
            {
                F.ShowDialog();
            }
        }

        private void cmdServerSettings2_Click(object sender, EventArgs e)
        {
            using (frmServerSettings F = new frmServerSettings())
            {
                F.InitRunBBS();
                F.ShowDialog();
            }
        }

        private void cmdUserEditor_Click(object sender, EventArgs e)
        {
            Dialog.OKCancel("Still need to implement this", "TODO");
        }
    }
}

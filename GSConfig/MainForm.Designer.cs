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
namespace RandM.GameSrv
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cmdUserEditor = new System.Windows.Forms.Button();
            this.cmdDoorGames = new System.Windows.Forms.Button();
            this.cmdMenuEditor = new System.Windows.Forms.Button();
            this.cmdNewUserQuestions = new System.Windows.Forms.Button();
            this.cmdLogOffProcess = new System.Windows.Forms.Button();
            this.cmdLogOnProcess = new System.Windows.Forms.Button();
            this.cmdServerSettings1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cmdServerSettings2 = new System.Windows.Forms.Button();
            this.cmdRunBBSSettings = new System.Windows.Forms.Button();
            this.chkRUNBBS = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuFileExit = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cmdUserEditor);
            this.groupBox1.Controls.Add(this.cmdDoorGames);
            this.groupBox1.Controls.Add(this.cmdMenuEditor);
            this.groupBox1.Controls.Add(this.cmdNewUserQuestions);
            this.groupBox1.Controls.Add(this.cmdLogOffProcess);
            this.groupBox1.Controls.Add(this.cmdLogOnProcess);
            this.groupBox1.Controls.Add(this.cmdServerSettings1);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 24);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(589, 120);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "\"Door Server\" Configuration";
            // 
            // cmdUserEditor
            // 
            this.cmdUserEditor.Location = new System.Drawing.Point(501, 66);
            this.cmdUserEditor.Name = "cmdUserEditor";
            this.cmdUserEditor.Size = new System.Drawing.Size(75, 40);
            this.cmdUserEditor.TabIndex = 7;
            this.cmdUserEditor.Text = "User \r\nEditor";
            this.cmdUserEditor.UseVisualStyleBackColor = true;
            this.cmdUserEditor.Click += new System.EventHandler(this.cmdUserEditor_Click);
            // 
            // cmdDoorGames
            // 
            this.cmdDoorGames.Location = new System.Drawing.Point(420, 66);
            this.cmdDoorGames.Name = "cmdDoorGames";
            this.cmdDoorGames.Size = new System.Drawing.Size(75, 40);
            this.cmdDoorGames.TabIndex = 6;
            this.cmdDoorGames.Text = "Door \r\nGames";
            this.cmdDoorGames.UseVisualStyleBackColor = true;
            this.cmdDoorGames.Click += new System.EventHandler(this.cmdDoorGames_Click);
            // 
            // cmdMenuEditor
            // 
            this.cmdMenuEditor.Location = new System.Drawing.Point(339, 66);
            this.cmdMenuEditor.Name = "cmdMenuEditor";
            this.cmdMenuEditor.Size = new System.Drawing.Size(75, 40);
            this.cmdMenuEditor.TabIndex = 5;
            this.cmdMenuEditor.Text = "Menu\r\nEditor";
            this.cmdMenuEditor.UseVisualStyleBackColor = true;
            this.cmdMenuEditor.Click += new System.EventHandler(this.cmdMenuEditor_Click);
            // 
            // cmdNewUserQuestions
            // 
            this.cmdNewUserQuestions.Location = new System.Drawing.Point(258, 66);
            this.cmdNewUserQuestions.Name = "cmdNewUserQuestions";
            this.cmdNewUserQuestions.Size = new System.Drawing.Size(75, 40);
            this.cmdNewUserQuestions.TabIndex = 4;
            this.cmdNewUserQuestions.Text = "New User Questions";
            this.cmdNewUserQuestions.UseVisualStyleBackColor = true;
            this.cmdNewUserQuestions.Click += new System.EventHandler(this.cmdNewUserQuestions_Click);
            // 
            // cmdLogOffProcess
            // 
            this.cmdLogOffProcess.Location = new System.Drawing.Point(177, 66);
            this.cmdLogOffProcess.Name = "cmdLogOffProcess";
            this.cmdLogOffProcess.Size = new System.Drawing.Size(75, 40);
            this.cmdLogOffProcess.TabIndex = 3;
            this.cmdLogOffProcess.Text = "Log Off Process";
            this.cmdLogOffProcess.UseVisualStyleBackColor = true;
            this.cmdLogOffProcess.Click += new System.EventHandler(this.cmdLogOffProcess_Click);
            // 
            // cmdLogOnProcess
            // 
            this.cmdLogOnProcess.Location = new System.Drawing.Point(96, 66);
            this.cmdLogOnProcess.Name = "cmdLogOnProcess";
            this.cmdLogOnProcess.Size = new System.Drawing.Size(75, 40);
            this.cmdLogOnProcess.TabIndex = 2;
            this.cmdLogOnProcess.Text = "Log On Process";
            this.cmdLogOnProcess.UseVisualStyleBackColor = true;
            this.cmdLogOnProcess.Click += new System.EventHandler(this.cmdLogOnProcess_Click);
            // 
            // cmdServerSettings1
            // 
            this.cmdServerSettings1.Location = new System.Drawing.Point(15, 66);
            this.cmdServerSettings1.Name = "cmdServerSettings1";
            this.cmdServerSettings1.Size = new System.Drawing.Size(75, 40);
            this.cmdServerSettings1.TabIndex = 1;
            this.cmdServerSettings1.Text = "Server Settings";
            this.cmdServerSettings1.UseVisualStyleBackColor = true;
            this.cmdServerSettings1.Click += new System.EventHandler(this.cmdServerSettings1_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(564, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "To configure GameSrv in the standard \"Door Server\" mode, please click the buttons" +
    " below and fill out the information on each of the configuration screens.";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cmdServerSettings2);
            this.groupBox2.Controls.Add(this.cmdRunBBSSettings);
            this.groupBox2.Controls.Add(this.chkRUNBBS);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox2.Location = new System.Drawing.Point(0, 133);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(589, 114);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "\"Run BBS\" Configuration";
            // 
            // cmdServerSettings2
            // 
            this.cmdServerSettings2.Location = new System.Drawing.Point(420, 62);
            this.cmdServerSettings2.Name = "cmdServerSettings2";
            this.cmdServerSettings2.Size = new System.Drawing.Size(75, 40);
            this.cmdServerSettings2.TabIndex = 8;
            this.cmdServerSettings2.Text = "Server Settings";
            this.cmdServerSettings2.UseVisualStyleBackColor = true;
            this.cmdServerSettings2.Click += new System.EventHandler(this.cmdServerSettings2_Click);
            // 
            // cmdRunBBSSettings
            // 
            this.cmdRunBBSSettings.Location = new System.Drawing.Point(501, 62);
            this.cmdRunBBSSettings.Name = "cmdRunBBSSettings";
            this.cmdRunBBSSettings.Size = new System.Drawing.Size(75, 40);
            this.cmdRunBBSSettings.TabIndex = 7;
            this.cmdRunBBSSettings.Text = "Run BBS Settings";
            this.cmdRunBBSSettings.UseVisualStyleBackColor = true;
            this.cmdRunBBSSettings.Click += new System.EventHandler(this.cmdRunBBSSettings_Click);
            // 
            // chkRUNBBS
            // 
            this.chkRUNBBS.AutoSize = true;
            this.chkRUNBBS.Location = new System.Drawing.Point(15, 75);
            this.chkRUNBBS.Name = "chkRUNBBS";
            this.chkRUNBBS.Size = new System.Drawing.Size(302, 17);
            this.chkRUNBBS.TabIndex = 2;
            this.chkRUNBBS.Text = "Disable \"Door Server\" mode and enable \"Run BBS\" mode";
            this.chkRUNBBS.UseVisualStyleBackColor = true;
            this.chkRUNBBS.CheckedChanged += new System.EventHandler(this.chkRUNBBS_CheckedChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 25);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(564, 29);
            this.label2.TabIndex = 1;
            this.label2.Text = "To configure GameSrv in the alternate \"Run BBS\" mode, please check the box to ind" +
    "icate you want to disable the Door Server, and then click the buttons to configu" +
    "re the BBS launch parameters.";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFile});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(589, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // mnuFile
            // 
            this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuFileExit});
            this.mnuFile.Name = "mnuFile";
            this.mnuFile.Size = new System.Drawing.Size(37, 20);
            this.mnuFile.Text = "&File";
            // 
            // mnuFileExit
            // 
            this.mnuFileExit.Name = "mnuFileExit";
            this.mnuFileExit.Size = new System.Drawing.Size(152, 22);
            this.mnuFileExit.Text = "E&xit";
            this.mnuFileExit.Click += new System.EventHandler(this.mnuFileExit_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(589, 247);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GameSrv Configuration";
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button cmdDoorGames;
        private System.Windows.Forms.Button cmdMenuEditor;
        private System.Windows.Forms.Button cmdNewUserQuestions;
        private System.Windows.Forms.Button cmdLogOffProcess;
        private System.Windows.Forms.Button cmdLogOnProcess;
        private System.Windows.Forms.Button cmdServerSettings1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button cmdServerSettings2;
        private System.Windows.Forms.Button cmdRunBBSSettings;
        private System.Windows.Forms.CheckBox chkRUNBBS;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button cmdUserEditor;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem mnuFile;
        private System.Windows.Forms.ToolStripMenuItem mnuFileExit;
    }
}


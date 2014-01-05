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
                _GameSrv.Dispose();
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.TLP = new System.Windows.Forms.TableLayoutPanel();
            this.lblRLogin = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblLast = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblOn = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.lblTime = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblDate = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.lblTelnet = new System.Windows.Forms.Label();
            this.lblWebSocket = new System.Windows.Forms.Label();
            this.lblGameSrvWFCScreen = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rtbStatus = new System.Windows.Forms.RichTextBox();
            this.tmrUpdateDisplay = new System.Windows.Forms.Timer(this.components);
            this.Tray = new System.Windows.Forms.NotifyIcon(this.components);
            this.TLP.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // TLP
            // 
            this.TLP.ColumnCount = 7;
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.TLP.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.TLP.Controls.Add(this.lblRLogin, 6, 0);
            this.TLP.Controls.Add(this.label6, 5, 0);
            this.TLP.Controls.Add(this.lblLast, 1, 0);
            this.TLP.Controls.Add(this.label1, 0, 0);
            this.TLP.Controls.Add(this.label2, 0, 1);
            this.TLP.Controls.Add(this.lblOn, 1, 1);
            this.TLP.Controls.Add(this.label5, 0, 2);
            this.TLP.Controls.Add(this.label3, 0, 4);
            this.TLP.Controls.Add(this.lblType, 1, 2);
            this.TLP.Controls.Add(this.lblTime, 1, 4);
            this.TLP.Controls.Add(this.label4, 2, 4);
            this.TLP.Controls.Add(this.lblDate, 3, 4);
            this.TLP.Controls.Add(this.label10, 5, 1);
            this.TLP.Controls.Add(this.label11, 5, 2);
            this.TLP.Controls.Add(this.lblTelnet, 6, 1);
            this.TLP.Controls.Add(this.lblWebSocket, 6, 2);
            this.TLP.Controls.Add(this.lblGameSrvWFCScreen, 4, 4);
            this.TLP.Controls.Add(this.groupBox1, 0, 3);
            this.TLP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TLP.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.TLP.Location = new System.Drawing.Point(0, 0);
            this.TLP.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.TLP.Name = "TLP";
            this.TLP.RowCount = 5;
            this.TLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TLP.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.TLP.Size = new System.Drawing.Size(854, 561);
            this.TLP.TabIndex = 0;
            this.TLP.CellPaint += new System.Windows.Forms.TableLayoutCellPaintEventHandler(this.TLP_CellPaint);
            // 
            // lblRLogin
            // 
            this.lblRLogin.BackColor = System.Drawing.Color.Blue;
            this.lblRLogin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRLogin.ForeColor = System.Drawing.Color.White;
            this.lblRLogin.Location = new System.Drawing.Point(807, 0);
            this.lblRLogin.Name = "lblRLogin";
            this.lblRLogin.Size = new System.Drawing.Size(44, 20);
            this.lblRLogin.TabIndex = 11;
            this.lblRLogin.Text = "0";
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.Blue;
            this.label6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label6.ForeColor = System.Drawing.Color.Cyan;
            this.label6.Location = new System.Drawing.Point(687, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(114, 20);
            this.label6.TabIndex = 10;
            this.label6.Text = "RLogin:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblLast
            // 
            this.lblLast.AutoSize = true;
            this.lblLast.BackColor = System.Drawing.Color.Blue;
            this.TLP.SetColumnSpan(this.lblLast, 4);
            this.lblLast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLast.ForeColor = System.Drawing.Color.White;
            this.lblLast.Location = new System.Drawing.Point(73, 0);
            this.lblLast.Name = "lblLast";
            this.lblLast.Size = new System.Drawing.Size(608, 20);
            this.lblLast.TabIndex = 1;
            this.lblLast.Text = "No callers yet...";
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Blue;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.ForeColor = System.Drawing.Color.Cyan;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Last:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Blue;
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.ForeColor = System.Drawing.Color.Cyan;
            this.label2.Location = new System.Drawing.Point(3, 20);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "On:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblOn
            // 
            this.lblOn.AutoSize = true;
            this.lblOn.BackColor = System.Drawing.Color.Blue;
            this.TLP.SetColumnSpan(this.lblOn, 4);
            this.lblOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOn.ForeColor = System.Drawing.Color.White;
            this.lblOn.Location = new System.Drawing.Point(73, 20);
            this.lblOn.Name = "lblOn";
            this.lblOn.Size = new System.Drawing.Size(608, 20);
            this.lblOn.TabIndex = 4;
            this.lblOn.Text = "No callers yet...";
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.Blue;
            this.label5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label5.ForeColor = System.Drawing.Color.Cyan;
            this.label5.Location = new System.Drawing.Point(3, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 20);
            this.label5.TabIndex = 5;
            this.label5.Text = "Type:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.ForeColor = System.Drawing.Color.Green;
            this.label3.Location = new System.Drawing.Point(3, 541);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 20);
            this.label3.TabIndex = 6;
            this.label3.Text = "Time:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.BackColor = System.Drawing.Color.Blue;
            this.TLP.SetColumnSpan(this.lblType, 4);
            this.lblType.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblType.ForeColor = System.Drawing.Color.White;
            this.lblType.Location = new System.Drawing.Point(73, 40);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(608, 20);
            this.lblType.TabIndex = 3;
            this.lblType.Text = "No callers yet...";
            // 
            // lblTime
            // 
            this.lblTime.AutoSize = true;
            this.lblTime.ForeColor = System.Drawing.Color.Lime;
            this.lblTime.Location = new System.Drawing.Point(73, 541);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(58, 20);
            this.lblTime.TabIndex = 8;
            this.lblTime.Text = "TIME GOES HERE";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.Green;
            this.label4.Location = new System.Drawing.Point(173, 541);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 18);
            this.label4.TabIndex = 7;
            this.label4.Text = "Date:";
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.ForeColor = System.Drawing.Color.Lime;
            this.lblDate.Location = new System.Drawing.Point(243, 541);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(148, 18);
            this.lblDate.TabIndex = 9;
            this.lblDate.Text = "DATE GOES HERE";
            // 
            // label10
            // 
            this.label10.BackColor = System.Drawing.Color.Blue;
            this.label10.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label10.ForeColor = System.Drawing.Color.Cyan;
            this.label10.Location = new System.Drawing.Point(687, 20);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(114, 20);
            this.label10.TabIndex = 12;
            this.label10.Text = "Telnet:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label11
            // 
            this.label11.BackColor = System.Drawing.Color.Blue;
            this.label11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label11.ForeColor = System.Drawing.Color.Cyan;
            this.label11.Location = new System.Drawing.Point(687, 40);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(114, 20);
            this.label11.TabIndex = 13;
            this.label11.Text = "WebSocket:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblTelnet
            // 
            this.lblTelnet.BackColor = System.Drawing.Color.Blue;
            this.lblTelnet.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTelnet.ForeColor = System.Drawing.Color.White;
            this.lblTelnet.Location = new System.Drawing.Point(807, 20);
            this.lblTelnet.Name = "lblTelnet";
            this.lblTelnet.Size = new System.Drawing.Size(44, 20);
            this.lblTelnet.TabIndex = 15;
            this.lblTelnet.Text = "0";
            // 
            // lblWebSocket
            // 
            this.lblWebSocket.BackColor = System.Drawing.Color.Blue;
            this.lblWebSocket.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWebSocket.ForeColor = System.Drawing.Color.White;
            this.lblWebSocket.Location = new System.Drawing.Point(807, 40);
            this.lblWebSocket.Name = "lblWebSocket";
            this.lblWebSocket.Size = new System.Drawing.Size(44, 20);
            this.lblWebSocket.TabIndex = 14;
            this.lblWebSocket.Text = "0";
            // 
            // lblGameSrvWFCScreen
            // 
            this.lblGameSrvWFCScreen.AutoSize = true;
            this.lblGameSrvWFCScreen.BackColor = System.Drawing.Color.Blue;
            this.TLP.SetColumnSpan(this.lblGameSrvWFCScreen, 3);
            this.lblGameSrvWFCScreen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGameSrvWFCScreen.ForeColor = System.Drawing.Color.White;
            this.lblGameSrvWFCScreen.Location = new System.Drawing.Point(537, 541);
            this.lblGameSrvWFCScreen.Name = "lblGameSrvWFCScreen";
            this.lblGameSrvWFCScreen.Size = new System.Drawing.Size(314, 20);
            this.lblGameSrvWFCScreen.TabIndex = 16;
            this.lblGameSrvWFCScreen.Tag = "GameSrv WFC Screen";
            this.lblGameSrvWFCScreen.Text = "Press [F1] For Help";
            this.lblGameSrvWFCScreen.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Black;
            this.TLP.SetColumnSpan(this.groupBox1, 7);
            this.groupBox1.Controls.Add(this.rtbStatus);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.ForeColor = System.Drawing.Color.White;
            this.groupBox1.Location = new System.Drawing.Point(3, 63);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(848, 475);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Status ";
            // 
            // rtbStatus
            // 
            this.rtbStatus.BackColor = System.Drawing.Color.Black;
            this.rtbStatus.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.rtbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbStatus.ForeColor = System.Drawing.Color.LightGray;
            this.rtbStatus.Location = new System.Drawing.Point(3, 22);
            this.rtbStatus.Name = "rtbStatus";
            this.rtbStatus.ReadOnly = true;
            this.rtbStatus.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedVertical;
            this.rtbStatus.Size = new System.Drawing.Size(842, 450);
            this.rtbStatus.TabIndex = 18;
            this.rtbStatus.TabStop = false;
            this.rtbStatus.Text = "";
            // 
            // tmrUpdateDisplay
            // 
            this.tmrUpdateDisplay.Interval = 1000;
            this.tmrUpdateDisplay.Tick += new System.EventHandler(this.tmrUpdateDisplay_Tick);
            // 
            // Tray
            // 
            this.Tray.Icon = ((System.Drawing.Icon)(resources.GetObject("Tray.Icon")));
            this.Tray.Text = "GameSrv WFC Screen";
            this.Tray.DoubleClick += new System.EventHandler(this.Tray_DoubleClick);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(854, 561);
            this.Controls.Add(this.TLP);
            this.Font = new System.Drawing.Font("Courier New", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.LightGray;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MinimumSize = new System.Drawing.Size(870, 600);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "GameSrv WFC Screen";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmMain_KeyUp);
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            this.TLP.ResumeLayout(false);
            this.TLP.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel TLP;
        private System.Windows.Forms.Label lblRLogin;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblLast;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblOn;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Label lblTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label lblTelnet;
        private System.Windows.Forms.Label lblWebSocket;
        private System.Windows.Forms.Label lblGameSrvWFCScreen;
        private System.Windows.Forms.Timer tmrUpdateDisplay;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox rtbStatus;
        private System.Windows.Forms.NotifyIcon Tray;
    }
}


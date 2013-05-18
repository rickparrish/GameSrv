namespace Upgrade
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.chkPlaintextPasswords = new System.Windows.Forms.CheckBox();
            this.cmdUpgrade = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.chkPlaintextPasswords);
            this.panel1.Controls.Add(this.cmdUpgrade);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 433);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(484, 28);
            this.panel1.TabIndex = 1;
            // 
            // chkPlaintextPasswords
            // 
            this.chkPlaintextPasswords.AutoSize = true;
            this.chkPlaintextPasswords.Location = new System.Drawing.Point(3, 7);
            this.chkPlaintextPasswords.Name = "chkPlaintextPasswords";
            this.chkPlaintextPasswords.Size = new System.Drawing.Size(202, 17);
            this.chkPlaintextPasswords.TabIndex = 1;
            this.chkPlaintextPasswords.Text = "Use plaintext passwords (less secure)";
            this.chkPlaintextPasswords.UseVisualStyleBackColor = true;
            // 
            // cmdUpgrade
            // 
            this.cmdUpgrade.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmdUpgrade.Location = new System.Drawing.Point(406, 3);
            this.cmdUpgrade.Name = "cmdUpgrade";
            this.cmdUpgrade.Size = new System.Drawing.Size(75, 23);
            this.cmdUpgrade.TabIndex = 0;
            this.cmdUpgrade.Text = "&Upgrade";
            this.cmdUpgrade.UseVisualStyleBackColor = true;
            this.cmdUpgrade.Click += new System.EventHandler(this.cmdUpgrade_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(0, 0);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
            this.richTextBox1.Size = new System.Drawing.Size(484, 433);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            this.richTextBox1.WordWrap = false;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 461);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmMain";
            this.Text = "GameSrv Upgrade Utility";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox chkPlaintextPasswords;
        private System.Windows.Forms.Button cmdUpgrade;
        private System.Windows.Forms.RichTextBox richTextBox1;

    }
}


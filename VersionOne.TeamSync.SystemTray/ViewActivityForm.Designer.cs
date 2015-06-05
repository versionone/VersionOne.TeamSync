namespace VersionOne.TeamSync.SystemTray
{
    partial class ViewActivityForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewActivityForm));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripComboBox1 = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripStopButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripRecyleButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripStartButton = new System.Windows.Forms.ToolStripButton();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.toolStripComboBox1,
            this.toolStripStopButton,
            this.toolStripRecyleButton,
            this.toolStripStartButton});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(749, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(54, 22);
            this.toolStripLabel1.Text = "Log level";
            // 
            // toolStripComboBox1
            // 
            this.toolStripComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.toolStripComboBox1.Items.AddRange(new object[] {
            "ALL",
            "DEBUG",
            "INFO",
            "WARN",
            "ERROR"});
            this.toolStripComboBox1.Name = "toolStripComboBox1";
            this.toolStripComboBox1.Size = new System.Drawing.Size(121, 25);
            this.toolStripComboBox1.SelectedIndexChanged += new System.EventHandler(this.toolStripComboBox1_SelectedIndexChanged_1);
            // 
            // toolStripStopButton
            // 
            this.toolStripStopButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripStopButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStopButton.Image")));
            this.toolStripStopButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripStopButton.Name = "toolStripStopButton";
            this.toolStripStopButton.Size = new System.Drawing.Size(51, 22);
            this.toolStripStopButton.Text = "Stop";
            this.toolStripStopButton.Click += new System.EventHandler(this.toolStripStopButton_Click);
            // 
            // toolStripRecyleButton
            // 
            this.toolStripRecyleButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripRecyleButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripRecyleButton.Image")));
            this.toolStripRecyleButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripRecyleButton.Margin = new System.Windows.Forms.Padding(0, 1, 5, 2);
            this.toolStripRecyleButton.Name = "toolStripRecyleButton";
            this.toolStripRecyleButton.Size = new System.Drawing.Size(67, 22);
            this.toolStripRecyleButton.Text = "Recycle";
            this.toolStripRecyleButton.Click += new System.EventHandler(this.toolStripRecyleButton_Click);
            // 
            // toolStripStartButton
            // 
            this.toolStripStartButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripStartButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripStartButton.Image")));
            this.toolStripStartButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripStartButton.Margin = new System.Windows.Forms.Padding(0, 1, 5, 2);
            this.toolStripStartButton.Name = "toolStripStartButton";
            this.toolStripStartButton.Size = new System.Drawing.Size(51, 22);
            this.toolStripStartButton.Text = "Start";
            this.toolStripStartButton.Click += new System.EventHandler(this.toolStripStartButton_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.BackColor = System.Drawing.Color.Black;
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Font = new System.Drawing.Font("Lucida Console", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.ForeColor = System.Drawing.Color.White;
            this.richTextBox1.Location = new System.Drawing.Point(0, 25);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(749, 586);
            this.richTextBox1.TabIndex = 3;
            this.richTextBox1.Text = "";
            // 
            // ViewActivityForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(749, 611);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.toolStrip1);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ViewActivityForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "VersionOne TeamSync Activity";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ViewActivityForm_FormClosing);
            this.Load += new System.EventHandler(this.ViewActivityForm_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox toolStripComboBox1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.ToolStripButton toolStripStopButton;
        private System.Windows.Forms.ToolStripButton toolStripRecyleButton;
        private System.Windows.Forms.ToolStripButton toolStripStartButton;
    }
}
namespace VersionOne.TeamSync.SystemTray
{
    partial class SystemTray
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SystemTray));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripMenuItem();
            this.viewActivityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.configureServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.startServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recycleServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stopServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitServiceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "VersionOne TeamSync Service";
            this.notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.AutoSize = false;
            this.contextMenuStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3,
            this.viewActivityToolStripMenuItem,
            this.configureServiceToolStripMenuItem,
            this.toolStripSeparator1,
            this.startServiceToolStripMenuItem,
            this.recycleServiceToolStripMenuItem,
            this.stopServiceToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitServiceToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.contextMenuStrip1.Size = new System.Drawing.Size(170, 150);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.AutoSize = false;
            this.toolStripSeparator3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSeparator3.Enabled = false;
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(156, 22);
            // 
            // viewActivityToolStripMenuItem
            // 
            this.viewActivityToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.viewActivityToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.viewActivityToolStripMenuItem.Name = "viewActivityToolStripMenuItem";
            this.viewActivityToolStripMenuItem.Padding = new System.Windows.Forms.Padding(0, 2, 0, 1);
            this.viewActivityToolStripMenuItem.Size = new System.Drawing.Size(156, 23);
            this.viewActivityToolStripMenuItem.Text = "View Activity...";
            this.viewActivityToolStripMenuItem.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.viewActivityToolStripMenuItem.Click += new System.EventHandler(this.viewActivityToolStripMenuItem_Click);
            // 
            // configureServiceToolStripMenuItem
            // 
            this.configureServiceToolStripMenuItem.Name = "configureServiceToolStripMenuItem";
            this.configureServiceToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.configureServiceToolStripMenuItem.Text = "Configure...";
            this.configureServiceToolStripMenuItem.Visible = false;
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(166, 6);
            // 
            // startServiceToolStripMenuItem
            // 
            this.startServiceToolStripMenuItem.Name = "startServiceToolStripMenuItem";
            this.startServiceToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.startServiceToolStripMenuItem.Text = "Start";
            this.startServiceToolStripMenuItem.Click += new System.EventHandler(this.startServiceToolStripMenuItem_Click);
            // 
            // recycleServiceToolStripMenuItem
            // 
            this.recycleServiceToolStripMenuItem.Name = "recycleServiceToolStripMenuItem";
            this.recycleServiceToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.recycleServiceToolStripMenuItem.Text = "Recycle";
            this.recycleServiceToolStripMenuItem.Click += new System.EventHandler(this.recycleServiceToolStripMenuItem_Click);
            // 
            // stopServiceToolStripMenuItem
            // 
            this.stopServiceToolStripMenuItem.Name = "stopServiceToolStripMenuItem";
            this.stopServiceToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.stopServiceToolStripMenuItem.Text = "Stop";
            this.stopServiceToolStripMenuItem.Click += new System.EventHandler(this.stopServiceToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(166, 6);
            // 
            // exitServiceToolStripMenuItem
            // 
            this.exitServiceToolStripMenuItem.Name = "exitServiceToolStripMenuItem";
            this.exitServiceToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.exitServiceToolStripMenuItem.Text = "Exit";
            this.exitServiceToolStripMenuItem.Click += new System.EventHandler(this.exitServiceToolStripMenuItem_Click);
            // 
            // SystemTray
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 221);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SystemTray";
            this.Opacity = 0D;
            this.ShowInTaskbar = false;
            this.Text = "VersionOne TeamSync Service";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SystemTray_FormClosing);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem startServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stopServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem configureServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewActivityToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem recycleServiceToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem toolStripSeparator3;
    }
}


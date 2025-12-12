using System;
using System.Drawing;
using System.Windows.Forms;

namespace FacturaApp.WinForms.Forms
{
    partial class MainForm
    {
        private Panel panelMain;
        private ToolStrip tareasStrip;
        private MenuStrip menu;
        private Panel dashboardWrapper;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            panelMain = new Panel();
            tareasStrip = new ToolStrip();
            menu = new MenuStrip();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.BackColor = Color.WhiteSmoke;
            panelMain.BackgroundImageLayout = ImageLayout.Stretch;
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 24);
            panelMain.Name = "panelMain";
            panelMain.Padding = new Padding(5);
            panelMain.Size = new Size(1002, 663);
            panelMain.TabIndex = 0;
            // 
            // tareasStrip
            // 
            tareasStrip.BackColor = Color.FromArgb(248, 249, 250);
            tareasStrip.Dock = DockStyle.Bottom;
            tareasStrip.GripStyle = ToolStripGripStyle.Hidden;
            tareasStrip.ImageScalingSize = new Size(24, 24);
            tareasStrip.Location = new Point(0, 687);
            tareasStrip.Name = "tareasStrip";
            tareasStrip.Padding = new Padding(10, 5, 10, 5);
            tareasStrip.Size = new Size(1002, 25);
            tareasStrip.TabIndex = 1;
            // 
            // menu
            // 
            menu.BackColor = Color.WhiteSmoke;
            menu.ImageScalingSize = new Size(24, 24);
            menu.Location = new Point(0, 0);
            menu.Name = "menu";
            menu.RenderMode = ToolStripRenderMode.System;
            menu.Size = new Size(1002, 24);
            menu.TabIndex = 2;
            // 
            // MainForm
            // 
            BackColor = Color.White;
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1002, 712);
            Controls.Add(panelMain);
            Controls.Add(tareasStrip);
            Controls.Add(menu);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menu;
            MinimumSize = new Size(800, 600);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CWIF: sistema de ventas";
            ResumeLayout(false);
            PerformLayout();
        }
    }
}

namespace SBot
{
    partial class SBotMainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.listclient = new System.Windows.Forms.Button();
            this.timer0 = new System.Windows.Forms.Timer(this.components);
            this.listboxConfigs = new System.Windows.Forms.ListBox();
            this.stopbot = new System.Windows.Forms.Button();
            this.startbot = new System.Windows.Forms.Button();
            this.listviewClients = new System.Windows.Forms.ListView();
            this.Client = new System.Windows.Forms.ColumnHeader();
            this.Running = new System.Windows.Forms.ColumnHeader();
            this.State = new System.Windows.Forms.ColumnHeader();
            this.button1 = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel4 = new System.Windows.Forms.ToolStripStatusLabel();
            this.buttonPBS = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listclient
            // 
            this.listclient.Location = new System.Drawing.Point(12, 12);
            this.listclient.Name = "listclient";
            this.listclient.Size = new System.Drawing.Size(87, 23);
            this.listclient.TabIndex = 0;
            this.listclient.Text = "List Clients";
            this.listclient.UseVisualStyleBackColor = true;
            this.listclient.Click += new System.EventHandler(this.Listclient_Click);
            // 
            // timer0
            // 
            this.timer0.Interval = 1000;
            this.timer0.Tick += new System.EventHandler(this.Timer0_Tick);
            // 
            // listboxConfigs
            // 
            this.listboxConfigs.FormattingEnabled = true;
            this.listboxConfigs.ItemHeight = 15;
            this.listboxConfigs.Location = new System.Drawing.Point(612, 75);
            this.listboxConfigs.Name = "listboxConfigs";
            this.listboxConfigs.Size = new System.Drawing.Size(127, 154);
            this.listboxConfigs.TabIndex = 10;
            this.listboxConfigs.SelectedIndexChanged += new System.EventHandler(this.ListBoxConfigs_SelectedIndexChanged);
            // 
            // stopbot
            // 
            this.stopbot.Enabled = false;
            this.stopbot.Location = new System.Drawing.Point(357, 12);
            this.stopbot.Name = "stopbot";
            this.stopbot.Size = new System.Drawing.Size(81, 23);
            this.stopbot.TabIndex = 7;
            this.stopbot.Text = "Stop";
            this.stopbot.UseVisualStyleBackColor = true;
            this.stopbot.Click += new System.EventHandler(this.Stopbot_Click);
            // 
            // startbot
            // 
            this.startbot.Enabled = false;
            this.startbot.Location = new System.Drawing.Point(270, 12);
            this.startbot.Name = "startbot";
            this.startbot.Size = new System.Drawing.Size(81, 23);
            this.startbot.TabIndex = 6;
            this.startbot.Text = "Start";
            this.startbot.UseVisualStyleBackColor = true;
            this.startbot.Click += new System.EventHandler(this.Startbot_Click);
            // 
            // listviewClients
            // 
            this.listviewClients.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.Client,
            this.Running,
            this.State});
            this.listviewClients.FullRowSelect = true;
            this.listviewClients.GridLines = true;
            this.listviewClients.Location = new System.Drawing.Point(12, 45);
            this.listviewClients.Name = "listviewClients";
            this.listviewClients.Size = new System.Drawing.Size(594, 184);
            this.listviewClients.TabIndex = 14;
            this.listviewClients.UseCompatibleStateImageBehavior = false;
            this.listviewClients.View = System.Windows.Forms.View.Details;
            this.listviewClients.SelectedIndexChanged += new System.EventHandler(this.ListviewClients_SelectedIndexChanged);
            // 
            // Client
            // 
            this.Client.Text = "Client";
            // 
            // Running
            // 
            this.Running.Text = "Running";
            // 
            // State
            // 
            this.State.Text = "State";
            this.State.Width = 9000;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(444, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(162, 23);
            this.button1.TabIndex = 15;
            this.button1.Text = "Control Mouse For Me";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.ButtonControlMouse_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2,
            this.toolStripStatusLabel3,
            this.toolStripStatusLabel4});
            this.statusStrip1.Location = new System.Drawing.Point(0, 238);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(751, 22);
            this.statusStrip1.TabIndex = 17;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(10, 17);
            this.toolStripStatusLabel1.Text = " ";
            // 
            // toolStripStatusLabel2
            // 
            this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
            this.toolStripStatusLabel2.Size = new System.Drawing.Size(10, 17);
            this.toolStripStatusLabel2.Text = " ";
            // 
            // toolStripStatusLabel3
            // 
            this.toolStripStatusLabel3.Name = "toolStripStatusLabel3";
            this.toolStripStatusLabel3.Size = new System.Drawing.Size(10, 17);
            this.toolStripStatusLabel3.Text = " ";
            // 
            // toolStripStatusLabel4
            // 
            this.toolStripStatusLabel4.Name = "toolStripStatusLabel4";
            this.toolStripStatusLabel4.Size = new System.Drawing.Size(10, 17);
            this.toolStripStatusLabel4.Text = " ";
            // 
            // buttonPBS
            // 
            this.buttonPBS.Location = new System.Drawing.Point(191, 12);
            this.buttonPBS.Name = "buttonPBS";
            this.buttonPBS.Size = new System.Drawing.Size(73, 23);
            this.buttonPBS.TabIndex = 18;
            this.buttonPBS.Text = "PBS";
            this.buttonPBS.UseVisualStyleBackColor = true;
            this.buttonPBS.Click += new System.EventHandler(this.buttonPBS_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(612, 45);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(127, 23);
            this.button2.TabIndex = 19;
            this.button2.Text = "Edit This Config";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(612, 12);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(127, 23);
            this.button3.TabIndex = 20;
            this.button3.Text = "Generate Templates";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // SBotMainForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(751, 260);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.buttonPBS);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listviewClients);
            this.Controls.Add(this.listboxConfigs);
            this.Controls.Add(this.stopbot);
            this.Controls.Add(this.startbot);
            this.Controls.Add(this.listclient);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Times New Roman", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "SBotMainForm";
            this.ShowIcon = false;
            this.Text = "SBot";
            this.TopMost = true;
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button listclient;
        private System.Windows.Forms.Timer timer0;
        private System.Windows.Forms.ListBox listboxConfigs;
        private System.Windows.Forms.Button stopbot;
        private System.Windows.Forms.Button startbot;
        private System.Windows.Forms.ListView listviewClients;
        private System.Windows.Forms.ColumnHeader Client;
        private System.Windows.Forms.ColumnHeader Running;
        private System.Windows.Forms.ColumnHeader State;
        private System.Windows.Forms.Button button1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripStatusLabel toolStripStatusLabel2;
        private ToolStripStatusLabel toolStripStatusLabel3;
        private ToolStripStatusLabel toolStripStatusLabel4;
        private Button buttonPBS;
        private Button button2;
        private Button button3;
    }
}

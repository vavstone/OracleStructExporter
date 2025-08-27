namespace OracleStructExporter.WinForm
{
    partial class MainForm
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.gridConnections = new System.Windows.Forms.DataGridView();
            this.colChecked = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colDB = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSchema = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel7 = new System.Windows.Forms.Panel();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.lblPassword = new System.Windows.Forms.Label();
            this.txtUsername = new System.Windows.Forms.TextBox();
            this.lblUsername = new System.Windows.Forms.Label();
            this.txtSID = new System.Windows.Forms.TextBox();
            this.lblSID = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.lblHost = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.panel6 = new System.Windows.Forms.Panel();
            this.tabCtrlLog = new System.Windows.Forms.TabControl();
            this.panel5 = new System.Windows.Forms.Panel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cbGetOnlyFirstPart = new System.Windows.Forms.CheckBox();
            this.cbSetSeqValTo1 = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbNameObjectMaskExclude = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbNameObjectMaskInclude = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtOutputFolder = new System.Windows.Forms.TextBox();
            this.lblOutputFolder = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbViews = new System.Windows.Forms.CheckBox();
            this.cbTypes = new System.Windows.Forms.CheckBox();
            this.cbTriggers = new System.Windows.Forms.CheckBox();
            this.cbTables = new System.Windows.Forms.CheckBox();
            this.cbSynonyms = new System.Windows.Forms.CheckBox();
            this.cbSequences = new System.Windows.Forms.CheckBox();
            this.cbProcedures = new System.Windows.Forms.CheckBox();
            this.cbPackages = new System.Windows.Forms.CheckBox();
            this.cbJobs = new System.Windows.Forms.CheckBox();
            this.cbFunctions = new System.Windows.Forms.CheckBox();
            this.cbDBlinks = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.gridConnections)).BeginInit();
            this.panel7.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel6.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            this.splitContainer1.Panel1.Controls.Add(this.panel2);
            this.splitContainer1.Panel1.Controls.Add(this.panel7);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.panel4);
            this.splitContainer1.Panel2.Controls.Add(this.panel3);
            this.splitContainer1.Size = new System.Drawing.Size(1193, 673);
            this.splitContainer1.SplitterDistance = 423;
            this.splitContainer1.TabIndex = 0;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(423, 31);
            this.panel1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(199, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Выберите подключения для выгрузки";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.gridConnections);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(423, 518);
            this.panel2.TabIndex = 1;
            // 
            // gridConnections
            // 
            this.gridConnections.AllowUserToAddRows = false;
            this.gridConnections.AllowUserToDeleteRows = false;
            this.gridConnections.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.gridConnections.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colChecked,
            this.colDB,
            this.colSchema});
            this.gridConnections.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridConnections.Location = new System.Drawing.Point(0, 0);
            this.gridConnections.Name = "gridConnections";
            this.gridConnections.Size = new System.Drawing.Size(423, 518);
            this.gridConnections.TabIndex = 0;
            // 
            // colChecked
            // 
            this.colChecked.HeaderText = "";
            this.colChecked.Name = "colChecked";
            this.colChecked.Width = 30;
            // 
            // colDB
            // 
            this.colDB.HeaderText = "БД";
            this.colDB.MinimumWidth = 50;
            this.colDB.Name = "colDB";
            this.colDB.ReadOnly = true;
            this.colDB.Width = 200;
            // 
            // colSchema
            // 
            this.colSchema.HeaderText = "Схема";
            this.colSchema.MinimumWidth = 50;
            this.colSchema.Name = "colSchema";
            this.colSchema.ReadOnly = true;
            this.colSchema.Width = 150;
            // 
            // panel7
            // 
            this.panel7.Controls.Add(this.txtPassword);
            this.panel7.Controls.Add(this.lblPassword);
            this.panel7.Controls.Add(this.txtUsername);
            this.panel7.Controls.Add(this.lblUsername);
            this.panel7.Controls.Add(this.txtSID);
            this.panel7.Controls.Add(this.lblSID);
            this.panel7.Controls.Add(this.txtPort);
            this.panel7.Controls.Add(this.lblPort);
            this.panel7.Controls.Add(this.txtHost);
            this.panel7.Controls.Add(this.lblHost);
            this.panel7.Controls.Add(this.label5);
            this.panel7.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel7.Location = new System.Drawing.Point(0, 518);
            this.panel7.Name = "panel7";
            this.panel7.Size = new System.Drawing.Size(423, 155);
            this.panel7.TabIndex = 2;
            // 
            // txtPassword
            // 
            this.txtPassword.Location = new System.Drawing.Point(109, 123);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(311, 20);
            this.txtPassword.TabIndex = 19;
            // 
            // lblPassword
            // 
            this.lblPassword.AutoSize = true;
            this.lblPassword.Location = new System.Drawing.Point(9, 126);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(48, 13);
            this.lblPassword.TabIndex = 18;
            this.lblPassword.Text = "Пароль:";
            // 
            // txtUsername
            // 
            this.txtUsername.Location = new System.Drawing.Point(109, 97);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Size = new System.Drawing.Size(311, 20);
            this.txtUsername.TabIndex = 17;
            // 
            // lblUsername
            // 
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(9, 100);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(60, 13);
            this.lblUsername.TabIndex = 16;
            this.lblUsername.Text = "UserName:";
            // 
            // txtSID
            // 
            this.txtSID.Location = new System.Drawing.Point(109, 71);
            this.txtSID.Name = "txtSID";
            this.txtSID.Size = new System.Drawing.Size(311, 20);
            this.txtSID.TabIndex = 15;
            // 
            // lblSID
            // 
            this.lblSID.AutoSize = true;
            this.lblSID.Location = new System.Drawing.Point(9, 74);
            this.lblSID.Name = "lblSID";
            this.lblSID.Size = new System.Drawing.Size(28, 13);
            this.lblSID.TabIndex = 14;
            this.lblSID.Text = "SID:";
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(109, 45);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(311, 20);
            this.txtPort.TabIndex = 13;
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(9, 48);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(35, 13);
            this.lblPort.TabIndex = 12;
            this.lblPort.Text = "Порт:";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(109, 19);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(311, 20);
            this.txtHost.TabIndex = 11;
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new System.Drawing.Point(9, 22);
            this.lblHost.Name = "lblHost";
            this.lblHost.Size = new System.Drawing.Size(34, 13);
            this.lblHost.TabIndex = 10;
            this.lblHost.Text = "Хост:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(118, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Или введите вручную:";
            // 
            // panel4
            // 
            this.panel4.Controls.Add(this.panel6);
            this.panel4.Controls.Add(this.panel5);
            this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel4.Location = new System.Drawing.Point(0, 228);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(766, 445);
            this.panel4.TabIndex = 1;
            // 
            // panel6
            // 
            this.panel6.Controls.Add(this.tabCtrlLog);
            this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel6.Location = new System.Drawing.Point(0, 0);
            this.panel6.Name = "panel6";
            this.panel6.Size = new System.Drawing.Size(766, 409);
            this.panel6.TabIndex = 1;
            // 
            // tabCtrlLog
            // 
            this.tabCtrlLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabCtrlLog.Location = new System.Drawing.Point(0, 0);
            this.tabCtrlLog.Name = "tabCtrlLog";
            this.tabCtrlLog.SelectedIndex = 0;
            this.tabCtrlLog.Size = new System.Drawing.Size(766, 409);
            this.tabCtrlLog.TabIndex = 0;
            // 
            // panel5
            // 
            this.panel5.Controls.Add(this.btnCancel);
            this.panel5.Controls.Add(this.btnExport);
            this.panel5.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel5.Location = new System.Drawing.Point(0, 409);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(766, 36);
            this.panel5.TabIndex = 0;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(679, 6);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 23;
            this.btnCancel.Text = "Отменить";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(579, 6);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 21;
            this.btnExport.Text = "Выгрузить";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.groupBox2);
            this.panel3.Controls.Add(this.label4);
            this.panel3.Controls.Add(this.tbNameObjectMaskExclude);
            this.panel3.Controls.Add(this.label3);
            this.panel3.Controls.Add(this.tbNameObjectMaskInclude);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Controls.Add(this.txtOutputFolder);
            this.panel3.Controls.Add(this.lblOutputFolder);
            this.panel3.Controls.Add(this.groupBox1);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel3.Location = new System.Drawing.Point(0, 0);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(766, 228);
            this.panel3.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cbGetOnlyFirstPart);
            this.groupBox2.Controls.Add(this.cbSetSeqValTo1);
            this.groupBox2.Location = new System.Drawing.Point(402, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(352, 115);
            this.groupBox2.TabIndex = 25;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Дополнительно:";
            // 
            // cbGetOnlyFirstPart
            // 
            this.cbGetOnlyFirstPart.AutoSize = true;
            this.cbGetOnlyFirstPart.Location = new System.Drawing.Point(15, 42);
            this.cbGetOnlyFirstPart.Name = "cbGetOnlyFirstPart";
            this.cbGetOnlyFirstPart.Size = new System.Drawing.Size(259, 17);
            this.cbGetOnlyFirstPart.TabIndex = 1;
            this.cbGetOnlyFirstPart.Text = "Брать только первую секцию партиц-х таблиц";
            this.cbGetOnlyFirstPart.UseVisualStyleBackColor = true;
            // 
            // cbSetSeqValTo1
            // 
            this.cbSetSeqValTo1.AutoSize = true;
            this.cbSetSeqValTo1.Location = new System.Drawing.Point(15, 19);
            this.cbSetSeqValTo1.Name = "cbSetSeqValTo1";
            this.cbSetSeqValTo1.Size = new System.Drawing.Size(253, 17);
            this.cbSetSeqValTo1.TabIndex = 0;
            this.cbSetSeqValTo1.Text = "Сбрасывать в DDL секвенсы в значение \"1\"";
            this.cbSetSeqValTo1.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 204);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(136, 13);
            this.label4.TabIndex = 32;
            this.label4.Text = "Не содержащие в имени:";
            // 
            // tbNameObjectMaskExclude
            // 
            this.tbNameObjectMaskExclude.Location = new System.Drawing.Point(150, 201);
            this.tbNameObjectMaskExclude.Name = "tbNameObjectMaskExclude";
            this.tbNameObjectMaskExclude.Size = new System.Drawing.Size(604, 20);
            this.tbNameObjectMaskExclude.TabIndex = 31;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 180);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 13);
            this.label3.TabIndex = 30;
            this.label3.Text = "Содержащие в имени:";
            // 
            // tbNameObjectMaskInclude
            // 
            this.tbNameObjectMaskInclude.Location = new System.Drawing.Point(150, 175);
            this.tbNameObjectMaskInclude.Name = "tbNameObjectMaskInclude";
            this.tbNameObjectMaskInclude.Size = new System.Drawing.Size(604, 20);
            this.tbNameObjectMaskInclude.TabIndex = 29;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 158);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(78, 13);
            this.label2.TabIndex = 28;
            this.label2.Text = "Маска имени:";
            // 
            // txtOutputFolder
            // 
            this.txtOutputFolder.Location = new System.Drawing.Point(107, 133);
            this.txtOutputFolder.Name = "txtOutputFolder";
            this.txtOutputFolder.Size = new System.Drawing.Size(647, 20);
            this.txtOutputFolder.TabIndex = 27;
            // 
            // lblOutputFolder
            // 
            this.lblOutputFolder.AutoSize = true;
            this.lblOutputFolder.Location = new System.Drawing.Point(9, 136);
            this.lblOutputFolder.Name = "lblOutputFolder";
            this.lblOutputFolder.Size = new System.Drawing.Size(87, 13);
            this.lblOutputFolder.TabIndex = 26;
            this.lblOutputFolder.Text = "Целевая папка:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbViews);
            this.groupBox1.Controls.Add(this.cbTypes);
            this.groupBox1.Controls.Add(this.cbTriggers);
            this.groupBox1.Controls.Add(this.cbTables);
            this.groupBox1.Controls.Add(this.cbSynonyms);
            this.groupBox1.Controls.Add(this.cbSequences);
            this.groupBox1.Controls.Add(this.cbProcedures);
            this.groupBox1.Controls.Add(this.cbPackages);
            this.groupBox1.Controls.Add(this.cbJobs);
            this.groupBox1.Controls.Add(this.cbFunctions);
            this.groupBox1.Controls.Add(this.cbDBlinks);
            this.groupBox1.Location = new System.Drawing.Point(3, 9);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(393, 118);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Тип объектов:";
            // 
            // cbViews
            // 
            this.cbViews.AutoSize = true;
            this.cbViews.Location = new System.Drawing.Point(314, 65);
            this.cbViews.Name = "cbViews";
            this.cbViews.Size = new System.Drawing.Size(53, 17);
            this.cbViews.TabIndex = 10;
            this.cbViews.Text = "views";
            this.cbViews.UseVisualStyleBackColor = true;
            // 
            // cbTypes
            // 
            this.cbTypes.AutoSize = true;
            this.cbTypes.Location = new System.Drawing.Point(314, 42);
            this.cbTypes.Name = "cbTypes";
            this.cbTypes.Size = new System.Drawing.Size(51, 17);
            this.cbTypes.TabIndex = 9;
            this.cbTypes.Text = "types";
            this.cbTypes.UseVisualStyleBackColor = true;
            // 
            // cbTriggers
            // 
            this.cbTriggers.AutoSize = true;
            this.cbTriggers.Location = new System.Drawing.Point(314, 19);
            this.cbTriggers.Name = "cbTriggers";
            this.cbTriggers.Size = new System.Drawing.Size(60, 17);
            this.cbTriggers.TabIndex = 8;
            this.cbTriggers.Text = "triggers";
            this.cbTriggers.UseVisualStyleBackColor = true;
            // 
            // cbTables
            // 
            this.cbTables.AutoSize = true;
            this.cbTables.Location = new System.Drawing.Point(172, 88);
            this.cbTables.Name = "cbTables";
            this.cbTables.Size = new System.Drawing.Size(54, 17);
            this.cbTables.TabIndex = 7;
            this.cbTables.Text = "tables";
            this.cbTables.UseVisualStyleBackColor = true;
            // 
            // cbSynonyms
            // 
            this.cbSynonyms.AutoSize = true;
            this.cbSynonyms.Location = new System.Drawing.Point(172, 65);
            this.cbSynonyms.Name = "cbSynonyms";
            this.cbSynonyms.Size = new System.Drawing.Size(72, 17);
            this.cbSynonyms.TabIndex = 6;
            this.cbSynonyms.Text = "synonyms";
            this.cbSynonyms.UseVisualStyleBackColor = true;
            // 
            // cbSequences
            // 
            this.cbSequences.AutoSize = true;
            this.cbSequences.Location = new System.Drawing.Point(172, 42);
            this.cbSequences.Name = "cbSequences";
            this.cbSequences.Size = new System.Drawing.Size(78, 17);
            this.cbSequences.TabIndex = 5;
            this.cbSequences.Text = "sequences";
            this.cbSequences.UseVisualStyleBackColor = true;
            // 
            // cbProcedures
            // 
            this.cbProcedures.AutoSize = true;
            this.cbProcedures.Location = new System.Drawing.Point(172, 19);
            this.cbProcedures.Name = "cbProcedures";
            this.cbProcedures.Size = new System.Drawing.Size(79, 17);
            this.cbProcedures.TabIndex = 4;
            this.cbProcedures.Text = "procedures";
            this.cbProcedures.UseVisualStyleBackColor = true;
            // 
            // cbPackages
            // 
            this.cbPackages.AutoSize = true;
            this.cbPackages.Location = new System.Drawing.Point(15, 88);
            this.cbPackages.Name = "cbPackages";
            this.cbPackages.Size = new System.Drawing.Size(73, 17);
            this.cbPackages.TabIndex = 3;
            this.cbPackages.Text = "packages";
            this.cbPackages.UseVisualStyleBackColor = true;
            // 
            // cbJobs
            // 
            this.cbJobs.AutoSize = true;
            this.cbJobs.Location = new System.Drawing.Point(15, 65);
            this.cbJobs.Name = "cbJobs";
            this.cbJobs.Size = new System.Drawing.Size(45, 17);
            this.cbJobs.TabIndex = 2;
            this.cbJobs.Text = "jobs";
            this.cbJobs.UseVisualStyleBackColor = true;
            // 
            // cbFunctions
            // 
            this.cbFunctions.AutoSize = true;
            this.cbFunctions.Location = new System.Drawing.Point(15, 42);
            this.cbFunctions.Name = "cbFunctions";
            this.cbFunctions.Size = new System.Drawing.Size(69, 17);
            this.cbFunctions.TabIndex = 1;
            this.cbFunctions.Text = "functions";
            this.cbFunctions.UseVisualStyleBackColor = true;
            // 
            // cbDBlinks
            // 
            this.cbDBlinks.AutoSize = true;
            this.cbDBlinks.Location = new System.Drawing.Point(15, 19);
            this.cbDBlinks.Name = "cbDBlinks";
            this.cbDBlinks.Size = new System.Drawing.Size(59, 17);
            this.cbDBlinks.TabIndex = 0;
            this.cbDBlinks.Text = "dblinks";
            this.cbDBlinks.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1193, 673);
            this.Controls.Add(this.splitContainer1);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Выгрузка DDL";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.gridConnections)).EndInit();
            this.panel7.ResumeLayout(false);
            this.panel7.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel6.ResumeLayout(false);
            this.panel5.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.DataGridView gridConnections;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colChecked;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDB;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSchema;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox cbViews;
        private System.Windows.Forms.CheckBox cbTypes;
        private System.Windows.Forms.CheckBox cbTriggers;
        private System.Windows.Forms.CheckBox cbTables;
        private System.Windows.Forms.CheckBox cbSynonyms;
        private System.Windows.Forms.CheckBox cbSequences;
        private System.Windows.Forms.CheckBox cbProcedures;
        private System.Windows.Forms.CheckBox cbPackages;
        private System.Windows.Forms.CheckBox cbJobs;
        private System.Windows.Forms.CheckBox cbFunctions;
        private System.Windows.Forms.CheckBox cbDBlinks;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox cbGetOnlyFirstPart;
        private System.Windows.Forms.CheckBox cbSetSeqValTo1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbNameObjectMaskExclude;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbNameObjectMaskInclude;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Label lblOutputFolder;
        private System.Windows.Forms.Panel panel6;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.TabControl tabCtrlLog;
        private System.Windows.Forms.Panel panel7;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.Label lblPassword;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.TextBox txtSID;
        private System.Windows.Forms.Label lblSID;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Label lblHost;
    }
}


namespace VcsManager.WinForm
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtTab1ProcessId = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtTab1VcsFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtTab1RepoList = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTab1OutDir = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.label5 = new System.Windows.Forms.Label();
            this.txtTab1CommitDate = new System.Windows.Forms.TextBox();
            this.btTab1Process = new System.Windows.Forms.Button();
            this.btTab1FillRepoList = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(845, 503);
            this.tabControl1.TabIndex = 9;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.btTab1FillRepoList);
            this.tabPage1.Controls.Add(this.btTab1Process);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.txtTab1VcsFolder);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.txtTab1RepoList);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.txtTab1OutDir);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(837, 477);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Controls.Add(this.txtTab1CommitDate);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtTab1ProcessId);
            this.groupBox1.Location = new System.Drawing.Point(10, 352);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(817, 82);
            this.groupBox1.TabIndex = 18;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Снимок по состоянию на:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 28);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(369, 13);
            this.label4.TabIndex = 18;
            this.label4.Text = "Номер коммита (1-й приоритет). Если заполнен, учитываем только его";
            // 
            // txtTab1ProcessId
            // 
            this.txtTab1ProcessId.Location = new System.Drawing.Point(623, 21);
            this.txtTab1ProcessId.Name = "txtTab1ProcessId";
            this.txtTab1ProcessId.Size = new System.Drawing.Size(188, 20);
            this.txtTab1ProcessId.TabIndex = 17;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 35);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(147, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Путь к папке репозиториев";
            // 
            // txtTab1VcsFolder
            // 
            this.txtTab1VcsFolder.Location = new System.Drawing.Point(250, 32);
            this.txtTab1VcsFolder.Name = "txtTab1VcsFolder";
            this.txtTab1VcsFolder.Size = new System.Drawing.Size(577, 20);
            this.txtTab1VcsFolder.TabIndex = 13;
            this.txtTab1VcsFolder.Text = "y:\\data\\repo\\";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(183, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Список репозиториев к обработке";
            // 
            // txtTab1RepoList
            // 
            this.txtTab1RepoList.Location = new System.Drawing.Point(250, 58);
            this.txtTab1RepoList.Multiline = true;
            this.txtTab1RepoList.Name = "txtTab1RepoList";
            this.txtTab1RepoList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtTab1RepoList.Size = new System.Drawing.Size(577, 288);
            this.txtTab1RepoList.TabIndex = 11;
            this.txtTab1RepoList.Text = "ORCL12\\F3_DATA";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(136, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Снимки сформировать в:";
            // 
            // txtTab1OutDir
            // 
            this.txtTab1OutDir.Location = new System.Drawing.Point(250, 6);
            this.txtTab1OutDir.Name = "txtTab1OutDir";
            this.txtTab1OutDir.Size = new System.Drawing.Size(577, 20);
            this.txtTab1OutDir.TabIndex = 9;
            this.txtTab1OutDir.Text = "e:\\tmp\\11\\";
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1317, 616);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 54);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(449, 13);
            this.label5.TabIndex = 20;
            this.label5.Text = "Дату (в формате yyyy-mm-dd) (2-й приоритет). Если не заполнен номер, учитываем да" +
    "ту";
            // 
            // txtTab1CommitDate
            // 
            this.txtTab1CommitDate.Location = new System.Drawing.Point(623, 47);
            this.txtTab1CommitDate.Name = "txtTab1CommitDate";
            this.txtTab1CommitDate.Size = new System.Drawing.Size(188, 20);
            this.txtTab1CommitDate.TabIndex = 19;
            // 
            // btTab1Process
            // 
            this.btTab1Process.Location = new System.Drawing.Point(617, 440);
            this.btTab1Process.Name = "btTab1Process";
            this.btTab1Process.Size = new System.Drawing.Size(212, 23);
            this.btTab1Process.TabIndex = 19;
            this.btTab1Process.Text = "Создать снимок";
            this.btTab1Process.UseVisualStyleBackColor = true;
            this.btTab1Process.Click += new System.EventHandler(this.btTab1Process_Click);
            // 
            // btTab1FillRepoList
            // 
            this.btTab1FillRepoList.Location = new System.Drawing.Point(10, 440);
            this.btTab1FillRepoList.Name = "btTab1FillRepoList";
            this.btTab1FillRepoList.Size = new System.Drawing.Size(338, 23);
            this.btTab1FillRepoList.TabIndex = 20;
            this.btTab1FillRepoList.Text = "Заполнить окно списком репозиториев из папки";
            this.btTab1FillRepoList.UseVisualStyleBackColor = true;
            this.btTab1FillRepoList.Click += new System.EventHandler(this.btTab1FillRepoList_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(845, 503);
            this.Controls.Add(this.tabControl1);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtTab1VcsFolder;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtTab1RepoList;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTab1OutDir;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtTab1ProcessId;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtTab1CommitDate;
        private System.Windows.Forms.Button btTab1Process;
        private System.Windows.Forms.Button btTab1FillRepoList;
    }
}


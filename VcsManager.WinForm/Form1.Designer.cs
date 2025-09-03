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
            this.txtInputFolder = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtRepoList = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtVcsFolder = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtProcessId = new System.Windows.Forms.TextBox();
            this.btnCreateCommit = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtInputFolder
            // 
            this.txtInputFolder.Location = new System.Drawing.Point(201, 35);
            this.txtInputFolder.Name = "txtInputFolder";
            this.txtInputFolder.Size = new System.Drawing.Size(577, 20);
            this.txtInputFolder.TabIndex = 0;
            this.txtInputFolder.Text = "e:\\tmp\\10\\";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(123, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Путь к исходной папке";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(183, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Список репозиториев к обработке";
            // 
            // txtRepoList
            // 
            this.txtRepoList.Location = new System.Drawing.Point(201, 77);
            this.txtRepoList.Name = "txtRepoList";
            this.txtRepoList.Size = new System.Drawing.Size(577, 20);
            this.txtRepoList.TabIndex = 2;
            this.txtRepoList.Text = "ORCL12\\F3_DATA";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 121);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(147, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Путь к папке репозиториев";
            // 
            // txtVcsFolder
            // 
            this.txtVcsFolder.Location = new System.Drawing.Point(201, 118);
            this.txtVcsFolder.Name = "txtVcsFolder";
            this.txtVcsFolder.Size = new System.Drawing.Size(577, 20);
            this.txtVcsFolder.TabIndex = 4;
            this.txtVcsFolder.Text = "e:\\tmp\\dbvcs\\";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 163);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(140, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Номер текущего коммита";
            // 
            // txtProcessId
            // 
            this.txtProcessId.Location = new System.Drawing.Point(201, 160);
            this.txtProcessId.Name = "txtProcessId";
            this.txtProcessId.Size = new System.Drawing.Size(577, 20);
            this.txtProcessId.TabIndex = 6;
            this.txtProcessId.Text = "1";
            // 
            // btnCreateCommit
            // 
            this.btnCreateCommit.Location = new System.Drawing.Point(201, 210);
            this.btnCreateCommit.Name = "btnCreateCommit";
            this.btnCreateCommit.Size = new System.Drawing.Size(192, 23);
            this.btnCreateCommit.TabIndex = 8;
            this.btnCreateCommit.Text = "Создать коммит";
            this.btnCreateCommit.UseVisualStyleBackColor = true;
            this.btnCreateCommit.Click += new System.EventHandler(this.btnCreateCommit_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 256);
            this.Controls.Add(this.btnCreateCommit);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtProcessId);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtVcsFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtRepoList);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtInputFolder);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtInputFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtRepoList;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtVcsFolder;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtProcessId;
        private System.Windows.Forms.Button btnCreateCommit;
    }
}


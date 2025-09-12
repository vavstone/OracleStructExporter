namespace TextProc
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
            this.label1 = new System.Windows.Forms.Label();
            this.tbVar1 = new System.Windows.Forms.TextBox();
            this.tbVar2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btDirect = new System.Windows.Forms.Button();
            this.btBack = new System.Windows.Forms.Button();
            this.btGetVar1FromFile = new System.Windows.Forms.Button();
            this.btGetVar2FromFile = new System.Windows.Forms.Button();
            this.btSaveVar2ToFile = new System.Windows.Forms.Button();
            this.btSaveVar1ToFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Вариант 1";
            // 
            // tbVar1
            // 
            this.tbVar1.Location = new System.Drawing.Point(76, 6);
            this.tbVar1.Multiline = true;
            this.tbVar1.Name = "tbVar1";
            this.tbVar1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbVar1.Size = new System.Drawing.Size(1160, 255);
            this.tbVar1.TabIndex = 1;
            // 
            // tbVar2
            // 
            this.tbVar2.Location = new System.Drawing.Point(76, 299);
            this.tbVar2.Multiline = true;
            this.tbVar2.Name = "tbVar2";
            this.tbVar2.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbVar2.Size = new System.Drawing.Size(1160, 245);
            this.tbVar2.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 302);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Вариант 2";
            // 
            // btDirect
            // 
            this.btDirect.Location = new System.Drawing.Point(442, 582);
            this.btDirect.Name = "btDirect";
            this.btDirect.Size = new System.Drawing.Size(75, 23);
            this.btDirect.TabIndex = 4;
            this.btDirect.Text = "Прямая";
            this.btDirect.UseVisualStyleBackColor = true;
            this.btDirect.Click += new System.EventHandler(this.btDirect_Click);
            // 
            // btBack
            // 
            this.btBack.Location = new System.Drawing.Point(695, 582);
            this.btBack.Name = "btBack";
            this.btBack.Size = new System.Drawing.Size(75, 23);
            this.btBack.TabIndex = 5;
            this.btBack.Text = "Обратная";
            this.btBack.UseVisualStyleBackColor = true;
            this.btBack.Click += new System.EventHandler(this.btBack_Click);
            // 
            // btGetVar1FromFile
            // 
            this.btGetVar1FromFile.Location = new System.Drawing.Point(381, 267);
            this.btGetVar1FromFile.Name = "btGetVar1FromFile";
            this.btGetVar1FromFile.Size = new System.Drawing.Size(205, 23);
            this.btGetVar1FromFile.TabIndex = 6;
            this.btGetVar1FromFile.Text = "Загрузить вариант 1 из файла";
            this.btGetVar1FromFile.UseVisualStyleBackColor = true;
            this.btGetVar1FromFile.Click += new System.EventHandler(this.btGetVar1FromFile_Click);
            // 
            // btGetVar2FromFile
            // 
            this.btGetVar2FromFile.Location = new System.Drawing.Point(381, 550);
            this.btGetVar2FromFile.Name = "btGetVar2FromFile";
            this.btGetVar2FromFile.Size = new System.Drawing.Size(205, 23);
            this.btGetVar2FromFile.TabIndex = 7;
            this.btGetVar2FromFile.Text = "Загрузить вариант 2 из файла";
            this.btGetVar2FromFile.UseVisualStyleBackColor = true;
            this.btGetVar2FromFile.Click += new System.EventHandler(this.btGetVar2FromFile_Click);
            // 
            // btSaveVar2ToFile
            // 
            this.btSaveVar2ToFile.Location = new System.Drawing.Point(635, 550);
            this.btSaveVar2ToFile.Name = "btSaveVar2ToFile";
            this.btSaveVar2ToFile.Size = new System.Drawing.Size(205, 23);
            this.btSaveVar2ToFile.TabIndex = 9;
            this.btSaveVar2ToFile.Text = "Сохранить вариант 2 в файл";
            this.btSaveVar2ToFile.UseVisualStyleBackColor = true;
            this.btSaveVar2ToFile.Click += new System.EventHandler(this.btSaveVar2ToFile_Click);
            // 
            // btSaveVar1ToFile
            // 
            this.btSaveVar1ToFile.Location = new System.Drawing.Point(635, 267);
            this.btSaveVar1ToFile.Name = "btSaveVar1ToFile";
            this.btSaveVar1ToFile.Size = new System.Drawing.Size(205, 23);
            this.btSaveVar1ToFile.TabIndex = 8;
            this.btSaveVar1ToFile.Text = "Сохранить вариант 1 в файл";
            this.btSaveVar1ToFile.UseVisualStyleBackColor = true;
            this.btSaveVar1ToFile.Click += new System.EventHandler(this.btSaveVar1ToFile_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1241, 617);
            this.Controls.Add(this.btSaveVar2ToFile);
            this.Controls.Add(this.btSaveVar1ToFile);
            this.Controls.Add(this.btGetVar2FromFile);
            this.Controls.Add(this.btGetVar1FromFile);
            this.Controls.Add(this.btBack);
            this.Controls.Add(this.btDirect);
            this.Controls.Add(this.tbVar2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbVar1);
            this.Controls.Add(this.label1);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Обработка текста";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbVar1;
        private System.Windows.Forms.TextBox tbVar2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btDirect;
        private System.Windows.Forms.Button btBack;
        private System.Windows.Forms.Button btGetVar1FromFile;
        private System.Windows.Forms.Button btGetVar2FromFile;
        private System.Windows.Forms.Button btSaveVar2ToFile;
        private System.Windows.Forms.Button btSaveVar1ToFile;
    }
}


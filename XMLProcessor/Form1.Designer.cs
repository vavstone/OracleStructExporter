namespace XMLProcessor
{
    partial class Form1
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
            this.btXMLBeauty = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tbInXML = new System.Windows.Forms.TextBox();
            this.tbXSD = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btXMLValidate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btXMLBeauty
            // 
            this.btXMLBeauty.Location = new System.Drawing.Point(54, 149);
            this.btXMLBeauty.Name = "btXMLBeauty";
            this.btXMLBeauty.Size = new System.Drawing.Size(238, 23);
            this.btXMLBeauty.TabIndex = 0;
            this.btXMLBeauty.Text = "Улучшить xml";
            this.btXMLBeauty.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Исходный xml:";
            // 
            // tbInXML
            // 
            this.tbInXML.Location = new System.Drawing.Point(116, 38);
            this.tbInXML.Name = "tbInXML";
            this.tbInXML.Size = new System.Drawing.Size(660, 20);
            this.tbInXML.TabIndex = 2;
            // 
            // tbXSD
            // 
            this.tbXSD.Location = new System.Drawing.Point(116, 90);
            this.tbXSD.Name = "tbXSD";
            this.tbXSD.Size = new System.Drawing.Size(660, 20);
            this.tbXSD.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Схема XSD:";
            // 
            // btXMLValidate
            // 
            this.btXMLValidate.Location = new System.Drawing.Point(473, 149);
            this.btXMLValidate.Name = "btXMLValidate";
            this.btXMLValidate.Size = new System.Drawing.Size(238, 23);
            this.btXMLValidate.TabIndex = 5;
            this.btXMLValidate.Text = "Валидировать xml";
            this.btXMLValidate.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 204);
            this.Controls.Add(this.btXMLValidate);
            this.Controls.Add(this.tbXSD);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbInXML);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btXMLBeauty);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btXMLBeauty;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbInXML;
        private System.Windows.Forms.TextBox tbXSD;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btXMLValidate;
    }
}


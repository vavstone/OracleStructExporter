using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TextProc.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TextProc
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void btDirect_Click(object sender, EventArgs e)
        {
            var inText = tbVar1.Text;
            var data = new HData();
            data.Value = inText;
            try
            {
                var res = Processor.Direct(data, " ");
                tbVar2.Text = res.Value;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }

        private void btBack_Click(object sender, EventArgs e)
        {
            var data = new HData();
            data.Value = tbVar2.Text;
            try
            {
                var res = Processor.Back(data, " ");
                tbVar1.Text = res.Value;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message + Environment.NewLine + exception.StackTrace);
            }
        }

        static string GetTextFromDialog()
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                return File.ReadAllText(file);
            }
            return "";
        }

        static void SaveTextToFile(string text)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                return;
            // получаем выбранный файл
            string filename = saveFileDialog.FileName;
            // сохраняем текст в файл
            System.IO.File.WriteAllText(filename, text);
            MessageBox.Show("Файл сохранен");
        }

        private void btGetVar1FromFile_Click(object sender, EventArgs e)
        {
            tbVar1.Text = GetTextFromDialog();
        }

        private void btGetVar2FromFile_Click(object sender, EventArgs e)
        {
            tbVar2.Text = GetTextFromDialog();
        }

        private void btSaveVar1ToFile_Click(object sender, EventArgs e)
        {
            SaveTextToFile(tbVar1.Text);
        }

        private void btSaveVar2ToFile_Click(object sender, EventArgs e)
        {
            SaveTextToFile(tbVar2.Text);
        }
    }
}


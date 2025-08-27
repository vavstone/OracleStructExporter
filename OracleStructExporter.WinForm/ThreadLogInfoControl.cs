using OracleStructExporter.Core;
using System.Windows.Forms;

namespace OracleStructExporter.WinForm
{
    public partial class ThreadLogInfoControl : UserControl
    {
        public Connection Connection;
        public ThreadLogInfoControl()
        {
            InitializeComponent();
        }

        public void SetProgressStatus(string text)
        {
            lblProgressStatus.Text = text;
        }

        public void SetLblStatus(string text)
        {
            lblStatus.Text = text;
        }

        public void ResetProgressBar()
        {
            progressBar.Style = ProgressBarStyle.Marquee;
        }

        //public void ResetControls()
        //{
        //    txtLog.Clear();
        //    progressBar.Style = ProgressBarStyle.Marquee;
        //}

        public void AppendText(string text)
        {
            txtLog.AppendText(text);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        public void SetProgressStyleIfFirstProgress(int totalObjects)
        {
            if (progressBar.Style == ProgressBarStyle.Marquee && totalObjects > 0)
            {
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Minimum = 0;
                progressBar.Maximum = totalObjects;
            }
        }

        public void SetProgressValue(int current)
        {
            progressBar.Value = current;
        }

        private void lblProgressStatus_Click(object sender, System.EventArgs e)
        {

        }
    }
}

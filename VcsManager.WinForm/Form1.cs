using System;
using System.Linq;
using System.Windows.Forms;

namespace VcsManager.WinForm
{
    public partial class MainForm : Form
    {
        private readonly VcsManager _vcsManager = new VcsManager();

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnCreateCommit_Click(object sender, EventArgs e)
        {
            string inputFolder = txtInputFolder.Text;
            string vcsFolder = txtVcsFolder.Text;
            int processId = int.Parse(txtProcessId.Text);
            var repolist = txtRepoList.Text.Split(';').ToList();

            _vcsManager.CreateCommit(inputFolder, repolist, vcsFolder, processId);
            MessageBox.Show("Коммит создан успешно!");
        }
    }
}

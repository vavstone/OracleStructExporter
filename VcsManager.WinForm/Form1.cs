using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ServiceCheck.Core;

namespace VcsManager.WinForm
{
    public partial class MainForm : Form
    {
        private readonly ServiceCheck.Core.VcsManager _vcsManager = new ServiceCheck.Core.VcsManager();

        public MainForm()
        {
            InitializeComponent();
        }

        //private void btnCreateCommit_Click(object sender, EventArgs e)
        //{
        //    string inputFolder = txtInputFolder.Text;
        //    string vcsFolder = txtVcsFolder.Text;
        //    int processId = int.Parse(txtProcessId.Text);
        //    var repolist = txtRepoList.Text.Split(';').ToList();

        //    _vcsManager.CreateCommit(inputFolder, repolist, vcsFolder, processId);
        //    MessageBox.Show("Коммит создан успешно!");
        //}

        private void btnCreateCommit_Click_1(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtTab1CommitDate.Text = DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void btTab1Process_Click(object sender, EventArgs e)
        {
            var repoName = txtTab1RepoList.Text;
            var repoFolder = txtTab1VcsFolder.Text;
            var outFolder = txtTab1OutDir.Text;
            int? commitNum = default;
            DateTime? commitDate = default;
            var txtCommitNum = txtTab1ProcessId.Text;
            var txtCommitDate = txtTab1CommitDate.Text;
            if (int.TryParse(txtCommitNum, out int tmpInt))
                commitNum = tmpInt;
            if (DateTime.TryParse(txtCommitDate, out DateTime tmpDate))
                commitDate = tmpDate;

            var connToProcess = repoName.Split(new []{";",Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var conn in connToProcess)
            {
                var outFolderForRepo = Path.Combine(outFolder, conn);
                var res = _vcsManager.CreateRepoSnapshot(conn, repoFolder, outFolderForRepo, commitNum, commitDate,
                    out List<CommitShortInfo> commitShortInfoList);
                var commitsJournalFilePath = Path.Combine(outFolder, "journal");
                if (!Directory.Exists(commitsJournalFilePath)) Directory.CreateDirectory(commitsJournalFilePath);
                var commitsJournalFileFullName = Path.Combine(commitsJournalFilePath, conn.Replace("\\","_") + ".csv");
                ServiceCheck.Core.VcsManager.SaveCommitShortInfoList(commitShortInfoList, commitsJournalFileFullName);
            }
            MessageBox.Show("Готово!");
        }

        private void btTab1FillRepoList_Click(object sender, EventArgs e)
        {
            var repoFolder = txtTab1VcsFolder.Text;
            var repoList = ServiceCheck.Core.VcsManager.GetDBAndUserNameListFromRepoFolder(repoFolder).OrderBy(c => c);
            txtTab1RepoList.Text = "";
            foreach (var repoItem in repoList)
            {
                txtTab1RepoList.Text += repoItem + Environment.NewLine;
            }
        }
    }
}

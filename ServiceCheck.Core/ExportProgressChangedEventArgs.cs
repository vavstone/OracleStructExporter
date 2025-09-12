using System;

namespace ServiceCheck.Core
{
    public class ExportProgressChangedEventArgs : EventArgs
    {
        public ExportProgressData Progress { get; private set; }

        public ExportProgressChangedEventArgs(ExportProgressData progress)
        {
            this.Progress = progress;
        }
    }
}

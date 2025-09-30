using System;

namespace ServiceCheck.Core
{
    public class ExportProgressChangedEventArgsOuter : EventArgs
    {
        public ExportProgressDataOuter Progress { get; private set; }

        public ExportProgressChangedEventArgsOuter(ExportProgressDataOuter progress)
        {
            this.Progress = progress;
        }
    }
}

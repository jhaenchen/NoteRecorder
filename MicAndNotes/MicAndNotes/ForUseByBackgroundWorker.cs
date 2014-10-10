using System;

namespace MicAndNotes
{
    internal class ForUseByBackgroundWorker
    {
        public readonly string FilePath;
        public readonly TimeSpan Span;

        public ForUseByBackgroundWorker(TimeSpan span, string path)
        {
            Span = span;
            FilePath = path;
        }
    }
}

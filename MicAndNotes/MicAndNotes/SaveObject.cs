using System;
using System.Collections.Generic;

namespace MicAndNotes
{
    [Serializable]
    internal class SaveObject
    {
        public readonly long RecordingDuration;
        public readonly string RecordingFilename;
        public readonly List<TimeNote> SectionOccurances;
        public readonly string TheNote;


        public SaveObject(long duration, List<TimeNote> occurances, string note, string recordingname)
        {
            RecordingDuration = duration;
            SectionOccurances = occurances;
            TheNote = note;
            RecordingFilename = recordingname;
        }
    }
}

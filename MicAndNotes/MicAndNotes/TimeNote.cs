using System;

namespace MicAndNotes
{
    [Serializable]
    internal class TimeNote
    {
        public string Note;
        public TimeSpan Occurance;

        public TimeNote(TimeSpan span, string note)
        {
            Occurance = span;
            Note = note;
        }

        public string Serialize()
        {
            return Occurance + "," + Note + ",";
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MicAndNotes
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MediaPlayer ClassPlayer = new MediaPlayer();
        private readonly BackgroundWorker bw = new BackgroundWorker();
        private readonly Stopwatch recordingTimer = new Stopwatch();
        private string lastTextFilled = "";
        private long recordingDuration;
        private string savedRecordingAs;
        private bool shouldContinue = true;
        private string textBackup;
        private List<TimeNote> timesForNote = new List<TimeNote>();
        private bool wasLastKeyEnter;

        public MainWindow()
        {
            InitializeComponent();
            bw.WorkerSupportsCancellation = false;
            bw.WorkerReportsProgress = false;
            bw.DoWork +=
                bw_DoWork;
        }

        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true,
            ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength,
            int hwndCallback);


        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && wasLastKeyEnter)
            {
                string toStore = Textbox.Text;
                if (lastTextFilled != "")
                {
                    toStore = Textbox.Text.Replace(lastTextFilled, String.Empty);
                }
                lastTextFilled = Textbox.Text;


                timesForNote.Add(new TimeNote(recordingTimer.Elapsed, toStore));
                wasLastKeyEnter = false;
            }
            else if (e.Key == Key.Enter)
            {
                wasLastKeyEnter = true;
            }
            else
            {
                wasLastKeyEnter = false;
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            RecordingIcon.Visibility = Visibility.Visible;
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            recordingTimer.Start();
            mciSendString("record recsound", "", 0, 0);
        }


        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            var b = (forUseByBackgroundWorker) e.Argument;
            var play = new MediaPlayer();

            play.Open(new Uri(b.filePath));
            // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
            play.Position = b.t;
            play.Play();
        }

        private void PlayBackButton_Click(object sender, RoutedEventArgs e)
        {
            string textArchive = textBackup;
            var playbackStartPoint = new TimeSpan(0);
            var forPlayback = new forUseByBackgroundWorker(playbackStartPoint, savedRecordingAs);


            ClassPlayer.Open(new Uri(savedRecordingAs));
            // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
            ClassPlayer.Position = playbackStartPoint;
            ClassPlayer.Play();

            //var play = new MediaPlayer();

            //play.Open(new Uri(forPlayback.filePath));
            //play.Position = forPlayback.t;
            //play.Play();


            //bw.RunWorkerAsync(forPlayback);
            ThreadPool.QueueUserWorkItem(o =>
            {
                shouldContinue = true;
                int counter = 0;
                var playBackStopwatch = new Stopwatch();

                playBackStopwatch.Start();

                while (shouldContinue)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (playBackStopwatch.Elapsed + playbackStartPoint < timesForNote[counter].occurance)
                            {
                                Textbox.Text = timesForNote[counter].note;
                                theSlider.Value = timesForNote[counter].occurance.Ticks;
                            }
                            else
                            {
                                counter++;
                            }
                        }
                        catch (Exception)
                        {
                            shouldContinue = false;
                            Textbox.Text = textArchive;
                            Textbox.ScrollToEnd();
                            theSlider.Value = 0;
                        }
                    }));
                    Thread.Sleep(1);
                }
            });
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingIcon.Visibility = Visibility.Hidden;
            string savedFileName = Guid.NewGuid().ToString();
            savedRecordingAs = Environment.CurrentDirectory + savedFileName + ".wav";
            recordingTimer.Stop();
            recordingDuration = recordingTimer.Elapsed.Ticks;
            mciSendString("save recsound " + Environment.CurrentDirectory + savedFileName + ".wav", "", 0, 0);
            mciSendString("close recsound ", "", 0, 0);
            theSlider.Maximum = recordingDuration;
            textBackup = Textbox.Text;
        }


        private void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            var toStart = new TimeSpan();
            int toPlayIndex = Textbox.CaretIndex;
            int lineIndex = Textbox.GetLineIndexFromCharacterIndex(toPlayIndex);
            String text = Textbox.GetLineText(lineIndex);
            for (int i = 0; i < timesForNote.Count; i++)
            {
                if (timesForNote[i].note.Contains(text))
                {
                    if (i > 0)
                    {
                        toStart = timesForNote[i - 1].occurance;
                    }
                    else
                    {
                        toStart = new TimeSpan(0);
                    }
                }
            }


            string textArchive = textBackup;

            var forPlayback = new forUseByBackgroundWorker(toStart, savedRecordingAs);

            ClassPlayer.Open(new Uri(savedRecordingAs));
            // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
            ClassPlayer.Position = toStart;
            ClassPlayer.Play();


            //bw.RunWorkerAsync(forPlayback);
            ThreadPool.QueueUserWorkItem(o =>
            {
                shouldContinue = true;
                int counter = 0;
                var playBackStopwatch = new Stopwatch();

                playBackStopwatch.Start();

                while (shouldContinue)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (playBackStopwatch.Elapsed + toStart < timesForNote[counter].occurance)
                            {
                                Textbox.Text = timesForNote[counter].note;
                                theSlider.Value = timesForNote[counter].occurance.Ticks;
                            }
                            else
                            {
                                counter++;
                            }
                        }
                        catch (Exception)
                        {
                            shouldContinue = false;
                            Textbox.Text = textArchive;
                            Textbox.ScrollToEnd();
                            theSlider.Value = 0;
                        }
                    }));
                    Thread.Sleep(1);
                }
            });
        }


        private void theSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sliderValue = new TimeSpan((long) theSlider.Value);
            for (int i = 0; i < timesForNote.Count - 1; i++)
            {
                int theFirst = timesForNote[i].occurance.CompareTo(sliderValue);
                int theSecond = timesForNote[i + 1].occurance.CompareTo(sliderValue);
                if (theFirst == -1 && theSecond == 1)
                {
                    Textbox.Text = timesForNote[i + 1].note;
                    currentlyViewedSection = i + 1;
                    break;
                }

                if (i == 0 && timesForNote[i].occurance.CompareTo(sliderValue) == 1)
                {
                    Textbox.Text = timesForNote[i].note;
                    currentlyViewedSection = i;
                    break;
                }
                Textbox.Text = timesForNote[i + 1].note;
                currentlyViewedSection = i + 1;
            }
        }

        private int currentlyViewedSection = 0;
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() != true) return;
            var toSave = new SaveObject(recordingDuration, timesForNote, textBackup,
                saveFileDialog1.FileName + "\\recording.wav");
            Directory.CreateDirectory(saveFileDialog1.FileName);
            Stream stream = File.Open(saveFileDialog1.FileName + "\\data.nr", FileMode.Create);
            var bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, toSave);
            stream.Close();
            File.Move(savedRecordingAs, saveFileDialog1.FileName + "\\recording.wav");
            File.Delete(savedRecordingAs);
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Note Recorder Files|*.nr";
            DialogResult result = openFileDialog1.ShowDialog();
            if (result.ToString() == "OK") // Test result.
            {
                object objectToSerialize;
                Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open);
                var bFormatter = new BinaryFormatter();
                var recoveredState = (SaveObject) bFormatter.Deserialize(stream);
                stream.Close();
                timesForNote = recoveredState.sectionOccurances;
                Textbox.Text = recoveredState.theNote;
                textBackup = recoveredState.theNote;
                recordingDuration = recoveredState.recordingDuration;
                theSlider.Maximum = recordingDuration;
                savedRecordingAs = recoveredState.recordingFilename;
            }
        }

        private void StopButton1_Click(object sender, RoutedEventArgs e)
        {
            ClassPlayer.Stop();
            shouldContinue = false;


            //var play = new MediaPlayer();

            //play.Open(new Uri(forPlayback.filePath));
            //play.Position = forPlayback.t;
            //play.Play();


            //bw.RunWorkerAsync(forPlayback);
            ThreadPool.QueueUserWorkItem(o =>
            {
                bool tempBoolWhy = true;
                while (tempBoolWhy)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (tempBoolWhy)
                            {
                                Textbox.Text = textBackup;
                                //theSlider.Value = 0;
                                tempBoolWhy = false;
                            }
                        }
                        catch (Exception)
                        {
                            shouldContinue = false;

                            Textbox.ScrollToEnd();
                            theSlider.Value = 0;
                        }
                    }));
                    Thread.Sleep(1);
                }
            });
        }

        private void ToolbarOpen_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            var openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Note Recorder Files|*.nr";
            DialogResult result = openFileDialog1.ShowDialog();
            if (result.ToString() == "OK") // Test result.
            {
                object objectToSerialize;
                Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open);
                var bFormatter = new BinaryFormatter();
                var recoveredState = (SaveObject) bFormatter.Deserialize(stream);
                stream.Close();
                timesForNote = recoveredState.sectionOccurances;
                Textbox.Text = recoveredState.theNote;
                textBackup = recoveredState.theNote;
                recordingDuration = recoveredState.recordingDuration;
                theSlider.Maximum = recordingDuration;
                savedRecordingAs = recoveredState.recordingFilename;
            }
        }

        private void ToolbarSave_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() != true) return;
            var toSave = new SaveObject(recordingDuration, timesForNote, textBackup,
                saveFileDialog1.FileName + "\\recording.wav");
            Directory.CreateDirectory(saveFileDialog1.FileName);
            Stream stream = File.Open(saveFileDialog1.FileName + "\\data.nr", FileMode.Create);
            var bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, toSave);
            stream.Close();
            File.Move(savedRecordingAs, saveFileDialog1.FileName + "\\recording.wav");
            File.Delete(savedRecordingAs);
        }

        private void ToolbarClose_Click(object sender, RoutedEventArgs e)
        {
            //Close();
        }

        private void ToolbarRecord_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            RecordingIcon.Visibility = Visibility.Visible;
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            recordingTimer.Start();
            mciSendString("record recsound", "", 0, 0);
        }

        private void ToolbarStopRecording_Click(object sender, RoutedEventArgs e)
        {
            RecordingIcon.Visibility = Visibility.Hidden;
            string savedFileName = Guid.NewGuid().ToString();
            savedRecordingAs = Environment.CurrentDirectory + savedFileName + ".wav";
            recordingTimer.Stop();
            recordingDuration = recordingTimer.Elapsed.Ticks;
            mciSendString("save recsound " + Environment.CurrentDirectory + savedFileName + ".wav", "", 0, 0);
            mciSendString("close recsound ", "", 0, 0);
            theSlider.Maximum = recordingDuration;
            textBackup = Textbox.Text;
        }

        private void UpdateSectionButton_Click(object sender, RoutedEventArgs e)
        {
            string currentSectionText = Textbox.Text;
            textBackup = textBackup.Replace(timesForNote[currentlyViewedSection].note, currentSectionText);
            timesForNote[currentlyViewedSection].note = currentSectionText;
            
        }

        [Serializable]
        private class SaveObject
        {
            public readonly long recordingDuration;
            public readonly string recordingFilename;
            public readonly List<TimeNote> sectionOccurances;
            public readonly string theNote;


            public SaveObject(long r, List<TimeNote> l, string s, string recordingname)
            {
                recordingDuration = r;
                sectionOccurances = l;
                theNote = s;
                recordingFilename = recordingname;
            }
        }

        [Serializable]
        private class TimeNote
        {
            public string note;
            public TimeSpan occurance;

            public TimeNote(TimeSpan o, string n)
            {
                occurance = o;
                note = n;
            }

            public string serialize()
            {
                return occurance + "," + note + ",";
            }
        }

        private class forUseByBackgroundWorker
        {
            public readonly string filePath;
            public readonly TimeSpan t;

            public forUseByBackgroundWorker(TimeSpan ts, string path)
            {
                t = ts;
                filePath = path;
            }
        }
    }
}
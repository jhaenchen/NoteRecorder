using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace MicAndNotes
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly BackgroundWorker _bw = new BackgroundWorker();
        private readonly MediaPlayer _classPlayer = new MediaPlayer();
        private readonly Stopwatch _recordingTimer = new Stopwatch();
        private int _currentlyViewedSection;
        private string _lastTextFilled = "";
        private long _recordingDuration;
        private string _savedRecordingAs;
        private bool _shouldContinue = true;
        private string _textBackup;
        private List<TimeNote> _timesForNote = new List<TimeNote>();
        private string _currentOpenNoteLocation;
        private SaveObject _currentSave;

        public MainWindow()
        {
            InitializeComponent();
            //_bw.WorkerSupportsCancellation = false;
            //_bw.WorkerReportsProgress = false;
            //_bw.DoWork +=
            //    bw_DoWork;
        }

        [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi, SetLastError = true,
            ExactSpelling = true)]
        private static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength,
            int hwndCallback);


        private bool _wasLastKeyEnter;

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _wasLastKeyEnter)
            {
                var toStore = Textbox.Text;
                if (!string.IsNullOrEmpty(_lastTextFilled))
                {
                    toStore = Textbox.Text.Replace(_lastTextFilled, String.Empty);
                }
                _lastTextFilled = Textbox.Text;


                _timesForNote.Add(new TimeNote(_recordingTimer.Elapsed, toStore));
                _wasLastKeyEnter = false;
            }
            else
            {
                _wasLastKeyEnter = e.Key == Key.Enter;
            }
        }

        private void PlayBackButton_Click(object sender, RoutedEventArgs e)
        {
            string textArchive = _textBackup;
            var playbackStartPoint = new TimeSpan(0);
            //var forPlayback = new ForUseByBackgroundWorker(playbackStartPoint, _savedRecordingAs);


            _classPlayer.Open(new Uri(_savedRecordingAs));
            // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
            _classPlayer.Position = playbackStartPoint;
            _classPlayer.Play();

            //var play = new MediaPlayer();

            //play.Open(new Uri(forPlayback.filePath));
            //play.Position = forPlayback.Span;
            //play.Play();


            //bw.RunWorkerAsync(forPlayback);
            ThreadPool.QueueUserWorkItem(o =>
            {
                _shouldContinue = true;
                int counter = 0;
                var playBackStopwatch = new Stopwatch();

                playBackStopwatch.Start();

                while (_shouldContinue)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //theSlider.Value = playBackStopwatch.ElapsedTicks;
                        try
                        {
                            if (playBackStopwatch.Elapsed + playbackStartPoint < _timesForNote[counter].Occurance)
                            {
                                Textbox.Text = _timesForNote[counter].Note;
                                _currentlyViewedSection = counter;
                                theSlider.Value = _timesForNote[counter].Occurance.Ticks;

                            }
                            else
                            {
                                counter++;
                            }
                        }
                        catch (Exception)
                        {
                            _shouldContinue = false;
                            //Textbox.Text = textArchive;
                            //Textbox.ScrollToEnd();
                            //theSlider.Value = 0;
                            //_currentlyViewedSection = -1;
                        }
                    }));
                    Thread.Sleep(1);
                }
            });
        }

        

        private void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            var toStart = new TimeSpan();
            int toPlayIndex = Textbox.CaretIndex;
            int lineIndex = Textbox.GetLineIndexFromCharacterIndex(toPlayIndex);
            string text = Textbox.GetLineText(lineIndex);
            for (int i = 0; i < _timesForNote.Count; i++)
            {
                if (_timesForNote[i].Note.Contains(text))
                {
                    toStart = i > 0 ? _timesForNote[i - 1].Occurance : new TimeSpan(0);
                }
            }


            string textArchive = _textBackup;

            _classPlayer.Open(new Uri(_savedRecordingAs));
            // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
            _classPlayer.Position = toStart;
            _classPlayer.Play();

            //var play = new MediaPlayer();

            //play.Open(new Uri(forPlayback.filePath));
            //play.Position = forPlayback.Span;
            //play.Play();


            //bw.RunWorkerAsync(forPlayback);
            ThreadPool.QueueUserWorkItem(o =>
            {
                _shouldContinue = true;
                int counter = 0;
                var playBackStopwatch = new Stopwatch();

                playBackStopwatch.Start();

                while (_shouldContinue)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //theSlider.Value = playBackStopwatch.ElapsedTicks;
                        try
                        {
                            if (playBackStopwatch.Elapsed + toStart < _timesForNote[counter].Occurance)
                            {
                                Textbox.Text = _timesForNote[counter].Note;
                                _currentlyViewedSection = counter;
                                theSlider.Value = _timesForNote[counter].Occurance.Ticks;

                            }
                            else
                            {
                                counter++;
                            }
                        }
                        catch (Exception)
                        {
                            _shouldContinue = false;
                            //Textbox.Text = textArchive;
                            //Textbox.ScrollToEnd();
                            //theSlider.Value = 0;
                            //_currentlyViewedSection = -1;
                        }
                    }));
                    Thread.Sleep(1);
                }
            });
        }


        private void theSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Textbox.Text != _textBackup && !_shouldContinue)
            {
                if (_timesForNote[_currentlyViewedSection].Note != Textbox.Text)
                {
                    _timesForNote[_currentlyViewedSection].Note = Textbox.Text + "\n";


                    string newBackup = _timesForNote.Aggregate("", (current, t) => current + t.Note);
                    _textBackup = newBackup;
                }
            }
            if (theSlider.IsFocused)
            {
                var sliderValue = new TimeSpan((long)theSlider.Value);
                for (int i = 0; i < _timesForNote.Count - 1; i++)
                {
                    int theFirst = _timesForNote[i].Occurance.CompareTo(sliderValue);
                    int theSecond = _timesForNote[i + 1].Occurance.CompareTo(sliderValue);
                    if (theFirst == -1 && theSecond == 1)
                    {
                        Textbox.Text = _timesForNote[i + 1].Note;
                        _currentlyViewedSection = i + 1;
                        break;
                    }

                    if (i == 0 && _timesForNote[i].Occurance.CompareTo(sliderValue) == 1)
                    {
                        Textbox.Text = _timesForNote[i].Note;
                        _currentlyViewedSection = i;
                        break;
                    }
                    Textbox.Text = _timesForNote[i + 1].Note;
                    _currentlyViewedSection = i + 1;
                }
            }
           
        }

        private void StopButton1_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = _textBackup;
            theSlider.Value = 0;

            _classPlayer.Stop();
            _shouldContinue = false;


            ////var play = new MediaPlayer();

            ////play.Open(new Uri(forPlayback.filePath));
            ////play.Position = forPlayback.Span;
            ////play.Play();


            ////bw.RunWorkerAsync(forPlayback);
            //ThreadPool.QueueUserWorkItem(o =>
            //{
            //    bool tempBoolWhy = true;
            //    while (tempBoolWhy)
            //    {
            //        Dispatcher.BeginInvoke(new Action(() =>
            //        {
            //            try
            //            {
            //                if (tempBoolWhy)
            //                {
            //                    Textbox.Text = _textBackup;
            //                    //theSlider.Value = 0;
            //                    tempBoolWhy = false;
            //                }
            //            }
            //            catch (Exception)
            //            {
            //                _shouldContinue = false;

            //                Textbox.ScrollToEnd();
            //                theSlider.Value = 0;
            //            }
            //        }));
            //        Thread.Sleep(1);
            //    }
            //});
        }

        private void ToolbarOpen_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            var openFileDialog1 = new OpenFileDialog {Filter = @"Note Recorder Files|*.nr"};
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Test result.
            {
                ZipArchive zip = ZipFile.OpenRead(openFileDialog1.FileName);
                try
                {
                    Directory.Delete("CurrentWorkingDirectory", true);
                }
                catch (DirectoryNotFoundException)
                {
                }

                zip.ExtractToDirectory("CurrentWorkingDirectory");
                Stream stream = File.Open("CurrentWorkingDirectory" + "\\data.nr", FileMode.Open);
                var bFormatter = new BinaryFormatter();
                var recoveredState = (SaveObject) bFormatter.Deserialize(stream);
                _currentSave = recoveredState;
                stream.Close();
                _timesForNote = recoveredState.SectionOccurances;
                Textbox.Text = recoveredState.TheNote;
                _textBackup = recoveredState.TheNote;
                _recordingDuration = recoveredState.RecordingDuration;
                theSlider.Maximum = _recordingDuration;
                _savedRecordingAs = Directory.GetCurrentDirectory() + "\\CurrentWorkingDirectory" + "\\recording.wav";
                _currentSave = new SaveObject(_recordingDuration, _timesForNote, recoveredState.TheNote,
                    _savedRecordingAs);
                _currentOpenNoteLocation = openFileDialog1.FileName;
            }
        }

        private void ToolbarSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSave != null)
            {
                var toSave = new SaveObject(_recordingDuration, _timesForNote, _textBackup,
                    _currentSave.RecordingFilename);
                try
                {
                    Directory.Delete("justASecond", true);
                }
                catch (DirectoryNotFoundException)
                {
                }
                ZipFile.ExtractToDirectory(_currentOpenNoteLocation, "justASecond");
                var t = new DirectoryInfo("justASecond");
                File.Delete(t.FullName + "\\data.nr");
                File.Delete(_currentOpenNoteLocation);

                Stream stream = File.Open(t + "\\data.nr", FileMode.Create);
                var bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, toSave);
                stream.Close();
                //File.Copy(_savedRecordingAs, t + "\\recording.wav");
                //File.Move(_savedRecordingAs, t + "\\recording.wav");
                File.Delete(_savedRecordingAs);
                Guid zipFileName = Guid.NewGuid();
                ZipFile.CreateFromDirectory(t.FullName, zipFileName.ToString());
                //Directory.Delete(t.FullName, true);
                File.Move(zipFileName.ToString(), _currentOpenNoteLocation);
            }
            else
            {
                ToolbarSaveAs_Click(sender, e);
            }
        }

        private void ToolbarClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void ToolbarSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog1 = new SaveFileDialog {RestoreDirectory = true};

            if (saveFileDialog1.ShowDialog() != true) return;
            var toSave = new SaveObject(_recordingDuration, _timesForNote, _textBackup,
                saveFileDialog1.FileName + "\\recording.wav");
            Directory.CreateDirectory(saveFileDialog1.FileName);
            Stream stream = File.Open(saveFileDialog1.FileName + "\\data.nr", FileMode.Create);
            var bFormatter = new BinaryFormatter();
            bFormatter.Serialize(stream, toSave);
            stream.Close();
            File.Move(_savedRecordingAs, saveFileDialog1.FileName + "\\recording.wav");
            File.Delete(_savedRecordingAs);
            Guid zipFileName = Guid.NewGuid();
            ZipFile.CreateFromDirectory(saveFileDialog1.FileName, zipFileName.ToString());
            Directory.Delete(saveFileDialog1.FileName, true);
            File.Move(zipFileName.ToString(), saveFileDialog1.FileName + ".nr");
        }

        private void StartRecording(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            RecordingIcon.Visibility = Visibility.Visible;
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            _recordingTimer.Start();
            mciSendString("record recsound", "", 0, 0);
        }

        private void StopRecording(object sender, RoutedEventArgs e)
        {
            RecordingIcon.Visibility = Visibility.Hidden;
            string savedFileName = Guid.NewGuid().ToString();
            _savedRecordingAs = Environment.CurrentDirectory + savedFileName + ".wav";
            _recordingTimer.Stop();
            _recordingDuration = _recordingTimer.Elapsed.Ticks;
            mciSendString("save recsound " + Environment.CurrentDirectory + savedFileName + ".wav", "", 0, 0);
            mciSendString("close recsound ", "", 0, 0);
            theSlider.Maximum = _timesForNote.Last().Occurance.Ticks;
            _textBackup = Textbox.Text;
        }
    }
}
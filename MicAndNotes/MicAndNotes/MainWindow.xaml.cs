using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows;
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
    public partial class MainWindow
    {
        private readonly MediaPlayer _classPlayer = new MediaPlayer();
        private readonly BackgroundWorker _bw = new BackgroundWorker();
        private readonly Stopwatch _recordingTimer = new Stopwatch();
        private string _lastTextFilled = "";
        private long _recordingDuration;
        private string _savedRecordingAs;
        private bool _shouldContinue = true;
        private string _textBackup;
        private List<TimeNote> _timesForNote = new List<TimeNote>();
        private bool _wasLastKeyEnter;

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


        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && _wasLastKeyEnter)
            {
                string toStore = Textbox.Text;
                if (_lastTextFilled != "")
                {
                    toStore = Textbox.Text.Replace(_lastTextFilled, String.Empty);
                }
                _lastTextFilled = Textbox.Text;


                _timesForNote.Add(new TimeNote(_recordingTimer.Elapsed, toStore));
                _wasLastKeyEnter = false;
            }
            else if (e.Key == Key.Enter)
            {
                _wasLastKeyEnter = true;
            }
            else
            {
                _wasLastKeyEnter = false;
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            RecordingIcon.Visibility = Visibility.Visible;
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            _recordingTimer.Start();
            mciSendString("record recsound", "", 0, 0);
        }


        //private void bw_DoWork(object sender, DoWorkEventArgs e)
        //{
        //    //var worker = sender as BackgroundWorker;
        //    var b = (ForUseByBackgroundWorker) e.Argument;
        //    var play = new MediaPlayer();

        //    play.Open(new Uri(b.FilePath));
        //    // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
        //    play.Position = b.Span;
        //    play.Play();
        //}

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
                        try
                        {
                            if (playBackStopwatch.Elapsed + playbackStartPoint < _timesForNote[counter].Occurance)
                            {
                                Textbox.Text = _timesForNote[counter].Note;
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
            _savedRecordingAs = Environment.CurrentDirectory + savedFileName + ".wav";
            _recordingTimer.Stop();
            _recordingDuration = _recordingTimer.Elapsed.Ticks;
            mciSendString("save recsound " + Environment.CurrentDirectory + savedFileName + ".wav", "", 0, 0);
            mciSendString("close recsound ", "", 0, 0);
            theSlider.Maximum = _recordingDuration;
            _textBackup = Textbox.Text;
        }


        private void FromCursor_Click(object sender, RoutedEventArgs e)
        {
            var toStart = new TimeSpan();
            var toPlayIndex = Textbox.CaretIndex;
            var lineIndex = Textbox.GetLineIndexFromCharacterIndex(toPlayIndex);
            var text = Textbox.GetLineText(lineIndex);
            for (var i = 0; i < _timesForNote.Count; i++)
            {
                if (_timesForNote[i].Note.Contains(text))
                {
                    toStart = i > 0 ? _timesForNote[i - 1].Occurance : new TimeSpan(0);
                }
            }


            var textArchive = _textBackup;

            //var forPlayback = new ForUseByBackgroundWorker(toStart, _savedRecordingAs);

            _classPlayer.Open(new Uri(_savedRecordingAs));
            // play.Open(new Uri(@"C:\SAMProjects\NoteRecorder\MicAndNotes\MicAndNotes\bin\Release6f6e0b14-623b-4c87-9cfd-eb3cf49fbf0c.wav"));
            _classPlayer.Position = toStart;
            _classPlayer.Play();


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
                        try
                        {
                            if (playBackStopwatch.Elapsed + toStart < _timesForNote[counter].Occurance)
                            {
                                Textbox.Text = _timesForNote[counter].Note;
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

        private int _currentlyViewedSection;

        private void StopButton1_Click(object sender, RoutedEventArgs e)
        {
            _classPlayer.Stop();
            _shouldContinue = false;


            //var play = new MediaPlayer();

            //play.Open(new Uri(forPlayback.filePath));
            //play.Position = forPlayback.Span;
            //play.Play();


            //bw.RunWorkerAsync(forPlayback);
            ThreadPool.QueueUserWorkItem(o =>
            {
                var tempBoolWhy = true;
                while (tempBoolWhy)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (tempBoolWhy)
                            {
                                Textbox.Text = _textBackup;
                                //theSlider.Value = 0;
                                tempBoolWhy = false;
                            }
                        }
                        catch (Exception)
                        {
                            _shouldContinue = false;

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
            var openFileDialog1 = new OpenFileDialog {Filter = @"Note Recorder Files|*.nr"};
            var result = openFileDialog1.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) // Test result.
            {
                Stream stream = File.Open(openFileDialog1.FileName, FileMode.Open);
                var bFormatter = new BinaryFormatter();
                var recoveredState = (SaveObject) bFormatter.Deserialize(stream);
                stream.Close();
                _timesForNote = recoveredState.SectionOccurances;
                Textbox.Text = recoveredState.TheNote;
                _textBackup = recoveredState.TheNote;
                _recordingDuration = recoveredState.RecordingDuration;
                theSlider.Maximum = _recordingDuration;
                _savedRecordingAs = recoveredState.RecordingFilename;
            }
        }

        private void ToolbarSave_Click(object sender, RoutedEventArgs e)
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
        }

        private void ToolbarClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToolbarRecord_Click(object sender, RoutedEventArgs e)
        {
            Textbox.Text = "";
            Textbox.IsEnabled = true;
            RecordingIcon.Visibility = Visibility.Visible;
            mciSendString("open new Type waveaudio Alias recsound", "", 0, 0);
            _recordingTimer.Start();
            mciSendString("record recsound", "", 0, 0);
        }

        private void ToolbarStopRecording_Click(object sender, RoutedEventArgs e)
        {
            RecordingIcon.Visibility = Visibility.Hidden;
            string savedFileName = Guid.NewGuid().ToString();
            _savedRecordingAs = Environment.CurrentDirectory + savedFileName + ".wav";
            _recordingTimer.Stop();
            _recordingDuration = _recordingTimer.Elapsed.Ticks;
            mciSendString("save recsound " + Environment.CurrentDirectory + savedFileName + ".wav", "", 0, 0);
            mciSendString("close recsound ", "", 0, 0);
            theSlider.Maximum = _recordingDuration;
            _textBackup = Textbox.Text;
        }

        private void UpdateSectionButton_Click(object sender, RoutedEventArgs e)
        {
            string currentSectionText = Textbox.Text;
            _textBackup = _textBackup.Replace(_timesForNote[_currentlyViewedSection].Note, currentSectionText);
            _timesForNote[_currentlyViewedSection].Note = currentSectionText;
            
        }
    }
}
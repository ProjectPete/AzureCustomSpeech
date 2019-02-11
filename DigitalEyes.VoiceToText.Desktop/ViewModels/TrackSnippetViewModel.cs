using DigitalEyes.VoiceToText.Desktop.Models;
using DigitalEyes.VoiceToText.Desktop.Views;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    [DataContract]
    public class TrackSnippetViewModel : BaseViewModel, IDisposable
    {
        private System.Windows.Shapes.Rectangle markerRectangle;

        public System.Windows.Shapes.Rectangle MarkerRectangle
        {
            get
            {
                return markerRectangle ?? (markerRectangle = new System.Windows.Shapes.Rectangle
                    {
                        Opacity = 0.4,
                        VerticalAlignment = VerticalAlignment.Top,
                        Fill = System.Windows.Media.Brushes.Red,
                        Height = 100,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    });
            }
        }

        private Timer selectionChangedTimer;

        private Timer SelectionChangedTimer
        {
            get
            {
                return selectionChangedTimer ?? (selectionChangedTimer = new System.Threading.Timer(DoSelectionMatch, null, Timeout.Infinite, Timeout.Infinite));
            }
        }

        private string rawText;

        [DataMember]
        public string RawText
        {
            get
            {
                return rawText ?? (rawText = FullText);
            }
            set
            {
                rawText = value;
            }
        }

        [DataMember]
        public long OffsetInTicks { get; set; }

        [IgnoreDataMember]
        public TrackSnippetWorkReport WorkReport
        {
            get
            {
                var numberOfApproved = TextParts.Count(a => a.IsOK);
                var workReport = new TrackSnippetWorkReport
                {
                    TotalParts = TextParts.Count(),
                    TotalApproved = TextParts.Count(a=>a.IsOK),
                    HasApproved = numberOfApproved > 0
                };
                return workReport;
            }
        }

        private bool isDrawn;

        [IgnoreDataMember]
        public bool IsDrawn
        {
            get
            {
                return isDrawn;
            }
            set
            {
                if (isDrawn != value)
                {
                    isDrawn = value;
                    RaisePropertyChanged("IsDrawn");
                    RaisePropertyChanged("IsDrawnInverted");

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ShowWait = true;
                        //await Task.Delay(50).ConfigureAwait(false);
                        Messenger.Default.Send("Scale", "TrackSnippetViewModel");

                        if (!value)
                        {
                            RaisePropertyChanged("WorkReport"); // recalculate stats
                        }

                        ShowWait = false;
                    });
                }
            }
        }

        public bool IsDrawnInverted
        {
            get
            {
                return !isDrawn;
            }
        }

        private bool showWait;

        [IgnoreDataMember]
        public bool ShowWait
        {
            get
            {
                return showWait;
            }
            set
            {
                if (showWait != value)
                {
                    showWait = value;
                    RaisePropertyChanged("ShowWait");
                }
            }
        }

        private RelayCommand<TextPart> playThisBitCommand;

        [IgnoreDataMember]
        public RelayCommand<TextPart> PlayThisBitCommand
        {
            get
            {
                return playThisBitCommand ?? (playThisBitCommand = new RelayCommand<TextPart>(DoPlayTextPart));
            }
        }

        private RelayCommand showStatsCommand;

        [IgnoreDataMember]
        public RelayCommand ShowStatsCommand
        {
            get
            {
                return showStatsCommand ?? (showStatsCommand = new RelayCommand(DoShowStats));
            }
        }

        private void DoShowStats()
        {
            var statsWin = new WaveformStatsWindow { DataContext = AudioVM };
            var win = App.Current.MainWindow;
            statsWin.Left = (win.Left + (win.Width / 2)) - (statsWin.Width / 2);
            statsWin.Top = (win.Top + (win.Height / 2)) - (statsWin.Height / 2);
            statsWin.Show();
        }

        private RelayCommand selectedDeleteCommand;

        [IgnoreDataMember]
        public RelayCommand SelectedDeleteCommand
        {
            get
            {
                return selectedDeleteCommand ?? (selectedDeleteCommand = new RelayCommand(DoSelectedDelete));
            }
        }

        private RelayCommand<object> textPartTextChangedCommand;

        [IgnoreDataMember]
        public RelayCommand<object> TextPartTextChangedCommand
        {
            get
            {
                return textPartTextChangedCommand ?? (textPartTextChangedCommand = new RelayCommand<object>(DoTextPartTextChanged));
            }
        }

        private RelayCommand<object> selectionChangedCommand;

        [IgnoreDataMember]
        public RelayCommand<object> SelectionChangedCommand
        {
            get
            {
                return selectionChangedCommand ?? (selectionChangedCommand = new RelayCommand<object>(DoSelectionMatchCommand));
            }
        }

        private void DoTextPartTextChanged(object param)
        {
            App.Current.Dispatcher.Invoke(() => RaisePropertyChanged("FullText"));
        }

        private void DoSelectedDelete()
        {
            if (SelectedTextParts == null || SelectedTextParts.Count == 0)
            {
                return;
            }
            var copied = SelectedTextParts.ToList();

            var answ = System.Windows.MessageBox.Show($"Are you sure you want to delete {copied.Count} selected text bits?", "Confirm delete...", MessageBoxButton.YesNo);
            if (answ == MessageBoxResult.Yes)
            {
                for (var x = 0; x < copied.Count; x++)
                {
                    TextParts.Remove(copied[x]);
                }
            }

            RaisePropertyChanged("FullText");
        }

        private RelayCommand<string> adjustSelectedCommand;

        [IgnoreDataMember]
        public RelayCommand<string> AdjustSelectedCommand
        {
            get
            {
                return adjustSelectedCommand ?? (adjustSelectedCommand = new RelayCommand<string>(DoAdjustSelected));
            }
        }

        private void DoAdjustSelected(string key)
        {
            var ary = key.Split('/');
            var val = int.Parse(ary[1]);

            switch (ary[0])
            {
                case "LONG":
                    SelectedTextChange(val);
                    break;
                case "POS":
                    SelectedTextMove(val);
                    break;
            }
        }

        private bool isSelected;

        [IgnoreDataMember]
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    RaisePropertyChanged("IsSelected");
                }
            }
        }

        private Storyboard markerStoryboard;

        [IgnoreDataMember]
        public Storyboard MarkerStoryboard
        {
            get
            {
                if (markerStoryboard == null)
                {
                    markerStoryboard = new Storyboard();
                    markerStoryboard.CurrentTimeInvalidated += MarkerStoryboard_CurrentTimeInvalidated;
                }

                return markerStoryboard;
            }
        }

        private TranslateTransform markerTransform;

        [IgnoreDataMember]
        public TranslateTransform MarkerTransform
        {
            get
            {
                return markerTransform ?? (markerTransform = new TranslateTransform(0, 0));
            }
        }

        private int sampledFrequency;

        [IgnoreDataMember]
        public int SampledFrequency
        {
            get
            {
                return sampledFrequency;
            }
            set
            {
                if (sampledFrequency != value)
                {
                    sampledFrequency = value;
                    RaisePropertyChanged("SampledFrequency");
                }
            }
        }

        private bool changeTimeWithSize;

        /// <summary>
        /// If checked, the selectedItems will be moved in time as well as changing size.
        /// This is the same effect as changing the timeline scale, but just for the selected
        /// </summary>
        [IgnoreDataMember]
        public bool ChangeTimeWithSize
        {
            get
            {
                return changeTimeWithSize;
            }
            set
            {
                if (changeTimeWithSize != value)
                {
                    changeTimeWithSize = value;
                    RaisePropertyChanged("ChangeTimeWithSize");
                }
            }
        }

        private System.Windows.Shapes.Rectangle positionMarker;

        [IgnoreDataMember]
        public System.Windows.Shapes.Rectangle PositionBar
        {
            get
            {
                return positionMarker;
            }
            set
            {
                if (positionMarker != value)
                {
                    positionMarker = value;
                    RaisePropertyChanged("PositionBar");
                }
            }
        }

        public bool IsPaused { get; set; }

        private AudoInfoViewModel audioVM;

        [IgnoreDataMember]
        public AudoInfoViewModel AudioVM
        {
            get
            {
                return audioVM ?? (audioVM = new AudoInfoViewModel());
            }
            set
            {
                if (audioVM != value)
                {
                    audioVM = value;
                    RaisePropertyChanged("AudioVM");
                }
            }
        }

        private RelayCommand selectedWordsMergeCommand;

        [IgnoreDataMember]
        public RelayCommand SelectedWordsMergeCommand
        {
            get
            {
                return selectedWordsMergeCommand ?? (selectedWordsMergeCommand = new RelayCommand(DoMergeWords, false));
            }
            set { selectedWordsMergeCommand = value; }
        }

        private void DoMergeWords()
        {
            var cnt = SelectedTextParts.Count;

            if (cnt == 0)
            {
                Messenger.Default.Send("No selection");
                return;
            }

            TextPart mergedWords = null;
            var toDelete = new List<TextPart>();
            TextPart firstPart = null;
            var foundCount = 0;

            var orderedParts = SelectedTextParts.Distinct().OrderBy(a => a.StartMills).ToList();

            foreach (var word in orderedParts)
            {
                //if (foundCount == cnt)
                //{
                //    break; // found all
                //}

                if (SelectedTextParts.Contains(word))
                {
                    if (mergedWords == null)
                    {
                        mergedWords = new TextPart { StartMills = word.StartMills, Text = "" };
                        firstPart = word;
                    }
                    else
                    {
                        toDelete.Add(word);
                    }
                    mergedWords.Text = mergedWords.Text.Trim() + " " + word.Text.Trim();
                    //mergedWords.TextWidth += word.TextWidth;
                    foundCount++;
                }
            }
            //mergedWords.Text = mergedWords.Text.Trim();

            firstPart.Text = mergedWords.Text;
            var lastPart = orderedParts[orderedParts.Count - 1];
            firstPart.TextWidth = (lastPart.StartMills + lastPart.TextWidth) - firstPart.StartMills;

            for (var ix = 0; ix < toDelete.Count; ix++)
            {
                TextParts.Remove(toDelete[ix]);
            }

            RaisePropertyChanged("FullText");
        }

        private RelayCommand addSpaceBeforeCommand;

        [IgnoreDataMember]
        public RelayCommand AddSpaceBeforeCommand
        {
            get
            {
                return addSpaceBeforeCommand ?? (addSpaceBeforeCommand = new RelayCommand(DoAddSpaceBefore, false));
            }
            set { addSpaceBeforeCommand = value; }
        }

        private void DoAddSpaceBefore()
        {
            if (SelectedTextParts.Count == 0)
            {
                Messenger.Default.Send("No selection");
                return;
            }

            TextPart firstWord = SelectedTextParts[0];
            int collectionIndex = 0;

            TextPart newSpace = new TextPart
            {
                TextWidth = 100,
                Text = " "
            };

            foreach (var part in TextParts)
            {
                if (part == firstWord)
                {
                    // Found first word and index
                    newSpace.StartMills = part.StartMills;

                    for (var i = collectionIndex+1; i < TextParts.Count; i++)
                    {
                        TextParts[i].StartMills += 1000;
                    }
                    break;
                }
                collectionIndex++;
            }

            TextParts.Insert(collectionIndex, newSpace);

            RaisePropertyChanged("FullText");
        }

        internal void SelectedTextChange(double widthChangeDelta)
        {
            var scaledDelta = widthChangeDelta / Scale;
            var cumulativeShift = scaledDelta / 2;

            var orderedSelectedParts = SelectedTextParts.OrderBy(a => a.StartMills);
            foreach (var part in orderedSelectedParts)
            {
                part.TextWidth += scaledDelta;

                if (part.TextWidth < 40)
                {
                    part.TextWidth = 40;
                }

                if (ChangeTimeWithSize)
                {
                    part.StartMills += cumulativeShift; // starts at half (for first) then shifts left "half * 2" for each next one
                    cumulativeShift += scaledDelta;
                }
            }
        }

        [IgnoreDataMember]
        private RelayCommand trackStopCommand;

        public RelayCommand TrackStopCommand
        {
            get
            {
                return trackStopCommand ?? (trackStopCommand = new RelayCommand(DoTrackStop, false));
            }
            set { trackStopCommand = value; }
        }

        [IgnoreDataMember]
        private RelayCommand trackPauseCommand;

        //internal void MarkerStoryboard_Begin()
        //{
        //}

        public RelayCommand TrackPauseCommand
        {
            get
            {
                return trackPauseCommand ?? (trackPauseCommand = new RelayCommand(DoTrackPause, false));
            }
            set { trackPauseCommand = value; }
        }

        private double playLength;

        [IgnoreDataMember]
        public double PlayPartLength
        {
            get
            {
                return playLength;
            }
            set
            {
                playLength = value;
                if (playLength > 0)
                {
                    RaisePropertyChanged("ShowPartPlay");
                }
            }
        }

 //       System.Threading.Timer playTimer;

        private string fullText;

        public string FullText
        {
            get
            {
                fullText = "";
                foreach(var text in TextParts)
                {
                    fullText += text.Text.Trim() + " ";
                }
                return fullText;
            }
        }

        private RelayCommand trackStartCommand;

        [IgnoreDataMember]
        public RelayCommand TrackStartCommand
        {
            get
            {
                return trackStartCommand ?? (trackStartCommand = new RelayCommand(DoTrackStart, false));
            }
            set { trackStartCommand = value; }
        }

        private TimeSpan? endAnimationTime;

        private async void DoTrackStart()
        {
            await Task.Delay(100).ConfigureAwait(false);
            if (partPlayStart != TimeSpan.Zero)
            {
                //await Task.Delay(100);
              //  PlayerStoryboard.BeginTime = MarkerStoryboard.BeginTime = partPlayStart;
                if (PlayPartLength > 0)
                {
                    var end = (TimeSpan.FromMilliseconds(PlayPartLength + partPlayStart.TotalMilliseconds));
                    endAnimationTime = end;
                  // PlayerStoryboard.Duration = MarkerStoryboard.Duration = (TimeSpan.FromMilliseconds(PlayPartLength + partPlayStart.TotalMilliseconds));
                    //PlayerStoryboard.SpeedRatio = 0.5;
//                    playTimer = new System.Threading.Timer(doTrackPartEnd, partPlayStart, (int)PlayPartLength, Timeout.Infinite);
                }
                else
                {
                    endAnimationTime = null;
                    partPlayStart = TimeSpan.Zero;
                }

                PlayerStoryboard.Resume(mediaElement);
                MarkerStoryboard.Resume(PositionBar);
            }
            else
            {
                PlayerStoryboard.Begin(mediaElement, true);
                MarkerStoryboard.Begin(PositionBar, true);
            }
        }

        //private void doTrackPartEnd(object state)
        //{
        //    playTimer.Change(Timeout.Infinite, Timeout.Infinite);

        //    App.Current.Dispatcher.Invoke(() =>
        //    {
        //        PlayerStoryboard.Stop();
        //        MarkerStoryboard.Stop();

        //        SeekToMousePointInTime(partPlayStart);
        //    });
        //}

        private TimeSpan partPlayStart = TimeSpan.Zero;

        internal void SeekToMousePointInTime(TimeSpan seekTime)
        {
            if (seekTime.Ticks < 0)
            {
                seekTime = TimeSpan.FromTicks(1);
            }
            partPlayStart = seekTime;

            MarkerStoryboard.Begin(PositionBar, true); // if the second parameter is missing or false, it doesn't work
            MarkerStoryboard.Seek(PositionBar, seekTime, TimeSeekOrigin.BeginTime);
            MarkerStoryboard.Pause(PositionBar); // stop doesn't work

            PlayerStoryboard.Begin(mediaElement, true); // if the second parameter is missing or false, it doesn't work
            PlayerStoryboard.Seek(mediaElement, seekTime, TimeSeekOrigin.BeginTime);
            PlayerStoryboard.Pause(mediaElement); // stop doesn't work
        }

        private void DoTrackStop()
        {
            PlayerStoryboard.Stop(mediaElement);
        }

        private void DoTrackPause()
        {
            if (!IsPaused)
            {
                Debug.WriteLine("Audio player was not paused");
                PlayerStoryboard.Pause(mediaElement);
                IsPaused = true;
                Debug.WriteLine("Audio player now paused");
            }
            else
            {
                Debug.WriteLine("Audio player was paused");
                PlayerStoryboard.Resume(mediaElement);
                IsPaused = false;
                Debug.WriteLine("Audio player now unpaused");
            }
        }

        internal void MarkerStoryboard_Resume()
        {
            MarkerStoryboard.Resume(PositionBar);
        }

        private Storyboard playerStoryboard;

        [IgnoreDataMember]
        public Storyboard PlayerStoryboard
        {
            get
            {
                return playerStoryboard;
            }
            set
            {
                if (playerStoryboard != value)
                {
                    playerStoryboard = value;
                    RaisePropertyChanged("PlayerStoryboard");
                }
            }
        }

        private MediaElement mediaElement;

        internal void MarkerStoryboard_Pause()
        {
            MarkerStoryboard.Pause(PositionBar);
        }

        private double scaleChangeDelta = 0;
        private double scale = 0.1;

        [DataMember]
        public double Scale
        {
            get
            {
                return scale;
            }
            set
            {
                if (scale != value)
                {
                    //if (initialised)
                    //{
                        scaleChangeDelta = value / scale;
                    //}
                    //else
                    //{
                    //    scaleChangeDelta = 0;
                    //    initialised = true;
                    //}

                    scale = value;
                    RaisePropertyChanged("Scale");
                }
            }
        }

        private double gain;

        [DataMember]
        public double Gain
        {
            get
            {
                if (gain == 0D)
                {
                    gain = 0.6;
                }
                return gain;
            }
            set
            {
                if (gain != value)
                {
                    gain = value;
                    RaisePropertyChanged("Gain");
                }
            }
        }

        //[DataMember]
        //public int NotificationCount
        //{
        //    get
        //    {
        //        return Aggregator.NotificationCount;
        //    }
        //    set
        //    {
        //        if (Aggregator.NotificationCount != value)
        //        {
        //            Aggregator.NotificationCount = value;
        //            RaisePropertyChanged("NotificationCount");
        //        }
        //    }
        //}

        internal void SelectedTextMove(double MillsChangeDelta)
        {
            foreach (var part in SelectedTextParts)
            {
                part.StartMills += MillsChangeDelta;
            }
        }

        [IgnoreDataMember]
        public SpeechRecognitionResult AudioSnippet { get; set; }

        [DataMember]
        public string FilePath { get; set; }

        // public WaveViewer Viewer { get; set; }

        private ObservableCollection<TextPart> selectedTextParts;

        [IgnoreDataMember]
        public ObservableCollection<TextPart> SelectedTextParts
        {
            get
            {
                return selectedTextParts ?? (selectedTextParts = new ObservableCollection<TextPart>());
            }
            set
            {
                if (selectedTextParts != value)
                {
                    selectedTextParts = value;
                    RaisePropertyChanged("SelectedTextParts");
                }
            }
        }

        private ObservableCollection<TextPart> textParts;

        [DataMember]
        public ObservableCollection<TextPart> TextParts
        {
            get
            {
                if (textParts?.Count > 0 && textParts[0].StartMills == 0)
                {
                    // temp hack so the play function doesn't think it's at the start and should play the whole track.
                    // See doTrackStart() - "TimeSpan.Zero" condition
                    textParts[0].StartMills = 1;
                }
                return textParts;
            }
            set
            {
                if (textParts != value)
                {
                    textParts = value;
                    RaisePropertyChanged("TextParts");
                }
            }
        }

        public string DummyText { get; set; }

        public string Text
        {
            get
            {
                if (AudioSnippet != null)
                {
                    return AudioSnippet.Text;
                }

                return DummyText;
            }
        }

        public void Reset()
        {
            ChangeScale(Scale, gain);
        }

        internal bool ChangeScale(double scale, double gain)
        {
            if (scale == Scale && gain == Gain)
            {
                return false;
            }

            Gain = gain;
            Scale = scale;
            RaisePropertyChanged("VisualWidth");
            return true;
        }

        internal void LoadWordsEvenly(string[] ary)
        {
            double wordEvenlyArrangedLength = (DurationMilliseconds * Scale) / ary.Length;
            TextParts = new ObservableCollection<TextPart>();
            foreach (var word in ary)
            {
                TextParts.Add(new TextPart { Text = word, TextWidth = wordEvenlyArrangedLength });
            }
        }

        //public void RespaceWords()
        //{
        //    if (scaleChangeDelta == 0)
        //    {
        //        return;
        //    }

        //    //var wordEvenlyArrangedLength = (DurationMilliseconds * Scale) / TextParts.Count;
        //    var words = TextParts.ToList();
        //    //TextParts.Clear();

        //    foreach (var textPart in words)
        //    {
        //        //var chg = (textPart.TextWidth / (scaleChangeDelta * 100));
        //        textPart.TextWidth *= scaleChangeDelta;
        //    //    TextParts.Add(textPart);
        //    }
        //}

        public TimeSpan Duration
        {
            get { return TimeSpan.FromMilliseconds(DurationMilliseconds); }
        }

        public double VisualWidth
        {
            get
            {
                return DurationMilliseconds * Scale; //0.1 scale = 10 miliseconds = 1 pixel 22 seconds = 2200 pixels 
            }
        }

        [DataMember]
        public double DurationMilliseconds { get; set; }

        public void CreatePlayerStoryboard(MediaElement MediaElement)
        {
            mediaElement = MediaElement;
            //NameScope.SetNameScope(this, new NameScope());
            MediaTimeline _audioTimeline = new MediaTimeline(new Uri(FilePath));
            //MediaElement _audioMediaElement = new MediaElement();
            //_audioMediaElement.Name = "audioMediaElement";
            //RegisterName(_audioMediaElement.Name, _audioMediaElement);
            //_audioMediaElement.LoadedBehavior = MediaState.Manual;
            //_audioMediaElement.UnloadedBehavior = MediaState.Manual;
            //PlayerStoryboard Storyboard.SetTargetName(_audioTimeline, _audioMediaElement.Name);
            Storyboard _packageStoryBoard = new Storyboard
            {
                SlipBehavior = SlipBehavior.Slip
            };
            _packageStoryBoard.Children.Add(_audioTimeline);
            //_packageStoryBoard.Children.Add(_imageTimeline);|
            //_packageStoryBoard.Begin(this);

            PlayerStoryboard = _packageStoryBoard;
            //PlayerStoryboard.Changed += PlayerStoryboard_Changed;
            //PlayerStoryboard.Completed += PlayerStoryboard_Completed;
            PlayerStoryboard.CurrentTimeInvalidated += Storyboard_Changed;
        }

        private void MarkerStoryboard_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            if (endAnimationTime == null)
            {
                return;
            }

            ClockGroup clockGroup = sender as ClockGroup;
            if (clockGroup?.Children.Count > 0)
            {
                AnimationClock mediaClock = clockGroup.Children[0] as AnimationClock;
                if (mediaClock.CurrentProgress.HasValue)
                {
                    if (mediaClock.CurrentTime >= endAnimationTime)
                    {
                        MarkerStoryboard.Pause(PositionBar);
                    }
                }
            }
        }

        private void Storyboard_Changed(object sender, EventArgs e)
        {
            if (endAnimationTime == null)
            {
                return;
            }

            ClockGroup clockGroup = sender as ClockGroup;
            if (clockGroup != null)
            {
                MediaClock mediaClock = clockGroup.Children[0] as MediaClock;
                if (mediaClock.CurrentProgress.HasValue)
                {
                    if (mediaClock.CurrentTime >= endAnimationTime)
                    {
                        PlayerStoryboard.Pause(mediaElement);
                    }
                }
            }
        }

        private void DoPlayTextPart(TextPart tp)
        {
            PlayerStoryboard.Stop(mediaElement);
            MarkerStoryboard.Stop(PositionBar);

            PlayPartLength = tp.TextWidth; // 1px = 1 mill
            var scaledStartMills = tp.StartMills;

            var seekTime = TimeSpan.FromMilliseconds(scaledStartMills);

            MarkerRectangle.Margin = new Thickness(scaledStartMills * Scale, 0, 0, 0);
            MarkerRectangle.Width = PlayPartLength * Scale;
            SeekToMousePointInTime(seekTime);
            TrackStartCommand.Execute(null);
        }

        public void GetSampleFrequencyAddPoints()
        {
            if (FilePath == null)
            {
                return;
            }

            Debug.WriteLine("GetSampleFrequencyAddPoints");

            PointsCount = 0;
            try
            {
                using (var wavFileReader = new WaveFileReader(FilePath))
                {
                    AudioVM.WaveFormat = wavFileReader.WaveFormat;
                    audioVM.Duration = wavFileReader.TotalTime;

                    Aggregator.NotificationCount = (int)((AudioVM.WaveFormat.SampleRate / (1000 * scale)) * AudioVM.WaveFormat.Channels); // * 22; // 100 * 22) / 22 ;  //(int)(wavFileReader.WaveFormat.AverageBytesPerSecond * 8/2 * scale) / 100;

                    Aggregator.RaiseRestart();

                    while (true)
                    {
                        var frame = wavFileReader.ReadNextSampleFrame(); // Read(buff, 0, 400);
                        if (frame == null)
                        {
                            break;
                        }

                        foreach (var b in frame)
                        {
                            try
                            {
                                Aggregator.Add(b);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.ToString());
                            }
                        }
                    }
                }

                RaisePropertyChanged("PointsCount");
                RaisePropertyChanged("SampledFrequency");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private int pointsCount;

        [DataMember]
        public int PointsCount
        {
            get
            {
                return pointsCount;
            }
            set
            {
                if (pointsCount != value)
                {
                    pointsCount = value;
                    RaisePropertyChanged("PointsCount");
                }
            }
        }

        private SampleAggregator aggregator;

        [IgnoreDataMember]
        public SampleAggregator Aggregator
        {
            get
            {
                if (aggregator == null)
                {
                    aggregator = new SampleAggregator();
                    aggregator.MaximumCalculated += Aggregator_MaximumCalculated;
                }

                return aggregator;
            }
        }

        private void Aggregator_MaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            pointsCount++;
            sampledFrequency = e.TotalPoints;
        }

        private RoutedEventArgs lastRoutedArgs = null;

        private void DoSelectionMatchCommand(object state)   // Not working
        {
            lastRoutedArgs = state as RoutedEventArgs;
            SelectionChangedTimer.Change(500, Timeout.Infinite);
        }

        private async void DoSelectionMatch(object state)
        {
            SelectionChangedTimer.Change(Timeout.Infinite, Timeout.Infinite);

            await Application.Current.Dispatcher.Invoke(async () =>
            {
                var lastSelectionTextbox = lastRoutedArgs.OriginalSource as System.Windows.Controls.TextBox;

                SelectedTextParts.Clear();
                RaisePropertyChanged("SelectedTextParts");
                await Task.Delay(200).ConfigureAwait(false);

                for (var a = 0; a < TextParts.Count; a++)
                {
                    TextParts[a].IsSelected = false;
                }

                await Task.Delay(100).ConfigureAwait(false);

                var selectedText = lastSelectionTextbox.SelectedText.Trim();

                if (selectedText?.Length == 0)
                {
                    return;
                }

                var selectionStart = lastSelectionTextbox.SelectionStart;

                var beforeSelection = lastSelectionTextbox.Text.Substring(0, selectionStart);

                var cumulativeText = "";
                var foundText = "";

                foreach (var txt in TextParts)
                {
                    cumulativeText += txt.Text + " ";
                    if (cumulativeText.Trim().Length > selectionStart)
                    {
                        txt.IsSelected = true;
                        SelectedTextParts.Add(txt);
                        foundText += txt.Text + " ";
                    }

                    if (foundText.Trim().Length >= selectedText.Length)
                    {
                        break;
                    }
                }
            });
        }

        public void Dispose()
        {
            if (aggregator != null)
            {
                aggregator.MaximumCalculated -= Aggregator_MaximumCalculated;
            }
        }

        public TrackSnippetViewModel()
        {
         //   this.selectedDeleteCommand = selectedDeleteCommand;
         //   this.markerStoryboard = markerStoryboard;
        }
    }
}

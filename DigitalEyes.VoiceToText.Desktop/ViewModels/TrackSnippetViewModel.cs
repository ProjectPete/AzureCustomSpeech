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
                if (markerRectangle == null)
                {
                    markerRectangle = new System.Windows.Shapes.Rectangle
                    {
                        Opacity = 0.4,
                        VerticalAlignment = VerticalAlignment.Top,
                        Fill = System.Windows.Media.Brushes.Red,
                        Height = 100,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Left
                    };
                }
                return markerRectangle;
            }
        }
        
        System.Threading.Timer selectionChangedTimer;
        System.Threading.Timer SelectionChangedTimer
        {
            get
            {
                if (selectionChangedTimer == null)
                {
                    selectionChangedTimer = new System.Threading.Timer(doSelectionMatch, null, Timeout.Infinite, Timeout.Infinite);
                }
                return selectionChangedTimer;
            }
        }

        string rawText;
        [DataMember]
        public string RawText
        {
            get
            {
                if (rawText == null)
                {
                    rawText = FullText;
                }

                return rawText;
            }
            set
            {
                rawText = value;
            }
        }

        long offsetInTicks;
        [DataMember]
        public long OffsetInTicks
        {
            get
            {
                return offsetInTicks;
            }
            set
            {
                offsetInTicks = value;
            }
        }

        [IgnoreDataMember]
        public TrackSnippetWorkReport WorkReport
        {
            get
            {
                var numberOfApproved = TextParts.Where(a => a.IsOK).Count();
                var workReport = new TrackSnippetWorkReport
                {
                    TotalParts = TextParts.Count(),
                    TotalApproved = TextParts.Where(a=>a.IsOK).Count(),
                    HasApproved = numberOfApproved > 0
                };
                return workReport;
            }
        }
        
        bool isDrawn;
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

                    App.Current.Dispatcher.Invoke(async () =>
                    {
                        ShowWait = true;
                        await Task.Delay(50);
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

        bool showWait;
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


        RelayCommand<TextPart> playThisBitCommand;
        [IgnoreDataMember]
        public RelayCommand<TextPart> PlayThisBitCommand
        {
            get
            {
                if (playThisBitCommand == null)
                {
                    playThisBitCommand = new RelayCommand<TextPart>(doPlayTextPart);
                }
                return playThisBitCommand;
            }
        }

        RelayCommand showStatsCommand;
        [IgnoreDataMember]
        public RelayCommand ShowStatsCommand
        {
            get
            {
                if (showStatsCommand == null)
                {
                    showStatsCommand = new RelayCommand(doshowStats);
                }
                return showStatsCommand;
            }
        }

        private void doshowStats()
        {
            var statsWin = new WaveformStatsWindow { DataContext = AudioVM };
            var win = App.Current.MainWindow;
            statsWin.Left = (win.Left + win.Width / 2) - (statsWin.Width / 2);
            statsWin.Top = (win.Top + win.Height / 2) - (statsWin.Height / 2);
            statsWin.Show();
        }

        RelayCommand selectedDeleteCommand;
        [IgnoreDataMember]
        public RelayCommand SelectedDeleteCommand
        {
            get
            {
                if (selectedDeleteCommand == null)
                {
                    selectedDeleteCommand = new RelayCommand(doSelectedDelete);
                }
                return selectedDeleteCommand;
            }
        }

        RelayCommand<object> textPartTextChangedCommand;
        [IgnoreDataMember]
        public RelayCommand<object> TextPartTextChangedCommand
        {
            get
            {
                if (textPartTextChangedCommand == null)
                {
                    textPartTextChangedCommand = new RelayCommand<object>(doTextPartTextChanged);
                }
                return textPartTextChangedCommand;
            }
        }

        RelayCommand<object> selectionChangedCommand;
        [IgnoreDataMember]
        public RelayCommand<object> SelectionChangedCommand
        {
            get
            {
                if (selectionChangedCommand == null)
                {
                    selectionChangedCommand = new RelayCommand<object>(doSelectionMatchCommand);
                }
                return selectionChangedCommand;
            }
        }

        private void doTextPartTextChanged(object param)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                RaisePropertyChanged("FullText");
            });
        }

        private void doSelectedDelete()
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

        RelayCommand<string> adjustSelectedCommand;
        [IgnoreDataMember]
        public RelayCommand<string> AdjustSelectedCommand
        {
            get
            {
                if (adjustSelectedCommand == null)
                {
                    adjustSelectedCommand = new RelayCommand<string>(doAdjustSelected);
                }

                return adjustSelectedCommand;
            }
        }

        private void doAdjustSelected(string key)
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
        
        bool isSelected;
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
        
        Storyboard markerStoryboard;
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

        TranslateTransform markerTransform;
        [IgnoreDataMember]
        public TranslateTransform MarkerTransform
        {
            get
            {
                if (markerTransform == null)
                {
                    markerTransform = new TranslateTransform(0, 0);
                }
                return markerTransform;
            }
        }

        int sampledFrequency;
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

        bool changeTimeWithSize;
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

        System.Windows.Shapes.Rectangle positionMarker;
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

        AudoInfoViewModel audioVM;
        [IgnoreDataMember]
        public AudoInfoViewModel AudioVM
        {
            get
            {
                if (audioVM == null)
                {
                    audioVM = new AudoInfoViewModel();
                }
                return audioVM;
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
        
        RelayCommand selectedWordsMergeCommand;
        [IgnoreDataMember]
        public RelayCommand SelectedWordsMergeCommand
        {
            get
            {
                if (selectedWordsMergeCommand == null)
                {
                    selectedWordsMergeCommand = new RelayCommand(doMergeWords, false);
                }
                return selectedWordsMergeCommand;
            }
            set { selectedWordsMergeCommand = value; }
        }

        private void doMergeWords()
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

        RelayCommand addSpaceBeforeCommand;
        [IgnoreDataMember]
        public RelayCommand AddSpaceBeforeCommand
        {
            get
            {
                if (addSpaceBeforeCommand == null)
                {
                    addSpaceBeforeCommand = new RelayCommand(doAddSpaceBefore, false);
                }
                return addSpaceBeforeCommand;
            }
            set { addSpaceBeforeCommand = value; }
        }

        private void doAddSpaceBefore()
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
        RelayCommand trackStopCommand;
        public RelayCommand TrackStopCommand
        {
            get
            {
                if (trackStopCommand == null)
                {
                    trackStopCommand = new RelayCommand(doTrackStop, false);
                }
                return trackStopCommand;
            }
            set { trackStopCommand = value; }
        }

        [IgnoreDataMember]
        RelayCommand trackPauseCommand;

        //internal void MarkerStoryboard_Begin()
        //{
        //}

        public RelayCommand TrackPauseCommand
        {
            get
            {
                if (trackPauseCommand == null)
                {
                    trackPauseCommand = new RelayCommand(doTrackPause, false);
                }
                return trackPauseCommand;
            }
            set { trackPauseCommand = value; }
        }

        double playLength;
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

        System.Threading.Timer playTimer;

        string fullText;
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


        RelayCommand trackStartCommand;
        [IgnoreDataMember]
        public RelayCommand TrackStartCommand
        {
            get
            {
                if (trackStartCommand == null)
                {
                    trackStartCommand = new RelayCommand(doTrackStart, false);
                }
                return trackStartCommand;
            }
            set { trackStartCommand = value; }
        }

        TimeSpan? endAnimationTime;

        private async void doTrackStart()
        {
            await Task.Delay(100);
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

        private void doTrackPartEnd(object state)
        {
            playTimer.Change(Timeout.Infinite, Timeout.Infinite);

            App.Current.Dispatcher.Invoke(() =>
            {
                PlayerStoryboard.Stop();
                MarkerStoryboard.Stop();

                SeekToMousePointInTime(partPlayStart);
            });
        }

        TimeSpan partPlayStart = TimeSpan.Zero;

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

        private void doTrackStop()
        {
            PlayerStoryboard.Stop(mediaElement);
        }

        private void doTrackPause()
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

        Storyboard playerStoryboard;
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

        MediaElement mediaElement;

        internal void MarkerStoryboard_Pause()
        {
            MarkerStoryboard.Pause(PositionBar);
        }

        double scaleChangeDelta = 0;
        double scale = 0.1;
        bool initialised;
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

        double gain;
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
        string filePath;

        [DataMember]
        public string FilePath
        {
            get
            {
                return filePath;
            }
            set
            {
                filePath = value;
                //DurationMilliseconds = WavFileUtils.GetSoundLength(value);
            }
        }

        // public WaveViewer Viewer { get; set; }

        ObservableCollection<TextPart> selectedTextParts;
        [IgnoreDataMember]
        public ObservableCollection<TextPart> SelectedTextParts
        {
            get
            {
                if (selectedTextParts == null)
                {
                    selectedTextParts = new ObservableCollection<TextPart>();
                }
                return selectedTextParts;
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

        string dummyText;

        ObservableCollection<TextPart> textParts;
        [DataMember]
        public ObservableCollection<TextPart> TextParts
        {
            get
            {
                if (textParts != null && textParts.Count > 0 && textParts[0].StartMills == 0)
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

        public string DummyText
        {
            get
            {
                return dummyText;
            }
            set
            {
                dummyText = value;
            }
        }
        
        public string Text
        {
            get
            {
                if (AudioSnippet != null)
                {
                    return AudioSnippet.Text;
                }
                
                return dummyText;
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
                return (DurationMilliseconds * Scale); //0.1 scale = 10 miliseconds = 1 pixel 22 seconds = 2200 pixels 
            }
        }

        double durationMilliseconds;
        [DataMember]
        public double DurationMilliseconds
        {
            get
            {
                return durationMilliseconds;
            }
            set
            {
                durationMilliseconds = value;
            }
        }

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
            Storyboard _packageStoryBoard = new Storyboard();
            _packageStoryBoard.SlipBehavior = SlipBehavior.Slip;
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
            if (clockGroup != null)
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

        private void doPlayTextPart(TextPart tp)
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

        int pointsCount;
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


        SampleAggregator aggregator;
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

        RoutedEventArgs lastRoutedArgs = null;

        private void doSelectionMatchCommand(object state)   // Not working
        {
            lastRoutedArgs = state as RoutedEventArgs;
            SelectionChangedTimer.Change(500, Timeout.Infinite);
        }

        private async void doSelectionMatch(object state)
        {
            SelectionChangedTimer.Change(Timeout.Infinite, Timeout.Infinite);

            await App.Current.Dispatcher.Invoke(async () => 
            {
                var lastSelectionTextbox = lastRoutedArgs.OriginalSource as System.Windows.Controls.TextBox;

                SelectedTextParts.Clear();
                RaisePropertyChanged("SelectedTextParts");
                await Task.Delay(200);

                for (var a = 0; a < TextParts.Count; a++)
                {
                    TextParts[a].IsSelected = false;
                }

                await Task.Delay(100);

                var selectedText = lastSelectionTextbox.SelectedText.Trim();

                if (selectedText == "")
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

        }
    }
}

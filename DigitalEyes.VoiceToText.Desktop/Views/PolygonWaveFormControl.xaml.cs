using DigitalEyes.VoiceToText.Desktop.Models;
using DigitalEyes.VoiceToText.Desktop.ViewModels;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace DigitalEyes.VoiceToText.Desktop
{
    /// <summary>
    /// Interaction logic for PolylineWaveFormControl.xaml
    /// </summary>
    public partial class PolygonWaveFormControl : UserControl
    {
        public static readonly DependencyProperty SampleAggregatorProperty = DependencyProperty.Register(
          "SampleAggregator", typeof(SampleAggregator), typeof(PolygonWaveFormControl), new PropertyMetadata(null, OnSampleAggregatorChanged));

        public SampleAggregator SampleAggregator
        {
            get
            {
                return (SampleAggregator)GetValue(SampleAggregatorProperty);
            }
            set { SetValue(SampleAggregatorProperty, value); }
        }

        public static readonly DependencyProperty PointsCountProperty = DependencyProperty.Register(
            "PointsCount", typeof(int), typeof(PolygonWaveFormControl), new PropertyMetadata(0, OnPointsCountChanged));

        private static void OnPointsCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var ctrl = d as PolygonWaveFormControl;
            //ctrl.txtPoints.Text = e.NewValue.ToString();
        }

        private UniformGrid ruler;

        public int PointsCount
        {
            get
            {
                return (int)GetValue(PointsCountProperty);
            }
            set { SetValue(PointsCountProperty, value); }
        }

        public static readonly DependencyProperty MarkerRectangleProperty = DependencyProperty.Register(
    "MarkerRectangle", typeof(Rectangle), typeof(PolygonWaveFormControl), new PropertyMetadata(null, MarkerRectangleChanged));

        private static void MarkerRectangleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = d as PolygonWaveFormControl;
            if (ctrl.MarkerRectangle.Parent != null)
            {
                ((Canvas)ctrl.MarkerRectangle.Parent).Children.Remove(ctrl.MarkerRectangle);
            }
            ctrl.mainCanvas.Children.Add(ctrl.MarkerRectangle);
        }

        public Rectangle MarkerRectangle
        {
            get
            {
                return (Rectangle)GetValue(MarkerRectangleProperty);
            }
            set { SetValue(MarkerRectangleProperty, value); }
        }

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration", typeof(TimeSpan), typeof(PolygonWaveFormControl), new PropertyMetadata(TimeSpan.Zero, OnDurationChanged));

        private static void OnDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //var ctrl = d as PolygonWaveFormControl;
            //ctrl.txtDuration.Text = e.NewValue.ToString();
        }

        public TimeSpan Duration
        {
            get
            {
                return (TimeSpan)GetValue(DurationProperty);
            }
            set { SetValue(DurationProperty, value); }
        }

        private Storyboard slidingStoryboard;

        public Storyboard SlidingStoryboard
        {
            get
            {
                if (slidingStoryboard == null)
                {
                    slidingStoryboard = new Storyboard();
                    Storyboard.SetTarget(slidingStoryboard, mainGrid);
                }
                return slidingStoryboard;
            }
        }

        private static void OnSampleAggregatorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = (PolygonWaveFormControl)sender;
            control.Subscribe();
        }

        public void Subscribe()
        {
            SampleAggregator.MaximumCalculated += OnMaximumCalculated;
        }

        private void OnMaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            if (IsEnabled)
            {
                this.CreatePoint(e.MaxSample, e.MinSample);
                this.PointsCount = Points;
            }
        }

        private int renderPosition;
        private double yTranslate = 40; //Changed later
        private double yScale = 40; // Changed later
        private double xScale = 1; // changed later

        private readonly Polygon waveForm = new Polygon { HorizontalAlignment = HorizontalAlignment.Left };
        private TrackSnippetViewModel vm = null;

        public PolygonWaveFormControl()
        {
            InitializeComponent();

            waveForm.Stroke = Brushes.Gray;
            waveForm.StrokeThickness = 1;
            waveForm.Fill = new SolidColorBrush(Colors.Green);
            mainCanvas.Children.Add(waveForm);

            Messenger.Default.Register<string>(this, "TrackSnippetViewModel", DoMessage);

            DataContextChanged += PolygonWaveFormControl_DataContextChanged;
            Unloaded += PolygonWaveFormControl_Unloaded;
        }

        private void PolygonWaveFormControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= PolygonWaveFormControl_Unloaded;
            vm.Dispose();
            SampleAggregator.MaximumCalculated -= OnMaximumCalculated;
            DataContextChanged -= PolygonWaveFormControl_DataContextChanged;
        }

        private void PolygonWaveFormControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            vm =  e.NewValue as TrackSnippetViewModel;
            MakeRuler();
        }

        private void MakeRuler()
        {
            ruler = new UniformGrid { Rows=1, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
            ruler.SetValue(Panel.ZIndexProperty, 999);

            var wholeSeconds = Math.Floor(vm.DurationMilliseconds / 1000);
            for (var x = 0; x < wholeSeconds ; x++)
            {
                for (var x2 = 0; x2 < 9; x2++)
                {
                    ruler.Children.Add(new Line { Margin = new Thickness(-1,0,-1,0), VerticalAlignment = VerticalAlignment.Top, StrokeEndLineCap = PenLineCap.Triangle, HorizontalAlignment = HorizontalAlignment.Right, Stroke = new SolidColorBrush(Colors.Gray), Y2 = 3, StrokeThickness = 2 });
                }

                var theSecondMarker = new StackPanel { Margin = new Thickness(-1, 0,-1,0) };
                theSecondMarker.Children.Add(new Line {VerticalAlignment = VerticalAlignment.Top, StrokeEndLineCap = PenLineCap.Triangle, HorizontalAlignment = HorizontalAlignment.Right, Stroke = new SolidColorBrush(Colors.Black), Y2 = 8, StrokeThickness = 4 });
                theSecondMarker.Children.Add(new TextBlock { VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 9, Text = $"{x + 1}" });
                ruler.Children.Add(theSecondMarker);
            }

            var rem = vm.DurationMilliseconds % 1000;
            double todo = Math.Ceiling(rem / 100);
            for (int x = 0; x < todo; x++)
            {
                ruler.Children.Add(new Line { Margin = new Thickness(-1, 0, -1, 0), VerticalAlignment = VerticalAlignment.Top, StrokeEndLineCap = PenLineCap.Triangle, HorizontalAlignment = HorizontalAlignment.Right, Stroke = new SolidColorBrush(Colors.Gray), Y2 = 3, StrokeThickness = 2 });
            }

            mainGrid.Children.Add(ruler);
        }

        private void DoMessage(string msg)
        {
            if (msg == "Scale")
            {
                renderPosition = 0;
                ClearAllPoints();

                if (Visibility == Visibility.Visible)
                {
                    RedrawPoints();
                }
            }
        }

        private void RedrawPoints()
        {
            if (vm == null || vm.FilePath == null || !vm.IsDrawn)
            {
                return;
            }

            yTranslate = ActualHeight / 2; // + & - shows from the middle 
            yScale = (ActualHeight / 2) * vm.Gain;
            xScale = vm.Scale;

            vm.MarkerStoryboard.Stop();
            vm.GetSampleFrequencyAddPoints();
            var screenVisibleWidth = Parent as FrameworkElement;

            vm.MarkerStoryboard.Children.Clear();
            var anim = new DoubleAnimation(0, Duration.TotalMilliseconds * xScale, Duration);
            Storyboard.SetTargetName(anim, "ttMarker");
            //Storyboard.SetTarget(anim, vm.MarkerTransform);
            Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.XProperty));
            vm.MarkerStoryboard.Children.Add(anim);

            ruler.Width = vm.DurationMilliseconds * xScale;
        }

        private void ClearAllPoints()
        {
            waveForm.Points.Clear();
        }

        private int Points
        {
            get { return waveForm.Points.Count / 2; } // each time points is a pair of values, for max and min
        }

        private int BottomPointIndex(int position)
        {
            return waveForm.Points.Count - position - 1;
        }

        private double SampleToYPosition(float value)
        {
            return yTranslate + value * yScale;
        }

        private int trimmedPointsFromStartCount = 0;

        private void CreatePoint(float topValue, float bottomValue)
        {
            try
            {
                var topYPos = SampleToYPosition(topValue);
                var bottomYPos = SampleToYPosition(bottomValue);

                var xPos = (renderPosition);

                if (renderPosition >= Points)
                {
                    int insertPos = Points;
                    waveForm.Points.Insert(insertPos, new Point(xPos, topYPos));
                    waveForm.Points.Insert(insertPos + 1, new Point(xPos, bottomYPos));
                }
                else
                {
                    trimmedPointsFromStartCount++;
                    waveForm.Points[renderPosition] = new Point(xPos, topYPos);
                    waveForm.Points[BottomPointIndex(renderPosition)] = new Point(xPos, bottomYPos);
                }
                renderPosition++;
            }
            catch (Exception exc)
            {
                Debug.WriteLine($"{exc}");
                throw;
            }
        }

        /// <summary>
        /// Clears the waveform and repositions on the left
        /// </summary>
        public void Reset()
        {
            renderPosition = 0;
            ClearAllPoints();
        }

        private bool isMouseDragging = false;
        private double dragWidth = 1;
        private Point startPos;

        private void LeftButtDown(object sender, MouseButtonEventArgs e)
        {
            //isMouseDown = true;
            isMouseDragging = true;
            startPos = e.GetPosition(mainCanvas);

            MarkerRectangle.Width = 0;

            var canv = sender as Canvas;
            vm.PlayPartLength = 0;

            var seekTime = TimeSpan.FromMilliseconds(startPos.X / xScale);

            vm.SeekToMousePointInTime(seekTime);
        }

        private void LeftButtLeave(object sender, MouseEventArgs e)
        {
            isMouseDragging = false;
        }

        private void LeftButtMove(object sender, MouseEventArgs e)
        {
            if (!isMouseDragging)
            {
                return;
            }

            var nowPos = e.GetPosition(mainCanvas);
            dragWidth = nowPos.X - startPos.X;
            if (dragWidth < 0)
            {
                dragWidth = 0;
            }
            MarkerRectangle.Margin = new Thickness(startPos.X, 0, 0, 0);
            MarkerRectangle.Width = dragWidth;
        }

        private void LeftButtUp(object sender, MouseButtonEventArgs e)
        {
            CheckEndOfSelectionDrag();
            // isMouseDown = false;
        }

        private void CheckEndOfSelectionDrag()
        {
            isMouseDragging = false;
            vm.PlayPartLength = (MarkerRectangle.Width / vm.Scale);
        }
    }
}

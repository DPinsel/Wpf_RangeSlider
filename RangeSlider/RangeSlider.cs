using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RangeSlider
{
    public class RangeSlider : Control
    {
        public event EventHandler DragStart;
        public event EventHandler DragStop;

        private static readonly DependencyProperty MinProperty;
        private static readonly DependencyProperty MaxProperty;

        private static readonly DependencyProperty MinSliderWidthProperty;
        private static readonly DependencyProperty MaxSliderWidthProperty;

        private static readonly DependencyProperty SliderStartProperty;
        private static readonly DependencyProperty SliderEndProperty;

        static RangeSlider()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));

            MinProperty = DependencyProperty.Register("Min", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0));
            MaxProperty = DependencyProperty.Register("Max", typeof(double), typeof(RangeSlider), new PropertyMetadata(100.0));
            MinSliderWidthProperty = DependencyProperty.Register("MinSliderWidth", typeof(double), typeof(RangeSlider), new PropertyMetadata(1.0));
            MaxSliderWidthProperty = DependencyProperty.Register("MaxSliderWidth", typeof(double), typeof(RangeSlider), new PropertyMetadata(double.MaxValue));
            SliderStartProperty = DependencyProperty.Register("SliderStart", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
            SliderEndProperty = DependencyProperty.Register("SliderEnd", typeof(double), typeof(RangeSlider), new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        }

        public RangeSlider()
        {
            Loaded += (s, e) => UpdateGui();
            SizeChanged += (s, e) => UpdateGui();
        }

        public double Min
        {
            get { return (double)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public double Max
        {
            get { return (double)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public double MinSliderWidth
        {
            get { return (double)GetValue(MinSliderWidthProperty); }
            set { SetValue(MinSliderWidthProperty, value); }
        }

        public double MaxSliderWidth
        {
            get { return (double)GetValue(MaxSliderWidthProperty); }
            set { SetValue(MaxSliderWidthProperty, value); }
        }

        public double SliderStart
        {
            get { return (double)GetValue(SliderStartProperty); }
            set { SetValue(SliderStartProperty, value); }
        }

        public double SliderEnd
        {
            get { return (double)GetValue(SliderEndProperty); }
            set { SetValue(SliderEndProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            var leftSliderPart = GetTemplateChild("PART_SliderLeft") as FrameworkElement;
            if (leftSliderPart != null)
                leftSliderPart.MouseLeftButtonDown += OnLeftSliderMouseDown;
            var rightSliderPart = GetTemplateChild("PART_SliderRight") as FrameworkElement;
            if (rightSliderPart != null)
                rightSliderPart.MouseLeftButtonDown += OnRightSliderMouseDown;
            var midSliderPart = GetTemplateChild("PART_SliderCenter") as FrameworkElement;
            if (midSliderPart != null)
                midSliderPart.MouseLeftButtonDown += OnMidSliderMouseDown;

            base.OnApplyTemplate();
        }

        private async void OnLeftSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            var leftSliderPart = GetTemplateChild("PART_SliderLeft") as FrameworkElement;
            if (leftSliderPart == null)
                return;

            var originalClickPosition = Mouse.GetPosition(leftSliderPart);
            CaptureMouseAndRaiseEvent();
            while (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsMouseOnTheRight(leftSliderPart, originalClickPosition))
                    ReduceWidthLeft(leftSliderPart, originalClickPosition);
                if (IsMouseOnTheLeft(leftSliderPart, originalClickPosition))
                    IncreaseWidthLeft(leftSliderPart, originalClickPosition);
                UpdateGui();

                await Task.Delay(25);
            }
            ReleaseMouseAndRaiseEvent();
        }

        private async void OnRightSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            var rightSliderPart = GetTemplateChild("PART_SliderRight") as FrameworkElement;
            if (rightSliderPart == null)
                return;

            var originalClickPosition = Mouse.GetPosition(rightSliderPart);
            CaptureMouseAndRaiseEvent();
            while (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsMouseOnTheRight(rightSliderPart, originalClickPosition))
                    IncreaseWidthRight(rightSliderPart, originalClickPosition);
                if (IsMouseOnTheLeft(rightSliderPart, originalClickPosition))
                    ReduceWidthRight(rightSliderPart, originalClickPosition);
                UpdateGui();

                await Task.Delay(25);
            }
            ReleaseMouseAndRaiseEvent();
        }

        private async void OnMidSliderMouseDown(object sender, MouseButtonEventArgs e)
        {
            var slider = GetTemplateChild("PART_Slider") as FrameworkElement;
            if (slider == null)
                return;

            var originalClickPosition = Mouse.GetPosition(slider);
            CaptureMouseAndRaiseEvent();
            while (e.LeftButton == MouseButtonState.Pressed)
            {
                if (IsMouseOnTheRight(slider, originalClickPosition) && CanMoveRight())
                    MoveRight(slider, originalClickPosition);
                if (IsMouseOnTheLeft(slider, originalClickPosition) && CanMoveLeft())
                    MoveLeft(slider, originalClickPosition);
                UpdateGui();

                await Task.Delay(25);
            }

            ReleaseMouseAndRaiseEvent();
        }

        private void UpdateGui()
        {
            var backgroundBar = (FrameworkElement)GetTemplateChild("PART_BackgroundBar");
            var leftHideBar = (FrameworkElement)GetTemplateChild("PART_LeftHideBar");
            var rightHideBar = (FrameworkElement)GetTemplateChild("PART_RightHideBar");

            var relativeSliderStart = SliderStart / Max;
            var relativeSliderEnd = SliderEnd / Max;
            var relativeSliderWidth = (SliderEnd - SliderStart) / Max;

            var absoluteWidth = backgroundBar.ActualWidth;
            var absoluteLeftHideBarWidth = absoluteWidth * relativeSliderStart;
            var absoluteRightHideBarWidth = absoluteWidth - (absoluteWidth * relativeSliderEnd);

            leftHideBar.Width = absoluteLeftHideBarWidth > 0 ? absoluteLeftHideBarWidth : 0;
            rightHideBar.Width = absoluteRightHideBarWidth > 0 ? absoluteRightHideBarWidth : 0;
        }

        private void CaptureMouseAndRaiseEvent()
        {
            Application.Current.MainWindow.CaptureMouse();
            DragStart?.Invoke(this, EventArgs.Empty);
        }

        private void ReleaseMouseAndRaiseEvent()
        {
            Application.Current.MainWindow.ReleaseMouseCapture();
            DragStop?.Invoke(this, EventArgs.Empty);
        }

        private bool IsMouseOnTheLeft(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var position = Mouse.GetPosition(clickedElement);
            return position.X < originalClickPosition.X;
        }

        private bool IsMouseOnTheRight(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var position = Mouse.GetPosition(clickedElement);
            return position.X > originalClickPosition.X;
        }

        private void IncreaseWidthLeft(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var movementSpeed = CalculateMovementSpeed(clickedElement, originalClickPosition);
            var maxSpeed = CalculateMaxSpeedToNotSurpassMaxRange();

            SliderStart -= movementSpeed > maxSpeed ? maxSpeed : movementSpeed;
        }

        private void IncreaseWidthRight(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var movementSpeed = CalculateMovementSpeed(clickedElement, originalClickPosition);
            var maxSpeed = CalculateMaxSpeedToNotSurpassMaxRange();

            SliderEnd += movementSpeed > maxSpeed ? maxSpeed : movementSpeed;
        }

        private void ReduceWidthLeft(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var movementSpeed = CalculateMovementSpeed(clickedElement, originalClickPosition);
            var maxSpeed = CalculateMaxSpeedToNotSurpassMinRange();

            SliderStart += movementSpeed > maxSpeed ? maxSpeed : movementSpeed;
        }

        private void ReduceWidthRight(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var movementSpeed = CalculateMovementSpeed(clickedElement, originalClickPosition);
            var maxSpeed = CalculateMaxSpeedToNotSurpassMinRange();

            SliderEnd -= movementSpeed > maxSpeed ? maxSpeed : movementSpeed;
        }

        private void MoveLeft(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var movementSpeed = CalculateMovementSpeed(clickedElement, originalClickPosition);
            var maxSpeedToNotSurpassMinRange = CalculateMaxSpeedToNotSurpassMinRange();
            var maxSpeedToNotSurpassMin = CalculateMaxSpeedToNotSurpassMin();

            var allSpeeds = new double[] {
                movementSpeed,
                maxSpeedToNotSurpassMinRange,
                maxSpeedToNotSurpassMin };

            var smallestSpeed = allSpeeds.Min();

            SliderStart -= smallestSpeed;
            SliderEnd -= smallestSpeed;
        }

        private void MoveRight(FrameworkElement clickedElement, Point originalClickPosition)
        {
            var movementSpeed = CalculateMovementSpeed(clickedElement, originalClickPosition);
            var maxSpeedToNotSurpassMinRange = CalculateMaxSpeedToNotSurpassMinRange();
            var maxSpeedToNotSurpassMax = CalculateMaxSpeedToNotSurpassMax();

            var allSpeeds = new double[] {
                movementSpeed,
                maxSpeedToNotSurpassMinRange,
                maxSpeedToNotSurpassMax };

            var smallestSpeed = allSpeeds.Min();

            SliderStart += smallestSpeed;
            SliderEnd += smallestSpeed;
        }

        private bool CanMoveLeft()
        {
            return SliderStart > Min;
        }

        private bool CanMoveRight()
        {
            return SliderEnd < Max;
        }

        private double CalculateMovementSpeed(FrameworkElement clickedElement, Point originalClickPosition)
        {
            // The bigger the factor the faster the slider will move to the mouse position
            const double sliderSpeedFactor = 40.0;

            var backgroundBar = (FrameworkElement)GetTemplateChild("PART_BackgroundBar");

            var clickedElementPosition = Mouse.GetPosition(clickedElement);
            var movedDistance = Math.Abs(clickedElementPosition.X - originalClickPosition.X);
            var completeDistance = backgroundBar.ActualWidth;
            var relativeMoveDistance = movedDistance / completeDistance;

            var movementSpeed = relativeMoveDistance * sliderSpeedFactor;
            return movementSpeed;
        }

        private double CalculateMaxSpeedToNotSurpassMinRange()
        {
            var actualRange = SliderEnd - SliderStart;
            var maxSpeed = actualRange - MinSliderWidth;

            return maxSpeed;
        }

        private double CalculateMaxSpeedToNotSurpassMaxRange()
        {
            var actualRange = SliderEnd - SliderStart;
            var maxSpeed = MaxSliderWidth - actualRange;

            return maxSpeed;
        }

        private double CalculateMaxSpeedToNotSurpassMin()
        {
            return SliderStart - Min;
        }

        private double CalculateMaxSpeedToNotSurpassMax()
        {
            return Max - SliderEnd;
        }

    }
}

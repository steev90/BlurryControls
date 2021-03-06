﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using BlurryControls.Helpers;
using BlurryControls.Internals;

namespace BlurryControls.Controls
{
    //the following definition represents an implementation which is similar to the native windows
    //MessageBox control. it contains a visual definition for a caption, an icon which is arranged 
    //in the content area, a prompt to the user and a dynamic set of buttons which provide options
    //for a user to answer the prompt

    //if an owner is set the window will automatically add a blur effect to it and removes it when
    //the dialog is closed in any way, restoring all previously set effects of the owner

    /// <summary>
    ///     Interaction logic for BlurBehindDialogWindow.xaml
    /// </summary>
    internal partial class BlurBehindDialogWindow
    {
        public BlurBehindDialogWindow()
        {
            InitializeComponent();
            Loaded += InitializeDialogSpecificVisualBehaviour;
            Loaded += InitializeCustomDialogButtons;
            Loaded += ApplyVisualBehaviour;
            Loaded += BlurOwner;
            Closed += UnblurOwner;
        }

        #region Fields

        private Color _menuBarColor;
        private Effect _previousEffect;

        #endregion

        #region Events

        /// <summary>
        ///     a delegate that is called when an action is performed by the user
        /// </summary>
        /// <param name="sender">the event raising control of type <see cref="BlurBehindDialogWindow" /></param>
        /// <param name="args">
        ///     a preset argument list which is provided when an action is performed of type
        ///     <see cref="BlurryDialogResultArgs" />
        /// </param>
        public delegate void BlurryDialogResultEventHandler(object sender, BlurryDialogResultArgs args);

        /// <summary>
        ///     an event that is raised when an action is performed by the user
        /// </summary>
        public event BlurryDialogResultEventHandler ResultAquired;

        #endregion

        #region Dependency Properties

        public new static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            "Title", typeof(string), typeof(BlurBehindDialogWindow),
            new PropertyMetadata(default(string), OnTitleChanged));

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var blurBehindDialogWindow = (BlurBehindDialogWindow) d;
            blurBehindDialogWindow.TitleTextBlock.Text = (string) e.NewValue;
        }

        /// <summary>
        ///     the caption show in the MenuBar
        /// </summary>
        public new string Title
        {
            get => (string) GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty DialogMessageProperty = DependencyProperty.Register(
            "DialogMessage", typeof(string), typeof(BlurBehindDialogWindow),
            new PropertyMetadata(default(string), OnDialogMessageChanged));

        private static void OnDialogMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var blurBehindDialogWindow = (BlurBehindDialogWindow) d;
            blurBehindDialogWindow.DialogMessageTextBlock.Text = (string) e.NewValue;
        }

        /// <summary>
        ///     gets or sets the DialogMessageProperty
        ///     an information or prompt that is shown to a user
        /// </summary>
        public string DialogMessage
        {
            get => (string) GetValue(DialogMessageProperty);
            set => SetValue(DialogMessageProperty, value);
        }

        /// <summary>
        ///     an icon in the left area of the MenuBar which is currently not in use
        /// </summary>
        internal new ImageSource Icon
        {
            get => (ImageSource) GetValue(IconProperty);
            set
            {
                SetValue(IconProperty, value);
                //TitleImage.Source = value;
                ContentImage.Source = value;
            }
        }

        public static readonly DependencyProperty DialogIconProperty = DependencyProperty.Register(
            "DialogIcon", typeof(BlurryDialogIcon), typeof(BlurBehindDialogWindow),
            new PropertyMetadata(default(BlurryDialogIcon), OnDialogIconChanged));

        private static void OnDialogIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var blurBehindDialogWindow = (BlurBehindDialogWindow) d;
            blurBehindDialogWindow.SetDialogIconSource((BlurryDialogIcon) e.NewValue);
        }


        /// <summary>
        ///     gets or sets the DialogIconProperty
        ///     represents a chosen value of enum <see cref="BlurryDialogIcon" />
        /// </summary>
        public BlurryDialogIcon DialogIcon
        {
            get => (BlurryDialogIcon) GetValue(DialogIconProperty);
            set => SetValue(DialogIconProperty, value);
        }

        public static readonly DependencyProperty ButtonProperty = DependencyProperty.Register(
            "Button", typeof(BlurryDialogButton), typeof(BlurBehindDialogWindow),
            new PropertyMetadata(default(BlurryDialogButton), OnButtonChanged));

        private static void OnButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var blurBehindDialogWindow = (BlurBehindDialogWindow) d;
            blurBehindDialogWindow.SetButtonSet((BlurryDialogButton) e.NewValue);
        }

        /// <summary>
        ///     gets or sets the ButtonProperty
        ///     a preset composition chosen from enum <see cref="BlurryDialogButton" />
        /// </summary>
        public BlurryDialogButton Button
        {
            get => (BlurryDialogButton) GetValue(ButtonProperty);
            set => SetValue(ButtonProperty, value);
        }

        public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(
            "Strength", typeof(double), typeof(BlurBehindDialogWindow), new PropertyMetadata(0.75, OnStrengthChanged));

        private static void OnStrengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var blurBehindDialogWindow = (BlurBehindDialogWindow) d;

            if (blurBehindDialogWindow.Background == null) return;
            var backgroundColor = ((SolidColorBrush) blurBehindDialogWindow.Background).Color
                .OfStrength((double) e.NewValue).Color;
            blurBehindDialogWindow.Background = new SolidColorBrush(backgroundColor);
            blurBehindDialogWindow._menuBarColor = blurBehindDialogWindow.Background.OfStrength(0d).Color;
            blurBehindDialogWindow.MenuBar.Background = blurBehindDialogWindow._menuBarColor.GetBrush();
        }

        /// <summary>
        ///     gets or sets the StrengthProperty
        ///     the strength property determines the opacity of the window controls
        ///     it is set to 0.75 by default and may not exceed 1
        /// </summary>
        public double Strength
        {
            get => (double) GetValue(StrengthProperty);
            set => SetValue(StrengthProperty, (value >= 1 ? 1d : value) <= 0 ? 0d : value);
        }

        public static readonly DependencyProperty CustomDialogButtonsProperty = DependencyProperty.Register(
            "CustomDialogButtons", typeof(ButtonCollection), typeof(BlurBehindDialogWindow),
            new PropertyMetadata(default(ButtonCollection)));

        /// <summary>
        ///     a button collection shown instead of the conventional dialog buttons
        /// </summary>
        public ButtonCollection CustomDialogButtons
        {
            get => (ButtonCollection) GetValue(CustomDialogButtonsProperty);
            set => SetValue(CustomDialogButtonsProperty, value);
        }

        public static readonly DependencyProperty CustomContentProperty = DependencyProperty.Register(
            "CustomContent", typeof(FrameworkElement), typeof(BlurBehindDialogWindow),
            new PropertyMetadata(default(FrameworkElement), OnCustomContentChanged));

        private static void OnCustomContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var blurBehindDialogWindow = (BlurBehindDialogWindow) d;
            blurBehindDialogWindow.CustomContentControl.Content = (FrameworkElement) e.NewValue;
        }

        /// <summary>
        ///     custom content to be shown instead of a conventional dialog text and icon
        /// </summary>
        public FrameworkElement CustomContent
        {
            get => (FrameworkElement) GetValue(CustomContentProperty);
            set => SetValue(CustomContentProperty, value);
        }

        public static readonly DependencyProperty BlurDurationProperty = DependencyProperty.Register(
            "BlurDuration", typeof(int), typeof(BlurBehindDialogWindow), new PropertyMetadata(500));

        /// <summary>
        ///     milliseconds it takes to blur the owner
        /// </summary>
        public int BlurDuration
        {
            get => (int) GetValue(BlurDurationProperty);
            set => SetValue(BlurDurationProperty, value);
        }

        public static readonly DependencyProperty UnblurDurationProperty = DependencyProperty.Register(
            "UnblurDuration", typeof(int), typeof(BlurBehindDialogWindow), new PropertyMetadata(333));

        /// <summary>
        ///     milliseconds it takes to blur the owner
        /// </summary>
        public int UnblurDuration
        {
            get => (int) GetValue(UnblurDurationProperty);
            set => SetValue(UnblurDurationProperty, value);
        }

        public static readonly DependencyProperty BlurRadiusProperty = DependencyProperty.Register(
            "BlurRadius", typeof(int), typeof(BlurBehindDialogWindow), new PropertyMetadata(10));

        /// <summary>
        ///     sets the blur strength which is applied to the owner
        /// </summary>
        public int BlurRadius
        {
            get => (int) GetValue(BlurRadiusProperty);
            set => SetValue(BlurRadiusProperty, value);
        }

        #endregion

        #region Visual Behaviour

        private void ApplyVisualBehaviour(object sender, RoutedEventArgs e)
        {
            MenuBar.MouseEnter += MenuBarOnMouseEnter;
            MenuBar.MouseLeave += MenuBarOnMouseLeave;
            ButtonGrid.MouseEnter += MenuBarOnMouseEnter;
            ButtonGrid.MouseLeave += MenuBarOnMouseLeave;
        }

        private void MenuBarOnMouseEnter(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!(sender is Grid menuBar)) return;

            var colorTargetPath = new PropertyPath("(Panel.Background).(SolidColorBrush.Color)");
            var menuBarMouseEnterAnimation = new ColorAnimation
            {
                To = Background.GetColor(),
                FillBehavior = FillBehavior.HoldEnd,
                Duration = new Duration(TimeSpan.FromMilliseconds(500))
            };
            Storyboard.SetTarget(menuBarMouseEnterAnimation, menuBar);
            Storyboard.SetTargetProperty(menuBarMouseEnterAnimation, colorTargetPath);

            var menuBarMouseEnterStoryboard = new Storyboard();
            menuBarMouseEnterStoryboard.Children.Add(menuBarMouseEnterAnimation);
            menuBarMouseEnterStoryboard.Begin();
        }

        private void MenuBarOnMouseLeave(object sender, MouseEventArgs mouseEventArgs)
        {
            if (!(sender is Grid menuBar)) return;

            var colorTargetPath = new PropertyPath("(Panel.Background).(SolidColorBrush.Color)");
            var menuBarMouseLeaveAnimation = new ColorAnimation
            {
                To = _menuBarColor,
                FillBehavior = FillBehavior.HoldEnd,
                Duration = new Duration(TimeSpan.FromMilliseconds(500))
            };
            Storyboard.SetTarget(menuBarMouseLeaveAnimation, menuBar);
            Storyboard.SetTargetProperty(menuBarMouseLeaveAnimation, colorTargetPath);

            var menuBarMouseLeaveStoryboard = new Storyboard();
            menuBarMouseLeaveStoryboard.Children.Add(menuBarMouseLeaveAnimation);
            menuBarMouseLeaveStoryboard.Begin();
        }

        private void InitializeDialogSpecificVisualBehaviour(object sender, RoutedEventArgs e)
        {
            //apply blurry filter to the window
            this.Blur();

            SystemParameters.StaticPropertyChanged += SystemParametersOnStaticPropertyChanged;
            Background = ((BlurryWindow) Application.Current.MainWindow)?.Background?.OfStrength(Strength) ??
                         ColorHelper.SystemWindowGlassBrushOfStrength(Strength);
            _menuBarColor = Background.OfStrength(0d).Color;
            MenuBar.Background = _menuBarColor.GetBrush();

            SetDialogIconSource(DialogIcon);
            SetButtonSet(Button);
        }

        private void SystemParametersOnStaticPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "WindowGlassBrush") return;

            Background = ((BlurryWindow) Application.Current.MainWindow)?.Background?.OfStrength(Strength) ??
                         ColorHelper.SystemWindowGlassBrushOfStrength(Strength);
            _menuBarColor = Background.OfStrength(0d).Color;
            MenuBar.Background = _menuBarColor.GetBrush();
        }

        private void BlurOwner(object sender, RoutedEventArgs e)
        {
            if (!(Owner?.Content is FrameworkElement content)) return;

            _previousEffect = content.Effect;
            content.Effect = null;
            var blurEffect = new BlurEffect();
            content.Effect = blurEffect;

            var blurEffectAnimation = new DoubleAnimation
            {
                From = 0,
                To = BlurRadius,
                Duration = TimeSpan.FromMilliseconds(BlurDuration)
            };

            Storyboard.SetTarget(blurEffectAnimation, content);
            Storyboard.SetTargetProperty(blurEffectAnimation, new PropertyPath("Effect.Radius"));

            var sb = new Storyboard();
            sb.Children.Add(blurEffectAnimation);
            sb.Begin();
        }

        private void UnblurOwner(object sender, EventArgs e)
        {
            if (!(Owner?.Content is FrameworkElement content)) return;

            var blurEffectAnimation = new DoubleAnimation
            {
                From = BlurRadius,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(UnblurDuration)
            };

            Storyboard.SetTarget(blurEffectAnimation, content);
            Storyboard.SetTargetProperty(blurEffectAnimation, new PropertyPath("Effect.Radius"));

            var sb = new Storyboard();
            sb.Children.Add(blurEffectAnimation);
            sb.Completed += delegate
            {
                // restore previous effect
                Owner.Effect = _previousEffect;
            };

            sb.Begin();
        }

        #endregion

        #region Basic Functionality

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private void MenuBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //single click on MenuBar enables drag move
            //this also enables native Windows10 gestures
            //such as Aero Snap and Aero Shake
            ReleaseCapture();
            SendMessage(new WindowInteropHelper(this).Handle, 0xA1, (IntPtr) 0x2, (IntPtr) 0);
        }

        private void CloseButton_OnClick(object sender, RoutedEventArgs e)
        {
            CancelButton_OnClick(sender, e);
        }

        #endregion

        #region Dialog Management

        /// <summary>
        ///     sets the ImageSource of <see cref="Icon" /> to a value corresponding to a
        ///     chosen value <see cref="DialogIcon" /> using <see cref="IconHelper.GetDialogImage" />
        /// </summary>
        /// <param name="value">a chosen value of enum <see cref="BlurryDialogIcon" /></param>
        private void SetDialogIconSource(BlurryDialogIcon value)
        {
            Icon = value.GetDialogImage();
            ContentImage.Visibility = value == BlurryDialogIcon.None ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        ///     sets the visibility of the buttons in ButtonGrid by a preset combination
        ///     provided by the <see cref="Button" /> property
        /// </summary>
        /// <param name="value">a chosen value of enum <see cref="BlurryDialogButton" /></param>
        private void SetButtonSet(BlurryDialogButton value)
        {
            switch (value)
            {
                case BlurryDialogButton.Ok:
                    OkButton.Visibility = Visibility.Visible;

                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    CancelButton.Visibility = Visibility.Collapsed;

                    ButtonGrid.Visibility = Visibility.Visible;
                    break;
                case BlurryDialogButton.OkCancel:
                    OkButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;

                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;

                    ButtonGrid.Visibility = Visibility.Visible;
                    break;
                case BlurryDialogButton.YesNo:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;

                    OkButton.Visibility = Visibility.Collapsed;
                    CancelButton.Visibility = Visibility.Collapsed;

                    ButtonGrid.Visibility = Visibility.Visible;
                    break;
                case BlurryDialogButton.YesNoCancel:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;

                    OkButton.Visibility = Visibility.Collapsed;

                    ButtonGrid.Visibility = Visibility.Visible;
                    break;
                case BlurryDialogButton.None:
                    OkButton.Visibility = Visibility.Collapsed;
                    YesButton.Visibility = Visibility.Collapsed;
                    NoButton.Visibility = Visibility.Collapsed;
                    CancelButton.Visibility = Visibility.Collapsed;

                    ButtonGrid.Visibility = Visibility.Collapsed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        /// <summary>
        ///     raises an event of type <see cref="BlurryDialogResultEventHandler" /> when the YesButton was clicked
        /// </summary>
        /// <param name="sender">the event raising control of type <see cref="BlurBehindDialogWindow" /></param>
        /// <param name="e">
        ///     a preset argument list which is provided when an action is performed of type
        ///     <see cref="BlurryDialogResultArgs" />
        /// </param>
        private void YesButton_OnClick(object sender, RoutedEventArgs e)
        {
            var resultArguments = new BlurryDialogResultArgs {Result = BlurryDialogResult.Yes};
            ResultAquired?.Invoke(this, resultArguments);
        }

        /// <summary>
        ///     raises an event of type <see cref="BlurryDialogResultEventHandler" /> when the NoButton was clicked
        /// </summary>
        /// <param name="sender">the event raising control of type <see cref="BlurBehindDialogWindow" /></param>
        /// <param name="e">
        ///     a preset argument list which is provided when an action is performed of type
        ///     <see cref="BlurryDialogResultArgs" />
        /// </param>
        private void NoButton_OnClick(object sender, RoutedEventArgs e)
        {
            var resultArguments = new BlurryDialogResultArgs {Result = BlurryDialogResult.No};
            ResultAquired?.Invoke(this, resultArguments);
        }


        /// <summary>
        ///     raises an event of type <see cref="BlurryDialogResultEventHandler" /> when the OkButton was clicked
        /// </summary>
        /// <param name="sender">the event raising control of type <see cref="BlurBehindDialogWindow" /></param>
        /// <param name="e">
        ///     a preset argument list which is provided when an action is performed of type
        ///     <see cref="BlurryDialogResultArgs" />
        /// </param>
        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var resultArguments = new BlurryDialogResultArgs {Result = BlurryDialogResult.Ok};
            ResultAquired?.Invoke(this, resultArguments);
        }


        /// <summary>
        ///     raises an event of type <see cref="BlurryDialogResultEventHandler" /> when the CancelButton was clicked
        /// </summary>
        /// <param name="sender">the event raising control of type <see cref="BlurBehindDialogWindow" /></param>
        /// <param name="e">
        ///     a preset argument list which is provided when an action is performed of type
        ///     <see cref="BlurryDialogResultArgs" />
        /// </param>
        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            var resultArguments = new BlurryDialogResultArgs {Result = BlurryDialogResult.Cancel};
            ResultAquired?.Invoke(this, resultArguments);
        }

        /// <summary>
        ///     apply handlers to all custom buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InitializeCustomDialogButtons(object sender, RoutedEventArgs e)
        {
            if (CustomDialogButtons == null || CustomDialogButtons.Count == 0) return;
            Button = BlurryDialogButton.None;
            ButtonGrid.Visibility = Visibility.Visible;

            foreach (var customButton in CustomDialogButtons.OfType<Button>())
                customButton.Click += CustomButtonCloseOnClick;

            CustomButtonPanel.Visibility = Visibility.Visible;
        }

        /// <summary>
        ///     close dialog when any button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="routedEventArgs"></param>
        private void CustomButtonCloseOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var resultArguments = new BlurryDialogResultArgs {Result = BlurryDialogResult.None};
            ResultAquired?.Invoke(this, resultArguments);

            var customButton = (Button) sender;
            customButton.Click -= CustomButtonCloseOnClick;

            Close();
        }

        #endregion
    }
}
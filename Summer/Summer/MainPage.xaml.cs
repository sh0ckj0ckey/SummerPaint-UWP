﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Analysis;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Summer
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            SetTitleBarArea();
            SwitchAppTheme();
            LoadAppVersion();

            CommonShadow.Receivers.Add(ShadowReceiverGrid);

            App.OnWindowSizeChanged += OnWindowSizeChanged;

            RegisterClosingConfirm();
        }

        #region 应用程序相关

        /// <summary>
        /// 保存提示对话框
        /// </summary>
        private ContentDialog _exitConfirmDialog = null;

        /// <summary>
        /// 保存提示对话框是否已经打开
        /// </summary>
        private bool _exitConfirmDialogShowing = false;

        /// <summary>
        /// 是否有墨迹没有保存
        /// </summary>
        private bool _someInkNotSaved = false;

        /// <summary>
        /// 添加关闭应用程序的保存提示
        /// </summary>
        private void RegisterClosingConfirm()
        {
            try
            {
                var resourceLoader = ResourceLoader.GetForCurrentView();

                _exitConfirmDialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = resourceLoader.GetString("SaveConfirmTitle"),
                    Content = resourceLoader.GetString("SaveConfirmContent"),
                    PrimaryButtonText = resourceLoader.GetString("ConfirmSaveButton"),
                    SecondaryButtonText = resourceLoader.GetString("DonotSaveButton"),
                    CloseButtonText = resourceLoader.GetString("CancelSaveButton"),
                    DefaultButton = ContentDialogButton.Close
                };

                Windows.UI.Core.Preview.SystemNavigationManagerPreview.GetForCurrentView().CloseRequested +=
                    async (sender, args) =>
                    {
                        bool isCanvasEmpty = SketchCanvas.InkPresenter.StrokeContainer.GetStrokes().Count <= 0;
                        if (_someInkNotSaved && !isCanvasEmpty)
                        {
                            args.Handled = true;
                            if (!_exitConfirmDialogShowing)
                            {
                                _exitConfirmDialogShowing = true;

                                _exitConfirmDialog.XamlRoot = this.XamlRoot;
                                _exitConfirmDialog.RequestedTheme = this.ActualTheme;
                                var result = await _exitConfirmDialog.ShowAsync();
                                if (result == ContentDialogResult.Primary)
                                {
                                    SaveSketchToFile();
                                }
                                else if (result == ContentDialogResult.Secondary)
                                {
                                    App.Current.Exit();
                                }

                                _exitConfirmDialogShowing = false;
                            }
                        }
                    };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 设置应用程序的标题栏区域
        /// </summary>
        private void SetTitleBarArea()
        {
            try
            {
                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;

                // 设置为可拖动区域
                Window.Current.SetTitleBar(AppTitleBar);

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;

                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveForegroundColor = Windows.UI.Colors.Gray;

                // 当窗口激活状态改变时，注册一个handler
                Window.Current.Activated += (s, e) =>
                {
                    try
                    {
                        if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
                            AppTitleLogo.Opacity = 0.7;
                        else
                            AppTitleLogo.Opacity = 1.0;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 切换应用程序的主题
        /// </summary>
        private void SwitchAppTheme()
        {
            try
            {
                // 设置标题栏颜色
                bool isLight = _appSettings.AppearanceIndex == 0;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                if (isLight)
                {
                    titleBar.ButtonForegroundColor = Colors.Black;
                    titleBar.ButtonHoverForegroundColor = Colors.Black;
                    titleBar.ButtonPressedForegroundColor = Colors.Black;
                    titleBar.ButtonHoverBackgroundColor = new Color() { A = 8, R = 0, G = 0, B = 0 };
                    titleBar.ButtonPressedBackgroundColor = new Color() { A = 16, R = 0, G = 0, B = 0 };
                }
                else
                {
                    titleBar.ButtonForegroundColor = Colors.White;
                    titleBar.ButtonHoverForegroundColor = Colors.White;
                    titleBar.ButtonPressedForegroundColor = Colors.White;
                    titleBar.ButtonHoverBackgroundColor = new Color() { A = 16, R = 255, G = 255, B = 255 };
                    titleBar.ButtonPressedBackgroundColor = new Color() { A = 24, R = 255, G = 255, B = 255 };
                }

                // 设置应用程序颜色
                if (Window.Current.Content is FrameworkElement rootElement)
                {
                    if (_appSettings.AppearanceIndex == 1)
                    {
                        rootElement.RequestedTheme = ElementTheme.Dark;
                    }
                    else
                    {
                        rootElement.RequestedTheme = ElementTheme.Light;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }


        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;
                UpdateCanvasSize(true);

                SketchCanvas.InkPresenter.StrokeInput.StrokeEnded += OnStrokeEnded;

                SketchScrollViewer.RegisterPropertyChangedCallback(ScrollViewer.ZoomFactorProperty, (o, d) =>
                {
                    try
                    {
                        CanvasZoomFactorTextBlock.Text = ((SketchScrollViewer.ZoomFactor * 100)).ToString("f0");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SketchCanvas.InkPresenter.StrokeInput.StrokeEnded -= OnStrokeEnded;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void BackgroundGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                UpdateCanvasSize(false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private void UpdateCanvasSize(bool forceUpdateCavas)
        {
            try
            {
                SketchScrollViewer.Height = CanvasGrid.ActualHeight;
                SketchScrollViewer.Width = CanvasGrid.ActualWidth;

                if (SketchGrid.Height < CanvasGrid.ActualHeight || forceUpdateCavas)
                {
                    SketchGrid.Height = CanvasGrid.ActualHeight;
                }
                if (SketchGrid.Width < CanvasGrid.ActualWidth || forceUpdateCavas)
                {
                    SketchGrid.Width = CanvasGrid.ActualWidth;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        #endregion

        #region 画布

        /// <summary>
        /// 是否开启形状识别
        /// </summary>
        private bool _shapesRecognitionEnabled = false;

        /// <summary>
        /// 形状识别器
        /// </summary>
        private readonly InkAnalyzer _shapesAnalyzer = new InkAnalyzer();

        IReadOnlyList<InkStroke> strokesShape = null;

        InkAnalysisResult resultShape = null;

        private async void OnStrokeEnded(InkStrokeInput sender, PointerEventArgs args)
        {
            _someInkNotSaved = true;

            await Task.Delay(600);
            if (_shapesRecognitionEnabled)
            {
                strokesShape = SketchCanvas.InkPresenter.StrokeContainer.GetStrokes();

                if (strokesShape.Count > 0)
                {
                    _shapesAnalyzer.AddDataForStrokes(strokesShape);

                    resultShape = await _shapesAnalyzer.AnalyzeAsync();

                    if (resultShape.Status == InkAnalysisStatus.Updated)
                    {
                        var drawings = _shapesAnalyzer.AnalysisRoot.FindNodes(InkAnalysisNodeKind.InkDrawing);

                        foreach (var drawing in drawings)
                        {
                            var shape = (InkAnalysisInkDrawing)drawing;
                            if (shape.DrawingKind == InkAnalysisDrawingKind.Drawing)
                            {
                                // Catch and process unsupported shapes (lines and so on) here.
                            }
                            else
                            {
                                // Process recognized shapes here.
                                if (shape.DrawingKind == InkAnalysisDrawingKind.Circle || shape.DrawingKind == InkAnalysisDrawingKind.Ellipse)
                                {
                                    DrawEllipse(shape);
                                }
                                else
                                {
                                    DrawPolygon(shape);
                                }
                                foreach (var strokeId in shape.GetStrokeIds())
                                {
                                    var stroke = SketchCanvas.InkPresenter.StrokeContainer.GetStrokeById(strokeId);
                                    stroke.Selected = true;
                                }
                            }
                            _shapesAnalyzer.RemoveDataForStrokes(shape.GetStrokeIds());
                        }
                        SketchCanvas.InkPresenter.StrokeContainer.DeleteSelected();
                    }
                }
            }
        }

        private void DrawEllipse(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Ellipse ellipse = new Ellipse();
            ellipse.Width = Math.Sqrt((points[0].X - points[2].X) * (points[0].X - points[2].X) +
                 (points[0].Y - points[2].Y) * (points[0].Y - points[2].Y));
            ellipse.Height = Math.Sqrt((points[1].X - points[3].X) * (points[1].X - points[3].X) +
                 (points[1].Y - points[3].Y) * (points[1].Y - points[3].Y));

            var rotAngle = Math.Atan2(points[2].Y - points[0].Y, points[2].X - points[0].X);
            RotateTransform rotateTransform = new RotateTransform();
            rotateTransform.Angle = rotAngle * 180 / Math.PI;
            rotateTransform.CenterX = ellipse.Width / 2.0;
            rotateTransform.CenterY = ellipse.Height / 2.0;

            TranslateTransform translateTransform = new TranslateTransform();
            translateTransform.X = shape.Center.X - ellipse.Width / 2.0;
            translateTransform.Y = shape.Center.Y - ellipse.Height / 2.0;

            TransformGroup transformGroup = new TransformGroup();
            transformGroup.Children.Add(rotateTransform);
            transformGroup.Children.Add(translateTransform);
            ellipse.RenderTransform = transformGroup;

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 91, 40, 0));
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = 2;
            // ShapesCanvas.Children.Add(ellipse);
        }

        private void DrawPolygon(InkAnalysisInkDrawing shape)
        {
            var points = shape.Points;
            Polygon polygon = new Polygon();

            foreach (var point in points)
            {
                polygon.Points.Add(point);
            }

            var brush = new SolidColorBrush(Windows.UI.ColorHelper.FromArgb(255, 91, 40, 0));
            polygon.Stroke = brush;
            polygon.StrokeThickness = 2;
            // ShapesCanvas.Children.Add(polygon);
        }

        #endregion

        #region 底部功能栏

        /// <summary>
        /// 启用手指触摸绘画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckDrawWithHand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen | Windows.UI.Core.CoreInputDeviceTypes.Touch;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 关闭手指绘画
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckDrawWithHand(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    SketchCanvas.InkPresenter.InputDeviceTypes = Windows.UI.Core.CoreInputDeviceTypes.Mouse | Windows.UI.Core.CoreInputDeviceTypes.Pen;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 开启形状识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckShapeRec(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    _shapesRecognitionEnabled = true;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 关闭形状识别
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckShapeRec(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ToggleButton)
                {
                    _shapesRecognitionEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 开启底图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnCheckPictureBackground(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new Windows.Storage.Pickers.FileOpenPicker();
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");

                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(stream);
                        PictureBackgroundImage.Source = bitmapImage;
                        PictureBackgroundImage.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    if (sender is ToggleButton toggleButton)
                    {
                        toggleButton.IsChecked = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 关闭底图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckPictureBackground(object sender, RoutedEventArgs e)
        {
            try
            {
                PictureBackgroundImage.Visibility = Visibility.Collapsed;
                PictureBackgroundImage.Source = null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 保存到文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickSave(object sender, RoutedEventArgs e)
        {
            SaveSketchToFile();
        }

        /// <summary>
        /// 将草图保存到图片文件
        /// </summary>
        private async void SaveSketchToFile()
        {
            try
            {
                var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                savePicker.FileTypeChoices.Add("PNG", new List<string>() { ".png" });
                savePicker.SuggestedFileName = "Summer Sketch";

                Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CanvasDevice device = CanvasDevice.GetSharedDevice();
                    CanvasRenderTarget renderTarget = new CanvasRenderTarget(device, (int)SketchCanvas.ActualWidth, (int)SketchCanvas.ActualHeight, 96);
                    using (var ds = renderTarget.CreateDrawingSession())
                    {
                        ds.Clear(SettingsService.Instance.AppearanceIndex == 1 ? Color.FromArgb(255, 46, 46, 46) : Colors.White);
                        ds.DrawInk(SketchCanvas.InkPresenter.StrokeContainer.GetStrokes());
                    }

                    using (var fileStream = await file.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await renderTarget.SaveAsync(fileStream, CanvasBitmapFileFormat.Png, 1f);
                    }

                    _someInkNotSaved = false;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 重置画布缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickResetCanvasZoom(object sender, RoutedEventArgs e)
        {
            //double zoomCenterX = SketchScrollViewer.HorizontalOffset + (SketchScrollViewer.ViewportWidth / 2);
            //double zoomCenterY = SketchScrollViewer.VerticalOffset + (SketchScrollViewer.ViewportHeight / 2);
            SketchScrollViewer.ChangeView(0, 0, 1.0f);
        }

        /// <summary>
        /// 缩小画布缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickCanvasZoomOut(object sender, RoutedEventArgs e)
        {
            float currentZoom = SketchScrollViewer.ZoomFactor;
            SketchScrollViewer.ChangeView(null, null, Math.Max(1, (currentZoom - 0.5f)));
        }

        /// <summary>
        /// 增大画布缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickCanvasZoomIn(object sender, RoutedEventArgs e)
        {
            float currentZoom = SketchScrollViewer.ZoomFactor;
            SketchScrollViewer.ChangeView(null, null, Math.Min(5, (currentZoom + 0.5f)));
        }

        #endregion

        #region 右侧设置栏

        /// <summary>
        /// 应用程序窗口尺寸变化，更新全屏按钮状态
        /// </summary>
        private void OnWindowSizeChanged()
        {
            FullscreenButton.IsChecked = ApplicationView.GetForCurrentView().IsFullScreenMode;
        }

        /// <summary>
        /// 全屏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCheckFullscreen(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (!view.IsFullScreenMode)
            {
                view.TryEnterFullScreenMode();
                VisualStateManager.GoToState(this, "FullScreenState", false);
            }
        }

        /// <summary>
        /// 退出全屏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUncheckFullscreen(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                VisualStateManager.GoToState(this, "NormalState", false);
            }
        }

        /// <summary>
        /// 小窗置顶
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnCheckTopmost(object sender, RoutedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                compactOptions.CustomSize = new Windows.Foundation.Size(960, 740);
                _ = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                VisualStateManager.GoToState(this, "PiPState", false);
            }
        }

        /// <summary>
        /// 取消小窗置顶
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnUncheckTopmost(object sender, RoutedEventArgs e)
        {
            _ = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(ApplicationViewMode.Default);
            VisualStateManager.GoToState(this, "NormalState", false);
        }

        /// <summary>
        /// 点击打开设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsTeachingTip.IsOpen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 点击关闭设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClickCloseSettings(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsTeachingTip.IsOpen = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #endregion

        #region 设置

        /// <summary>
        /// 设置
        /// </summary>
        private readonly SettingsService _appSettings = SettingsService.Instance;

        /// <summary>
        /// 应用程序版本号
        /// </summary>
        private string _appVersion = string.Empty;

        /// <summary>
        /// 点击切换主题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SwitchAppTheme();
        }

        /// <summary>
        /// 获取应用程序的版本号
        /// </summary>
        /// <returns></returns>
        private void LoadAppVersion()
        {
            try
            {
                Package package = Package.Current;
                PackageId packageId = package.Id;
                PackageVersion version = packageId.Version;
                _appVersion = string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        #endregion
    }
}

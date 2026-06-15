Imports System.Windows.Interop
Imports QING.Core

Public Class FormMain

    Private ReadOnly PageHost As New UiKitShellHost()
    Public PageTuner As PageTuner
    Public PageSavedTunes As PageSavedTunes
    Private PageSettings As PageSettings
    Private PageAbout As PageAbout
    Public PageNav As PageUiKitLeft
    Private IsSizeSaveable As Boolean = False

    Public PageLeft As MyPageLeft
    Public PageRight As MyPageRight
    Public Property Hidden As Boolean

    Public Sub New()
        ApplicationStartTick = GetTimeMs()
        FrmMain = Me
        ThemeCheckAll(False)
        ThemeRefresh(Settings.Get(Of Integer)("UiLauncherTheme"))

        PageNav = New PageUiKitLeft()
        PageTuner = New PageTuner()
        PageSavedTunes = New PageSavedTunes()
        PageSettings = New PageSettings()
        PageAbout = New PageAbout()

        InitializeComponent()
        Opacity = 0

        PanMainLeft.Child = PageNav
        PageLeft = PageNav
        PanMainRight.Child = PageTuner
        PageRight = PageTuner
        PageHost.CurrentPage = UiKitDemoPage.Tuner
        PageNav.Configure(UiKitDemoPage.Tuner, PageTuner)
        PageTuner.PageState = MyPageRight.PageStates.ContentStay
    End Sub

    Private Sub FormMain_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Handle = New WindowInteropHelper(Me).Handle
        UpdateBackgroundAndTitleBar()
        BtnExtraBack.ShowCheck = AddressOf BtnExtraBack_ShowCheck

        Dim Resizer As New MyResizer(Me)
        Resizer.addResizerDown(ResizerB)
        Resizer.addResizerLeft(ResizerL)
        Resizer.addResizerLeftDown(ResizerLB)
        Resizer.addResizerLeftUp(ResizerLT)
        Resizer.addResizerRight(ResizerR)
        Resizer.addResizerRightDown(ResizerRB)
        Resizer.addResizerRightUp(ResizerRT)
        Resizer.addResizerUp(ResizerT)

        ThemeRefreshMain()
        BtnTitleSelect0.SetChecked(True, False, False)
        Height = Math.Max(Settings.Get(Of Integer)("WindowHeight"), MinHeight)
        Width = Math.Max(Settings.Get(Of Integer)("WindowWidth"), MinWidth)
        Top = (GetWPFSize(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height) - Height) / 2
        Left = (GetWPFSize(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width) - Width) / 2
        IsSizeSaveable = True
        ShowWindowToTop()

        ' Initialize Car Database
        Dim localCarsJsonPath = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cars.json")
        CarDatabase.Initialize(localCarsJsonPath)

        ' Initialize Saved Tunes Database
        SavedTunesDatabase.Initialize()

        ' Update Left Sidebar Status
        Dim apiKey = Settings.Get(Of String)("GeminiApiKey", "")
        Dim aiReady = Not String.IsNullOrWhiteSpace(apiKey)
        PageNav.UpdateStatus(CarDatabase.CarsList.Count, True, aiReady, "Gemini 2.5")

        ' Load cars in tuner combobox
        PageTuner.PageTuner_Loaded(Nothing, Nothing)

        ' Fetch updates asynchronously
        Dim fetchTask = Task.Run(Async Function() As Task
                                     Dim updated = Await CarDatabase.FetchUpdatesAsync()
                                     If updated Then
                                         RunInUi(Sub()
                                                     PageNav.UpdateStatus(CarDatabase.CarsList.Count, True, aiReady, "Gemini 2.5")
                                                     PageTuner.PageTuner_Loaded(Nothing, Nothing)
                                                 End Sub)
                                     End If
                                 End Function)

        AniStart({
            AaCode(Sub() AniControlEnabled = 0, 50),
            AaOpacity(Me, Settings.Get(Of Integer)("UiLauncherTransparent") / 1000 + 0.4, 250, 100),
            AaDouble(Sub(i) TransformPos.Y += i, -TransformPos.Y, 600, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaDouble(Sub(i) TransformRotate.Angle += i, -TransformRotate.Angle, 500, 100, New AniEaseOutBack(AniEasePower.Weak)),
            AaCode(Sub()
                       PanBack.RenderTransform = Nothing
                       PageNav.TriggerShowAnimation()
                       Logger.Info("FH6 调校工具桌面客户端已启动。")
                   End Sub, , True)
        }, "Form Show")

        Hint("数据已加载完成，准备调校您的座驾！", HintType.Green)
    End Sub

    Public Shared Sub UpdateBackgroundAndTitleBar(value)
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        FrmMain.UpdateBackgroundAndTitleBar()
    End Sub

    Public Sub UpdateBackgroundAndTitleBar()
        ShapeTitleLogo.Visibility = Visibility.Collapsed
        LabTitleLogo.Visibility = Visibility.Visible
        LabTitleStatus.Visibility = Visibility.Visible
        ImageTitleLogo.Visibility = Visibility.Collapsed
        PanTitleSelect.Visibility = Visibility.Visible
        LabTitleLogo.Text = "FH6 调校工具"
        LabTitleStatus.Text = "FH6 Adjust Tool"
        PanTitleMain.ColumnDefinitions(0).Width = New GridLength(1, GridUnitType.Star)
    End Sub

    Private Sub BtnTitleClose_Click(sender As Object, e As EventArgs) Handles BtnTitleClose.Click
        Close()
    End Sub

    Private Sub BtnTitleMin_Click(sender As Object, e As EventArgs) Handles BtnTitleMin.Click
        WindowState = WindowState.Minimized
    End Sub

    Private Sub BtnTitlePin_Click(sender As Object, e As EventArgs) Handles BtnTitlePin.Click
        Topmost = Not Topmost
        If Topmost Then
            BtnTitlePin.Opacity = 1.0
            BtnTitlePin.ToolTip = "取消置顶"
        Else
            BtnTitlePin.Opacity = 0.5
            BtnTitlePin.ToolTip = "窗口置顶"
        End If
    End Sub

    Private Sub FormDragMove(sender As Object, e As MouseButtonEventArgs) Handles PanTitle.MouseLeftButtonDown, PanMsg.MouseLeftButtonDown
        If e.ClickCount >= 2 Then
            WindowState = If(WindowState = WindowState.Maximized, WindowState.Normal, WindowState.Maximized)
        ElseIf sender.IsMouseDirectlyOver Then
            DragMove()
        End If
    End Sub

    Private Sub FormMain_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter, Me.DragOver
        e.Handled = True
        e.Effects = DragDropEffects.None
    End Sub

    Private Sub FormMain_Drop(sender As Object, e As DragEventArgs) Handles Me.Drop
    End Sub

    Private Sub FormMain_SizeChanged() Handles Me.SizeChanged, Me.Loaded
        If IsSizeSaveable Then
            Settings.Set("WindowHeight", CInt(Height))
            Settings.Set("WindowWidth", CInt(Width))
        End If
        RectForm.Rect = New Rect(0, 0, BorderForm.ActualWidth, BorderForm.ActualHeight)
        PanForm.Width = BorderForm.ActualWidth + 0.001
        PanForm.Height = BorderForm.ActualHeight + 0.001
        PanMain.Width = PanForm.Width
        PanMain.Height = Math.Max(0, PanForm.Height - PanTitle.ActualHeight)
        If WindowState = WindowState.Maximized Then WindowState = WindowState.Normal
    End Sub

    Private Sub BtnTitleSelect_Click(sender As MyRadioButton, raiseByMouse As Boolean) Handles BtnTitleSelect0.Check, BtnTitleSelect1.Check, BtnTitleSelect2.Check, BtnTitleSelect3.Check
        PageChange(CType(Val(sender.Tag), UiKitDemoPage))
    End Sub

    Public Sub PageChange(page As UiKitDemoPage)
        If PageHost.CurrentPage = page Then Return
        PageHost.LastPage = PageHost.CurrentPage
        PageHost.CurrentPage = page

        Dim target = GetRightPage(page)
        
        ' Update sidebar active key and status
        Dim apiKey = Settings.Get(Of String)("GeminiApiKey", "")
        Dim aiReady = Not String.IsNullOrWhiteSpace(apiKey)
        PageNav.UpdateStatus(CarDatabase.CarsList.Count, True, aiReady, "Gemini 2.5")
        
        PageNav.Configure(page, target)
        PageChangeAnim(target)
        Hint("已导航至：" & UiKitShellText.GetPageTitle(page))
    End Sub

    Private Function GetRightPage(page As UiKitDemoPage) As MyPageRight
        Select Case page
            Case UiKitDemoPage.SavedTunes
                Return PageSavedTunes
            Case UiKitDemoPage.Settings
                Return PageSettings
            Case UiKitDemoPage.About
                Return PageAbout
            Case Else
                Return PageTuner
        End Select
    End Function

    Private Sub PageChangeAnim(target As MyPageRight)
        If target Is Nothing Then Return
        AniStop("FrmMain PageChangeRight")
        AniControlEnabled += 1
        If PanMainRight.Child IsNot Nothing AndAlso TypeOf PanMainRight.Child Is MyPageRight Then
            CType(PanMainRight.Child, MyPageRight).PageOnExit()
        End If
        AniControlEnabled -= 1
        AniStart({
            AaCode(Sub()
                       AniControlEnabled += 1
                       If PanMainRight.Child IsNot Nothing AndAlso TypeOf PanMainRight.Child Is MyPageRight Then
                           CType(PanMainRight.Child, MyPageRight).PageOnForceExit()
                       End If
                       PanMainRight.Child = target
                       target.Opacity = 0
                       AniControlEnabled -= 1
                       BtnExtraBack.ShowRefresh()
                   End Sub, 110),
            AaCode(Sub()
                       target.Opacity = 1
                       target.PageOnEnter()
                   End Sub, 30, True)
        }, "FrmMain PageChangeRight")
    End Sub

    Private Sub PanMainLeft_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles PanMainLeft.SizeChanged
        If Not e.WidthChanged Then Return
        RectLeftBackground.Width = e.NewSize.Width
        RectLeftShadow.Opacity = If(e.NewSize.Width > 0, 1, 0)
    End Sub

    Public Sub ShowWindowToTop()
        Visibility = Visibility.Visible
        ShowInTaskbar = True
        WindowState = WindowState.Normal
        Topmost = True
        Topmost = False
        Activate()
        Focus()
    End Sub

    Public Sub BackToTop() Handles BtnExtraBack.Click
        Dim scroll = UiKitShellNavigation.GetActiveScroll(PanMainRight.Child)
        If scroll IsNot Nothing Then scroll.PerformVerticalOffsetDelta(-scroll.VerticalOffset)
    End Sub

    Private Function BtnExtraBack_ShowCheck() As Boolean
        Dim scroll = UiKitShellNavigation.GetActiveScroll(PanMainRight.Child)
        Return scroll IsNot Nothing AndAlso scroll.Visibility = Visibility.Visible AndAlso scroll.VerticalOffset > Height + If(BtnExtraBack.Show, 0, 700)
    End Function

    Public Sub DragDoing()
    End Sub

    Public Sub DragStop()
    End Sub

    Public Sub DragTick()
    End Sub

    Public Sub SliderDrag_Finish()
    End Sub

    Public Shared Sub EndProgramForce(returnValue As ProcessReturnValues)
        Environment.Exit(CInt(returnValue))
    End Sub

End Class

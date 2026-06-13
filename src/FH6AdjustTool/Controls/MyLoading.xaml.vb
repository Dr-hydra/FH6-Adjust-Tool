Imports QING.UIKIT.MyLoading

Public Class MyLoading

    Public Event IsErrorChanged(sender As Object, isError As Boolean)
    Public Event StateChanged(sender As Object, newState As MyLoadingState, oldState As MyLoadingState)
    Public Event Click(sender As Object, e As MouseButtonEventArgs)

    Public Property AutoRun As Boolean = True
    Private Uuid As Integer = GetUuid()

#Region "颜色"

    Public Property Foreground As SolidColorBrush
        Get
            Return GetValue(ForegroundProperty)
        End Get
        Set(value As SolidColorBrush)
            SetValue(ForegroundProperty, value)
        End Set
    End Property
    Public Shared ReadOnly ForegroundProperty As DependencyProperty = DependencyProperty.Register("Foreground", GetType(SolidColorBrush), GetType(MyLoading))
    Public Sub New()
        InitializeComponent()
        SetResourceReference(ForegroundProperty, "ColorBrush3")
    End Sub

#End Region

#Region "文本"

    Private Property _ShowProgress As Boolean = False
    Public Property ShowProgress As Boolean
        Get
            Return _ShowProgress
        End Get
        Set(value As Boolean)
            If _ShowProgress = value Then Return
            _ShowProgress = value
            RefreshText()
        End Set
    End Property

    Private _Text As String = "加载中"
    Public Property Text As String
        Get
            Return _Text
        End Get
        Set(value As String)
            _Text = value
            RefreshText()
        End Set
    End Property

    Private _TextError As String = "加载失败"
    Public Property TextError As String
        Get
            Return _TextError
        End Get
        Set(value As String)
            _TextError = value
            RefreshText()
        End Set
    End Property
    ''' <summary>
    ''' 是否在使用 Loader 时使用 Loader 的错误信息替换默认的错误文本提示。
    ''' </summary>
    Public Property TextErrorInherit As Boolean = True

    Private Sub RefreshText() Handles Me.IsErrorChanged, Me.Loaded, _State.ProgressChanged
        RunInUi(
        Sub()
            If InnerState = MyLoadingState.Error Then
                If TextErrorInherit AndAlso State.IsLoader Then
                    Dim Ex As Exception = CType(State, Object).Error
                    If Ex Is Nothing Then
                        LabText.Text = "未知错误"
                    Else
                        Ex = Ex.RootException()
                        LabText.Text = If(Ex.IsBadNetwork(), "网络环境不好，稍后再试，或使用 VPN 代理网络环境", Ex.Message)
                    End If
                Else
                    LabText.Text = TextError
                End If
            Else
                If ShowProgress AndAlso State.IsLoader Then
                    LabText.Text = Text & " - " & Math.Floor(CType(State, Object).Progress * 100) & "%"
                Else
                    LabText.Text = Text
                End If
            End If
        End Sub)
    End Sub

#End Region

#Region "状态改变"

    '状态枚举
    Public Enum MyLoadingState
        Unloaded = -1
        Run = 0
        [Stop] = 1
        [Error] = 2
    End Enum

    '用于外部改变的控件状态
    Private WithEvents _State As ILoadingTrigger
    Public Property State As ILoadingTrigger
        Get
            InitState()
            Return _State
        End Get
        Set(value As ILoadingTrigger)
            _State = value
            RefreshState()
        End Set
    End Property
    Private Sub InitState() Handles Me.Loaded
        If _State Is Nothing Then
            _State = New MyLoadingStateSimulator
            If AutoRun Then _State.LoadingState = MyLoadingState.Run
        End If
    End Sub

    Public Sub Run()
        InitState()
        State.LoadingState = MyLoadingState.Run
    End Sub

    Public Sub [Stop]()
        InitState()
        State.LoadingState = MyLoadingState.Stop
    End Sub

    Public Sub [Error]()
        InitState()
        State.LoadingState = MyLoadingState.Error
    End Sub

    Private Sub RefreshState() Handles _State.LoadingStateChanged, Me.Loaded, Me.Unloaded
        If _State Is Nothing Then InitState()
        If _State Is Nothing Then Return
        If _State.LoadingState = MyLoadingState.Run AndAlso Not IsLoaded Then InnerState = MyLoadingState.Stop
        InnerState = _State.LoadingState
        OuterState = _State.LoadingState
        UpdateVisibilityBasedOnState()
        AniLoop()
    End Sub

    '用于处理外部事件的状态
    Private Property _OuterState As MyLoadingState = MyLoadingState.Unloaded
    Private Property OuterState As MyLoadingState
        Get
            Return _OuterState
        End Get
        Set(value As MyLoadingState)
            If _OuterState = value Then Return
            Dim OldValue = _OuterState
            _OuterState = value
            '触发事件
            RaiseEvent StateChanged(Me, value, OldValue)
            If (OldValue = MyLoadingState.Error) <> (value = MyLoadingState.Error) Then RaiseEvent IsErrorChanged(Me, value = MyLoadingState.Error)
        End Set
    End Property


    '用于处理内部动画及事件的状态
    Private Property _InnerState As MyLoadingState = MyLoadingState.Unloaded
    Private Property InnerState As MyLoadingState
        Get
            Return _InnerState
        End Get
        Set(value As MyLoadingState)
            If _InnerState = value Then Return
            Dim OldValue = _InnerState
            _InnerState = value
            '触发事件
            AniLoop()
            If (OldValue = MyLoadingState.Error) <> (value = MyLoadingState.Error) Then
                ErrorAnimation(Me, value = MyLoadingState.Error)
            Else
                UpdateVisibilityBasedOnState()
            End If
        End Set
    End Property

#End Region

#Region "动画"

    ''' <summary>
    ''' 是否需要动画
    ''' </summary>
    Public Property HasAnimation As Boolean = True

    ''' <summary>
    ''' 动画循环是否在运行中。
    ''' </summary>
    Private IsLooping As Boolean = False
    Private Sub AniLoop()
        ' 镐子已移除，旋转加载环由 XAML 自动执行旋转动画，此处无需进行循环动画
        If Not HasAnimation OrElse IsLooping OrElse Not InnerState = MyLoadingState.Run OrElse AniSpeed > 10 OrElse Not IsLoaded Then Return
        IsLooping = True
        ErrorAnimationWaiting = False
    End Sub

    Private Sub UpdateVisibilityBasedOnState()
        RunInUi(
        Sub()
            If PathSpinner Is Nothing OrElse PathError Is Nothing Then Return
            Select Case InnerState
                Case MyLoadingState.Run
                    PathSpinner.Visibility = Visibility.Visible
                    PathSpinner.Opacity = 1
                    PathError.Visibility = Visibility.Collapsed
                    PathError.Opacity = 0
                Case MyLoadingState.Error
                    PathSpinner.Visibility = Visibility.Collapsed
                    PathSpinner.Opacity = 0
                    PathError.Visibility = Visibility.Visible
                    PathError.Opacity = 1
                Case Else ' Stop or Unloaded
                    PathSpinner.Visibility = Visibility.Collapsed
                    PathSpinner.Opacity = 0
                    PathError.Visibility = Visibility.Collapsed
                    PathError.Opacity = 0
            End Select
        End Sub)
    End Sub

    ''' <summary>
    ''' 标记是否还有没有退去错误动画的等待。
    ''' </summary>
    Private ErrorAnimationWaiting As Boolean = False
    Private Sub ErrorAnimation(sender As Object, isError As Boolean)
        If isError Then
            '由正常变为错误
            Dim Wait As Integer = If(ErrorAnimationWaiting, 400, 0)
            PathError.Visibility = Visibility.Visible
            AniStart({
                AaColor(PanBack, ForegroundProperty, "ColorBrushRedLight", 300),
                AaOpacity(PathError, 1 - PathError.Opacity, 100, 300 + Wait),
                AaScaleTransform(PathError, 1 - CType(PathError.RenderTransform, ScaleTransform).ScaleX, 400, 300 + Wait, New AniEaseOutBack),
                AaOpacity(PathSpinner, -PathSpinner.Opacity, 100, 300 + Wait),
                AaCode(Sub() PathSpinner.Visibility = Visibility.Collapsed, 400 + Wait, True)
            }, "MyLoader Error " & Uuid)
        Else
            '由错误变为正常
            PathSpinner.Visibility = Visibility.Visible
            AniStart({
                AaOpacity(PathError, -PathError.Opacity, 100),
                AaScaleTransform(PathError, 0.5 - CType(PathError.RenderTransform, ScaleTransform).ScaleX, 200),
                AaColor(PanBack, ForegroundProperty, "ColorBrush3", 300),
                AaOpacity(PathSpinner, 1 - PathSpinner.Opacity, 100),
                AaCode(Sub() PathError.Visibility = Visibility.Collapsed, 200, True)
            }, "MyLoader Error " & Uuid)
        End If
    End Sub

#End Region

#Region "Events"

    Private Sub Button_MouseUp(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonUp
        RaiseEvent Click(sender, e)
    End Sub
    Private IsMouseDown As Boolean = False
    Private Sub Button_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles Me.MouseLeftButtonDown
        '记录鼠标是否在点击事件之后离开，
        IsMouseDown = True
    End Sub
    Private Sub Button_MouseLeave(sender As Object, e As Object) Handles Me.MouseLeave, Me.MouseLeftButtonUp
        IsMouseDown = False
    End Sub

#End Region

End Class

Public Interface ILoadingTrigger
    ReadOnly Property IsLoader As Boolean
    Property LoadingState As MyLoadingState
    Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState)
    Event ProgressChanged(NewProgress As Double, OldProgress As Double)
End Interface

Public Class MyLoadingStateSimulator
    Implements ILoadingTrigger
    Private Property _LoadingState As MyLoadingState = MyLoadingState.Unloaded
    Public Property LoadingState As MyLoadingState Implements ILoadingTrigger.LoadingState
        Get
            Return _LoadingState
        End Get
        Set(value As MyLoadingState)
            If _LoadingState = value Then Return
            Dim OldState = _LoadingState
            _LoadingState = value
            RaiseEvent LoadingStateChanged(value, OldState)
        End Set
    End Property
    Public ReadOnly Property IsLoader As Boolean = False Implements ILoadingTrigger.IsLoader

    Public Event LoadingStateChanged(NewState As MyLoadingState, OldState As MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged
    Public Event ProgressChanged(NewProgress As Double, OldProgress As Double) Implements ILoadingTrigger.ProgressChanged
End Class

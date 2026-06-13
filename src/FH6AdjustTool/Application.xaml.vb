Imports System.Windows.Threading

Public Class Application

    Public Shared ShowingTooltips As New List(Of Border)

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
        ToolTipService.InitialShowDelayProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(300))
        ToolTipService.BetweenShowDelayProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(400))
        ToolTipService.ShowDurationProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(9999999))
        ToolTipService.PlacementProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(Primitives.PlacementMode.Bottom))
        ToolTipService.HorizontalOffsetProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(8.0))
        ToolTipService.VerticalOffsetProperty.OverrideMetadata(GetType(DependencyObject), New FrameworkPropertyMetadata(4.0))

        Try
            QING.Core.Main.Init(New UiKitLogger With {.logFolder = Path.Combine(PathExeFolder, "logs"), .MinLevel = LogLevel.Info})
        Catch
        End Try

        AniControlEnabled += 1
    End Sub

    Private Sub Application_DispatcherUnhandledException(sender As Object, e As DispatcherUnhandledExceptionEventArgs) Handles Me.DispatcherUnhandledException
        e.Handled = True
        MessageBox.Show(e.Exception.GetDisplay(True), "QING.UIKIT", MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub

    Private Sub TooltipLoaded(sender As Border, e As EventArgs)
        ShowingTooltips.Add(sender)
    End Sub

    Private Sub TooltipUnloaded(sender As Border, e As RoutedEventArgs)
        ShowingTooltips.Remove(sender)
    End Sub

End Class


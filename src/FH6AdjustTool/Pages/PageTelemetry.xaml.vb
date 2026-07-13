Imports System.Text.Json
Imports System.Windows.Threading
Imports Microsoft.Web.WebView2.Core
Imports QING.Core.Telemetry

Public Class PageTelemetry

    Private Shared ReadOnly JsonOptions As New JsonSerializerOptions() With {
        .PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        .WriteIndented = False
    }

    Private ReadOnly PushTimer As New DispatcherTimer() With {
        .Interval = TimeSpan.FromMilliseconds(250)
    }
    Private IsWebInitialized As Boolean = False

    Public Sub New()
        InitializeComponent()
        AddHandler PushTimer.Tick, AddressOf PushTimer_Tick
        AddHandler TelemetryRuntime.Service.LapCompleted, AddressOf Service_LapCompleted
        AddHandler TelemetryRuntime.Service.IssueMarked, AddressOf Service_IssueMarked
        AddHandler TelemetryRuntime.Service.StatusChanged, AddressOf Service_StatusChanged
    End Sub

    Private Async Sub PageTelemetry_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If IsWebInitialized Then Return
        IsWebInitialized = True

        Try
            Await InitializeWebViewAsync()
            If Settings.Get(Of Boolean)("TelemetryAutoStart", False) Then
                Await StartTelemetryAsync(Settings.Get(Of String)("TelemetryUdpHost", "127.0.0.1"),
                                          Settings.Get(Of Integer)("TelemetryUdpPort", 5400))
            End If
        Catch ex As Exception
            Hint("遥测页面初始化失败：" & ex.Message, HintType.Red)
        End Try
    End Sub

    Private Sub PageTelemetry_PageEnter() Handles Me.PageEnter
        PushTimer.Start()
    End Sub

    Private Sub PageTelemetry_PageExit() Handles Me.PageExit
        PushTimer.Stop()
    End Sub

    Private Async Function InitializeWebViewAsync() As Task
        Await WebTelemetry.EnsureCoreWebView2Async(Nothing)
        WebTelemetry.CoreWebView2.Settings.AreDefaultContextMenusEnabled = False
        WebTelemetry.CoreWebView2.Settings.AreDevToolsEnabled = Settings.Get(Of Boolean)("SystemDebugMode", False)
        AddHandler WebTelemetry.CoreWebView2.WebMessageReceived, AddressOf CoreWebView2_WebMessageReceived

        Dim dashboardDir = IO.Path.Combine(PathExeFolder, "Web", "TelemetryDashboard")
        Dim indexPath = IO.Path.Combine(dashboardDir, "index.html")
        If IO.File.Exists(indexPath) Then
            WebTelemetry.CoreWebView2.SetVirtualHostNameToFolderMapping("telemetry.fh6.local", dashboardDir, CoreWebView2HostResourceAccessKind.Allow)
            WebTelemetry.Source = New Uri("https://telemetry.fh6.local/index.html")
        Else
            WebTelemetry.NavigateToString("<html><body style='font-family:Segoe UI;padding:24px'>Telemetry dashboard assets are missing.</body></html>")
        End If
    End Function

    Private Async Sub CoreWebView2_WebMessageReceived(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim requestId As String = ""
        Dim ok As Boolean = True
        Dim result As Object = Nothing
        Dim errorMessage As String = Nothing
        Try
            Dim message = e.TryGetWebMessageAsString()
            If String.IsNullOrWhiteSpace(message) Then Return

            Using doc = JsonDocument.Parse(message)
                Dim root = doc.RootElement
                requestId = GetString(root, "id", "")
                Dim action = GetString(root, "action", "")
                Dim payload As JsonElement = Nothing
                root.TryGetProperty("payload", payload)

                result = Await HandleBridgeActionAsync(action, payload)
            End Using
        Catch ex As Exception
            ok = False
            errorMessage = ex.Message
        End Try

        Await SendBridgeResponseAsync(requestId, ok, result, errorMessage)
    End Sub

    Private Async Function HandleBridgeActionAsync(action As String, payload As JsonElement) As Task(Of Object)
        Select Case action
            Case "getSnapshot"
                Return BuildDashboardPayload()
            Case "start"
                Dim host = GetString(payload, "host", Settings.Get(Of String)("TelemetryUdpHost", "127.0.0.1"))
                Dim port = GetInt(payload, "port", Settings.Get(Of Integer)("TelemetryUdpPort", 5400))
                Settings.Set("TelemetryUdpHost", host)
                Settings.Set("TelemetryUdpPort", port)
                Return Await StartTelemetryAsync(host, port)
            Case "stop"
                Await TelemetryRuntime.Service.StopAsync("manual_stop")
                Return BuildDashboardPayload()
            Case "markGate"
                Dim trackName = GetString(payload, "trackName", "手动路线")
                Dim ok = TelemetryRuntime.Service.MarkGate(trackName)
                If Not ok Then Throw New Exception("当前还没有有效遥测位置，无法标记起终点。")
                Return BuildDashboardPayload()
            Case "refreshContext"
                TelemetryRuntime.Service.SetContext(GetCurrentTelemetryContext())
                Return BuildDashboardPayload()
            Case "getRecentSamples"
                Dim limit = GetInt(payload, "limit", 1000)
                Return TelemetryRuntime.Store.GetRecentSamples(GetString(payload, "sessionId", TelemetryRuntime.Service.CurrentSessionId), limit)
            Case "getSessions"
                Return TelemetryRuntime.Store.GetRecentSessions(GetInt(payload, "limit", 50))
            Case "getLaps"
                Return TelemetryRuntime.Store.GetRecentLaps(GetString(payload, "sessionId", ""), GetInt(payload, "limit", 100))
            Case "getComparisons"
                Return TelemetryRuntime.Store.GetTuneComparisons(GetInt(payload, "limit", 50))
            Case "saveIssue"
                Return SaveManualIssue(payload)
            Case Else
                Throw New Exception("未知遥测操作：" & action)
        End Select
    End Function

    Private Async Function StartTelemetryAsync(host As String, port As Integer) As Task(Of Object)
        Dim context = GetCurrentTelemetryContext()
        TelemetryRuntime.Service.SetContext(context)
        Await TelemetryRuntime.Service.StartAsync(host, port, context)
        Return BuildDashboardPayload()
    End Function

    Private Function BuildDashboardPayload() As Object
        Dim service = TelemetryRuntime.Service
        Dim sessionId = service.CurrentSessionId
        Return New With {
            .databasePath = TelemetryRuntime.DefaultDatabasePath,
            .snapshot = service.GetSnapshot(700),
            .sessions = TelemetryRuntime.Store.GetRecentSessions(30),
            .laps = TelemetryRuntime.Store.GetRecentLaps(sessionId, 80),
            .comparisons = TelemetryRuntime.Store.GetTuneComparisons(50),
            .settings = New With {
                .host = Settings.Get(Of String)("TelemetryUdpHost", "127.0.0.1"),
                .port = Settings.Get(Of Integer)("TelemetryUdpPort", 5400),
                .autoStart = Settings.Get(Of Boolean)("TelemetryAutoStart", False)
            }
        }
    End Function

    Private Function GetCurrentTelemetryContext() As TelemetrySessionContext
        If FrmMain IsNot Nothing AndAlso FrmMain.PageTuner IsNot Nothing Then
            Return FrmMain.PageTuner.GetTelemetryContext()
        End If
        Return New TelemetrySessionContext()
    End Function

    Private Function SaveManualIssue(payload As JsonElement) As TelemetryIssueMarker
        Dim service = TelemetryRuntime.Service
        Dim sample = service.LiveState.CurrentSample
        If sample Is Nothing OrElse String.IsNullOrWhiteSpace(service.CurrentSessionId) Then
            Throw New Exception("没有正在记录的遥测样本。")
        End If

        Dim marker As New TelemetryIssueMarker() With {
            .SessionId = service.CurrentSessionId,
            .SampleSequence = sample.Sequence,
            .CreatedAtMs = TelemetryService.NowMs(),
            .IssueType = GetString(payload, "issueType", "manual"),
            .Severity = GetString(payload, "severity", "info"),
            .Message = GetString(payload, "message", "手动标记")
        }
        TelemetryRuntime.Store.InsertIssueMarker(marker)
        service.LiveState.AddIssue(marker)
        Return marker
    End Function

    Private Async Function SendBridgeResponseAsync(requestId As String, ok As Boolean, payload As Object, Optional errorMessage As String = Nothing) As Task
        If WebTelemetry.CoreWebView2 Is Nothing OrElse String.IsNullOrWhiteSpace(requestId) Then Return
        Dim envelope = New Dictionary(Of String, Object) From {
            {"id", requestId},
            {"ok", ok},
            {"payload", payload},
            {"error", errorMessage}
        }
        Dim json = JsonSerializer.Serialize(envelope, JsonOptions)
        Await WebTelemetry.CoreWebView2.ExecuteScriptAsync("window.telemetryBridge.receive(" & json & ");")
    End Function

    Private Sub PostWebEvent(eventType As String, payload As Object)
        If WebTelemetry.CoreWebView2 Is Nothing Then Return
        Dim envelope = New Dictionary(Of String, Object) From {
            {"type", eventType},
            {"payload", payload}
        }
        WebTelemetry.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(envelope, JsonOptions))
    End Sub

    Private Sub PushTimer_Tick(sender As Object, e As EventArgs)
        PostWebEvent("telemetry-live", New With {
            .databasePath = TelemetryRuntime.DefaultDatabasePath,
            .snapshot = TelemetryRuntime.Service.GetSnapshot(700)
        })
    End Sub

    Private Sub Service_LapCompleted(sender As Object, e As TelemetryLapEventArgs)
        Dispatcher.BeginInvoke(New Action(Sub() PostWebEvent("lap-completed", e.Lap)))
    End Sub

    Private Sub Service_IssueMarked(sender As Object, e As TelemetryIssueEventArgs)
        Dispatcher.BeginInvoke(New Action(Sub() PostWebEvent("issue-marked", e.Marker)))
    End Sub

    Private Sub Service_StatusChanged(sender As Object, e As TelemetryStatusEventArgs)
        Dispatcher.BeginInvoke(New Action(Sub() PostWebEvent("status", e.Status)))
    End Sub

    Private Shared Function GetString(root As JsonElement, name As String, defaultValue As String) As String
        If root.ValueKind = JsonValueKind.Undefined OrElse root.ValueKind = JsonValueKind.Null Then Return defaultValue
        Dim item As JsonElement = Nothing
        If root.TryGetProperty(name, item) Then
            If item.ValueKind = JsonValueKind.String Then Return item.GetString()
            Return item.ToString()
        End If
        Return defaultValue
    End Function

    Private Shared Function GetInt(root As JsonElement, name As String, defaultValue As Integer) As Integer
        If root.ValueKind = JsonValueKind.Undefined OrElse root.ValueKind = JsonValueKind.Null Then Return defaultValue
        Dim item As JsonElement = Nothing
        If root.TryGetProperty(name, item) Then
            Dim intValue As Integer
            If item.ValueKind = JsonValueKind.Number AndAlso item.TryGetInt32(intValue) Then Return intValue
            If Integer.TryParse(item.ToString(), intValue) Then Return intValue
        End If
        Return defaultValue
    End Function

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Return New List(Of Tuple(Of String, FrameworkElement))()
    End Function

End Class

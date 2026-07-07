Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports QING.Core

Public Class PageUiKitLeft

    Private CurrentPage As UiKitDemoPage = UiKitDemoPage.Tuner
    Private CurrentSubPage As UiKitSubPage = UiKitSubPage.TunerMain

    Public Sub Configure(page As UiKitDemoPage, subPage As UiKitSubPage)
        CurrentPage = page
        CurrentSubPage = subPage
        
        If page = UiKitDemoPage.Tuner Then
            GridTunerAI.Visibility = Visibility.Visible
            GridOtherNav.Visibility = Visibility.Collapsed
            Width = 320
        Else
            GridTunerAI.Visibility = Visibility.Collapsed
            GridOtherNav.Visibility = Visibility.Visible
            Width = 160
            
            ' Set title
            Select Case page
                Case UiKitDemoPage.SavedTunes
                    LabNavTitle.Text = "保存的调校"
                Case UiKitDemoPage.Telemetry
                    LabNavTitle.Text = "遥测分析"
                Case UiKitDemoPage.Settings
                    LabNavTitle.Text = "参数设置"
                Case UiKitDemoPage.About
                    LabNavTitle.Text = "关于项目"
                Case Else
                    LabNavTitle.Text = "栏目导航"
            End Select
            
            PopulateSecondaryNav(page, subPage)
        End If
    End Sub

    Public Sub UpdateStatus(carCount As Integer, isCached As Boolean, aiReady As Boolean, aiModel As String)
        ' Status items removed from sidebar layout, stubbed to prevent NullReferenceException.
    End Sub

    Private Sub PopulateSecondaryNav(page As UiKitDemoPage, subPage As UiKitSubPage)
        PanSecondaryNav.Children.Clear()
        
        Dim navItems = GetSubNavItems(page)
        
        If navItems IsNot Nothing Then
            For Each navItem In navItems
                Dim item As New MyListItem With {
                    .Title = navItem.Item1,
                    .Type = MyListItem.CheckType.RadioBox,
                    .Tag = navItem.Item2,
                    .Height = 38,
                    .Margin = New Thickness(0, 0, 0, 4),
                    .IsScaleAnimationEnabled = False
                }
                AddHandler item.Check, AddressOf SecondaryNavItem_Check
                PanSecondaryNav.Children.Add(item)
            Next
            SetChecked(subPage)
        End If
    End Sub

    Private Function GetSubNavItems(page As UiKitDemoPage) As List(Of Tuple(Of String, UiKitSubPage))
        Select Case page
            Case UiKitDemoPage.SavedTunes
                Return New List(Of Tuple(Of String, UiKitSubPage)) From {
                    New Tuple(Of String, UiKitSubPage)("存档导入", UiKitSubPage.SavedTunesSaveImport),
                    New Tuple(Of String, UiKitSubPage)("导入分享码", UiKitSubPage.SavedTunesShareImport),
                    New Tuple(Of String, UiKitSubPage)("方案列表", UiKitSubPage.SavedTunesList)
                }
            Case UiKitDemoPage.Telemetry
                Return New List(Of Tuple(Of String, UiKitSubPage)) From {
                    New Tuple(Of String, UiKitSubPage)("遥测仪表盘", UiKitSubPage.TelemetryDashboard)
                }
            Case UiKitDemoPage.Settings
                Return New List(Of Tuple(Of String, UiKitSubPage)) From {
                    New Tuple(Of String, UiKitSubPage)("计量单位", UiKitSubPage.SettingsUnits),
                    New Tuple(Of String, UiKitSubPage)("存档导入", UiKitSubPage.SettingsSaveImport),
                    New Tuple(Of String, UiKitSubPage)("遥测数据", UiKitSubPage.SettingsTelemetry),
                    New Tuple(Of String, UiKitSubPage)("智能接口", UiKitSubPage.SettingsAi),
                    New Tuple(Of String, UiKitSubPage)("主题配色", UiKitSubPage.SettingsThemeColors),
                    New Tuple(Of String, UiKitSubPage)("背景磨砂", UiKitSubPage.SettingsBackground)
                }
            Case UiKitDemoPage.About
                Return New List(Of Tuple(Of String, UiKitSubPage)) From {
                    New Tuple(Of String, UiKitSubPage)("功能说明", UiKitSubPage.AboutFeatures),
                    New Tuple(Of String, UiKitSubPage)("版权免责", UiKitSubPage.AboutDisclaimer),
                    New Tuple(Of String, UiKitSubPage)("致谢数据", UiKitSubPage.AboutCredits)
                }
            Case Else
                Return New List(Of Tuple(Of String, UiKitSubPage))()
        End Select
    End Function

    Private Sub SetChecked(subPage As UiKitSubPage)
        For Each child In PanSecondaryNav.Children
            Dim item = TryCast(child, MyListItem)
            If item IsNot Nothing Then
                item.SetChecked(CInt(CType(item.Tag, UiKitSubPage)) = CInt(subPage), False, False)
            End If
        Next
    End Sub

    Private Sub SecondaryNavItem_Check(sender As Object, e As RouteEventArgs)
        If Not e.RaiseByMouse Then Return
        
        Dim item = TryCast(sender, MyListItem)
        If item Is Nothing OrElse item.Tag Is Nothing OrElse FrmMain Is Nothing Then Return

        Dim subPage = CType(item.Tag, UiKitSubPage)
        If subPage = CurrentSubPage Then Return
        FrmMain.SubPageChange(subPage)
    End Sub

    Public ChatHistory As New List(Of ChatMessage)

    Public Sub AddChatBubble(role As String, text As String)
        Dim border As New Border()
        border.CornerRadius = New CornerRadius(10)
        border.Padding = New Thickness(12, 8, 12, 8)
        border.Margin = New Thickness(0, 0, 0, 8)
        border.MaxWidth = 230
        
        Dim textBlock As New TextBlock()
        textBlock.Text = text
        textBlock.TextWrapping = TextWrapping.Wrap
        textBlock.FontSize = 12
        textBlock.LineHeight = 1.4
        
        border.Child = textBlock
        
        If role.Equals("user", StringComparison.OrdinalIgnoreCase) Then
            border.HorizontalAlignment = HorizontalAlignment.Right
            border.SetResourceReference(Border.BackgroundProperty, "ColorBrush3") ' Dynamic highlight blue
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushWhite")
        Else
            border.HorizontalAlignment = HorizontalAlignment.Left
            border.SetResourceReference(Border.BackgroundProperty, "ColorBrushGray8") ' Dynamic light gray
            textBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
        End If
        
        PanChatList.Children.Add(border)
        
        ScrollChat.UpdateLayout()
        ScrollChat.ScrollToEnd()
    End Sub

    Private Sub TxtChatInput_KeyUp(sender As Object, e As KeyEventArgs) Handles TxtChatInput.KeyUp
        If e.Key = Key.Enter Then
            SendChatMessage()
        End If
    End Sub

    Private Sub BtnSendChat_Click(sender As Object, e As EventArgs) Handles BtnSendChat.Click
        SendChatMessage()
    End Sub

    Private Async Sub SendChatMessage()
        Dim text = TxtChatInput.Text.Trim()
        If String.IsNullOrWhiteSpace(text) Then Return

        ' Clear text box
        TxtChatInput.Text = ""

        ' Add user message bubble
        AddChatBubble("user", text)

        ' Save to history
        ChatHistory.Add(New ChatMessage() With {.Role = "user", .Content = text})

        ' Lock UI
        BtnSendChat.IsEnabled = False
        TxtChatInput.IsEnabled = False
        LoadAI.Run()

        Try
            ' Retrieve settings
            Dim provider = Settings.Get(Of String)("AiProvider", "Gemini")
            Dim apiKey = Settings.Get(Of String)("AiApiKey", "")
            Dim model = Settings.Get(Of String)("AiModel", "3.1-flash")
            Dim customUrl = Settings.Get(Of String)("AiCustomUrl", "")

            If String.IsNullOrWhiteSpace(apiKey) Then
                AddChatBubble("assistant", "错误：未在设置中配置 AI API 密钥。")
                Return
            End If

            ' Call AI Client
            Dim reply = Await Task.Run(Function()
                Dim client = AiClientFactory.GetClient(provider)
                Dim systemPrompt = "你是一名极限竞速：地平线6 (Forza Horizon 6) 的调校专家。请根据之前的诊断报告，使用简体中文回答用户关于车辆调校的问题。回答要简明扼要且富有建设性。"
                Return client.ChatAsync(apiKey, ChatHistory, model, customUrl, systemPrompt)
            End Function)

            ' Add AI reply bubble
            AddChatBubble("assistant", reply)

            ' Save to history
            ChatHistory.Add(New ChatMessage() With {.Role = "assistant", .Content = reply})

        Catch ex As Exception
            AddChatBubble("assistant", "错误：" & ex.Message)
        Finally
            ' Unlock UI
            LoadAI.Stop()
            BtnSendChat.IsEnabled = True
            TxtChatInput.IsEnabled = True
            TxtChatInput.Focus()
        End Try
    End Sub

End Class

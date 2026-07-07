Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Input
Imports QING.Core

Public Class PageUiKitLeft

    Private CurrentPage As UiKitDemoPage = UiKitDemoPage.Tuner
    Private CurrentRight As MyPageRight
    Private _currentScroll As MyScrollViewer = Nothing

    Public Sub Configure(page As UiKitDemoPage, rightPage As MyPageRight)
        CurrentPage = page
        CurrentRight = rightPage
        
        If page = UiKitDemoPage.Tuner Then
            GridTunerAI.Visibility = Visibility.Visible
            GridOtherNav.Visibility = Visibility.Collapsed
            Width = 320
            SetCurrentScroll(Nothing)
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
            
            ' Populate dynamic secondary nav items
            PopulateSecondaryNav(page, rightPage)
            
            ' Bind scroll event
            Dim scroll = UiKitShellNavigation.GetActiveScroll(rightPage)
            SetCurrentScroll(scroll)
        End If
    End Sub

    Public Sub UpdateStatus(carCount As Integer, isCached As Boolean, aiReady As Boolean, aiModel As String)
        ' Status items removed from sidebar layout, stubbed to prevent NullReferenceException.
    End Sub

    Private Sub PopulateSecondaryNav(page As UiKitDemoPage, rightPage As MyPageRight)
        PanSecondaryNav.Children.Clear()
        
        Dim navItems As List(Of Tuple(Of String, FrameworkElement)) = Nothing
        
        If TypeOf rightPage Is PageSavedTunes Then
            navItems = CType(rightPage, PageSavedTunes).GetSecondaryNavItems()
        ElseIf TypeOf rightPage Is PageTelemetry Then
            navItems = CType(rightPage, PageTelemetry).GetSecondaryNavItems()
        ElseIf TypeOf rightPage Is PageSettings Then
            navItems = CType(rightPage, PageSettings).GetSecondaryNavItems()
        ElseIf TypeOf rightPage Is PageAbout Then
            navItems = CType(rightPage, PageAbout).GetSecondaryNavItems()
        End If
        
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
            
            ' Select the first one initially
            If PanSecondaryNav.Children.Count > 0 Then
                CType(PanSecondaryNav.Children(0), MyListItem).SetChecked(True, False, False)
            End If
        End If
    End Sub

    Private Sub SetCurrentScroll(scroll As MyScrollViewer)
        If _currentScroll IsNot Nothing Then
            RemoveHandler _currentScroll.ScrollChanged, AddressOf ScrollViewer_ScrollChanged
        End If
        _currentScroll = scroll
        If _currentScroll IsNot Nothing Then
            AddHandler _currentScroll.ScrollChanged, AddressOf ScrollViewer_ScrollChanged
        End If
    End Sub

    Private Sub ScrollViewer_ScrollChanged(sender As Object, e As ScrollChangedEventArgs)
        UpdateSelectedNavBasedOnScroll()
    End Sub

    Private Sub UpdateSelectedNavBasedOnScroll()
        If PanSecondaryNav.Children.Count = 0 OrElse CurrentRight Is Nothing Then Return
        Dim scroll = UiKitShellNavigation.GetActiveScroll(CurrentRight)
        If scroll Is Nothing Then Return
        
        Dim activeItem As MyListItem = Nothing
        Dim maxVal As Double = Double.MinValue
        
        For Each child In PanSecondaryNav.Children
            Dim item = TryCast(child, MyListItem)
            If item IsNot Nothing Then
                Dim target = TryCast(item.Tag, FrameworkElement)
                If target IsNot Nothing AndAlso target.IsVisible Then
                    Try
                        Dim relativeY = target.TransformToVisual(scroll).Transform(New Point(0, 0)).Y
                        ' Threshold of 100 pixels from top
                        If relativeY <= 100 AndAlso relativeY > maxVal Then
                            maxVal = relativeY
                            activeItem = item
                        End If
                    Catch
                        ' Visual not connected, ignore
                    End Try
                End If
            End If
        Next
        
        ' If none found (e.g. all below threshold), pick the first one
        If activeItem Is Nothing AndAlso PanSecondaryNav.Children.Count > 0 Then
            activeItem = TryCast(PanSecondaryNav.Children(0), MyListItem)
        End If
        
        If activeItem IsNot Nothing AndAlso Not activeItem.Checked Then
            For Each child In PanSecondaryNav.Children
                Dim item = TryCast(child, MyListItem)
                If item IsNot Nothing Then
                    item.SetChecked(item.Equals(activeItem), False, True)
                End If
            Next
        End If
    End Sub

    Private Sub SecondaryNavItem_Check(sender As Object, e As RouteEventArgs)
        If Not e.RaiseByMouse Then Return
        
        Dim item = TryCast(sender, MyListItem)
        If item Is Nothing OrElse item.Tag Is Nothing Then Return
        
        Dim target = TryCast(item.Tag, FrameworkElement)
        If target Is Nothing Then Return
        
        Dim scroll = UiKitShellNavigation.GetActiveScroll(CurrentRight)
        If scroll IsNot Nothing Then
            Try
                Dim relativePoint = target.TransformToVisual(scroll).Transform(New Point(0, 0))
                Dim offsetDelta = relativePoint.Y - 15
                scroll.PerformVerticalOffsetDelta(offsetDelta)
            Catch ex As Exception
                Logger.Error("滚动到指定卡片失败: " & ex.Message)
            End Try
        End If
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

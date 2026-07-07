Imports System.Windows
Imports System.Windows.Controls
Imports QING.Core

Public Class PageSettings

    Private IsInitializing As Boolean = True

    Public Sub New()
        InitializeComponent()
        IsInitializing = True

        ' Initialize ComboBoxes
        ComboUnitWeight.Items.Add("lbs")
        ComboUnitWeight.Items.Add("kg")
        
        ComboUnitSpeed.Items.Add("mph")
        ComboUnitSpeed.Items.Add("kmh")
        
        ComboUnitPressure.Items.Add("psi")
        ComboUnitPressure.Items.Add("bar")
        
        ComboUnitSprings.Items.Add("lbs/in")
        ComboUnitSprings.Items.Add("n/mm")
        ComboUnitSprings.Items.Add("kgf/mm")

        ' Initialize AI ComboBoxes
        ComboAiProvider.Items.Add("Gemini")
        ComboAiProvider.Items.Add("OpenAI-Compatible")

        IsInitializing = False
    End Sub

    Private Sub PageSettings_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        IsInitializing = True

        ' Load saved settings
        Dim weightUnit = Settings.Get(Of String)("UnitWeight", "lbs")
        Dim speedUnit = Settings.Get(Of String)("UnitSpeed", "mph")
        Dim pressureUnit = Settings.Get(Of String)("UnitPressure", "psi")
        Dim springsUnit = Settings.Get(Of String)("UnitSprings", "lbs/in")
        
        Dim provider = Settings.Get(Of String)("AiProvider", "Gemini")
        Dim apiKey = Settings.Get(Of String)("AiApiKey", "")
        Dim customUrl = Settings.Get(Of String)("AiCustomUrl", "")
        Dim model = Settings.Get(Of String)("AiModel", "gemini-2.5-flash")

        ' Set selected items
        ComboUnitWeight.SelectedIndex = ComboUnitWeight.Items.Cast(Of String)().ToList().IndexOf(weightUnit)
        ComboUnitSpeed.SelectedIndex = ComboUnitSpeed.Items.Cast(Of String)().ToList().IndexOf(speedUnit)
        ComboUnitPressure.SelectedIndex = ComboUnitPressure.Items.Cast(Of String)().ToList().IndexOf(pressureUnit)
        ComboUnitSprings.SelectedIndex = ComboUnitSprings.Items.Cast(Of String)().ToList().IndexOf(springsUnit)

        TxtAiApiKey.Text = apiKey
        TxtAiCustomUrl.Text = customUrl

        If provider.Equals("OpenAI-Compatible", StringComparison.OrdinalIgnoreCase) Then
            ComboAiProvider.SelectedIndex = 1
        Else
            ComboAiProvider.SelectedIndex = 0
        End If

        UpdateModelList()
        SelectModelInCombo(model)
        UpdateVisibility()

        If String.IsNullOrWhiteSpace(apiKey) Then
            LabKeyStatus.Text = "未配置 API 密钥"
        Else
            LabKeyStatus.Text = "已配置 API 密钥 (未测试)"
        End If

        ' Refresh personalization settings
        SettingService.RefreshSettings(Me)
        UpdateHslPanelVisibility()
        UpdateSliderLabels()
        UpdateBackgroundLabels()
        LoadBackgroundImagePath()

        IsInitializing = False
    End Sub

    ' Save setting on change
    Private Sub ComboUnitWeight_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboUnitWeight.SelectionChanged
        If IsInitializing OrElse ComboUnitWeight.SelectedIndex = -1 Then Return
        Settings.Set("UnitWeight", ComboUnitWeight.SelectedItem.ToString())
        Hint("已更新重量单位为：" & ComboUnitWeight.SelectedItem.ToString(), HintType.Blue)
    End Sub

    Private Sub ComboUnitSpeed_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboUnitSpeed.SelectionChanged
        If IsInitializing OrElse ComboUnitSpeed.SelectedIndex = -1 Then Return
        Settings.Set("UnitSpeed", ComboUnitSpeed.SelectedItem.ToString())
        Hint("已更新速度单位为：" & ComboUnitSpeed.SelectedItem.ToString(), HintType.Blue)
    End Sub

    Private Sub ComboUnitPressure_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboUnitPressure.SelectionChanged
        If IsInitializing OrElse ComboUnitPressure.SelectedIndex = -1 Then Return
        Settings.Set("UnitPressure", ComboUnitPressure.SelectedItem.ToString())
        Hint("已更新胎压单位为：" & ComboUnitPressure.SelectedItem.ToString(), HintType.Blue)
    End Sub

    Private Sub ComboUnitSprings_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboUnitSprings.SelectionChanged
        If IsInitializing OrElse ComboUnitSprings.SelectedIndex = -1 Then Return
        Settings.Set("UnitSprings", ComboUnitSprings.SelectedItem.ToString())
        Hint("已更新弹簧硬度单位为：" & ComboUnitSprings.SelectedItem.ToString(), HintType.Blue)
    End Sub

    Private Sub UpdateModelList()
        Dim providerIndex = ComboAiProvider.SelectedIndex
        ComboAiModel.Items.Clear()
        If providerIndex = 0 Then ' Gemini
            ComboAiModel.Items.Add("3.1-flash")
            ComboAiModel.Items.Add("gemini-2.5-flash")
            ComboAiModel.Items.Add("gemini-2.5-flash-lite")
            ComboAiModel.Items.Add("gemini-2.0-flash")
            ComboAiModel.Items.Add("gemini-flash-latest")
            ComboAiModel.Items.Add("自定义 (Custom)...")
        Else ' OpenAI-Compatible
            ComboAiModel.Items.Add("gpt-5.4")
            ComboAiModel.Items.Add("gpt-4o")
            ComboAiModel.Items.Add("gpt-4o-mini")
            ComboAiModel.Items.Add("deepseek-chat")
            ComboAiModel.Items.Add("自定义 (Custom)...")
        End If
    End Sub

    Private Sub SelectModelInCombo(model As String)
        Dim idx As Integer = -1
        For i As Integer = 0 To ComboAiModel.Items.Count - 2
            If ComboAiModel.Items(i).ToString().Equals(model, StringComparison.OrdinalIgnoreCase) Then
                idx = i
                Exit For
            End If
        Next

        If idx <> -1 Then
            ComboAiModel.SelectedIndex = idx
            TxtAiModelCustom.Text = ""
        Else
            ComboAiModel.SelectedIndex = ComboAiModel.Items.Count - 1
            TxtAiModelCustom.Text = model
        End If
    End Sub

    Private Sub UpdateVisibility()
        If GridCustomUrl Is Nothing OrElse GridModelCustom Is Nothing Then Return

        If ComboAiProvider.SelectedIndex = 1 Then
            GridCustomUrl.Visibility = Visibility.Visible
        Else
            GridCustomUrl.Visibility = Visibility.Collapsed
        End If

        If ComboAiModel.SelectedIndex = ComboAiModel.Items.Count - 1 Then
            GridModelCustom.Visibility = Visibility.Visible
        Else
            GridModelCustom.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub ComboAiProvider_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAiProvider.SelectionChanged
        UpdateVisibility()
        If IsInitializing OrElse ComboAiProvider.SelectedIndex = -1 Then Return

        Dim provider = If(ComboAiProvider.SelectedIndex = 0, "Gemini", "OpenAI-Compatible")
        Settings.Set("AiProvider", provider)

        Dim defaultModel = If(provider = "Gemini", "3.1-flash", "gpt-5.4")
        Settings.Set("AiModel", defaultModel)

        IsInitializing = True
        UpdateModelList()
        SelectModelInCombo(defaultModel)
        UpdateVisibility()
        IsInitializing = False

        Hint("已切换供应商为：" & provider, HintType.Blue)
    End Sub

    Private Sub ComboAiModel_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboAiModel.SelectionChanged
        UpdateVisibility()
        If IsInitializing OrElse ComboAiModel.SelectedIndex = -1 Then Return

        Dim isCustom = (ComboAiModel.SelectedIndex = ComboAiModel.Items.Count - 1)
        If Not isCustom Then
            Dim selectedModel = ComboAiModel.SelectedItem.ToString()
            Settings.Set("AiModel", selectedModel)
            Hint("已更新运行模型为：" & selectedModel, HintType.Blue)
        Else
            Dim customModel = TxtAiModelCustom.Text.Trim()
            Settings.Set("AiModel", customModel)
        End If
    End Sub

    Private Sub TxtAiModelCustom_ValidatedTextChanged(sender As Object, e As RoutedEventArgs) Handles TxtAiModelCustom.ValidatedTextChanged
        If IsInitializing Then Return
        If ComboAiModel.SelectedIndex = ComboAiModel.Items.Count - 1 Then
            Settings.Set("AiModel", TxtAiModelCustom.Text.Trim())
        End If
    End Sub

    Private Sub TxtAiCustomUrl_ValidatedTextChanged(sender As Object, e As RoutedEventArgs) Handles TxtAiCustomUrl.ValidatedTextChanged
        If IsInitializing Then Return
        Settings.Set("AiCustomUrl", TxtAiCustomUrl.Text.Trim())
    End Sub

    Private Sub TxtAiApiKey_ValidatedTextChanged(sender As Object, e As RoutedEventArgs) Handles TxtAiApiKey.ValidatedTextChanged
        If IsInitializing Then Return
        Settings.Set("AiApiKey", TxtAiApiKey.Text.Trim())
    End Sub

    ' Save & Test AI Config
    Private Async Sub BtnTestKey_Click(sender As Object, e As EventArgs) Handles BtnTestKey.Click
        Dim provider = If(ComboAiProvider.SelectedIndex = 0, "Gemini", "OpenAI-Compatible")
        Dim apiKey = TxtAiApiKey.Text.Trim()
        Dim customUrl = TxtAiCustomUrl.Text.Trim()
        
        Dim model = ""
        If ComboAiModel.SelectedIndex = ComboAiModel.Items.Count - 1 Then
            model = TxtAiModelCustom.Text.Trim()
        ElseIf ComboAiModel.SelectedIndex <> -1 Then
            model = ComboAiModel.SelectedItem.ToString()
        End If

        ' Disable buttons and show loader
        BtnTestKey.IsEnabled = False
        LoadKey.Run()
        LabKeyStatus.Text = "正在验证接口配置..."

        If String.IsNullOrEmpty(apiKey) Then
            Settings.Set("AiApiKey", "")
            LabKeyStatus.Text = "未配置 API 密钥"
            Hint("已清除 API 密钥配置。", HintType.Blue)
            LoadKey.Stop()
            BtnTestKey.IsEnabled = True
            Return
        End If

        Try
            Dim isValid = Await Task.Run(Function()
                Dim client = AiClientFactory.GetClient(provider)
                Return client.ValidateKeyAsync(apiKey, model, customUrl)
            End Function)
            
            If isValid Then
                Settings.Set("AiProvider", provider)
                Settings.Set("AiApiKey", apiKey)
                Settings.Set("AiCustomUrl", customUrl)
                Settings.Set("AiModel", model)
                
                LabKeyStatus.Text = "配置验证成功并已保存 ✓"
                Hint("AI 接口配置验证成功！", HintType.Green)
            Else
                LabKeyStatus.Text = "验证失败：无法连接或无效密钥"
                Hint("验证失败，请确认密钥/接口地址/模型正确且网络畅通。", HintType.Red)
            End If
        Catch ex As Exception
            LabKeyStatus.Text = "验证失败：" & ex.Message
            Hint("网络请求出错，请检查连接。", HintType.Red)
        Finally
            LoadKey.Stop()
            BtnTestKey.IsEnabled = True
        End Try
    End Sub

    Private Sub Preset_Check(sender As Object, e As EventArgs) Handles _
        RadioTheme0.Check, RadioTheme1.Check, RadioTheme5.Check, _
        RadioTheme7.Check, RadioTheme9.Check, RadioTheme10.Check, _
        RadioThemeCustom.Check

        If FrmMain Is Nothing Then Return
        
        ' Update HSL panel visibility
        UpdateHslPanelVisibility()
        
        ' Apply the selected theme colors
        Dim themeId = Settings.Get(Of Integer)("UiLauncherTheme")
        ThemeRefresh(themeId)
        ThemeRefreshMain()
    End Sub

    Private Sub Slider_Change(sender As Object, user As Boolean) Handles _
        SliderHue.Change, SliderSat.Change, SliderLight.Change

        If FrmMain Is Nothing Then Return

        UpdateSliderLabels()

        ' If Custom theme is active, apply HSL sliders immediately
        Dim themeId = Settings.Get(Of Integer)("UiLauncherTheme")
        If themeId = 14 Then
            ThemeRefresh(14)
            ThemeRefreshMain()
        End If
    End Sub

    Private Sub SliderWindowTrans_Change(sender As Object, user As Boolean) Handles SliderWindowTrans.Change
        If FrmMain Is Nothing Then Return
        UpdateBackgroundLabels()
        FrmMain.UpdateWindowOpacity()
    End Sub

    Private Sub SliderControlOpacity_Change(sender As Object, user As Boolean) Handles SliderControlOpacity.Change
        If FrmMain Is Nothing Then Return
        UpdateBackgroundLabels()
        FrmMain.UpdateControlOpacity()
    End Sub

    Private Sub SliderBgOpacity_Change(sender As Object, user As Boolean) Handles SliderBgOpacity.Change
        If FrmMain Is Nothing Then Return
        UpdateBackgroundLabels()
        FrmMain.LoadBackgroundImage()
    End Sub

    Private Sub SliderBgClarity_Change(sender As Object, user As Boolean) Handles SliderBgClarity.Change
        If FrmMain Is Nothing Then Return
        UpdateBackgroundLabels()
        FrmMain.LoadBackgroundImage()
    End Sub

    Private Sub BtnSelectBg_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnSelectBg.Click
        Dim openFileDialog As New OpenFileDialog()
        openFileDialog.Filter = "图像文件 (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|所有文件 (*.*)|*.*"
        If openFileDialog.ShowDialog() = True Then
            Settings.Set("UiBackgroundImagePath", openFileDialog.FileName)
            LabImagePath.Text = openFileDialog.FileName
            If FrmMain IsNot Nothing Then
                FrmMain.LoadBackgroundImage()
            End If
        End If
    End Sub

    Private Sub BtnResetBg_Click(sender As Object, e As MouseButtonEventArgs) Handles BtnResetBg.Click
        Settings.Set("UiBackgroundImagePath", "")
        LabImagePath.Text = "使用默认内置背景图"
        If FrmMain IsNot Nothing Then
            FrmMain.LoadBackgroundImage()
        End If
    End Sub

    Private Sub UpdateHslPanelVisibility()
        If PanCustomHSL Is Nothing OrElse RadioThemeCustom Is Nothing Then Return
        PanCustomHSL.Visibility = If(RadioThemeCustom.Checked, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub UpdateSliderLabels()
        If SliderHue Is Nothing OrElse SliderSat Is Nothing OrElse SliderLight Is Nothing Then Return
        LabHue.Text = "色调: " & CInt(SliderHue.Value)
        LabSat.Text = "饱和度: " & CInt(SliderSat.Value) & "%"
        LabLight.Text = "亮度微调: " & (CInt(SliderLight.Value) - 20)
    End Sub

    Private Sub UpdateBackgroundLabels()
        If SliderWindowTrans Is Nothing OrElse SliderBgOpacity Is Nothing OrElse SliderBgClarity Is Nothing OrElse SliderControlOpacity Is Nothing Then Return
        LabWindowTrans.Text = "窗口不透明度: " & CInt(SliderWindowTrans.Value) & "%"
        LabControlOpacity.Text = "控件不透明度: " & CInt(SliderControlOpacity.Value) & "%"
        LabBgOpacity.Text = "背景图不透明度: " & CInt(SliderBgOpacity.Value) & "%"
        LabBgClarity.Text = "背景图片清晰度: " & CInt(SliderBgClarity.Value) & "%"
    End Sub

    Private Sub LoadBackgroundImagePath()
        If LabImagePath Is Nothing Then Return
        Dim path = Settings.Get(Of String)("UiBackgroundImagePath")
        If String.IsNullOrWhiteSpace(path) Then
            LabImagePath.Text = "使用默认内置背景图"
        Else
            LabImagePath.Text = path
        End If
    End Sub

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Dim list As New List(Of Tuple(Of String, FrameworkElement))()
        list.Add(New Tuple(Of String, FrameworkElement)("计量单位", CardUnits))
        list.Add(New Tuple(Of String, FrameworkElement)("遥测数据", CardTelemetry))
        list.Add(New Tuple(Of String, FrameworkElement)("智能接口", CardApiKey))
        list.Add(New Tuple(Of String, FrameworkElement)("主题配色", CardThemeColors))
        list.Add(New Tuple(Of String, FrameworkElement)("背景磨砂", CardThemeBackground))
        Return list
    End Function
End Class

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

    ' Theme Color Toggle
    Private Sub BtnThemeNext_Click(sender As Object, e As EventArgs) Handles BtnThemeNext.Click
        ThemeRefresh((ThemeNow + 1) Mod 5)
        ThemeRefreshMain()
        Hint("主题强调色已切换。", HintType.Green)
    End Sub

    Private Sub BtnThemeReset_Click(sender As Object, e As EventArgs) Handles BtnThemeReset.Click
        ThemeRefresh(0)
        ThemeRefreshMain()
        Hint("已恢复默认蓝色主题。", HintType.Blue)
    End Sub

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Dim list As New List(Of Tuple(Of String, FrameworkElement))()
        list.Add(New Tuple(Of String, FrameworkElement)("计量单位", CardUnits))
        list.Add(New Tuple(Of String, FrameworkElement)("智能接口", CardApiKey))
        Return list
    End Function
End Class

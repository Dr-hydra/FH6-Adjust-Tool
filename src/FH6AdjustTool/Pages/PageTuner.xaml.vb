Imports System.Windows
Imports System.Windows.Controls
Imports System.Text.Json
Imports QING.Core
Imports QING.Core.Telemetry

Public Class PageTuner

    Private ActiveMode As String = "General"
    Private IsInitializing As Boolean = True
    Private IsLoadedOnce As Boolean = False
    Private CurrentResult As TuningResult

    Public Sub New()
        InitializeComponent()
        IsInitializing = True
        
        ' Initialize ComboBoxes
        ComboDriveType.Items.Add("AWD")
        ComboDriveType.Items.Add("RWD")
        ComboDriveType.Items.Add("FWD")
        ComboDriveType.SelectedIndex = 0
        
        ComboClass.Items.Add("D")
        ComboClass.Items.Add("C")
        ComboClass.Items.Add("B")
        ComboClass.Items.Add("A")
        ComboClass.Items.Add("S1")
        ComboClass.Items.Add("S2")
        ComboClass.Items.Add("R")
        ComboClass.Items.Add("X")
        ComboClass.SelectedIndex = 3 ' Default Class A
        
        ComboCompound.Items.Add("Street")
        ComboCompound.Items.Add("Sport")
        ComboCompound.Items.Add("Race Semi-Slick")
        ComboCompound.Items.Add("Race Slick")
        ComboCompound.Items.Add("Rally")
        ComboCompound.Items.Add("Drift")
        ComboCompound.Items.Add("Snow")
        ComboCompound.Items.Add("Drag")
        ComboCompound.SelectedIndex = 0 ' Default Street
        
        ComboDragDist.Items.Add("quarter")
        ComboDragDist.Items.Add("half")
        ComboDragDist.Items.Add("top")
        ComboDragDist.SelectedIndex = 0
        
        ' Populate Mode Buttons
        SetModeHighlight(BtnModeGeneral)
        
        IsInitializing = False
    End Sub

    Public Sub PageTuner_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ' Load cars to Combo only once
        If Not IsLoadedOnce Then
            IsLoadedOnce = True
            RefreshCarCombo()
            
            ' Trigger initial calculation only on first load
            If CurrentResult Is Nothing Then
                Recalculate()
            End If
        End If

        ' Hook up AI Enhancement button click in sidebar
        If FrmMain IsNot Nothing AndAlso FrmMain.PageNav IsNot Nothing Then
            RemoveHandler FrmMain.PageNav.BtnAIEnhance.Click, AddressOf BtnAIEnhance_Click
            AddHandler FrmMain.PageNav.BtnAIEnhance.Click, AddressOf BtnAIEnhance_Click
        End If
    End Sub

    ' Refresh the ComboBox list based on search or full list
    Private Sub RefreshCarCombo(Optional keyword As String = "")
        ComboCar.Items.Clear()
        Dim searchResults = If(String.IsNullOrWhiteSpace(keyword), 
            CarDatabase.CarsList, 
            CarDatabase.CarsList.Where(Function(c) c.make.Contains(keyword, StringComparison.OrdinalIgnoreCase) OrElse 
                                                 c.model.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList()
        )
        
        For Each car In searchResults
            ComboCar.Items.Add($"{car.make} {car.model} ({car.year}) [{car.drive}]")
        Next
        
        If ComboCar.Items.Count > 0 Then
            ComboCar.SelectedIndex = 0
        End If
    End Sub

    ' Handle Search Click
    Private Sub BtnCarSearch_Click(sender As Object, e As EventArgs) Handles BtnCarSearch.Click
        RefreshCarCombo(TxtCarSearch.Text)
    End Sub

    ' Handle Car Selection
    Private Sub ComboCar_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ComboCar.SelectionChanged
        If IsInitializing OrElse ComboCar.SelectedIndex = -1 Then Return
        
        Try
            Dim selectedText = ComboCar.SelectedItem.ToString()
            Dim selectedCar = CarDatabase.CarsList.FirstOrDefault(Function(c) $"{c.make} {c.model} ({c.year}) [{c.drive}]" = selectedText)
            
            If selectedCar IsNot Nothing Then
                IsInitializing = True
                
                ' Auto-fill specifications
                TxtWeight.Text = selectedCar.weight.ToString()
                TxtPi.Text = selectedCar.pi.ToString()
                
                ' Set drive type
                Dim driveIndex = ComboDriveType.Items.Cast(Of String)().ToList().IndexOf(selectedCar.drive)
                If driveIndex <> -1 Then ComboDriveType.SelectedIndex = driveIndex
                
                ' Set class
                Dim classIndex = ComboClass.Items.Cast(Of String)().ToList().IndexOf(selectedCar.cls)
                If classIndex <> -1 Then ComboClass.SelectedIndex = classIndex
                
                ' Default weight distribution: AWD/FWD = 52, RWD = 48
                TxtWeightDist.Text = If(selectedCar.drive = "RWD", "48", "52")
                
                ' Fill gearing if available
                If selectedCar.gears IsNot Nothing AndAlso selectedCar.gears.Count > 0 Then
                    TxtGears.Text = selectedCar.gears.Count.ToString()
                End If
                
                IsInitializing = False
            End If
        Catch ex As Exception
            ' Silent fail
        End Try
    End Sub

    ' Mode Selection Click Handles
    Private Sub BtnModeRace_Click(sender As Object, e As EventArgs) Handles BtnModeRace.Click
        ActiveMode = "Race"
        SetModeHighlight(BtnModeRace)
    End Sub
    Private Sub BtnModeTouge_Click(sender As Object, e As EventArgs) Handles BtnModeTouge.Click
        ActiveMode = "Touge"
        SetModeHighlight(BtnModeTouge)
    End Sub
    Private Sub BtnModeWangan_Click(sender As Object, e As EventArgs) Handles BtnModeWangan.Click
        ActiveMode = "Wangan"
        SetModeHighlight(BtnModeWangan)
    End Sub
    Private Sub BtnModeDrift_Click(sender As Object, e As EventArgs) Handles BtnModeDrift.Click
        ActiveMode = "Drift"
        SetModeHighlight(BtnModeDrift)
    End Sub
    Private Sub BtnModeDrag_Click(sender As Object, e As EventArgs) Handles BtnModeDrag.Click
        ActiveMode = "Drag"
        SetModeHighlight(BtnModeDrag)
    End Sub
    Private Sub BtnModeRally_Click(sender As Object, e As EventArgs) Handles BtnModeRally.Click
        ActiveMode = "Rally"
        SetModeHighlight(BtnModeRally)
    End Sub
    Private Sub BtnModeRain_Click(sender As Object, e As EventArgs) Handles BtnModeRain.Click
        ActiveMode = "Rain"
        SetModeHighlight(BtnModeRain)
    End Sub
    Private Sub BtnModeGeneral_Click(sender As Object, e As EventArgs) Handles BtnModeGeneral.Click
        ActiveMode = "General"
        SetModeHighlight(BtnModeGeneral)
    End Sub

    Private Sub SetModeHighlight(activeBtn As MyButton)
        Dim btns = {BtnModeRace, BtnModeTouge, BtnModeWangan, BtnModeDrift, BtnModeDrag, BtnModeRally, BtnModeRain, BtnModeGeneral}
        For Each btn In btns
            If btn Is activeBtn Then
                btn.ColorType = MyButton.ColorState.Highlight
            Else
                btn.ColorType = MyButton.ColorState.Normal
            End If
        Next
    End Sub

    ' Toggle sections visibility
    Private Sub ChkGearing_Change(sender As Object, user As Boolean) Handles ChkGearing.Change
        PanelGearingInputs.Visibility = If(ChkGearing.Checked, Visibility.Visible, Visibility.Collapsed)
    End Sub

    Private Sub ChkAero_Change(sender As Object, user As Boolean) Handles ChkAero.Change
        PanelAeroInputs.Visibility = If(ChkAero.Checked, Visibility.Visible, Visibility.Collapsed)
    End Sub

    ' Manual Calculation Click
    Private Sub BtnCalculate_Click(sender As Object, e As RoutedEventArgs) Handles BtnCalculate.Click
        Recalculate()
    End Sub

    ' Recalculate tuning parameters
    Private Sub Recalculate()
        If IsInitializing Then Return
        
        Try
            Dim s As New TuningState()
            PopulateTuningState(s)

            ' Perform calculations
            CurrentResult = TuningCalculator.Calculate(s)
            
            ' Render results
            RenderResults(CurrentResult)
            
        Catch ex As Exception
            ' Silent fail during typing inputs
        End Try
    End Sub

    Private Sub PopulateTuningState(s As TuningState)
        s.TuneId = ActiveMode
        s.DriveType = If(ComboDriveType.SelectedItem?.ToString(), "AWD")
        s.CarClass = If(ComboClass.SelectedItem?.ToString(), "A")
        s.Compound = If(ComboCompound.SelectedItem?.ToString(), "Street")
        s.DragDist = If(ComboDragDist.SelectedItem?.ToString(), "quarter")
        
        ' Unit systems (Load from settings, fallback to metric/imperial defaults)
        s.WeightUnit = Settings.Get(Of String)("UnitWeight", "lbs")
        s.SpeedUnit = Settings.Get(Of String)("UnitSpeed", "mph")
        s.PressureUnit = Settings.Get(Of String)("UnitPressure", "psi")
        s.SpringsUnit = Settings.Get(Of String)("UnitSprings", "lbs/in")
        
        Double.TryParse(TxtWeight.Text, s.Weight)
        Double.TryParse(TxtWeightDist.Text, s.WeightDist)
        Integer.TryParse(TxtPi.Text, s.Pi)
        s.TireWF = TxtTireWF.Text
        s.TireWR = TxtTireWR.Text
        
        s.FeelBalance = SliderBalance.Value
        s.FeelAggression = SliderAggression.Value
        
        s.IncludeGearing = ChkGearing.Checked
        If s.IncludeGearing Then
            Double.TryParse(TxtRedlineRpm.Text, s.RedlineRpm)
            Double.TryParse(TxtPeakRpm.Text, s.PeakTorqueRpm)
            Double.TryParse(TxtMaxTorque.Text, s.MaxTorque)
            Double.TryParse(TxtTopspeed.Text, s.Topspeed)
            Integer.TryParse(TxtGears.Text, s.Gears)
        End If
        
        s.HasAero = ChkAero.Checked
        If s.HasAero Then
            Double.TryParse(TxtAeroF.Text, s.AeroF)
            Double.TryParse(TxtAeroR.Text, s.AeroR)
            Double.TryParse(TxtDragCd.Text, s.DragCd)
        End If
    End Sub

    Private Sub BtnSaveTune_Click(sender As Object, e As EventArgs) Handles BtnSaveTune.Click
        Dim name As String = TxtTuneName.Text.Trim()
        If String.IsNullOrEmpty(name) Then
            Hint("请输入调校名称！", HintType.Red)
            Return
        End If

        ' Build the state
        Dim s As New TuningState()
        PopulateTuningState(s)

        ' Build the result
        If CurrentResult Is Nothing Then
            Hint("尚无计算结果，无法保存！", HintType.Red)
            Return
        End If

        ' Update the result with any manual fine-tuning edits made in UI
        UpdateCurrentResultFromUI()

        ' Retrieve the selected car text
        Dim carText As String = If(ComboCar.SelectedItem IsNot Nothing, ComboCar.SelectedItem.ToString(), "")
        Dim searchKeyword As String = TxtCarSearch.Text.Trim()

        Dim tune As New SavedTune()
        
        ' Check if a tune with the same name already exists
        Dim existing = SavedTunesDatabase.Tunes.FirstOrDefault(Function(t) t.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        If existing IsNot Nothing Then
            If MessageBox.Show("已存在名为 “" & name & "” 的调校。是否要覆盖它？", "确认覆盖", MessageBoxButton.OKCancel, MessageBoxImage.Question) = MessageBoxResult.OK Then
                tune.Id = existing.Id
            Else
                Return
            End If
        End If

        tune.Name = name
        tune.SavedAt = DateTime.Now
        tune.CarSearchKeyword = searchKeyword
        tune.SelectedCarText = carText
        tune.State = s
        tune.Result = CurrentResult

        SavedTunesDatabase.SaveTune(tune)
        Hint("调校 “" & name & "” 保存成功！", HintType.Green)
    End Sub

    Public Sub LoadSavedTune(tune As SavedTune)
        If tune Is Nothing Then Return
        
        IsInitializing = True
        
        Try
            TxtTuneName.Text = tune.Name
            
            ' Restore car search keyword and combobox selection
            TxtCarSearch.Text = tune.CarSearchKeyword
            RefreshCarCombo(tune.CarSearchKeyword)
            
            If Not String.IsNullOrEmpty(tune.SelectedCarText) Then
                Dim carIndex = ComboCar.Items.Cast(Of String)().ToList().IndexOf(tune.SelectedCarText)
                If carIndex <> -1 Then
                    ComboCar.SelectedIndex = carIndex
                Else
                    ' Fallback to first item if not matched exactly
                    If ComboCar.Items.Count > 0 Then ComboCar.SelectedIndex = 0
                End If
            End If
            
            ' Restore mode button highlight
            ActiveMode = tune.State.TuneId
            Dim targetBtn As MyButton = BtnModeGeneral
            Select Case ActiveMode
                Case "Race" : targetBtn = BtnModeRace
                Case "Touge" : targetBtn = BtnModeTouge
                Case "Wangan" : targetBtn = BtnModeWangan
                Case "Drift" : targetBtn = BtnModeDrift
                Case "Drag" : targetBtn = BtnModeDrag
                Case "Rally" : targetBtn = BtnModeRally
                Case "Rain" : targetBtn = BtnModeRain
                Case "General" : targetBtn = BtnModeGeneral
            End Select
            SetModeHighlight(targetBtn)
            
            ' Restore drive type
            Dim driveIndex = ComboDriveType.Items.Cast(Of String)().ToList().IndexOf(tune.State.DriveType)
            If driveIndex <> -1 Then ComboDriveType.SelectedIndex = driveIndex
            
            ' Restore class
            Dim classIndex = ComboClass.Items.Cast(Of String)().ToList().IndexOf(tune.State.CarClass)
            If classIndex <> -1 Then ComboClass.SelectedIndex = classIndex
            
            ' Restore compound
            Dim compoundIndex = ComboCompound.Items.Cast(Of String)().ToList().IndexOf(tune.State.Compound)
            If compoundIndex <> -1 Then ComboCompound.SelectedIndex = compoundIndex
            
            ' Restore basic inputs
            TxtWeight.Text = tune.State.Weight.ToString()
            TxtWeightDist.Text = tune.State.WeightDist.ToString()
            TxtPi.Text = tune.State.Pi.ToString()
            TxtTireWF.Text = tune.State.TireWF
            TxtTireWR.Text = tune.State.TireWR
            
            ' Restore feel adjusters
            SliderBalance.Value = tune.State.FeelBalance
            SliderAggression.Value = tune.State.FeelAggression
            
            ' Restore gearing section
            ChkGearing.Checked = tune.State.IncludeGearing
            PanelGearingInputs.Visibility = If(ChkGearing.Checked, Visibility.Visible, Visibility.Collapsed)
            CardGearingResult.Visibility = If(ChkGearing.Checked, Visibility.Visible, Visibility.Collapsed)
            
            TxtRedlineRpm.Text = tune.State.RedlineRpm.ToString()
            TxtPeakRpm.Text = tune.State.PeakTorqueRpm.ToString()
            TxtMaxTorque.Text = tune.State.MaxTorque.ToString()
            TxtTopspeed.Text = tune.State.Topspeed.ToString()
            TxtGears.Text = tune.State.Gears.ToString()
            
            Dim dragDistIndex = ComboDragDist.Items.Cast(Of String)().ToList().IndexOf(tune.State.DragDist)
            If dragDistIndex <> -1 Then ComboDragDist.SelectedIndex = dragDistIndex
            
            ' Restore aero section
            ChkAero.Checked = tune.State.HasAero
            PanelAeroInputs.Visibility = If(ChkAero.Checked, Visibility.Visible, Visibility.Collapsed)
            CardAeroResult.Visibility = If(ChkAero.Checked, Visibility.Visible, Visibility.Collapsed)
            
            TxtAeroF.Text = tune.State.AeroF.ToString()
            TxtAeroR.Text = tune.State.AeroR.ToString()
            TxtDragCd.Text = tune.State.DragCd.ToString()
            
        Catch ex As Exception
            ' Log error
            Console.WriteLine("Error loading saved tune: " & ex.Message)
        Finally
            IsInitializing = False
        End Try
        
        ' Display the saved, potentially edited result directly; otherwise recalculate.
        If tune.Result IsNot Nothing Then
            CurrentResult = tune.Result
            RenderResults(tune.Result)
        Else
            Recalculate()
        End If
    End Sub

    Private Sub RenderResults(r As TuningResult)
        ' Tires
        OutTirePressF.Text = GetValByKey(r.Tires, "Front Pressure")
        OutTirePressR.Text = GetValByKey(r.Tires, "Rear Pressure")
        TipTires.Text = "轮胎提示: " & r.Tires.Tip

        ' Alignment
        OutCamberF.Text = GetValByKey(r.Alignment, "Front Camber")
        OutCamberR.Text = GetValByKey(r.Alignment, "Rear Camber")
        OutToeF.Text = GetValByKey(r.Alignment, "Front Toe")
        OutToeR.Text = GetValByKey(r.Alignment, "Rear Toe")
        OutCaster.Text = GetValByKey(r.Alignment, "Front Caster")
        TipAlignment.Text = "定位提示: " & r.Alignment.Tip

        ' Suspension
        OutSpringF.Text = GetValByKey(r.Suspension, "Front Spring")
        OutSpringR.Text = GetValByKey(r.Suspension, "Rear Spring")
        OutRideF.Text = GetValByKey(r.Suspension, "Front Ride Height")
        OutRideR.Text = GetValByKey(r.Suspension, "Rear Ride Height")
        TipSpring.Text = "弹簧与高度: " & r.Suspension.Tip

        ' ARB
        OutArbF.Text = GetValByKey(r.ARB, "Front ARB")
        OutArbR.Text = GetValByKey(r.ARB, "Rear ARB")
        TipArb.Text = "防倾杆: " & r.ARB.Tip

        ' Damping
        OutReboundF.Text = GetValByKey(r.Damping, "Front Rebound")
        OutReboundR.Text = GetValByKey(r.Damping, "Rear Rebound")
        OutBumpF.Text = GetValByKey(r.Damping, "Front Bump")
        OutBumpR.Text = GetValByKey(r.Damping, "Rear Bump")
        TipDamping.Text = "阻尼系统: " & r.Damping.Tip

        ' Braking & Diff
        OutBrakeBal.Text = GetValByKey(r.Braking, "Brake Balance")
        OutBrakePress.Text = GetValByKey(r.Braking, "Brake Pressure")
        OutBrakeTrail.Text = GetValByKey(r.Braking, "Trail Brake Rating")
        TipBraking.Text = "刹车技巧: " & r.Braking.Tip

        OutDiffF.Text = GetValByKey(r.Diff, "Front Accel") & " / " & GetValByKey(r.Diff, "Front Decel")
        OutDiffR.Text = GetValByKey(r.Diff, "Rear Accel") & " / " & GetValByKey(r.Diff, "Rear Decel")
        
        Dim centerVal = GetValByKey(r.Diff, "Center Balance")
        If String.IsNullOrEmpty(centerVal) Then
            RowDiffC.Visibility = Visibility.Collapsed
        Else
            RowDiffC.Visibility = Visibility.Visible
            OutDiffC.Text = centerVal
        End If
        TipDiff.Text = "差速器锁: " & r.Diff.Tip

        ' Gearing (Conditional)
        If r.Gearing IsNot Nothing Then
            CardGearingResult.Visibility = Visibility.Visible
            PanelGearingList.Children.Clear()
            For Each item In r.Gearing.Values
                Dim rowGrid As New Grid With {.Margin = New Thickness(0, 0, 0, 6)}
                rowGrid.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(150)})
                rowGrid.ColumnDefinitions.Add(New ColumnDefinition With {.Width = New GridLength(1, GridUnitType.Star)})
                
                Dim lbl As New TextBlock With {
                    .Text = item.Key,
                    .VerticalAlignment = VerticalAlignment.Center,
                    .Foreground = CType(Application.Current.FindResource("ColorBrush2"), Brush)
                }
                Grid.SetColumn(lbl, 0)
                
                Dim txt As New MyTextBox With {
                    .Text = item.Value,
                    .Height = 34,
                    .Tag = item.Key
                }
                Grid.SetColumn(txt, 1)
                
                rowGrid.Children.Add(lbl)
                rowGrid.Children.Add(txt)
                
                PanelGearingList.Children.Add(rowGrid)
            Next
            TipGearing.Text = "传动比调整: " & r.Gearing.Tip
        Else
            CardGearingResult.Visibility = Visibility.Collapsed
        End If

        ' Aero (Conditional)
        If r.Aero IsNot Nothing Then
            CardAeroResult.Visibility = Visibility.Visible
            OutAeroF.Text = GetValByKey(r.Aero, "Front Downforce")
            OutAeroR.Text = GetValByKey(r.Aero, "Rear Downforce")
            OutAeroBalance.Text = GetValByKey(r.Aero, "Aero Balance")
            TipAero.Text = "空力说明: " & r.Aero.Tip
        Else
            CardAeroResult.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub UpdateValByKey(cat As TuningCategory, key As String, val As String)
        If cat Is Nothing OrElse cat.Values Is Nothing Then Return
        Dim item = cat.Values.FirstOrDefault(Function(i) i.Key = key)
        If item IsNot Nothing Then
            item.Value = val
        Else
            cat.Values.Add(New TuningItem With {.Key = key, .Value = val})
        End If
    End Sub

    Private Sub UpdateDiffFromUI(cat As TuningCategory, text As String, accelKey As String, decelKey As String)
        If cat Is Nothing OrElse String.IsNullOrEmpty(text) Then Return
        Dim parts = text.Split("/"c)
        If parts.Length >= 1 Then
            UpdateValByKey(cat, accelKey, parts(0).Trim())
        End If
        If parts.Length >= 2 Then
            UpdateValByKey(cat, decelKey, parts(1).Trim())
        End If
    End Sub

    Private Sub UpdateCurrentResultFromUI()
        If CurrentResult Is Nothing Then Return

        ' Tires
        UpdateValByKey(CurrentResult.Tires, "Front Pressure", OutTirePressF.Text)
        UpdateValByKey(CurrentResult.Tires, "Rear Pressure", OutTirePressR.Text)

        ' Alignment
        UpdateValByKey(CurrentResult.Alignment, "Front Camber", OutCamberF.Text)
        UpdateValByKey(CurrentResult.Alignment, "Rear Camber", OutCamberR.Text)
        UpdateValByKey(CurrentResult.Alignment, "Front Toe", OutToeF.Text)
        UpdateValByKey(CurrentResult.Alignment, "Rear Toe", OutToeR.Text)
        UpdateValByKey(CurrentResult.Alignment, "Front Caster", OutCaster.Text)

        ' Suspension
        UpdateValByKey(CurrentResult.Suspension, "Front Spring", OutSpringF.Text)
        UpdateValByKey(CurrentResult.Suspension, "Rear Spring", OutSpringR.Text)
        UpdateValByKey(CurrentResult.Suspension, "Front Ride Height", OutRideF.Text)
        UpdateValByKey(CurrentResult.Suspension, "Rear Ride Height", OutRideR.Text)

        ' ARB
        UpdateValByKey(CurrentResult.ARB, "Front ARB", OutArbF.Text)
        UpdateValByKey(CurrentResult.ARB, "Rear ARB", OutArbR.Text)

        ' Damping
        UpdateValByKey(CurrentResult.Damping, "Front Rebound", OutReboundF.Text)
        UpdateValByKey(CurrentResult.Damping, "Rear Rebound", OutReboundR.Text)
        UpdateValByKey(CurrentResult.Damping, "Front Bump", OutBumpF.Text)
        UpdateValByKey(CurrentResult.Damping, "Rear Bump", OutBumpR.Text)

        ' Braking
        UpdateValByKey(CurrentResult.Braking, "Brake Balance", OutBrakeBal.Text)
        UpdateValByKey(CurrentResult.Braking, "Brake Pressure", OutBrakePress.Text)
        UpdateValByKey(CurrentResult.Braking, "Trail Brake Rating", OutBrakeTrail.Text)

        ' Diff
        UpdateDiffFromUI(CurrentResult.Diff, OutDiffF.Text, "Front Accel", "Front Decel")
        UpdateDiffFromUI(CurrentResult.Diff, OutDiffR.Text, "Rear Accel", "Rear Decel")
        If RowDiffC.Visibility = Visibility.Visible Then
            UpdateValByKey(CurrentResult.Diff, "Center Balance", OutDiffC.Text)
        End If

        ' Aero
        If CurrentResult.Aero IsNot Nothing Then
            UpdateValByKey(CurrentResult.Aero, "Front Downforce", OutAeroF.Text)
            UpdateValByKey(CurrentResult.Aero, "Rear Downforce", OutAeroR.Text)
            UpdateValByKey(CurrentResult.Aero, "Aero Balance", OutAeroBalance.Text)
        End If

        ' Gearing
        If CurrentResult.Gearing IsNot Nothing Then
            For Each child As Object In PanelGearingList.Children
                If TypeOf child Is Grid Then
                    Dim grid = CType(child, Grid)
                    Dim txt = grid.Children.OfType(Of MyTextBox)().FirstOrDefault()
                    If txt IsNot Nothing AndAlso txt.Tag IsNot Nothing Then
                        Dim gearKey = txt.Tag.ToString()
                        UpdateValByKey(CurrentResult.Gearing, gearKey, txt.Text.Trim())
                    End If
                End If
            Next
        End If
    End Sub

    Private Function GetValByKey(cat As TuningCategory, key As String) As String
        Dim item = cat?.Values?.FirstOrDefault(Function(i) i.Key = key)
        Return If(item?.Value, "")
    End Function

    ' AI Enhancement Async call
    Private Async Sub BtnAIEnhance_Click(sender As Object, e As MouseButtonEventArgs)
        Dim provider = Settings.Get(Of String)("AiProvider", "Gemini")
        Dim apiKey = Settings.Get(Of String)("AiApiKey", "")
        Dim model = Settings.Get(Of String)("AiModel", "3.1-flash")
        Dim customUrl = Settings.Get(Of String)("AiCustomUrl", "")

        If String.IsNullOrWhiteSpace(apiKey) Then
            FrmMain.PageChange(UiKitDemoPage.Settings)
            Hint("请先在设置页中配置您的 AI 智能接口配置。", HintType.Blue)
            Return
        End If

        Dim nav = FrmMain.PageNav

        ' Lock UI and show spinner
        nav.BtnAIEnhance.IsEnabled = False
        nav.LoadAI.Run()
        
        If nav.PanChatList.Children.Contains(nav.LabAISummary) Then
            nav.LabAISummary.Text = "AI 正在对当前车辆规格及调校进行分析，请稍候..."
        Else
            nav.PanChatList.Children.Clear()
            Dim placeholderTextBlock As New TextBlock With {
                .Text = "AI 正在对当前车辆规格及调校进行分析，请稍候...",
                .TextWrapping = TextWrapping.Wrap,
                .FontSize = 12.5,
                .LineHeight = 1.5
            }
            placeholderTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray3")
            nav.PanChatList.Children.Add(placeholderTextBlock)
        End If

        Try
            ' Construct the current tuning state
            Dim s As New TuningState()
            s.TuneId = ActiveMode
            s.DriveType = If(ComboDriveType.SelectedItem?.ToString(), "AWD")
            s.CarClass = If(ComboClass.SelectedItem?.ToString(), "A")
            s.Compound = If(ComboCompound.SelectedItem?.ToString(), "Street")
            s.DragDist = If(ComboDragDist.SelectedItem?.ToString(), "quarter")
            s.WeightUnit = Settings.Get(Of String)("UnitWeight", "lbs")
            s.SpeedUnit = Settings.Get(Of String)("UnitSpeed", "mph")
            s.PressureUnit = Settings.Get(Of String)("UnitPressure", "psi")
            s.SpringsUnit = Settings.Get(Of String)("UnitSprings", "lbs/in")
            Double.TryParse(TxtWeight.Text, s.Weight)
            Double.TryParse(TxtWeightDist.Text, s.WeightDist)
            Integer.TryParse(TxtPi.Text, s.Pi)
            s.TireWF = TxtTireWF.Text
            s.TireWR = TxtTireWR.Text
            s.FeelBalance = SliderBalance.Value
            s.FeelAggression = SliderAggression.Value
            s.IncludeGearing = ChkGearing.Checked
            If s.IncludeGearing Then
                Double.TryParse(TxtRedlineRpm.Text, s.RedlineRpm)
                Double.TryParse(TxtPeakRpm.Text, s.PeakTorqueRpm)
                Double.TryParse(TxtMaxTorque.Text, s.MaxTorque)
                Double.TryParse(TxtTopspeed.Text, s.Topspeed)
                Integer.TryParse(TxtGears.Text, s.Gears)
            End If
            s.HasAero = ChkAero.Checked
            If s.HasAero Then
                Double.TryParse(TxtAeroF.Text, s.AeroF)
                Double.TryParse(TxtAeroR.Text, s.AeroR)
                Double.TryParse(TxtDragCd.Text, s.DragCd)
            End If

            ' Fetch enhancement from AI Client asynchronously
            Dim jsonText = Await Task.Run(Function()
                Dim client = AiClientFactory.GetClient(provider)
                Return client.EnhanceTuneAsync(apiKey, s, CurrentResult, model, customUrl)
            End Function)
            
            ' Parse response JSON
            Dim trimmed = jsonText.Trim()
            If trimmed.StartsWith("<") Then
                Throw New Exception("服务器返回了非 JSON 格式的 HTML 响应（通常是网络代理错误或认证拦截页面）。请检查您的 API 地址或网络配置。")
            End If

            Try
                Using doc = JsonDocument.Parse(trimmed)
                    Dim root = doc.RootElement
                    Dim summary = root.GetProperty("summary").GetString()
                    
                    ' Build a detailed report for the chat window
                    Dim reportBuilder As New System.Text.StringBuilder()
                    reportBuilder.AppendLine("✦ AI 智能诊断调优报告 ✦")
                    reportBuilder.AppendLine()
                    reportBuilder.AppendLine("【综合诊断】")
                    reportBuilder.AppendLine(summary)
                    reportBuilder.AppendLine()
                    
                    Dim notesObj As JsonElement = Nothing
                    If root.TryGetProperty("notes", notesObj) Then
                        Dim hasNotes As Boolean = False
                        Dim notesText As New System.Text.StringBuilder()
                        notesText.AppendLine("【具体参数微调建议】")
                        For Each prop In notesObj.EnumerateObject()
                            Dim noteVal = prop.Value.GetString()
                            If Not String.IsNullOrEmpty(noteVal) Then
                                hasNotes = True
                                notesText.AppendLine($"• {TranslateSectionKey(prop.Name)}: {noteVal}")
                            End If
                        Next
                        If hasNotes Then
                            reportBuilder.Append(notesText.ToString())
                            reportBuilder.AppendLine()
                        End If
                    End If

                    Dim tipsObj As JsonElement = Nothing
                    If root.TryGetProperty("tips", tipsObj) Then
                        Dim hasTips As Boolean = False
                        Dim tipsText As New System.Text.StringBuilder()
                        tipsText.AppendLine("【调校小贴士】")
                        For Each prop In tipsObj.EnumerateObject()
                            Dim tipVal = prop.Value.GetString()
                            If Not String.IsNullOrEmpty(tipVal) Then
                                hasTips = True
                                tipsText.AppendLine($"• {TranslateSectionName(prop.Name)}: {tipVal}")
                            End If
                        Next
                        If hasTips Then
                            reportBuilder.Append(tipsText.ToString())
                        End If
                    End If
                    
                    ' Initialize chat conversation in left panel
                    nav.PanChatList.Children.Clear()
                    nav.ChatHistory.Clear()
                    
                    ' Add initial report bubble
                    Dim firstReport = reportBuilder.ToString().Trim()
                    nav.AddChatBubble("assistant", firstReport)
                    nav.ChatHistory.Add(New ChatMessage() With {.Role = "assistant", .Content = firstReport})
                    
                    ' Show input bar
                    nav.GridChatInput.Visibility = Visibility.Visible
                    
                    ' Clean up any old AI notes in the input boxes
                    If notesObj.ValueKind <> JsonValueKind.Undefined Then
                        ApplyAiNotes(notesObj)
                    End If
                    
                    ' Apply tips to section tip labels
                    If tipsObj.ValueKind <> JsonValueKind.Undefined Then
                        ApplyAiTips(tipsObj)
                    End If
                End Using
            Catch ex As JsonException
                Throw New Exception("无法解析 AI 返回的 JSON 调配数据。这可能是因为所选的 AI 模型响应格式不符合规范。原始响应为: " & If(trimmed.Length > 200, trimmed.Substring(0, 200) & "...", trimmed))
            End Try

            Hint("AI 增强调校完成！已在右侧输出卡片中添加具体微调备注。", HintType.Green)

        Catch ex As Exception
            nav.PanChatList.Children.Clear()
            Dim errorTextBlock As New TextBlock With {
                .Text = $"AI 增强失败。原因：{ex.Message}",
                .TextWrapping = TextWrapping.Wrap,
                .FontSize = 12.5,
                .LineHeight = 1.5
            }
            errorTextBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushRedLight")
            nav.PanChatList.Children.Add(errorTextBlock)
            Hint("AI 增强出错，请检查 API 密钥或网络连接。", HintType.Red)
        Finally
            nav.LoadAI.Stop()
            nav.BtnAIEnhance.IsEnabled = True
        End Try
    End Sub

    Private Sub ApplyAiNotes(notesObj As JsonElement)
        ' Helper method to clean up any previous AI note from the input box text
        Dim cleanNote = Sub(item As MyTextBox)
            If item Is Nothing OrElse String.IsNullOrEmpty(item.Text) Then Return
            
            Dim baseValue = item.Text
            Dim idx = baseValue.IndexOf(" (AI:")
            If idx <> -1 Then
                baseValue = baseValue.Substring(0, idx).Trim()
                item.Text = baseValue
            End If
            
            Dim idx2 = baseValue.IndexOf(" (")
            If idx2 <> -1 Then
                baseValue = baseValue.Substring(0, idx2).Trim()
                item.Text = baseValue
            End If
        End Sub

        cleanNote(OutTirePressF)
        cleanNote(OutTirePressR)
        cleanNote(OutCamberF)
        cleanNote(OutCamberR)
        cleanNote(OutToeF)
        cleanNote(OutToeR)
        cleanNote(OutCaster)
        cleanNote(OutSpringF)
        cleanNote(OutSpringR)
        cleanNote(OutRideF)
        cleanNote(OutRideR)
        cleanNote(OutArbF)
        cleanNote(OutArbR)
        cleanNote(OutReboundF)
        cleanNote(OutReboundR)
        cleanNote(OutBumpF)
        cleanNote(OutBumpR)
        cleanNote(OutBrakeBal)
        cleanNote(OutBrakePress)
    End Sub

    ' Apply AI tips to sections
    Private Sub ApplyAiTips(tipsObj As JsonElement)
        Dim applyTip = Sub(txtBlock As TextBlock, sectionName As String, baseTip As String)
            If txtBlock Is Nothing Then Return
            Dim tipVal As JsonElement = Nothing
            If tipsObj.TryGetProperty(sectionName, tipVal) Then
                Dim tip = tipVal.GetString()
                If Not String.IsNullOrEmpty(tip) Then
                    txtBlock.Text = "✦ AI建议: " & tip
                End If
            Else
                txtBlock.Text = baseTip
            End If
        End Sub

        applyTip(TipTires, "Tires", "轮胎: " & CurrentResult.Tires.Tip)
        applyTip(TipAlignment, "Alignment", "定位: " & CurrentResult.Alignment.Tip)
        applyTip(TipSpring, "Suspension", "弹簧: " & CurrentResult.Suspension.Tip)
        applyTip(TipArb, "ARB", "防倾杆: " & CurrentResult.ARB.Tip)
        applyTip(TipDamping, "Damping", "阻尼: " & CurrentResult.Damping.Tip)
        applyTip(TipBraking, "Braking", "制动: " & CurrentResult.Braking.Tip)
        applyTip(TipDiff, "Diff", "差速器: " & CurrentResult.Diff.Tip)
    End Sub

    Private Function TranslateSectionKey(key As String) As String
        Select Case key
            Case "Tires/Front Pressure" : Return "轮胎/前气压"
            Case "Tires/Rear Pressure" : Return "轮胎/后气压"
            Case "Alignment/Front Camber" : Return "定位/前外倾角"
            Case "Alignment/Rear Camber" : Return "定位/后外倾角"
            Case "Alignment/Front Toe" : Return "定位/前前束"
            Case "Alignment/Rear Toe" : Return "定位/后前束"
            Case "Alignment/Front Caster" : Return "定位/前倾角"
            Case "Suspension/Front Spring" : Return "悬挂/前弹簧硬度"
            Case "Suspension/Rear Spring" : Return "悬挂/后弹簧硬度"
            Case "Suspension/Front Ride Height" : Return "悬挂/前车身高度"
            Case "Suspension/Rear Ride Height" : Return "悬挂/后车身高度"
            Case "ARB/Front ARB" : Return "防倾杆/前防倾杆"
            Case "ARB/Rear ARB" : Return "防倾杆/后防倾杆"
            Case "Damping/Front Rebound" : Return "阻尼/前回弹阻尼"
            Case "Damping/Rear Rebound" : Return "阻尼/后回弹阻尼"
            Case "Damping/Front Bump" : Return "阻尼/前压缩阻尼"
            Case "Damping/Rear Bump" : Return "阻尼/后压缩阻尼"
            Case "Braking/Brake Balance" : Return "制动/刹车平衡"
            Case "Braking/Brake Pressure" : Return "制动/刹车压力"
            Case "Gearing/Final Drive" : Return "齿轮比/主减速比"
            Case Else
                If key.StartsWith("Gearing/") Then
                    Return "齿轮比/" & key.Substring(8)
                End If
                Return key
        End Select
    End Function

    Private Function TranslateSectionName(section As String) As String
        Select Case section
            Case "Tires" : Return "轮胎"
            Case "Alignment" : Return "定位"
            Case "Suspension" : Return "悬挂"
            Case "ARB" : Return "防倾杆"
            Case "Damping" : Return "阻尼"
            Case "Braking" : Return "制动"
            Case "Diff" : Return "差速器"
            Case "Aero" : Return "气动"
            Case "Gearing" : Return "齿轮比"
            Case Else : Return section
        End Select
    End Function

    Public Function GetTelemetryContext() As TelemetrySessionContext
        Dim tuneName = TxtTuneName.Text.Trim()
        Dim carText As String = If(ComboCar.SelectedItem IsNot Nothing, ComboCar.SelectedItem.ToString(), "")
        Dim saved = If(String.IsNullOrWhiteSpace(tuneName),
            Nothing,
            SavedTunesDatabase.Tunes.FirstOrDefault(Function(t) t.Name.Equals(tuneName, StringComparison.OrdinalIgnoreCase)))

        Dim tuneId As String
        If saved IsNot Nothing Then
            tuneId = saved.Id
        ElseIf Not String.IsNullOrWhiteSpace(tuneName) Then
            tuneId = "draft:" & tuneName
        Else
            tuneId = "draft:" & ActiveMode
            tuneName = "未命名调校"
        End If

        Return New TelemetrySessionContext() With {
            .TuneId = tuneId,
            .TuneName = tuneName,
            .CarName = carText,
            .Source = "Tuner"
        }
    End Function

End Class

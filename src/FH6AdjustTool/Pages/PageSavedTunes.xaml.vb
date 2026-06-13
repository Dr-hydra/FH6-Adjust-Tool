Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Media
Imports System.Collections.Generic
Imports QING.Core

Public Class PageSavedTunes
    Public Sub PageSavedTunes_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        RefreshTunesList()
        AddHandler BtnImport.Click, AddressOf OnImportClicked
    End Sub

    Private Sub OnImportClicked(sender As Object, e As RoutedEventArgs)
        Dim code As String = TxtImportCode.Text.Trim()
        If String.IsNullOrEmpty(code) Then
            Hint("请先粘贴分享码", HintType.Blue)
            Return
        End If
        Dim tune As SavedTune = Nothing
        If Not ShareCodec.TryDecode(code, tune) OrElse tune Is Nothing Then
            Hint("分享码无效或版本不兼容，请确认来源", HintType.Red)
            Return
        End If
        ' 避免重名时自动加后缀
        Dim baseName As String = tune.Name
        If SavedTunesDatabase.Tunes.Any(Function(t) t.Name = baseName) Then
            tune.Name = baseName & " (导入)"
        End If
        SavedTunesDatabase.SaveTune(tune)
        TxtImportCode.Text = ""
        Hint("已导入调校 "" & tune.Name & """, HintType.Green)
        RefreshTunesList()
    End Sub

    Public Sub RefreshTunesList()
        PanelTunesList.Children.Clear()
        Dim tunes = SavedTunesDatabase.Tunes
        If tunes Is Nothing OrElse tunes.Count = 0 Then
            LabNoTunes.Visibility = Visibility.Visible
            Return
        End If

        LabNoTunes.Visibility = Visibility.Collapsed

        ' Loop saved tunes (newest first)
        For Each tune In tunes.OrderByDescending(Function(t) t.SavedAt).ToList()
            ' 确保 Result 不为空，如果为空或者没有 Tires 记录，则根据 State 重新计算并保存以做兼容
            If tune.Result Is Nothing OrElse tune.Result.Tires Is Nothing OrElse tune.Result.Tires.Values.Count = 0 Then
                Try
                    tune.Result = TuningCalculator.Calculate(tune.State)
                    SavedTunesDatabase.SaveTune(tune)
                Catch ex As Exception
                    ' 忽略计算异常
                End Try
            End If

            ' Each tune is placed in an expandable MyCard
            Dim rowCard As New MyCard() With {
                .Title = tune.Name,
                .CanSwap = True,
                .IsSwapped = True,
                .Margin = New Thickness(0, 0, 0, 15)
            }

            ' The card body content container
            Dim bodyPanel As New StackPanel() With {
                .Margin = New Thickness(20, 40, 20, 18)
            }

            ' 1. Specs Title
            Dim specsTitle As New TextBlock() With {
                .Text = "🚗 车辆规格与参数配置 (不可编辑)",
                .FontWeight = FontWeights.SemiBold,
                .FontSize = 13,
                .Margin = New Thickness(0, 0, 0, 8)
            }
            specsTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
            bodyPanel.Children.Add(specsTitle)

            ' Specs Grid (UniformGrid with 3 columns)
            Dim specsGrid As New UniformGrid() With {
                .Columns = 3,
                .Margin = New Thickness(0, 0, 0, 12)
            }
            specsGrid.Children.Add(CreateReadOnlySpec("车辆型号", tune.SelectedCarText))
            specsGrid.Children.Add(CreateReadOnlySpec("车重与配重", $"{tune.State.Weight} {tune.State.WeightUnit} ({tune.State.WeightDist}% 前)"))
            specsGrid.Children.Add(CreateReadOnlySpec("性能等级", $"{tune.State.CarClass} {tune.State.Pi}"))
            specsGrid.Children.Add(CreateReadOnlySpec("驱动形式", tune.State.DriveType))
            specsGrid.Children.Add(CreateReadOnlySpec("轮胎配方", tune.State.Compound))
            specsGrid.Children.Add(CreateReadOnlySpec("调校模式", tune.State.TuneId))
            bodyPanel.Children.Add(specsGrid)

            ' Gearing and Aero quick read-only indicator
            If tune.State.IncludeGearing OrElse tune.State.HasAero Then
                Dim quickGrid As New UniformGrid() With {
                    .Columns = 3,
                    .Margin = New Thickness(0, 0, 0, 12)
                }
                If tune.State.IncludeGearing Then
                    quickGrid.Children.Add(CreateReadOnlySpec("变速箱齿轮比", $"已启用 ({tune.State.Gears} 挡)"))
                End If
                If tune.State.HasAero Then
                    quickGrid.Children.Add(CreateReadOnlySpec("空气动力学下压力", $"前: {tune.State.AeroF} / 后: {tune.State.AeroR}"))
                End If
                bodyPanel.Children.Add(quickGrid)
            End If

            ' Divider
            Dim divider As New Border() With {
                .Height = 1,
                .Margin = New Thickness(0, 4, 0, 12)
            }
            divider.SetResourceReference(Border.BackgroundProperty, "ColorBrushBorder")
            bodyPanel.Children.Add(divider)

            ' 2. Results Title
            Dim resultsTitle As New TextBlock() With {
                .Text = "🛠️ 调校结果数值微调 (可手动输入修改)",
                .FontWeight = FontWeights.SemiBold,
                .FontSize = 13,
                .Margin = New Thickness(0, 0, 0, 12)
            }
            resultsTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
            bodyPanel.Children.Add(resultsTitle)

            ' Results editable text field mapping
            Dim txtMap As New Dictionary(Of String, MyTextBox)()
            Dim createField = Function(catName As String, keyName As String, defaultValue As String) As MyTextBox
                                  Dim tb As New MyTextBox() With {.Text = defaultValue}
                                  txtMap.Add($"{catName}/{keyName}", tb)
                                  Return tb
                              End Function

            ' 2-Column Results Grid
            Dim resultsGrid As New Grid()
            resultsGrid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})
            resultsGrid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(28)})
            resultsGrid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})

            Dim leftPanel As New StackPanel()
            Grid.SetColumn(leftPanel, 0)
            Dim rightPanel As New StackPanel()
            Grid.SetColumn(rightPanel, 2)

            ' --- Left Panel: Tires, Alignment, ARB, Brakes, Aero ---
            ' Tires
            Dim tiresTitle As New TextBlock() With {.Text = "🔘 轮胎与气压", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 0, 0, 6)}
            tiresTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            leftPanel.Children.Add(tiresTitle)
            leftPanel.Children.Add(CreateEditableField("前胎气压", createField("Tires", "Front Pressure", GetResultVal(tune.Result.Tires, "Front Pressure"))))
            leftPanel.Children.Add(CreateEditableField("后胎气压", createField("Tires", "Rear Pressure", GetResultVal(tune.Result.Tires, "Rear Pressure"))))

            ' Alignment
            Dim alignmentTitle As New TextBlock() With {.Text = "📐 悬挂定位角", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
            alignmentTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            leftPanel.Children.Add(alignmentTitle)
            leftPanel.Children.Add(CreateEditableField("前外倾角 (Camber)", createField("Alignment", "Front Camber", GetResultVal(tune.Result.Alignment, "Front Camber"))))
            leftPanel.Children.Add(CreateEditableField("后外倾角 (Camber)", createField("Alignment", "Rear Camber", GetResultVal(tune.Result.Alignment, "Rear Camber"))))
            leftPanel.Children.Add(CreateEditableField("前束角 (Toe)", createField("Alignment", "Front Toe", GetResultVal(tune.Result.Alignment, "Front Toe"))))
            leftPanel.Children.Add(CreateEditableField("后束角 (Toe)", createField("Alignment", "Rear Toe", GetResultVal(tune.Result.Alignment, "Rear Toe"))))
            leftPanel.Children.Add(CreateEditableField("主销后倾 (Caster)", createField("Alignment", "Front Caster", GetResultVal(tune.Result.Alignment, "Front Caster"))))

            ' ARBs
            Dim arbTitle As New TextBlock() With {.Text = "📏 防倾杆", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
            arbTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            leftPanel.Children.Add(arbTitle)
            leftPanel.Children.Add(CreateEditableField("前防倾杆", createField("ARB", "Front ARB", GetResultVal(tune.Result.ARB, "Front ARB"))))
            leftPanel.Children.Add(CreateEditableField("后防倾杆", createField("ARB", "Rear ARB", GetResultVal(tune.Result.ARB, "Rear ARB"))))

            ' Brakes
            Dim brakesTitle As New TextBlock() With {.Text = "🛑 制动系统", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
            brakesTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            leftPanel.Children.Add(brakesTitle)
            leftPanel.Children.Add(CreateEditableField("刹车平衡", createField("Braking", "Brake Balance", GetResultVal(tune.Result.Braking, "Brake Balance"))))
            leftPanel.Children.Add(CreateEditableField("刹车压力", createField("Braking", "Brake Pressure", GetResultVal(tune.Result.Braking, "Brake Pressure"))))

            ' Aero
            If tune.State.HasAero AndAlso tune.Result.Aero IsNot Nothing Then
                Dim aeroTitle As New TextBlock() With {.Text = "🍃 空气动力学", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
                aeroTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
                leftPanel.Children.Add(aeroTitle)
                leftPanel.Children.Add(CreateEditableField("前下压力", createField("Aero", "Front Downforce", GetResultVal(tune.Result.Aero, "Front Downforce"))))
                leftPanel.Children.Add(CreateEditableField("后下压力", createField("Aero", "Rear Downforce", GetResultVal(tune.Result.Aero, "Rear Downforce"))))
            End If

            ' --- Right Panel: Springs, Damping, Diff, Gearing ---
            ' Springs
            Dim springsTitle As New TextBlock() With {.Text = "🌀 弹簧与高度", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 0, 0, 6)}
            springsTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            rightPanel.Children.Add(springsTitle)
            rightPanel.Children.Add(CreateEditableField("前悬弹簧", createField("Suspension", "Front Spring", GetResultVal(tune.Result.Suspension, "Front Spring"))))
            rightPanel.Children.Add(CreateEditableField("后悬弹簧", createField("Suspension", "Rear Spring", GetResultVal(tune.Result.Suspension, "Rear Spring"))))
            rightPanel.Children.Add(CreateEditableField("前车身高度", createField("Suspension", "Front Ride Height", GetResultVal(tune.Result.Suspension, "Front Ride Height"))))
            rightPanel.Children.Add(CreateEditableField("后车身高度", createField("Suspension", "Rear Ride Height", GetResultVal(tune.Result.Suspension, "Rear Ride Height"))))

            ' Damping
            Dim dampingTitle As New TextBlock() With {.Text = "⚡ 悬挂阻尼", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
            dampingTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            rightPanel.Children.Add(dampingTitle)
            rightPanel.Children.Add(CreateEditableField("前轮阻尼回弹", createField("Damping", "Front Rebound", GetResultVal(tune.Result.Damping, "Front Rebound"))))
            rightPanel.Children.Add(CreateEditableField("后轮阻尼回弹", createField("Damping", "Rear Rebound", GetResultVal(tune.Result.Damping, "Rear Rebound"))))
            rightPanel.Children.Add(CreateEditableField("前轮阻尼收缩", createField("Damping", "Front Bump", GetResultVal(tune.Result.Damping, "Front Bump"))))
            rightPanel.Children.Add(CreateEditableField("后轮阻尼收缩", createField("Damping", "Rear Bump", GetResultVal(tune.Result.Damping, "Rear Bump"))))

            ' Diff
            Dim diffTitle As New TextBlock() With {.Text = "🔒 差速器锁", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
            diffTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
            rightPanel.Children.Add(diffTitle)
            rightPanel.Children.Add(CreateEditableField("前加速锁", createField("Diff", "Front Accel", GetResultVal(tune.Result.Diff, "Front Accel"))))
            rightPanel.Children.Add(CreateEditableField("前减速锁", createField("Diff", "Front Decel", GetResultVal(tune.Result.Diff, "Front Decel"))))
            rightPanel.Children.Add(CreateEditableField("后加速锁", createField("Diff", "Rear Accel", GetResultVal(tune.Result.Diff, "Rear Accel"))))
            rightPanel.Children.Add(CreateEditableField("后减速锁", createField("Diff", "Rear Decel", GetResultVal(tune.Result.Diff, "Rear Decel"))))
            If tune.State.DriveType = "AWD" Then
                rightPanel.Children.Add(CreateEditableField("中控扭矩分配", createField("Diff", "Center Balance", GetResultVal(tune.Result.Diff, "Center Balance"))))
            End If

            ' Gearing
            If tune.State.IncludeGearing AndAlso tune.Result.Gearing IsNot Nothing Then
                Dim gearingTitle As New TextBlock() With {.Text = "⚙️ 变速箱齿轮比", .FontWeight = FontWeights.Bold, .Margin = New Thickness(0, 8, 0, 6)}
                gearingTitle.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
                rightPanel.Children.Add(gearingTitle)
                For Each item In tune.Result.Gearing.Values
                    rightPanel.Children.Add(CreateEditableField(item.Key, createField("Gearing", item.Key, item.Value)))
                Next
            End If

            resultsGrid.Children.Add(leftPanel)
            resultsGrid.Children.Add(rightPanel)
            bodyPanel.Children.Add(resultsGrid)

            ' 3. Action divider
            Dim actionDivider As New Border() With {
                .Height = 1,
                .Margin = New Thickness(0, 15, 0, 15)
            }
            actionDivider.SetResourceReference(Border.BackgroundProperty, "ColorBrushBorder")
            bodyPanel.Children.Add(actionDivider)

            ' Naming TextBox to allow renaming the tune directly inside the card
            Dim renamePanel As New Grid() With {.Margin = New Thickness(0, 0, 0, 15)}
            renamePanel.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(140)})
            renamePanel.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})
            Dim nameLabel As New TextBlock() With {
                .Text = "📝 方案命名修改",
                .FontSize = 12,
                .FontWeight = FontWeights.Bold,
                .VerticalAlignment = VerticalAlignment.Center
            }
            nameLabel.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush2")
            Grid.SetColumn(nameLabel, 0)
            renamePanel.Children.Add(nameLabel)

            Dim txtNameEdit As New MyTextBox() With {.Text = tune.Name, .Height = 32}
            Grid.SetColumn(txtNameEdit, 1)
            renamePanel.Children.Add(txtNameEdit)
            bodyPanel.Children.Add(renamePanel)

            ' Action buttons panel
            Dim btnPanel As New StackPanel() With {
                .Orientation = Orientation.Horizontal,
                .HorizontalAlignment = HorizontalAlignment.Right
            }

            ' 💾 Save changes (Overwrite)
            Dim btnSave As New MyButton() With {
                .Text = "💾 覆盖保存",
                .Padding = New Thickness(16, 6, 16, 6),
                .Height = 32,
                .ColorType = MyButton.ColorState.Highlight,
                .Margin = New Thickness(0, 0, 8, 0),
                .ToolTip = "保存当前的数值修改并覆盖此调校方案"
            }
            AddHandler btnSave.Click, Sub(s, ev)
                                          Dim newName As String = txtNameEdit.Text.Trim()
                                          If String.IsNullOrEmpty(newName) Then
                                              Hint("方案名称不能为空！", HintType.Red)
                                              Return
                                          End If
                                          tune.Name = newName
                                          UpdateTuneResultsFromFields(tune, txtMap)
                                          SavedTunesDatabase.SaveTune(tune)
                                          Hint("调校 “" & tune.Name & "” 修改保存成功！", HintType.Green)
                                          RefreshTunesList()
                                      End Sub
            btnPanel.Children.Add(btnSave)

            ' ➕ Save As
            Dim btnSaveAs As New MyButton() With {
                .Text = "➕ 另存为",
                .Padding = New Thickness(16, 6, 16, 6),
                .Height = 32,
                .Margin = New Thickness(0, 0, 8, 0),
                .ToolTip = "将当前的数值另存为一个新命名的调校方案"
            }
            AddHandler btnSaveAs.Click, Sub(s, ev)
                                            Dim currentName As String = txtNameEdit.Text.Trim()
                                            Dim newName As String = Microsoft.VisualBasic.Interaction.InputBox("请输入新的调校方案名称：", "另存为新调校", currentName & " - 副本").Trim()
                                            If String.IsNullOrEmpty(newName) Then Return

                                            ' Check duplicate
                                            Dim existing = SavedTunesDatabase.Tunes.FirstOrDefault(Function(t) t.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
                                            If existing IsNot Nothing Then
                                                If MessageBox.Show("已存在名为 “" & newName & "” 的调校。是否要覆盖它？", "确认覆盖", MessageBoxButton.OKCancel, MessageBoxImage.Question) <> MessageBoxResult.OK Then
                                                    Return
                                                End If
                                            End If

                                            Dim newTune As New SavedTune() With {
                                                 .Id = Guid.NewGuid().ToString(),
                                                 .Name = newName,
                                                 .SavedAt = DateTime.Now,
                                                 .CarSearchKeyword = tune.CarSearchKeyword,
                                                 .SelectedCarText = tune.SelectedCarText,
                                                 .State = New TuningState() With {
                                                     .Make = tune.State.Make,
                                                     .Model = tune.State.Model,
                                                     .TuneId = tune.State.TuneId,
                                                     .DriveType = tune.State.DriveType,
                                                     .Surface = tune.State.Surface,
                                                     .InputDevice = tune.State.InputDevice,
                                                     .Weight = tune.State.Weight,
                                                     .WeightDist = tune.State.WeightDist,
                                                     .RedlineRpm = tune.State.RedlineRpm,
                                                     .PeakTorqueRpm = tune.State.PeakTorqueRpm,
                                                     .MaxTorque = tune.State.MaxTorque,
                                                     .Topspeed = tune.State.Topspeed,
                                                     .Gears = tune.State.Gears,
                                                     .TireWF = tune.State.TireWF,
                                                     .TireWR = tune.State.TireWR,
                                                     .Compound = tune.State.Compound,
                                                     .HasAero = tune.State.HasAero,
                                                     .AeroF = tune.State.AeroF,
                                                     .AeroR = tune.State.AeroR,
                                                     .DragCd = tune.State.DragCd,
                                                     .Pi = tune.State.Pi,
                                                     .CarClass = tune.State.CarClass,
                                                     .WeightUnit = tune.State.WeightUnit,
                                                     .SpeedUnit = tune.State.SpeedUnit,
                                                     .PressureUnit = tune.State.PressureUnit,
                                                     .SpringsUnit = tune.State.SpringsUnit,
                                                     .FeelBalance = tune.State.FeelBalance,
                                                     .FeelAggression = tune.State.FeelAggression,
                                                     .IncludeGearing = tune.State.IncludeGearing,
                                                     .DragDist = tune.State.DragDist
                                                 },
                                                 .Result = New TuningResult() With {
                                                     .Tires = CloneCategory(tune.Result.Tires),
                                                     .Alignment = CloneCategory(tune.Result.Alignment),
                                                     .Suspension = CloneCategory(tune.Result.Suspension),
                                                     .ARB = CloneCategory(tune.Result.ARB),
                                                     .Damping = CloneCategory(tune.Result.Damping),
                                                     .Braking = CloneCategory(tune.Result.Braking),
                                                     .Diff = CloneCategory(tune.Result.Diff),
                                                     .Gearing = CloneCategory(tune.Result.Gearing),
                                                     .Aero = CloneCategory(tune.Result.Aero)
                                                 }
                                             }
                                            UpdateTuneResultsFromFields(newTune, txtMap)
                                            SavedTunesDatabase.SaveTune(newTune)
                                            Hint("另存新调校 “" & newName & "” 成功！", HintType.Green)
                                            RefreshTunesList()
                                        End Sub
            btnPanel.Children.Add(btnSaveAs)

            ' 🗑️ Delete
            Dim btnDelete As New MyButton() With {
                .Text = "🗑️ 删除调校",
                .Padding = New Thickness(16, 6, 16, 6),
                .Height = 32,
                .Margin = New Thickness(0, 0, 8, 0),
                .ToolTip = "彻底删除此调校方案"
            }
            AddHandler btnDelete.Click, Sub(s, ev)
                                            If MessageBox.Show("确定要删除调校 “" & tune.Name & "” 吗？此操作无法撤销。", "确认删除", MessageBoxButton.OKCancel, MessageBoxImage.Warning) = MessageBoxResult.OK Then
                                                SavedTunesDatabase.DeleteTune(tune.Id)
                                                RefreshTunesList()
                                            End If
                                        End Sub
            btnPanel.Children.Add(btnDelete)

            ' 📋 Copy share code
            Dim btnShare As New MyButton() With {
                .Text = "📋 分享",
                .Padding = New Thickness(16, 6, 16, 6),
                .Height = 32,
                .Margin = New Thickness(0, 0, 8, 0),
                .ToolTip = "生成分享码并复制到剪贴板，约300-500字符，可发给其他玩家导入"
            }
            Dim localTune = tune
            Dim localTxtMap = txtMap
            AddHandler btnShare.Click, Sub(s, ev)
                                           Try
                                               UpdateTuneResultsFromFields(localTune, localTxtMap)
                                               Dim shareCode As String = ShareCodec.Encode(localTune)
                                               Clipboard.SetText(shareCode)
                                               Hint("分享码已复制（" & shareCode.Length & " 字符）", HintType.Green)
                                           Catch ex As Exception
                                               Hint("生成分享码失败：" & ex.Message, HintType.Red)
                                           End Try
                                       End Sub
            btnPanel.Children.Add(btnShare)

            ' 🚗 Load to main page
            Dim btnLoad As New MyButton() With {
                .Text = "🚗 在主页加载",
                .Padding = New Thickness(16, 6, 16, 6),
                .Height = 32,
                .ToolTip = "将此方案的车辆规格参数加载到主页重新计算"
            }
            AddHandler btnLoad.Click, Sub(s, ev)
                                          If FrmMain IsNot Nothing AndAlso FrmMain.PageTuner IsNot Nothing Then
                                              FrmMain.PageTuner.LoadSavedTune(tune)
                                              FrmMain.PageChange(UiKitDemoPage.Tuner)
                                          End If
                                      End Sub
            btnPanel.Children.Add(btnLoad)

            bodyPanel.Children.Add(btnPanel)

            rowCard.Children.Add(bodyPanel)
            PanelTunesList.Children.Add(rowCard)
        Next
    End Sub

    ' Helper to build read-only spec item
    Private Function CreateReadOnlySpec(label As String, value As String) As UIElement
        Dim panel As New StackPanel() With {.Margin = New Thickness(0, 0, 0, 8)}
        Dim lbl As New TextBlock() With {
            .Text = label,
            .FontSize = 11,
            .Margin = New Thickness(0, 0, 0, 2)
        }
        lbl.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray3")
        Dim val As New TextBlock() With {
            .Text = value,
            .FontSize = 12.5,
            .FontWeight = FontWeights.SemiBold
        }
        val.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush1")
        panel.Children.Add(lbl)
        panel.Children.Add(val)
        Return panel
    End Function

    ' Helper to build editable result row
    Private Function CreateEditableField(label As String, ByRef txtBox As MyTextBox) As UIElement
        Dim grid As New Grid() With {.Margin = New Thickness(0, 0, 0, 8)}
        grid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(140)})
        grid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})

        Dim lbl As New TextBlock() With {
            .Text = label,
            .FontSize = 11.5,
            .VerticalAlignment = VerticalAlignment.Center
        }
        lbl.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
        Grid.SetColumn(lbl, 0)
        grid.Children.Add(lbl)

        txtBox.Height = 28
        txtBox.VerticalAlignment = VerticalAlignment.Center
        Grid.SetColumn(txtBox, 1)
        grid.Children.Add(txtBox)

        Return grid
    End Function

    ' Helper to extract result from TuningCategory
    Private Function GetResultVal(cat As TuningCategory, key As String) As String
        If cat Is Nothing Then Return "--"
        Dim item = cat.Values.FirstOrDefault(Function(i) i.Key = key)
        Return If(item IsNot Nothing, item.Value, "--")
    End Function

    Private Sub SetResultVal(cat As TuningCategory, key As String, val As String)
        If cat Is Nothing Then Return
        Dim item = cat.Values.FirstOrDefault(Function(i) i.Key = key)
        If item IsNot Nothing Then
            item.Value = val
        Else
            cat.Values.Add(New TuningItem With {.Key = key, .Value = val})
        End If
    End Sub

    ' Update results back to SavedTune model from UI input textboxes
    Private Sub UpdateTuneResultsFromFields(tune As SavedTune, txtMap As Dictionary(Of String, MyTextBox))
        Dim readVal = Function(path As String) As String
                          If txtMap.ContainsKey(path) Then
                              Return txtMap(path).Text.Trim()
                          End If
                          Return ""
                      End Function

        ' Tires
        SetResultVal(tune.Result.Tires, "Front Pressure", readVal("Tires/Front Pressure"))
        SetResultVal(tune.Result.Tires, "Rear Pressure", readVal("Tires/Rear Pressure"))

        ' Alignment
        SetResultVal(tune.Result.Alignment, "Front Camber", readVal("Alignment/Front Camber"))
        SetResultVal(tune.Result.Alignment, "Rear Camber", readVal("Alignment/Rear Camber"))
        SetResultVal(tune.Result.Alignment, "Front Toe", readVal("Alignment/Front Toe"))
        SetResultVal(tune.Result.Alignment, "Rear Toe", readVal("Alignment/Rear Toe"))
        SetResultVal(tune.Result.Alignment, "Front Caster", readVal("Alignment/Front Caster"))

        ' Suspension
        SetResultVal(tune.Result.Suspension, "Front Spring", readVal("Suspension/Front Spring"))
        SetResultVal(tune.Result.Suspension, "Rear Spring", readVal("Suspension/Rear Spring"))
        SetResultVal(tune.Result.Suspension, "Front Ride Height", readVal("Suspension/Front Ride Height"))
        SetResultVal(tune.Result.Suspension, "Rear Ride Height", readVal("Suspension/Rear Ride Height"))

        ' ARB
        SetResultVal(tune.Result.ARB, "Front ARB", readVal("ARB/Front ARB"))
        SetResultVal(tune.Result.ARB, "Rear ARB", readVal("ARB/Rear ARB"))

        ' Damping
        SetResultVal(tune.Result.Damping, "Front Rebound", readVal("Damping/Front Rebound"))
        SetResultVal(tune.Result.Damping, "Rear Rebound", readVal("Damping/Rear Rebound"))
        SetResultVal(tune.Result.Damping, "Front Bump", readVal("Damping/Front Bump"))
        SetResultVal(tune.Result.Damping, "Rear Bump", readVal("Damping/Rear Bump"))

        ' Braking
        SetResultVal(tune.Result.Braking, "Brake Balance", readVal("Braking/Brake Balance"))
        SetResultVal(tune.Result.Braking, "Brake Pressure", readVal("Braking/Brake Pressure"))

        ' Diff
        SetResultVal(tune.Result.Diff, "Front Accel", readVal("Diff/Front Accel"))
        SetResultVal(tune.Result.Diff, "Front Decel", readVal("Diff/Front Decel"))
        SetResultVal(tune.Result.Diff, "Rear Accel", readVal("Diff/Rear Accel"))
        SetResultVal(tune.Result.Diff, "Rear Decel", readVal("Diff/Rear Decel"))
        If tune.State.DriveType = "AWD" Then
            SetResultVal(tune.Result.Diff, "Center Balance", readVal("Diff/Center Balance"))
        End If

        ' Gearing
        If tune.State.IncludeGearing AndAlso tune.Result.Gearing IsNot Nothing Then
            For Each item In tune.Result.Gearing.Values
                SetResultVal(tune.Result.Gearing, item.Key, readVal($"Gearing/{item.Key}"))
            Next
        End If

        ' Aero
        If tune.State.HasAero AndAlso tune.Result.Aero IsNot Nothing Then
            SetResultVal(tune.Result.Aero, "Front Downforce", readVal("Aero/Front Downforce"))
            SetResultVal(tune.Result.Aero, "Rear Downforce", readVal("Aero/Rear Downforce"))
        End If
    End Sub

    Private Function CloneCategory(cat As TuningCategory) As TuningCategory
        If cat Is Nothing Then Return Nothing
        Dim newCat As New TuningCategory() With {.Tip = cat.Tip}
        For Each item In cat.Values
            newCat.Values.Add(New TuningItem With {.Key = item.Key, .Value = item.Value})
        Next
        Return newCat
    End Function

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Dim list As New List(Of Tuple(Of String, FrameworkElement))()
        list.Add(New Tuple(Of String, FrameworkElement)("导入分享码", CardImport))
        list.Add(New Tuple(Of String, FrameworkElement)("方案列表", CardList))
        Return list
    End Function
End Class


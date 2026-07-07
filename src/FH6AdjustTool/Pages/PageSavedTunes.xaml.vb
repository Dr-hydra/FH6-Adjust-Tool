Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Controls.Primitives
Imports System.Windows.Documents
Imports System.Windows.Media
Imports System.Collections.Generic
Imports System.Text.Json
Imports QING.Core

Public Class PageSavedTunes
    Private HandlersAttached As Boolean = False
    Private ScannedSaveTuneCandidates As New List(Of SaveTuneImportCandidate)()
    Private BatchDeleteMode As Boolean = False

    Public Sub PageSavedTunes_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        RefreshTunesList()
        UpdateSaveImportPathLabel()
        If Not HandlersAttached Then
            HandlersAttached = True
            AddHandler BtnImport.Click, AddressOf OnImportClicked
            AddHandler BtnScanSaveTunes.Click, AddressOf OnScanSaveTunesClicked
            AddHandler BtnImportSelectedSaveTunes.Click, AddressOf OnImportSelectedSaveTunesClicked
            AddHandler TxtSaveTuneVehicleFilter.ValidatedTextChanged, AddressOf OnSaveTuneVehicleFilterChanged
            AddHandler TxtTuneListVehicleFilter.ValidatedTextChanged, AddressOf OnTuneListVehicleFilterChanged
            AddHandler BtnDeleteFilteredTunes.Click, AddressOf OnDeleteCheckedTunesClicked
            AddHandler BtnCancelBatchDelete.Click, AddressOf OnCancelBatchDeleteClicked
        End If
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
        If SavedTunesDatabase.NameExists(baseName) Then
            tune.Name = baseName & " (导入)"
        End If
        SavedTunesDatabase.SaveTune(tune)
        TxtImportCode.Text = ""
        Hint("已导入调校 “" & tune.Name & "”", HintType.Green)
        RefreshTunesList()
    End Sub

    Private Async Sub OnScanSaveTunesClicked(sender As Object, e As RoutedEventArgs)
        BtnScanSaveTunes.IsEnabled = False
        BtnImportSelectedSaveTunes.IsEnabled = False
        PanelSaveTuneCandidates.Children.Clear()
        LabSaveImportStatus.Text = "正在扫描存档调校..."
        UpdateSaveImportPathLabel()

        Try
            Dim configured = Settings.Get(Of String)("GameSavePath", "")
            Dim results = Await Task.Run(Function() SaveTuneImportService.Scan(configured, 300).ToList())
            ScannedSaveTuneCandidates = results
            RenderSaveTuneCandidates()
            LabSaveImportStatus.Text = If(results.Count = 0,
                "没有找到可导入的 Tuning_* 调校容器。请在设置中确认存档目录，或选择 ContainersRoot 所在目录。",
                $"找到 {results.Count} 个可导入调校，默认未勾选。")
        Catch ex As Exception
            LabSaveImportStatus.Text = "扫描失败：" & ex.Message
            Hint("扫描存档调校失败：" & ex.Message, HintType.Red)
        Finally
            BtnScanSaveTunes.IsEnabled = True
            BtnImportSelectedSaveTunes.IsEnabled = True
        End Try
    End Sub

    Private Sub OnImportSelectedSaveTunesClicked(sender As Object, e As RoutedEventArgs)
        Dim selected = PanelSaveTuneCandidates.Children.
            OfType(Of FrameworkElement)().
            Select(Function(row) TryCast(row.Tag, Tuple(Of MyCheckBox, SaveTuneImportCandidate))).
            Where(Function(item) item IsNot Nothing AndAlso item.Item1.Checked).
            Select(Function(item) item.Item2).
            ToList()

        If selected.Count = 0 Then
            Hint("请先扫描并勾选要导入的调校。", HintType.Blue)
            Return
        End If

        Dim imported As Integer = 0
        For Each candidate In selected
            Dim tune = candidate.ImportedTune
            tune.Id = Guid.NewGuid().ToString()
            tune.Name = EnsureUniqueTuneName(tune.Name)
            tune.SavedAt = DateTime.Now
            SavedTunesDatabase.SaveTune(tune)
            imported += 1
        Next

        Hint($"已导入 {imported} 个存档调校。", HintType.Green)
        LabSaveImportStatus.Text = $"已导入 {imported} 个存档调校。"
        RefreshTunesList()
    End Sub

    Private Sub RenderSaveTuneCandidates()
        PanelSaveTuneCandidates.Children.Clear()
        Dim keyword = If(TxtSaveTuneVehicleFilter Is Nothing, "", TxtSaveTuneVehicleFilter.Text.Trim())
        Dim filtered = ScannedSaveTuneCandidates.
            Where(Function(candidate) String.IsNullOrWhiteSpace(keyword) OrElse
                                       candidate.VehicleName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 OrElse
                                       candidate.CarId.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).
            ToList()

        If ScannedSaveTuneCandidates.Count > 0 Then
            LabSaveImportStatus.Text = If(String.IsNullOrWhiteSpace(keyword),
                $"找到 {ScannedSaveTuneCandidates.Count} 个可导入调校，默认未勾选。",
                $"车型筛选后显示 {filtered.Count} / {ScannedSaveTuneCandidates.Count} 个调校，默认未勾选。")
        End If

        For Each candidate In filtered
            Dim row As New Border() With {
                .Padding = New Thickness(12),
                .Margin = New Thickness(0, 0, 0, 8),
                .CornerRadius = New CornerRadius(6)
            }
            row.SetResourceReference(Border.BackgroundProperty, "ColorBrushSemiTransparent")

            Dim panel As New StackPanel()
            Dim check As New MyCheckBox() With {
                .Text = $"{candidate.TuneName}  ·  {candidate.VehicleName}",
                .Checked = False,
                .Margin = New Thickness(0, 0, 0, 6)
            }
            panel.Children.Add(check)

            Dim info = New TextBlock() With {
                .Text = $"作者：{If(String.IsNullOrWhiteSpace(candidate.Author), "未知", candidate.Author)}    车辆 ID：{candidate.CarId}    时间：{FormatCandidateTime(candidate)}",
                .TextWrapping = TextWrapping.Wrap,
                .FontSize = 11,
                .Margin = New Thickness(24, 0, 0, 2)
            }
            info.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray3")
            panel.Children.Add(info)

            Dim detail = New TextBlock() With {
                .Text = $"来源：{candidate.FolderName}    滑块：{candidate.SliderCount}    部件槽：{candidate.PartCount}",
                .TextWrapping = TextWrapping.Wrap,
                .FontSize = 11,
                .Margin = New Thickness(24, 0, 0, 0)
            }
            detail.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray3")
            panel.Children.Add(detail)

            row.Child = panel
            row.Tag = New Tuple(Of MyCheckBox, SaveTuneImportCandidate)(check, candidate)
            PanelSaveTuneCandidates.Children.Add(row)
        Next
    End Sub

    Private Sub OnSaveTuneVehicleFilterChanged(sender As Object, e As RoutedEventArgs)
        RenderSaveTuneCandidates()
    End Sub

    Private Sub OnTuneListVehicleFilterChanged(sender As Object, e As RoutedEventArgs)
        RefreshTunesList()
    End Sub

    Private Sub OnDeleteCheckedTunesClicked(sender As Object, e As RoutedEventArgs)
        If Not BatchDeleteMode Then
            SetBatchDeleteMode(True)
            Return
        End If

        Dim selected = PanelTunesList.Children.
            OfType(Of FrameworkElement)().
            Select(Function(row) TryCast(row.Tag, Tuple(Of MyCheckBox, SavedTuneSummary))).
            Where(Function(item) item IsNot Nothing AndAlso item.Item1.Checked).
            ToList()

        If selected.Count = 0 Then
            Hint("请先勾选要删除的调校方案。", HintType.Blue)
            Return
        End If

        If MessageBox.Show($"确定要删除已勾选的 {selected.Count} 个调校方案吗？此操作无法撤销。", "确认批量删除", MessageBoxButton.OKCancel, MessageBoxImage.Warning) <> MessageBoxResult.OK Then
            Return
        End If

        For Each item In selected
            SavedTunesDatabase.DeleteTune(item.Item2.Id)
        Next

        Hint($"已删除 {selected.Count} 个调校方案。", HintType.Green)
        SetBatchDeleteMode(False)
        RefreshTunesList()
    End Sub

    Private Sub OnCancelBatchDeleteClicked(sender As Object, e As RoutedEventArgs)
        SetBatchDeleteMode(False)
    End Sub

    Private Sub SetBatchDeleteMode(enabled As Boolean)
        If BatchDeleteMode = enabled Then Return
        BatchDeleteMode = enabled
        UpdateBatchDeleteControls()
    End Sub

    Private Sub UpdateBatchDeleteControls()
        If BtnDeleteFilteredTunes Is Nothing OrElse BtnCancelBatchDelete Is Nothing Then Return
        BtnDeleteFilteredTunes.Text = If(BatchDeleteMode, "删除勾选", "批量删除")
        BtnDeleteFilteredTunes.ToolTip = If(BatchDeleteMode, "删除列表中已勾选的调校方案", "进入批量删除选择模式")
        BtnCancelBatchDelete.Visibility = If(BatchDeleteMode, Visibility.Visible, Visibility.Collapsed)

        If PanelTunesList Is Nothing Then Return
        For Each item In PanelTunesList.Children.OfType(Of FrameworkElement)()
            Dim tag = TryCast(item.Tag, Tuple(Of MyCheckBox, SavedTuneSummary))
            If tag Is Nothing Then Continue For
            tag.Item1.Visibility = If(BatchDeleteMode, Visibility.Visible, Visibility.Collapsed)
            If Not BatchDeleteMode Then tag.Item1.Checked = False
        Next
    End Sub

    Private Sub UpdateSaveImportPathLabel()
        If LabSaveImportPath Is Nothing Then Return
        Dim configured = Settings.Get(Of String)("GameSavePath", "")
        Dim effective = If(String.IsNullOrWhiteSpace(configured), SaveTuneImportService.ResolveDefaultSavePath(), configured)
        Dim mode = If(String.IsNullOrWhiteSpace(configured), "自动识别", "手动指定")
        Dim exists = If(Directory.Exists(effective), "可用", "不存在")
        LabSaveImportPath.Text = $"存档目录（{mode}，{exists}）：{effective}"
    End Sub

    Private Function EnsureUniqueTuneName(baseName As String) As String
        Dim name = If(String.IsNullOrWhiteSpace(baseName), "存档导入调校", baseName.Trim())
        If Not SavedTunesDatabase.NameExists(name) Then Return name

        Dim index = 2
        Do
            Dim candidate = $"{name} ({index})"
            If Not SavedTunesDatabase.NameExists(candidate) Then Return candidate
            index += 1
        Loop
    End Function

    Private Function FormatCandidateTime(candidate As SaveTuneImportCandidate) As String
        Dim value = If(candidate.SavedAt = DateTime.MinValue, candidate.LastWriteTime, candidate.SavedAt)
        If value = DateTime.MinValue Then Return "未知"
        Return value.ToString("yyyy-MM-dd HH:mm")
    End Function

    Public Sub RefreshTunesList()
        PanelTunesList.Children.Clear()
        Dim allSummaries = SavedTunesDatabase.Summaries
        Dim keyword = If(TxtTuneListVehicleFilter Is Nothing, "", TxtTuneListVehicleFilter.Text.Trim())
        Dim summaries = allSummaries.
            Where(Function(summary) String.IsNullOrWhiteSpace(keyword) OrElse
                                    GetSummaryVehicleText(summary).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0).
            ToList()

        If LabTuneListStatus IsNot Nothing Then
            LabTuneListStatus.Text = If(String.IsNullOrWhiteSpace(keyword),
                $"共 {allSummaries.Count} 个已保存调校。",
                $"车型筛选后显示 {summaries.Count} / {allSummaries.Count} 个已保存调校。")
        End If

        If summaries Is Nothing OrElse summaries.Count = 0 Then
            LabNoTunes.Visibility = Visibility.Visible
            Return
        End If

        LabNoTunes.Visibility = Visibility.Collapsed

        For Each summary In summaries
            Dim rowCard As New MyCard() With {
                .Title = "",
                .CanSwap = True,
                .Margin = New Thickness(0, 0, 0, 15)
            }

            Dim bodyPanel As New StackPanel() With {
                .Margin = New Thickness(20, 40, 20, 18)
            }
            Dim check As New MyCheckBox() With {
                .Text = "删除",
                .Checked = False,
                .HorizontalAlignment = HorizontalAlignment.Right,
                .VerticalAlignment = VerticalAlignment.Top,
                .Margin = New Thickness(0, 8, 42, 0),
                .Visibility = If(BatchDeleteMode, Visibility.Visible, Visibility.Collapsed),
                .ToolTip = "选中此调校以批量删除"
            }
            bodyPanel.Children.Add(CreateSummaryPanel(summary))
            rowCard.SwapControl = bodyPanel
            rowCard.Children.Add(bodyPanel)
            rowCard.Children.Add(check)
            rowCard.Tag = New Tuple(Of MyCheckBox, SavedTuneSummary)(check, summary)
            rowCard.IsSwapped = True
            ConfigureTuneCardHeader(rowCard, summary)

            Dim loaded As Boolean = False
            AddHandler rowCard.PreviewSwap, Sub(s, ev)
                                                If BatchDeleteMode AndAlso check.IsMouseOver Then
                                                    ev.Handled = True
                                                    Return
                                                End If
                                                If loaded OrElse Not rowCard.IsSwapped Then Return
                                                loaded = True
                                                LoadTuneDetails(summary.Id, bodyPanel)
                                                rowCard.Dispatcher.BeginInvoke(New Action(Sub() rowCard.TriggerForceResize()))
                                            End Sub

            PanelTunesList.Children.Add(rowCard)
        Next
    End Sub

    Private Function GetSummaryVehicleText(summary As SavedTuneSummary) As String
        Return $"{summary.SelectedCarText} {summary.CarSearchKeyword} {summary.Make} {summary.Model}".Trim()
    End Function

    Private Sub ConfigureTuneCardHeader(card As MyCard, summary As SavedTuneSummary)
        Dim vehicle = If(String.IsNullOrWhiteSpace(summary.SelectedCarText), $"{summary.Make} {summary.Model}".Trim(), summary.SelectedCarText)
        If String.IsNullOrWhiteSpace(vehicle) Then vehicle = "未知车型"
        Dim meta = $"{vehicle} · {summary.SavedAt:yyyy-MM-dd HH:mm}"

        card.Inlines.Clear()
        card.Inlines.Add(New Run(summary.Name) With {
            .FontSize = 13,
            .FontWeight = FontWeights.Bold
        })

        Dim metaRun As New Run("    " & meta) With {
            .FontSize = 11,
            .FontWeight = FontWeights.Normal
        }
        metaRun.SetResourceReference(TextElement.ForegroundProperty, "ColorBrushGray3")
        card.Inlines.Add(metaRun)
    End Sub

    Private Function CreateSummaryPanel(summary As SavedTuneSummary) As UIElement
        Dim grid As New UniformGrid() With {
            .Columns = 3,
            .Margin = New Thickness(0, 0, 0, 4)
        }
        grid.Children.Add(CreateReadOnlySpec("车辆型号", If(String.IsNullOrWhiteSpace(summary.SelectedCarText), $"{summary.Make} {summary.Model}".Trim(), summary.SelectedCarText)))
        grid.Children.Add(CreateReadOnlySpec("性能等级", $"{summary.CarClass} {summary.Pi}".Trim()))
        grid.Children.Add(CreateReadOnlySpec("保存时间", summary.SavedAt.ToString("yyyy-MM-dd HH:mm")))
        Return grid
    End Function

    Private Sub LoadTuneDetails(tuneId As String, bodyPanel As StackPanel)
        Dim tune = SavedTunesDatabase.GetTune(tuneId)
        If tune Is Nothing Then
            Hint("加载调校详情失败。", HintType.Red)
            Return
        End If

        EnsureTuneResult(tune)
        Dim summary = SavedTunesDatabase.Summaries.FirstOrDefault(Function(item) item.Id = tuneId)
        bodyPanel.Children.Clear()
        If summary IsNot Nothing Then
            bodyPanel.Children.Add(CreateSummaryPanel(summary))
        End If

        Dim divider As New Border() With {
            .Height = 1,
            .Margin = New Thickness(0, 10, 0, 12)
        }
        divider.SetResourceReference(Border.BackgroundProperty, "ColorBrushBorder")
        bodyPanel.Children.Add(divider)

        Dim title As New TextBlock() With {
            .Text = "调校结果数值微调",
            .FontWeight = FontWeights.SemiBold,
            .FontSize = 13,
            .Margin = New Thickness(0, 0, 0, 12)
        }
        title.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrush3")
        bodyPanel.Children.Add(title)

        Dim txtMap As New Dictionary(Of String, MyTextBox)()
        Dim resultsGrid As New Grid()
        resultsGrid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})
        resultsGrid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(28)})
        resultsGrid.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})

        Dim leftPanel As New StackPanel()
        Grid.SetColumn(leftPanel, 0)
        Dim rightPanel As New StackPanel()
        Grid.SetColumn(rightPanel, 2)

        AddCategoryEditor(leftPanel, "轮胎与气压", "Tires", tune.Result.Tires, txtMap)
        AddCategoryEditor(leftPanel, "悬挂定位角", "Alignment", tune.Result.Alignment, txtMap)
        AddCategoryEditor(leftPanel, "防倾杆", "ARB", tune.Result.ARB, txtMap)
        AddCategoryEditor(leftPanel, "制动系统", "Braking", tune.Result.Braking, txtMap)
        AddCategoryEditor(leftPanel, "空气动力学", "Aero", tune.Result.Aero, txtMap)
        AddCategoryEditor(rightPanel, "弹簧与高度", "Suspension", tune.Result.Suspension, txtMap)
        AddCategoryEditor(rightPanel, "悬挂阻尼", "Damping", tune.Result.Damping, txtMap)
        AddCategoryEditor(rightPanel, "差速器锁", "Diff", tune.Result.Diff, txtMap)
        AddCategoryEditor(rightPanel, "变速箱齿轮比", "Gearing", tune.Result.Gearing, txtMap)

        resultsGrid.Children.Add(leftPanel)
        resultsGrid.Children.Add(rightPanel)
        bodyPanel.Children.Add(resultsGrid)

        Dim actionDivider As New Border() With {
            .Height = 1,
            .Margin = New Thickness(0, 15, 0, 15)
        }
        actionDivider.SetResourceReference(Border.BackgroundProperty, "ColorBrushBorder")
        bodyPanel.Children.Add(actionDivider)

        Dim renamePanel As New Grid() With {.Margin = New Thickness(0, 0, 0, 15)}
        renamePanel.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(140)})
        renamePanel.ColumnDefinitions.Add(New ColumnDefinition() With {.Width = New GridLength(1, GridUnitType.Star)})
        Dim nameLabel As New TextBlock() With {
            .Text = "方案命名修改",
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

        bodyPanel.Children.Add(CreateActionButtons(tune, txtMap, txtNameEdit))
        bodyPanel.UpdateLayout()
    End Sub

    Private Sub EnsureTuneResult(tune As SavedTune)
        If tune.Result IsNot Nothing AndAlso tune.Result.Tires IsNot Nothing AndAlso tune.Result.Tires.Values.Count > 0 Then Return
        Try
            tune.Result = TuningCalculator.Calculate(tune.State)
            SavedTunesDatabase.SaveTune(tune)
        Catch
        End Try
    End Sub

    Private Sub AddCategoryEditor(target As StackPanel, title As String, catName As String, category As TuningCategory, txtMap As Dictionary(Of String, MyTextBox))
        If category Is Nothing OrElse category.Values Is Nothing OrElse category.Values.Count = 0 Then Return

        Dim titleBlock As New TextBlock() With {
            .Text = title,
            .FontWeight = FontWeights.Bold,
            .Margin = New Thickness(0, If(target.Children.Count = 0, 0, 8), 0, 6)
        }
        titleBlock.SetResourceReference(TextBlock.ForegroundProperty, "ColorBrushGray1")
        target.Children.Add(titleBlock)

        For Each item In category.Values
            Dim tb As New MyTextBox() With {.Text = item.Value}
            txtMap($"{catName}/{item.Key}") = tb
            target.Children.Add(CreateEditableField(item.Key, tb))
        Next
    End Sub

    Private Function CreateActionButtons(tune As SavedTune, txtMap As Dictionary(Of String, MyTextBox), txtNameEdit As MyTextBox) As UIElement
        Dim btnPanel As New StackPanel() With {
            .Orientation = Orientation.Horizontal,
            .HorizontalAlignment = HorizontalAlignment.Right
        }

        Dim btnSave As New MyButton() With {
            .Text = "覆盖保存",
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

        Dim btnSaveAs As New MyButton() With {
            .Text = "另存为",
            .Padding = New Thickness(16, 6, 16, 6),
            .Height = 32,
            .Margin = New Thickness(0, 0, 8, 0),
            .ToolTip = "将当前的数值另存为一个新命名的调校方案"
        }
        AddHandler btnSaveAs.Click, Sub(s, ev)
                                        Dim currentName As String = txtNameEdit.Text.Trim()
                                        Dim newName As String = Microsoft.VisualBasic.Interaction.InputBox("请输入新的调校方案名称：", "另存为新调校", currentName & " - 副本").Trim()
                                        If String.IsNullOrEmpty(newName) Then Return

                                        If SavedTunesDatabase.NameExists(newName) Then
                                            If MessageBox.Show("已存在名为 “" & newName & "” 的调校。是否要覆盖它？", "确认覆盖", MessageBoxButton.OKCancel, MessageBoxImage.Question) <> MessageBoxResult.OK Then
                                                Return
                                            End If
                                        End If

                                        UpdateTuneResultsFromFields(tune, txtMap)
                                        Dim newTune = JsonSerializer.Deserialize(Of SavedTune)(JsonSerializer.Serialize(tune))
                                        If newTune Is Nothing Then Return
                                        newTune.Id = Guid.NewGuid().ToString()
                                        newTune.Name = newName
                                        newTune.SavedAt = DateTime.Now
                                        SavedTunesDatabase.SaveTune(newTune)
                                        Hint("另存新调校 “" & newName & "” 成功！", HintType.Green)
                                        RefreshTunesList()
                                    End Sub
        btnPanel.Children.Add(btnSaveAs)

        Dim btnDelete As New MyButton() With {
            .Text = "删除调校",
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

        Dim btnShare As New MyButton() With {
            .Text = "分享",
            .Padding = New Thickness(16, 6, 16, 6),
            .Height = 32,
            .Margin = New Thickness(0, 0, 8, 0),
            .ToolTip = "生成分享码并复制到剪贴板"
        }
        AddHandler btnShare.Click, Sub(s, ev)
                                       Try
                                           UpdateTuneResultsFromFields(tune, txtMap)
                                           Dim shareCode As String = ShareCodec.Encode(tune)
                                           Clipboard.SetText(shareCode)
                                           Hint("分享码已复制（" & shareCode.Length & " 字符）", HintType.Green)
                                       Catch ex As Exception
                                           Hint("生成分享码失败：" & ex.Message, HintType.Red)
                                       End Try
                                   End Sub
        btnPanel.Children.Add(btnShare)

        Dim btnLoad As New MyButton() With {
            .Text = "在主页加载",
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

        Return btnPanel
    End Function

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

    Public Sub ApplySubPage(subPage As UiKitSubPage)
        CardSaveImport.Visibility = If(subPage = UiKitSubPage.SavedTunesSaveImport, Visibility.Visible, Visibility.Collapsed)
        CardImport.Visibility = If(subPage = UiKitSubPage.SavedTunesShareImport, Visibility.Visible, Visibility.Collapsed)
        CardList.Visibility = If(subPage = UiKitSubPage.SavedTunesList, Visibility.Visible, Visibility.Collapsed)
        If PanBack IsNot Nothing Then PanBack.PerformVerticalOffsetDelta(-PanBack.VerticalOffset)
    End Sub

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Dim list As New List(Of Tuple(Of String, FrameworkElement))()
        list.Add(New Tuple(Of String, FrameworkElement)("存档导入", CardSaveImport))
        list.Add(New Tuple(Of String, FrameworkElement)("导入分享码", CardImport))
        list.Add(New Tuple(Of String, FrameworkElement)("方案列表", CardList))
        Return list
    End Function
End Class


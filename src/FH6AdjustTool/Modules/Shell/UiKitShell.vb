Public Enum UiKitDemoPage
    Tuner = 0
    SavedTunes = 1
    Telemetry = 2
    Settings = 3
    About = 4
End Enum

Public Enum UiKitSubPage
    TunerMain = 0
    SavedTunesSaveImport = 10
    SavedTunesShareImport = 11
    SavedTunesList = 12
    TelemetryDashboard = 20
    SettingsUnits = 30
    SettingsSaveImport = 31
    SettingsTelemetry = 32
    SettingsAi = 33
    SettingsThemeColors = 34
    SettingsBackground = 35
    AboutFeatures = 40
    AboutDisclaimer = 41
    AboutCredits = 42
End Enum

Public Class UiKitShellHost
    Public Property CurrentPage As UiKitDemoPage = UiKitDemoPage.Tuner
    Public Property LastPage As UiKitDemoPage = UiKitDemoPage.Tuner
End Class

Public Module UiKitShellText
    Public Function GetPageTitle(page As UiKitDemoPage) As String
        Select Case page
            Case UiKitDemoPage.Tuner
                Return "调校计算"
            Case UiKitDemoPage.SavedTunes
                Return "保存的调校"
            Case UiKitDemoPage.Telemetry
                Return "遥测分析"
            Case UiKitDemoPage.Settings
                Return "软件设置"
            Case UiKitDemoPage.About
                Return "关于软件"
            Case Else
                Return "调校计算"
        End Select
    End Function

    Public Function GetSubPageTitle(subPage As UiKitSubPage) As String
        Select Case subPage
            Case UiKitSubPage.SavedTunesSaveImport
                Return "存档导入"
            Case UiKitSubPage.SavedTunesShareImport
                Return "导入分享码"
            Case UiKitSubPage.SavedTunesList
                Return "方案列表"
            Case UiKitSubPage.TelemetryDashboard
                Return "遥测仪表盘"
            Case UiKitSubPage.SettingsUnits
                Return "计量单位"
            Case UiKitSubPage.SettingsSaveImport
                Return "存档导入"
            Case UiKitSubPage.SettingsTelemetry
                Return "遥测数据"
            Case UiKitSubPage.SettingsAi
                Return "智能接口"
            Case UiKitSubPage.SettingsThemeColors
                Return "主题配色"
            Case UiKitSubPage.SettingsBackground
                Return "背景磨砂"
            Case UiKitSubPage.AboutFeatures
                Return "功能说明"
            Case UiKitSubPage.AboutDisclaimer
                Return "版权免责"
            Case UiKitSubPage.AboutCredits
                Return "致谢数据"
            Case Else
                Return "调校计算"
        End Select
    End Function
End Module

Public Module UiKitShellNavigation
    Public Function GetActiveScroll(child As Object) As MyScrollViewer
        If child Is Nothing OrElse TypeOf child IsNot MyPageRight Then Return Nothing
        Dim page As MyPageRight = child
        If String.IsNullOrWhiteSpace(page.PanScroll) Then Return Nothing
        Return TryCast(page.FindName(page.PanScroll), MyScrollViewer)
    End Function
End Module

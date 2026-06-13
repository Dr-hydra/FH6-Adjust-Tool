Public Enum UiKitDemoPage
    Tuner = 0
    SavedTunes = 1
    Settings = 2
    About = 3
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
            Case UiKitDemoPage.Settings
                Return "软件设置"
            Case UiKitDemoPage.About
                Return "关于软件"
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

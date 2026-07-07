Imports System.Windows

Public Class PageAbout
    ' About page has no complex code-behind logic

    Public Sub ApplySubPage(subPage As UiKitSubPage)
        CardFeatures.Visibility = If(subPage = UiKitSubPage.AboutFeatures, Visibility.Visible, Visibility.Collapsed)
        CardDisclaimer.Visibility = If(subPage = UiKitSubPage.AboutDisclaimer, Visibility.Visible, Visibility.Collapsed)
        CardCredits.Visibility = If(subPage = UiKitSubPage.AboutCredits, Visibility.Visible, Visibility.Collapsed)
        If PanBack IsNot Nothing Then PanBack.PerformVerticalOffsetDelta(-PanBack.VerticalOffset)
    End Sub

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Dim list As New List(Of Tuple(Of String, FrameworkElement))()
        list.Add(New Tuple(Of String, FrameworkElement)("功能说明", CardFeatures))
        list.Add(New Tuple(Of String, FrameworkElement)("版权免责", CardDisclaimer))
        list.Add(New Tuple(Of String, FrameworkElement)("致谢数据", CardCredits))
        Return list
    End Function
End Class

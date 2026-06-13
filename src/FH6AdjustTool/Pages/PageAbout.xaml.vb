Imports System.Windows

Public Class PageAbout
    ' About page has no complex code-behind logic

    Public Function GetSecondaryNavItems() As List(Of Tuple(Of String, FrameworkElement))
        Dim list As New List(Of Tuple(Of String, FrameworkElement))()
        list.Add(New Tuple(Of String, FrameworkElement)("版权免责", CardDisclaimer))
        list.Add(New Tuple(Of String, FrameworkElement)("致谢数据", CardCredits))
        Return list
    End Function
End Class

Public Module ModMain

    Public Enum HintType
        Blue
        Green
        Red
    End Enum

    Public FrmMain As FormMain

    Public Class MyMsgBoxConverter
        Public Type As MyMsgBoxType
        Public Text As String
        Public Title As String
        Public Button1 As String
        Public Button2 As String
        Public Button3 As String
        Public IsWarn As Boolean
        Public ForceWait As Boolean
        Public HighLight As Boolean
        Public HintText As String
        Public ValidateRules As ObjectModel.Collection(Of Validate)
        Public Content As Object
        Public Button1Action As Action
        Public Button2Action As Action
        Public Button3Action As Action
        Public Result As Integer
        Public Input As String
        Public WaitFrame As New System.Windows.Threading.DispatcherFrame(True)
        Public IsExited As Boolean
    End Class

    Public Enum MyMsgBoxType
        Text
        Input
        [Select]
    End Enum

    Public WaitingMyMsgBox As New List(Of MyMsgBoxConverter)

    Public Sub Hint(text As String, Optional type As HintType = HintType.Blue, Optional log As Boolean = True)
        If FrmMain Is Nothing OrElse FrmMain.PanHint Is Nothing Then Return
        RunInUi(Sub()
            Dim border As New Border With {
                .CornerRadius = New CornerRadius(6),
                .Padding = New Thickness(12, 8, 12, 8),
                .Margin = New Thickness(0, 0, 0, 8),
                .Background = If(type = HintType.Green, CType(Color6, SolidColorBrush), If(type = HintType.Red, New SolidColorBrush(Color.FromRgb(&HFB, &HDD, &HDD)), CType(Color7, SolidColorBrush)))
            }
            border.Child = New TextBlock With {.Text = text, .FontSize = 12, .Foreground = CType(Color1, SolidColorBrush)}
            FrmMain.PanHint.Children.Insert(0, border)
            AniStart(AaCode(Sub() FrmMain.PanHint.Children.Remove(border), 2400), "Hint " & GetUuid())
        End Sub)
    End Sub

    Public Function MyMsgBox(caption As String, Optional title As String = "提示", Optional button1 As String = "确定", Optional button2 As String = "", Optional button3 As String = "", Optional isWarn As Boolean = False, Optional forceWait As Boolean = False, Optional highLight As Boolean = False, Optional button1Action As Action = Nothing, Optional button2Action As Action = Nothing, Optional button3Action As Action = Nothing) As Integer
        Return MessageBox.Show(caption, title, MessageBoxButton.OK, If(isWarn, MessageBoxImage.Warning, MessageBoxImage.Information))
    End Function

    Public Function MyMsgBoxInput(text As String, Optional title As String = "输入", Optional defaultInput As String = "", Optional hintText As String = "", Optional validateRules As ObjectModel.Collection(Of Validate) = Nothing, Optional button1 As String = "确定", Optional button2 As String = "取消", Optional isWarn As Boolean = False) As String
        Return defaultInput
    End Function

    Public Function MyMsgBoxSelect(selections As IEnumerable(Of String), Optional title As String = "选择", Optional button1 As String = "确定", Optional button2 As String = "取消", Optional isWarn As Boolean = False) As Integer
        Return 0
    End Function

    Public Sub MyMsgBoxTick()
    End Sub

    Public Sub FeedbackInfo()
    End Sub

    Public Sub TimerMain()
    End Sub

End Module

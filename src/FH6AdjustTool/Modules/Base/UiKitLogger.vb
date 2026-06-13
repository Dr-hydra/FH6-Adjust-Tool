Public Class UiKitLogger
    Inherits FileLogger

    Private Shared Function FilterAccessToken(Raw As String, FilterChar As Char) As String
        If Raw Is Nothing Then Return Nothing
        Return Raw.RegexReplace("(?i)(access[_-]?token[""'\s:=]+)[^""'\s&]+", Function(m) m.Groups(1).Value & New String(FilterChar, 8))
    End Function

    Private Shared Function FilterUserName(Raw As String, FilterChar As Char) As String
        If Raw Is Nothing Then Return Nothing
        Dim UserName = Environment.UserName
        If String.IsNullOrWhiteSpace(UserName) Then Return Raw
        Return Raw.Replace(UserName, New String(FilterChar, Math.Min(UserName.Length, 8)))
    End Function

    ''' <inheritdoc/>
    Public Overrides Function Format(Text As String, Level As LogLevel, FilePath As String, Ex As Exception) As String
        Text = MyBase.Format(Text, Level, FilePath, Ex)
        Text = FilterUserName(FilterAccessToken(Text, "*"c), "*"c)
        Return Text
    End Function

    ''' <inheritdoc/>
    Public Overrides Sub HandleBehavior(RawMessage As String, FormattedMessage As String, Behavior As LogBehavior, Ex As Exception)
        If IsProgramEnding Then Return
        MyBase.HandleBehavior(RawMessage, FormattedMessage, Behavior, Ex)
        Dim BriefText = If(Ex Is Nothing, RawMessage, If(RawMessage Is Nothing, "", $"{RawMessage}：") & Ex.GetDisplay(False))
        BriefText = FilterUserName(FilterAccessToken(BriefText, "*"c), "*"c)
        Dim DetailText = If(Ex Is Nothing, RawMessage, If(RawMessage Is Nothing, "", $"{RawMessage}：") & Ex.GetDisplay(True))
        DetailText = FilterUserName(FilterAccessToken(DetailText, "*"c), "*"c)
        Select Case Behavior
            Case LogBehavior.None
                '啥也不干
            Case LogBehavior.ToastIfDebug
                If BuildType = BuildTypes.Debug OrElse ModeDebug Then Hint("[调试模式] " & BriefText, HintType.Blue, False)
            Case LogBehavior.Toast
                Hint(BriefText, HintType.Red, False)
            Case LogBehavior.Alert
                MyMsgBox(DetailText, "错误", IsWarn:=True)
            Case LogBehavior.AlertThenFeedback
                MyMsgBox(DetailText, "错误", IsWarn:=True)
            Case LogBehavior.AlertThenCrash
                Static FirstTrigger As Boolean = True
                If FirstTrigger Then
                    FirstTrigger = False
                    MsgBox(DetailText, MsgBoxStyle.Critical, "错误")
                Else
                    Thread.Sleep(2000)
                End If
                FormMain.EndProgramForce(ProcessReturnValues.Exception)
        End Select
    End Sub

End Class


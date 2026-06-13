Public Module ModSecret

    Public Color1 As New MyColor("#343d4a")
    Public Color2 As New MyColor("#0b5bcb")
    Public Color3 As New MyColor("#1370f3")
    Public Color4 As New MyColor("#4890f5")
    Public Color5 As New MyColor("#96c0f9")
    Public Color6 As New MyColor("#d5e6fd")
    Public Color7 As New MyColor("#e0eafd")
    Public Color8 As New MyColor("#eaf2fe")
    Public ColorBg0 As New MyColor("#96c0f9")
    Public ColorBg1 As New MyColor(190, Color7)
    Public ColorGray1 As New MyColor(64)
    Public ColorGray2 As New MyColor(115)
    Public ColorGray3 As New MyColor(140)
    Public ColorGray4 As New MyColor(166)
    Public ColorGray5 As New MyColor(204)
    Public ColorGray6 As New MyColor(235)
    Public ColorGray7 As New MyColor(240)
    Public ColorGray8 As New MyColor(245)
    Public ColorSemiTransparent As New MyColor(1, Color8)
    Public ThemeNow As Integer = -1
    Public ColorHue As Integer = 210
    Public ColorSat As Integer = 85
    Public ColorLightAdjust As Integer = 0
    Private ColorHueTopbarDeltas As Integer() = {0, 0, 0}

    Public Sub ThemeRefresh(Optional newTheme As Integer = -1)
        If newTheme < 0 Then newTheme = Settings.Get(Of Integer)("UiLauncherTheme")
        ThemeNow = newTheme
        ApplyThemePalette(ThemeNow)
        Color1 = New MyColor().FromHSL2(ColorHue, ColorSat * 0.2, 25 + ColorLightAdjust * 0.3)
        Color2 = New MyColor().FromHSL2(ColorHue, ColorSat, 45 + ColorLightAdjust)
        Color3 = New MyColor().FromHSL2(ColorHue, ColorSat, 55 + ColorLightAdjust)
        Color4 = New MyColor().FromHSL2(ColorHue, ColorSat, 65 + ColorLightAdjust)
        Color5 = New MyColor().FromHSL2(ColorHue, ColorSat, 80 + ColorLightAdjust * 0.4)
        Color6 = New MyColor().FromHSL2(ColorHue, ColorSat, 91 + ColorLightAdjust * 0.1)
        Color7 = New MyColor().FromHSL2(ColorHue, ColorSat, 95)
        Color8 = New MyColor().FromHSL2(ColorHue, ColorSat, 97)
        ColorBg0 = Color4 * 0.4 + Color5 * 0.4 + ColorGray4 * 0.2
        ColorBg1 = New MyColor(190, Color7)
        ColorSemiTransparent = New MyColor(1, Color8)
        Apply("ColorBrush1", Color1)
        Apply("ColorBrush2", Color2)
        Apply("ColorBrush3", Color3)
        Apply("ColorBrush4", Color4)
        Apply("ColorBrush5", Color5)
        Apply("ColorBrush6", Color6)
        Apply("ColorBrush7", Color7)
        Apply("ColorBrush8", Color8)
        Apply("ColorBrushBg0", ColorBg0)
        Apply("ColorBrushBg1", ColorBg1)
        ApplyColor("ColorObject1", Color1)
        ApplyColor("ColorObject2", Color2)
        ApplyColor("ColorObject3", Color3)
        ApplyColor("ColorObject4", Color4)
        ApplyColor("ColorObject5", Color5)
        ApplyColor("ColorObject6", Color6)
        ApplyColor("ColorObject7", Color7)
        ApplyColor("ColorObject8", Color8)
        ApplyColor("ColorObjectBg0", ColorBg0)
        ApplyColor("ColorObjectBg1", ColorBg1)
        ThemeRefreshMain()
    End Sub

    Public Sub ThemeRefreshMain()
        If FrmMain Is Nothing OrElse Not FrmMain.IsLoaded Then Return
        Dim titleBrush = New LinearGradientBrush With {.EndPoint = New Point(1, 0), .StartPoint = New Point(0, 0)}
        titleBrush.GradientStops.Add(New GradientStop With {.Offset = 0, .Color = CType(New MyColor().FromHSL2(ColorHue + ColorHueTopbarDeltas(0), ColorSat, 48 + ColorLightAdjust), Color)})
        titleBrush.GradientStops.Add(New GradientStop With {.Offset = 0.5, .Color = CType(New MyColor().FromHSL2(ColorHue + ColorHueTopbarDeltas(1), ColorSat, 54 + ColorLightAdjust), Color)})
        titleBrush.GradientStops.Add(New GradientStop With {.Offset = 1, .Color = CType(New MyColor().FromHSL2(ColorHue + ColorHueTopbarDeltas(2), ColorSat, 48 + ColorLightAdjust), Color)})
        FrmMain.PanTitle.Background = titleBrush
        FrmMain.PanTitle.Background.Freeze()
        FrmMain.PanForm.Background = CType(ColorGray8, SolidColorBrush)
        FrmMain.PanForm.Background.Freeze()
    End Sub

    Private Sub ApplyThemePalette(themeId As Integer)
        ColorHueTopbarDeltas = New Integer() {0, 0, 0}
        Select Case themeId
            Case 1
                ColorHue = 175
                ColorSat = 72
                ColorLightAdjust = 1
                ColorHueTopbarDeltas = New Integer() {-8, 0, 8}
            Case 2
                ColorHue = 122
                ColorSat = 72
                ColorLightAdjust = 0
            Case 3
                ColorHue = 48
                ColorSat = 90
                ColorLightAdjust = 3
            Case 4
                ColorHue = 28
                ColorSat = 62
                ColorLightAdjust = -1
            Case 5
                ColorHue = 215
                ColorSat = 18
                ColorLightAdjust = -18
            Case 6
                ColorHue = 330
                ColorSat = 72
                ColorLightAdjust = 0
            Case 7
                ColorHue = 272
                ColorSat = 78
                ColorLightAdjust = -1
            Case 8
                ColorHue = 43
                ColorSat = 76
                ColorLightAdjust = 0
            Case 9
                ColorHue = 24
                ColorSat = 86
                ColorLightAdjust = 0
            Case 10
                ColorHue = 355
                ColorSat = 78
                ColorLightAdjust = -1
            Case 11
                ColorHue = 198
                ColorSat = 92
                ColorLightAdjust = -2
                ColorHueTopbarDeltas = New Integer() {-12, 0, 12}
            Case 12, 13
                ColorHue = 292
                ColorSat = 82
                ColorLightAdjust = 0
                ColorHueTopbarDeltas = New Integer() {-70, 0, 70}
            Case 14
                ColorHue = Settings.Get(Of Integer)("UiLauncherThemeHue")
                ColorSat = Settings.Get(Of Integer)("UiLauncherThemeSat")
                ColorLightAdjust = Settings.Get(Of Integer)("UiLauncherThemeLight")
            Case Else
                ColorHue = 210
                ColorSat = 85
                ColorLightAdjust = 0
        End Select
    End Sub

    Friend Sub ThemeCheckAll(effectSetup As Boolean)
    End Sub

    Friend Function ThemeCheckOne(id As Integer) As Boolean
        Return True
    End Function

    Friend Function ThemeUnlock(id As Integer, Optional showDoubleHint As Boolean = True, Optional unlockHint As String = Nothing) As Boolean
        Return True
    End Function

    Private Sub Apply(key As String, value As MyColor)
        If Application.Current IsNot Nothing Then Application.Current.Resources(key) = CType(value, SolidColorBrush)
    End Sub

    Private Sub ApplyColor(key As String, value As MyColor)
        If Application.Current IsNot Nothing Then Application.Current.Resources(key) = CType(value, Color)
    End Sub

End Module

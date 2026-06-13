Public Module Settings

    Private ReadOnly Values As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase) From {
        {"SystemDebugMode", False},
        {"SystemSystemCache", ""},
        {"UiLauncherTheme", 0},
        {"UiLauncherThemeHue", 210},
        {"UiLauncherThemeSat", 70},
        {"UiLauncherThemeLight", 0},
        {"UiLauncherTransparent", 600},
        {"UiBackgroundOpacity", 280},
        {"UiBackgroundBlur", 8},
        {"UiLauncherLogo", False},
        {"WindowHeight", 720},
        {"WindowWidth", 1040},
        {"UnitWeight", "lbs"},
        {"UnitSpeed", "mph"},
        {"UnitPressure", "psi"},
        {"UnitSprings", "lbs/in"},
        {"GeminiApiKey", ""},
        {"AiProvider", "Gemini"},
        {"AiModel", "3.1-flash"},
        {"AiApiKey", ""},
        {"AiCustomUrl", ""}
    }

    Sub New()
        Try
            Dim loaded = SettingsPersister.Load()
            For Each kvp In loaded
                Values(kvp.Key) = kvp.Value
            Next
        Catch
        End Try
        
        Try
            Dim legacyKey As String = [Get]("GeminiApiKey")
            Dim newKey As String = [Get]("AiApiKey")
            If Not String.IsNullOrEmpty(legacyKey) AndAlso String.IsNullOrEmpty(newKey) Then
                [Set]("AiApiKey", legacyKey)
                [Set]("AiProvider", "Gemini")
                [Set]("AiModel", "3.1-flash")
            End If
        Catch
        End Try
    End Sub

    Public Function [Get](Of T)(key As String) As T
        Dim value As Object = Nothing
        If Values.TryGetValue(key, value) Then
            If TypeOf value Is T Then Return DirectCast(value, T)
            Return CType(Convert.ChangeType(value, GetType(T)), T)
        End If
        Return Nothing
    End Function

    Public Function [Get](Of T)(key As String, defaultValue As T) As T
        Dim value As Object = Nothing
        If Values.TryGetValue(key, value) Then
            If TypeOf value Is T Then Return DirectCast(value, T)
            Return CType(Convert.ChangeType(value, GetType(T)), T)
        End If
        Return defaultValue
    End Function

    Public Function [Get](key As String) As String
        Dim value As Object = Nothing
        If Values.TryGetValue(key, value) AndAlso value IsNot Nothing Then Return value.ToString()
        Return ""
    End Function

    Public Sub [Set](key As String, value As Object)
        Values(key) = value
        Try
            SettingsPersister.Save(Values)
        Catch
        End Try
    End Sub

    Public Sub SetSafe(key As String, value As Object)
        [Set](key, value)
    End Sub

    Public Sub Reset(key As String)
        Values.Remove(key)
        Try
            SettingsPersister.Save(Values)
        Catch
        End Try
    End Sub

End Module

Public Class SettingService

    Public Shared Function GetKey(obj As DependencyObject) As String
        If obj Is Nothing Then Return Nothing
        Return CStr(obj.GetValue(KeyProperty))
    End Function

    Public Shared Sub SetKey(obj As DependencyObject, value As String)
        obj.SetValue(KeyProperty, value)
    End Sub

    Public Shared ReadOnly KeyProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Key", GetType(String), GetType(SettingService), New PropertyMetadata(Nothing))

    Public Shared Function GetValue(obj As DependencyObject) As String
        If obj Is Nothing Then Return Nothing
        Return CStr(obj.GetValue(ValueProperty))
    End Function

    Public Shared Sub SetValue(obj As DependencyObject, value As String)
        obj.SetValue(ValueProperty, value)
    End Sub

    Public Shared ReadOnly ValueProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Value", GetType(String), GetType(SettingService), New PropertyMetadata(Nothing))

    Public Shared Sub ResetSettings(target As DependencyObject)
        If target Is Nothing Then Return
        For Each child In LogicalTreeHelper.GetChildren(target).OfType(Of DependencyObject)()
            ResetSettings(child)
        Next
        Dim key = GetKey(target)
        If key IsNot Nothing Then Settings.Reset(key)
    End Sub

    Public Shared Sub RefreshSettings(target As DependencyObject)
        If target Is Nothing Then Return
        For Each child In LogicalTreeHelper.GetChildren(target).OfType(Of DependencyObject)()
            RefreshSettings(child)
        Next
        Dim key = GetKey(target)
        If key Is Nothing Then Return
        If TypeOf target Is ISettingControl Then DirectCast(target, ISettingControl).RefreshSetting(Settings.Get(key))
    End Sub

    Public Shared Sub SaveSetting(sender As Object)
        If AniControlEnabled <> 0 OrElse TypeOf sender IsNot ISettingControl Then Return
        Dim key = GetKey(TryCast(sender, DependencyObject))
        If key Is Nothing Then Return
        Dim value = DirectCast(sender, ISettingControl).GetCurrentSetting()
        If value IsNot Nothing Then Settings.Set(key, value)
    End Sub

End Class

Public Interface ISettingControl

    Sub RefreshSetting(NewValue As String)

    Function GetCurrentSetting() As String

End Interface


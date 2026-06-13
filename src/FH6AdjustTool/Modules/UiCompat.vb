Imports System.Runtime.CompilerServices
Imports System.Windows.Markup

<ContentProperty("Events")>
Public Class CustomEventCollection
    Implements IEnumerable(Of CustomEvent)

    Private ReadOnly Items As New List(Of CustomEvent)

    Public ReadOnly Property Events As List(Of CustomEvent)
        Get
            Return Items
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator(Of CustomEvent) Implements IEnumerable(Of CustomEvent).GetEnumerator
        Return Items.GetEnumerator()
    End Function

    Private Function GetUntypedEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Items.GetEnumerator()
    End Function
End Class

Public Class CustomEventService
    Public Shared ReadOnly EventsProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("Events", GetType(CustomEventCollection), GetType(CustomEventService), New PropertyMetadata(Nothing))

    Public Shared Sub SetEvents(d As DependencyObject, value As CustomEventCollection)
        d.SetValue(EventsProperty, value)
    End Sub

    Public Shared Function GetEvents(d As DependencyObject) As CustomEventCollection
        Dim value = TryCast(d.GetValue(EventsProperty), CustomEventCollection)
        If value Is Nothing Then
            value = New CustomEventCollection()
            d.SetValue(EventsProperty, value)
        End If
        Return value
    End Function

    Public Shared ReadOnly EventTypeProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("EventType", GetType(CustomEvent.EventType), GetType(CustomEventService), New PropertyMetadata(CustomEvent.EventType.None))

    Public Shared Sub SetEventType(d As DependencyObject, value As CustomEvent.EventType)
        d.SetValue(EventTypeProperty, value)
    End Sub

    Public Shared Function GetEventType(d As DependencyObject) As CustomEvent.EventType
        Return CType(d.GetValue(EventTypeProperty), CustomEvent.EventType)
    End Function

    Public Shared ReadOnly EventDataProperty As DependencyProperty =
        DependencyProperty.RegisterAttached("EventData", GetType(String), GetType(CustomEventService), New PropertyMetadata(Nothing))

    Public Shared Sub SetEventData(d As DependencyObject, value As String)
        d.SetValue(EventDataProperty, value)
    End Sub

    Public Shared Function GetEventData(d As DependencyObject) As String
        Return CStr(d.GetValue(EventDataProperty))
    End Function
End Class

Public Class CustomEvent
    Inherits DependencyObject

    Public Enum EventType
        None = 0
        OpenUrl
        OpenFile
        CopyText
        ShowHint
        ShowMessage
        SetSetting
        打开帮助
    End Enum

    Public Property Type As EventType
    Public Property Data As String

    Public Sub New()
    End Sub

    Public Sub New(type As EventType, data As String)
        Me.Type = type
        Me.Data = data
    End Sub

    Public Sub Raise()
        Select Case Type
            Case EventType.CopyText
                Clipboard.SetText(If(Data, ""))
            Case EventType.ShowHint
                Hint(If(Data, ""))
            Case EventType.ShowMessage
                MyMsgBox(If(Data, ""))
            Case EventType.SetSetting
                Dim parts = If(Data, "").Split("|"c)
                If parts.Length >= 2 Then Settings.SetSafe(parts(0), parts(1))
        End Select
    End Sub
End Class

Public Module UiCompat
    <Extension>
    Public Sub RaiseCustomEvent(control As DependencyObject)
        If control Is Nothing Then Return
        Dim events = CustomEventService.GetEvents(control).ToList()
        Dim eventType = CustomEventService.GetEventType(control)
        If eventType <> CustomEvent.EventType.None Then events.Add(New CustomEvent(eventType, CustomEventService.GetEventData(control)))
        For Each item In events
            item.Raise()
        Next
    End Sub

    Public Sub RaiseCustomEvent()
    End Sub
End Module

Public Module ModAnimation

    Public AniControlEnabled As Integer
    Public AniSpeed As Double = 1

    Public Enum AniEasePower
        Weak = 0
        Middle = 1
        Strong = 2
        ExtraStrong = 3
    End Enum

    Public Class AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle)
        End Sub
    End Class

    Public Class AniEaseLinear
        Inherits AniEase
    End Class
    Public Class AniEaseInFluent
        Inherits AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle, Optional scale As Double = 1)
        End Sub
    End Class
    Public Class AniEaseOutFluent
        Inherits AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle, Optional scale As Double = 1)
        End Sub
    End Class
    Public Class AniEaseInoutFluent
        Inherits AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle, Optional scale As Double = 1)
        End Sub
    End Class
    Public Class AniEaseInBack
        Inherits AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle)
        End Sub
    End Class
    Public Class AniEaseOutBack
        Inherits AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle)
        End Sub
    End Class
    Public Class AniEaseInout
        Inherits AniEase
    End Class
    Public Class AniEaseOutElastic
        Inherits AniEase
        Public Sub New(Optional power As AniEasePower = AniEasePower.Middle)
        End Sub
    End Class
    Public Class AniEaseOutFluentWithInitial
        Inherits AniEase
        Public Sub New(initialSpeed As Double, duration As Double, distance As Double)
        End Sub
    End Class

    Public Class AniData
        Public Property Apply As Action
    End Class

    Public Sub AniStart()
    End Sub

    Public Sub AniStart(data As AniData, Optional name As String = "", Optional refreshTime As Boolean = False)
        data?.Apply?.Invoke()
    End Sub

    Public Sub AniStart(data As IEnumerable(Of AniData), Optional name As String = "", Optional refreshTime As Boolean = False)
        If data Is Nothing Then Return
        For Each item In data
            item?.Apply?.Invoke()
        Next
    End Sub

    Public Sub AniStop(name As String)
    End Sub

    Public Function AniIsRun(name As String) As Boolean
        Return False
    End Function

    Public Function AaCode(action As Action, Optional delay As Integer = 0, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = action}
    End Function

    Public Function AaOpacity(obj As UIElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Opacity = Math.Max(0, Math.Min(1, obj.Opacity + value))}
    End Function

    Public Function AaWidth(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Width = Math.Max(0, If(Double.IsNaN(obj.Width), obj.ActualWidth, obj.Width) + value)}
    End Function

    Public Function AaHeight(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Height = Math.Max(0, If(Double.IsNaN(obj.Height), obj.ActualHeight, obj.Height) + value)}
    End Function

    Public Function AaX(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Margin = New Thickness(obj.Margin.Left + value, obj.Margin.Top, obj.Margin.Right - value, obj.Margin.Bottom)}
    End Function

    Public Function AaY(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Margin = New Thickness(obj.Margin.Left, obj.Margin.Top + value, obj.Margin.Right, obj.Margin.Bottom - value)}
    End Function

    Public Function AaTranslateX(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            If Not TypeOf obj.RenderTransform Is TranslateTransform Then obj.RenderTransform = New TranslateTransform()
            DirectCast(obj.RenderTransform, TranslateTransform).X += value
        End Sub}
    End Function

    Public Function AaTranslateY(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            If Not TypeOf obj.RenderTransform Is TranslateTransform Then obj.RenderTransform = New TranslateTransform()
            DirectCast(obj.RenderTransform, TranslateTransform).Y += value
        End Sub}
    End Function

    Public Function AaScaleTransform(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            If Not TypeOf obj.RenderTransform Is ScaleTransform Then obj.RenderTransform = New ScaleTransform(1, 1)
            Dim scale = DirectCast(obj.RenderTransform, ScaleTransform)
            scale.ScaleX += value
            scale.ScaleY += value
        End Sub}
    End Function

    Public Function AaRotateTransform(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            If Not TypeOf obj.RenderTransform Is RotateTransform Then obj.RenderTransform = New RotateTransform()
            DirectCast(obj.RenderTransform, RotateTransform).Angle += value
        End Sub}
    End Function

    Public Function AaScale(obj As FrameworkElement, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False, Optional absolute As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            obj.Width = Math.Max(0, If(Double.IsNaN(obj.Width), obj.ActualWidth, obj.Width) + value)
            obj.Height = Math.Max(0, If(Double.IsNaN(obj.Height), obj.ActualHeight, obj.Height) + value)
        End Sub}
    End Function

    Public Function AaColor(obj As DependencyObject, prop As DependencyProperty, target As Object, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            If TypeOf target Is String Then
                DirectCast(obj, FrameworkElement).SetResourceReference(prop, CStr(target))
            ElseIf prop.PropertyType Is GetType(Color) Then
                obj.SetValue(prop, CType(New MyColor(CType(obj.GetValue(prop), Color)) + DirectCast(target, MyColor), Color))
            ElseIf GetType(Brush).IsAssignableFrom(prop.PropertyType) Then
                obj.SetValue(prop, CType(New MyColor(CType(obj.GetValue(prop), Brush)) + DirectCast(target, MyColor), SolidColorBrush))
            Else
                obj.SetValue(prop, target)
            End If
        End Sub}
    End Function

    Public Function AaDouble(action As Action(Of Double), value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() action(value)}
    End Function

    Public Function AaValue(obj As Object, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Value += value}
    End Function

    Public Function AaRadius(obj As Object, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.Radius += value}
    End Function

    Public Function AaStrokeThickness(obj As Object, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.StrokeThickness = Math.Max(0, obj.StrokeThickness + value)}
    End Function

    Public Function AaBorderThickness(obj As Control, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() obj.BorderThickness = New Thickness(Math.Max(0, obj.BorderThickness.Left + value))}
    End Function

    Public Function AaGridLengthWidth(column As ColumnDefinition, value As Double, Optional time As Integer = 0, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub() column.Width = New GridLength(Math.Max(0, column.Width.Value + value))}
    End Function

    Public Function AaTextAppear(obj As Object, Optional hide As Boolean = False, Optional timePerText As Boolean = True, Optional time As Integer = 70, Optional delay As Integer = 0, Optional ease As AniEase = Nothing, Optional after As Boolean = False) As AniData
        Return New AniData With {.Apply = Sub()
            If TypeOf obj Is UIElement Then DirectCast(obj, UIElement).Opacity = If(hide, 0, 1)
        End Sub}
    End Function

    Public Function AaStack(stack As StackPanel, Optional time As Integer = 100, Optional delay As Integer = 25) As List(Of AniData)
        Return stack.Children.OfType(Of UIElement)().Select(Function(child) AaOpacity(child, 1 - child.Opacity)).ToList()
    End Function

End Module

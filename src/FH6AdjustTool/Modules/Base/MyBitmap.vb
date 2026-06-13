Imports System.Drawing.Imaging

Public Class MyBitmap
    Public Property Pic As System.Drawing.Bitmap

    Public Sub New()
        Pic = New System.Drawing.Bitmap(1, 1)
    End Sub

    Public Sub New(filePathOrResourceName As String)
        Dim source = CType((New ImageSourceConverter()).ConvertFromString(filePathOrResourceName), ImageSource)
        Pic = FromImageSource(source)
    End Sub

    Public Sub New(image As ImageSource)
        Pic = FromImageSource(image)
    End Sub

    Public Sub New(image As System.Drawing.Image)
        Pic = New System.Drawing.Bitmap(image)
    End Sub

    Public Sub New(bitmap As System.Drawing.Bitmap)
        Pic = bitmap
    End Sub

    Public Sub New(brush As ImageBrush)
        Me.New(brush.ImageSource)
    End Sub

    Public Shared Widening Operator CType(image As System.Drawing.Image) As MyBitmap
        If image Is Nothing Then Return Nothing
        Return New MyBitmap(image)
    End Operator

    Public Shared Widening Operator CType(image As MyBitmap) As System.Drawing.Image
        Return image?.Pic
    End Operator

    Public Shared Widening Operator CType(image As ImageSource) As MyBitmap
        If image Is Nothing Then Return Nothing
        Return New MyBitmap(image)
    End Operator

    Public Shared Widening Operator CType(image As MyBitmap) As ImageSource
        If image Is Nothing OrElse image.Pic Is Nothing Then Return Nothing
        Dim rect = New System.Drawing.Rectangle(0, 0, image.Pic.Width, image.Pic.Height)
        Dim data = image.Pic.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb)
        Try
            Dim result = BitmapSource.Create(image.Pic.Width, image.Pic.Height, image.Pic.HorizontalResolution, image.Pic.VerticalResolution,
                                             PixelFormats.Bgra32, Nothing, data.Scan0, rect.Width * rect.Height * 4, data.Stride)
            result.Freeze()
            Return result
        Finally
            image.Pic.UnlockBits(data)
        End Try
    End Operator

    Public Shared Widening Operator CType(bitmap As System.Drawing.Bitmap) As MyBitmap
        If bitmap Is Nothing Then Return Nothing
        Return New MyBitmap(bitmap)
    End Operator

    Public Shared Widening Operator CType(image As MyBitmap) As System.Drawing.Bitmap
        Return image?.Pic
    End Operator

    Public Shared Widening Operator CType(brush As ImageBrush) As MyBitmap
        If brush Is Nothing Then Return Nothing
        Return New MyBitmap(brush)
    End Operator

    Public Shared Widening Operator CType(image As MyBitmap) As ImageBrush
        If image Is Nothing Then Return Nothing
        Return New ImageBrush(CType(image, ImageSource))
    End Operator

    Private Shared Function FromImageSource(source As ImageSource) As System.Drawing.Bitmap
        Using stream As New MemoryStream()
            Dim encoder As New PngBitmapEncoder()
            encoder.Frames.Add(BitmapFrame.Create(CType(source, BitmapSource)))
            encoder.Save(stream)
            stream.Position = 0
            Return New System.Drawing.Bitmap(stream)
        End Using
    End Function

    Public Function Clip(x As Integer, y As Integer, width As Integer, height As Integer) As MyBitmap
        Dim bitmap As New System.Drawing.Bitmap(width, height, Pic.PixelFormat)
        Using graphics = System.Drawing.Graphics.FromImage(bitmap)
            graphics.DrawImage(Pic, New System.Drawing.Rectangle(0, 0, width, height), New System.Drawing.Rectangle(x, y, width, height), System.Drawing.GraphicsUnit.Pixel)
        End Using
        Return bitmap
    End Function

    Public Function RotateFlip(type As System.Drawing.RotateFlipType) As MyBitmap
        Dim bitmap As New System.Drawing.Bitmap(Pic)
        bitmap.RotateFlip(type)
        Return bitmap
    End Function
End Class

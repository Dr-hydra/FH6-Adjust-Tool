Imports System.Globalization
Imports System.Runtime.CompilerServices

Public Module ModBase

    Public Const VersionBaseName As String = "2.0.1"
    Public Const VersionCode As Integer = 201
    Public Const VersionDisplay As String = "v2.0.1"
    Public Const CommitHash As String = ""
    Public Const BuildTypeDisplay As String = "UI Kit"
    Public Const VersionBranchMain As String = "main"

    Public Enum BuildTypes
        Debug = 100
        Release = 50
        Snapshot = 0
    End Enum

    Public Const BuildType As BuildTypes = BuildTypes.Release
    Public ReadOnly Property ModeDebug As Boolean
        Get
            Return Settings.Get(Of Boolean)("SystemDebugMode")
        End Get
    End Property

    Public Enum ProcessReturnValues
        Success = 0
        Cancel = 1
        Exception = 2
        Fail = 3
        TaskDone = 4
    End Enum

    Public Handle As IntPtr
    Public PathExe As String = If(Environment.ProcessPath, AppDomain.CurrentDomain.BaseDirectory & AppDomain.CurrentDomain.FriendlyName)
    Public PathExeFolder As String = AppDomain.CurrentDomain.BaseDirectory.TrimEnd("\"c) & "\"
    Public PathImage As String = "pack://application:,,,/QING.UIKIT;component/Images/"
    Public PathTemp As String = IO.Path.Combine(IO.Path.GetTempPath(), "QING.UIKIT") & "\"
    Public PathAppdata As String = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QING.UIKIT") & "\"
    Public PathPure As String = PathTemp
    Public Lang As String = "zh_CN"
    Public ApplicationStartTick As Long = GetTimeMs()
    Public ApplicationOpenTime As Date = Date.Now
    Public Identify As String = Guid.NewGuid().ToString("N").Substring(0, 8)
    Public IsProgramEnding As Boolean = False
    Public Is32BitSystem As Boolean = Not Environment.Is64BitOperatingSystem
    Public IsGBKEncoding As Boolean = Encoding.Default.CodePage = 936
    Public OsDrive As String = IO.Path.GetPathRoot(Environment.SystemDirectory)
    Public ReadOnly DPI As Integer = CInt(System.Drawing.Graphics.FromHwnd(IntPtr.Zero).DpiX)
    Public DragControl As Object

    Private UuidCounter As Integer
    Private ReadOnly RandomSource As New Random()

    Public Function GetUuid() As Integer
        Return Threading.Interlocked.Increment(UuidCounter)
    End Function

    Public Function GetTimeMs() As Long
        Return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    End Function

    Public Function RandomInteger(min As Integer, max As Integer) As Integer
        SyncLock RandomSource
            Return RandomSource.Next(min, max + 1)
        End SyncLock
    End Function

    Public Function GetWPFSize(PixelSize As Double) As Double
        Return PixelSize * 96 / DPI
    End Function

    Public Function GetPixelSize(WPFSize As Double) As Double
        Return WPFSize / 96 * DPI
    End Function

    Public Function Val(Str As Object) As Double
        If Str Is Nothing Then Return 0
        Return Conversion.Val(Str.ToString())
    End Function

    Public Sub RunInUiWait(Action As Action)
        If Application.Current Is Nothing OrElse Application.Current.Dispatcher.CheckAccess() Then
            Action()
        Else
            Application.Current.Dispatcher.Invoke(Action)
        End If
    End Sub

    Public Function RunInUiWait(Of T)(Action As Func(Of T)) As T
        If Application.Current Is Nothing OrElse Application.Current.Dispatcher.CheckAccess() Then
            Return Action()
        End If
        Return Application.Current.Dispatcher.Invoke(Action)
    End Function

    Public Sub RunInUi(Action As Action, Optional ForceWaitUntilLoaded As Boolean = False)
        If Application.Current Is Nothing OrElse Application.Current.Dispatcher.CheckAccess() Then
            Action()
        ElseIf ForceWaitUntilLoaded Then
            Application.Current.Dispatcher.Invoke(Action)
        Else
            Application.Current.Dispatcher.BeginInvoke(Action)
        End If
    End Sub

    Public Function RunInUi() As Boolean
        Return Application.Current Is Nothing OrElse Application.Current.Dispatcher.CheckAccess()
    End Function

    Public Sub RunInThread(Action As Action)
        Dim thread As New Threading.Thread(Sub() Action()) With {.IsBackground = True}
        thread.Start()
    End Sub

    Public Function RunInNewThread(Action As Action, Optional Name As String = Nothing, Optional Priority As Threading.ThreadPriority = Threading.ThreadPriority.Normal) As Threading.Thread
        Dim thread As New Threading.Thread(Sub()
            Try
                Action()
            Catch ex As Exception
                Logger.Warn(ex, "Background action failed")
            End Try
        End Sub)
        thread.IsBackground = True
        If Not String.IsNullOrWhiteSpace(Name) Then thread.Name = Name
        thread.Priority = Priority
        thread.Start()
        Return thread
    End Function

    Public Function CTypeDynamic(Value As Object, TargetType As Type) As Object
        If Value Is Nothing Then Return Nothing
        If TargetType.IsEnum Then Return [Enum].Parse(TargetType, Value.ToString(), True)
        Return Convert.ChangeType(Value, TargetType, CultureInfo.InvariantCulture)
    End Function

    Public Sub OpenWebsite(Url As String)
        Process.Start(New ProcessStartInfo(Url) With {.UseShellExecute = True})
    End Sub

    Public Sub OpenExplorer(Path As String)
        Process.Start(New ProcessStartInfo(Path) With {.UseShellExecute = True})
    End Sub

    Public Sub NetDownloadByLoader(urls As IEnumerable(Of String), targetPath As String, Optional SimulateBrowserHeaders As Boolean = False)
        Directory.CreateDirectory(IO.Path.GetDirectoryName(targetPath))
        Using client As New HttpClient()
            If SimulateBrowserHeaders Then client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 QING.UIKIT")
            Dim lastError As Exception = Nothing
            For Each url In urls
                Try
                    Dim bytes = client.GetByteArrayAsync(url).GetAwaiter().GetResult()
                    File.WriteAllBytes(targetPath, bytes)
                    Return
                Catch ex As Exception
                    lastError = ex
                End Try
            Next
            Throw New IOException("Download failed.", lastError)
        End Using
    End Sub

    Public Function ReadReg(Key As String, Optional DefaultValue As String = "") As String
        Return DefaultValue
    End Function

    Public Sub WriteReg(Key As String, Value As String)
    End Sub

    Public Function ReadIni(Section As String, Key As String, Optional DefaultValue As String = "") As String
        Return DefaultValue
    End Function

    Public Sub WriteIni(Section As String, Key As String, Value As String)
    End Sub

    Public Sub DeleteIniKey(Section As String, Key As String)
    End Sub

    Public Function HasIniKey(Section As String, Key As String) As Boolean
        Return False
    End Function

    Public Function HasReg(Key As String) As Boolean
        Return False
    End Function

    Public Sub CheckPermissionWithException(Folder As String)
        Directory.CreateDirectory(Folder)
    End Sub

    Public Function CheckPermission(Folder As String) As Boolean
        Try
            Directory.CreateDirectory(Folder)
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Sub ExtractResources(FilePath As String, ResourceName As String)
    End Sub

    Public Class Logo
        Public Const IconButtonSetup As String = "M960 594.385l-105.792-30.827c-13.824 38.656-31.573 75.264-52.736 109.568l66.56 88.064-94.208 94.208-88.064-66.56c-34.304 21.163-70.912 38.912-109.568 52.736L527.616 960H396.384l-30.827-105.792c-38.656-13.824-75.264-31.573-109.568-52.736l-88.064 66.56-94.208-94.208 66.56-88.064c-21.163-34.304-38.912-70.912-52.736-109.568L64 527.616V396.384l105.792-30.827c13.824-38.656 31.573-75.264 52.736-109.568l-66.56-88.064 94.208-94.208 88.064 66.56c34.304-21.163 70.912-38.912 109.568-52.736L396.384 64h131.232l30.827 105.792c38.656 13.824 75.264 31.573 109.568 52.736l88.064-66.56 94.208 94.208-66.56 88.064c21.163 34.304 38.912 70.912 52.736 109.568L960 396.384v131.232zM512 307.2c-113.109 0-204.8 91.691-204.8 204.8s91.691 204.8 204.8 204.8 204.8-91.691 204.8-204.8-91.691-204.8-204.8-204.8z"
        Public Const IconButtonPin As String = "M16 12V4h1V2H7v2h1v8l-2 2v2h5.2v6h1.6v-6H18v-2l-2-2z"
    End Class

    Public Class MyColor
        Public A As Double = 255
        Public R As Double
        Public G As Double
        Public B As Double

        Public Sub New()
        End Sub

        Public Sub New(value As String)
            Me.New(CType(ColorConverter.ConvertFromString(value), Color))
        End Sub

        Public Sub New(gray As Double)
            Me.New(255, gray, gray, gray)
        End Sub

        Public Sub New(r As Double, g As Double, b As Double)
            Me.New(255, r, g, b)
        End Sub

        Public Sub New(a As Double, r As Double, g As Double, b As Double)
            Me.A = a
            Me.R = r
            Me.G = g
            Me.B = b
        End Sub

        Public Sub New(a As Double, color As MyColor)
            Me.New(a, color.R, color.G, color.B)
        End Sub

        Public Sub New(color As Color)
            Me.New(color.A, color.R, color.G, color.B)
        End Sub

        Public Sub New(brush As Brush)
            If TypeOf brush Is SolidColorBrush Then
                Dim c = DirectCast(brush, SolidColorBrush).Color
                A = c.A : R = c.R : G = c.G : B = c.B
            End If
        End Sub

        Public Shared Widening Operator CType(str As String) As MyColor
            Return New MyColor(CType(ColorConverter.ConvertFromString(str), Color))
        End Operator

        Public Shared Widening Operator CType(color As Color) As MyColor
            Return New MyColor(color)
        End Operator

        Public Shared Widening Operator CType(color As MyColor) As Color
            Return System.Windows.Media.Color.FromArgb(ToByte(color.A), ToByte(color.R), ToByte(color.G), ToByte(color.B))
        End Operator

        Public Shared Widening Operator CType(brush As Brush) As MyColor
            Return New MyColor(brush)
        End Operator

        Public Shared Widening Operator CType(color As MyColor) As SolidColorBrush
            Return New SolidColorBrush(CType(color, Color))
        End Operator

        Public Shared Widening Operator CType(color As MyColor) As Brush
            Return New SolidColorBrush(CType(color, Color))
        End Operator

        Public Shared Operator +(a As MyColor, b As MyColor) As MyColor
            Return New MyColor(a.A + b.A, a.R + b.R, a.G + b.G, a.B + b.B)
        End Operator

        Public Shared Operator -(a As MyColor, b As MyColor) As MyColor
            Return New MyColor(a.A - b.A, a.R - b.R, a.G - b.G, a.B - b.B)
        End Operator

        Public Shared Operator *(a As MyColor, value As Double) As MyColor
            Return New MyColor(a.A * value, a.R * value, a.G * value, a.B * value)
        End Operator

        Public Function FromHSL2(h As Double, s As Double, l As Double) As MyColor
            s /= 100
            l /= 100
            Dim c = (1 - Math.Abs(2 * l - 1)) * s
            Dim x = c * (1 - Math.Abs((h / 60) Mod 2 - 1))
            Dim m = l - c / 2
            Dim rr As Double, gg As Double, bb As Double
            Select Case CInt(Math.Floor(h / 60)) Mod 6
                Case 0 : rr = c : gg = x
                Case 1 : rr = x : gg = c
                Case 2 : gg = c : bb = x
                Case 3 : gg = x : bb = c
                Case 4 : rr = x : bb = c
                Case Else : rr = c : bb = x
            End Select
            Return New MyColor(255, (rr + m) * 255, (gg + m) * 255, (bb + m) * 255)
        End Function

        Private Shared Function ToByte(value As Double) As Byte
            Return CByte(Math.Max(0, Math.Min(255, Math.Round(value))))
        End Function
    End Class

    Public NotInheritable Class RouteEventArgs
        Inherits EventArgs

        Public Property RaiseByMouse As Boolean
        Public Property Handled As Boolean

        Public Sub New(Optional raiseByMouse As Boolean = False)
            Me.RaiseByMouse = raiseByMouse
        End Sub
    End Class

    <Extension>
    Public Function IsInstanceOfGenericType(genericType As Type, obj As Object) As Boolean
        Dim type = If(TryCast(obj, Type), obj?.GetType())
        While type IsNot Nothing
            If type.IsGenericType AndAlso type.GetGenericTypeDefinition() Is genericType Then Return True
            type = type.BaseType
        End While
        Return False
    End Function

End Module

Public Class Logo
    Public Const IconButtonSetup As String = ModBase.Logo.IconButtonSetup
    Public Const IconButtonPin As String = ModBase.Logo.IconButtonPin
End Class

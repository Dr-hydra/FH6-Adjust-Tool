Public Module ModLoader

    Public Enum LoadState
        Waiting
        Loading
        Finished
        Failed
        Aborted
        Interrupted
    End Enum

    Public MustInherit Class LoaderBase
        Implements ILoadingTrigger

        Public Event OnStateChangedUi(loader As LoaderBase, newState As LoadState, oldState As LoadState)
        Public Event OnStateChangedThread(loader As LoaderBase, newState As LoadState, oldState As LoadState)
        Public Event PreviewFinish()
        Public HasOnStateChangedThread As Boolean
        Public Name As String = "Loader"
        Public [Error] As Exception
        Public IsAborted As Boolean
        Public ReadOnly Property IsLoader As Boolean = True Implements ILoadingTrigger.IsLoader

        Private _State As LoadState = LoadState.Waiting
        Public Property State As LoadState
            Get
                Return _State
            End Get
            Set(value As LoadState)
                Dim old = _State
                _State = value
                RaiseEvent OnStateChangedUi(Me, value, old)
                RaiseEvent LoadingStateChanged(LoadingState, LoadingState)
            End Set
        End Property

        Public Property LoadingState As MyLoading.MyLoadingState Implements ILoadingTrigger.LoadingState
            Get
                Select Case State
                    Case LoadState.Loading
                        Return MyLoading.MyLoadingState.Run
                    Case LoadState.Failed
                        Return MyLoading.MyLoadingState.Error
                    Case Else
                        Return MyLoading.MyLoadingState.Stop
                End Select
            End Get
            Set(value As MyLoading.MyLoadingState)
            End Set
        End Property

        Public Property Progress As Double
        Public Event LoadingStateChanged(newState As MyLoading.MyLoadingState, oldState As MyLoading.MyLoadingState) Implements ILoadingTrigger.LoadingStateChanged
        Public Event ProgressChanged(newProgress As Double, oldProgress As Double) Implements ILoadingTrigger.ProgressChanged

        Public Overridable Function StartGetInput(input As Object, inputInvoke As [Delegate]) As Object
            If input IsNot Nothing Then Return input
            If inputInvoke IsNot Nothing Then Return inputInvoke.DynamicInvoke()
            Return Nothing
        End Function

        Public Overridable Sub Start(Optional input As Object = Nothing, Optional IsForceRestart As Boolean = False)
            State = LoadState.Loading
            RaiseEvent PreviewFinish()
            State = LoadState.Finished
        End Sub
    End Class

    Public Class LoaderTask(Of TIn, TOut)
        Inherits LoaderBase

        Private ReadOnly TaskFunc As Func(Of TIn, TOut)

        Public Sub New(name As String, taskFunc As Func(Of TIn, TOut), Optional input As TIn = Nothing)
            Me.Name = name
            Me.TaskFunc = taskFunc
        End Sub
    End Class

    Public Class LoaderCombo(Of TIn)
        Inherits LoaderBase
        Public Sub New(name As String, loaders As IEnumerable(Of LoaderBase), Optional input As TIn = Nothing)
            Me.Name = name
        End Sub
    End Class

End Module

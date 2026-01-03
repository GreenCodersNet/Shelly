' ###  LocalAIEngine.vb - v2.0.3 ###

' ##########################################################
'  Shelly - Local AI Module  
'  Simple single-shot inference with proper Llama-3 chat template
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
' ##########################################################

Imports System.IO
Imports System.Threading
Imports LLama
Imports LLama.Common
Imports LLama.Native

Public Class LocalAIEngine
    Implements IDisposable

    Private _model As LLamaWeights
    Private _modelParams As ModelParams
    Private _isLoaded As Boolean = False
    Private _isDisposed As Boolean = False
    Private _currentGpuLayerCount As Integer = 0
    Private _requestedGpuLayerCount As Integer = 0
    Private _lastLoadWasFallback As Boolean = False
    Private _backendInfo As String = "Unknown"

    Public ReadOnly Property IsModelLoaded As Boolean
        Get
            Return _isLoaded AndAlso _model IsNot Nothing
        End Get
    End Property

    Public ReadOnly Property IsUsingGpu As Boolean
        Get
            Return _currentGpuLayerCount <> 0
        End Get
    End Property

    Public ReadOnly Property CurrentGpuLayerCount As Integer
        Get
            Return _currentGpuLayerCount
        End Get
    End Property

    Public ReadOnly Property RequestedGpuLayerCount As Integer
        Get
            Return _requestedGpuLayerCount
        End Get
    End Property

    Public ReadOnly Property LastLoadWasFallback As Boolean
        Get
            Return _lastLoadWasFallback
        End Get
    End Property

    ''' <summary>
    ''' Returns detailed info about the current backend (CPU/CUDA/etc)
    ''' </summary>
    Public ReadOnly Property BackendInfo As String
        Get
            Return _backendInfo
        End Get
    End Property

    ''' <summary>
    ''' Gets diagnostic string for display in UI
    ''' </summary>
    Public Function GetDiagnosticInfo() As String
        If Not _isLoaded Then Return "No model loaded"

        Dim sb As New Text.StringBuilder()
        sb.AppendLine("=== LLamaSharp Diagnostics ===")
        sb.AppendLine("Requested GPU Layers: " & _requestedGpuLayerCount.ToString())
        sb.AppendLine("Actual GPU Layers: " & _currentGpuLayerCount.ToString())
        sb.AppendLine("Backend: " & _backendInfo)
        sb.AppendLine("Model Loaded: " & _isLoaded.ToString())
        
        ' Try to get native library info
        Try
            Dim nativeInfo = NativeLibraryConfig.All.ToString()
            sb.AppendLine("Native Config: " & nativeInfo)
        Catch
        End Try

        Return sb.ToString()
    End Function

    Public Async Function LoadModelAsync(
        modelPath As String,
        Optional progress As IProgress(Of String) = Nothing,
        Optional useGpu As Boolean = False,
        Optional gpuLayerCount As Integer = -1
    ) As Task(Of Boolean)

        _lastLoadWasFallback = False
        _requestedGpuLayerCount = If(useGpu, gpuLayerCount, 0)

        If String.IsNullOrWhiteSpace(modelPath) OrElse Not File.Exists(modelPath) Then
            progress?.Report("Error: Model file not found.")
            Return False
        End If

        ' Detect backend before loading
        _backendInfo = DetectBackend()
        Debug.WriteLine("[LocalAIEngine] Detected backend: " & _backendInfo)

        If useGpu AndAlso Not _backendInfo.Contains("CUDA") Then
            progress?.Report("CUDA not detected - using CPU")
            useGpu = False
            gpuLayerCount = 0
            _lastLoadWasFallback = True
        End If

        Try
            UnloadModel()

            Dim actualGpuLayers = If(useGpu, gpuLayerCount, 0)
            Dim modeText = If(actualGpuLayers = 0, "CPU", "GPU (" & actualGpuLayers.ToString() & " layers)")
            
            progress?.Report("Loading on " & modeText & "...")
            Debug.WriteLine("[LocalAIEngine] Loading with GpuLayerCount=" & actualGpuLayers.ToString())

            _modelParams = New ModelParams(modelPath) With {
                .ContextSize = 2048,
                .GpuLayerCount = actualGpuLayers
            }

            _model = Await Task.Run(Function() LLamaWeights.LoadFromFile(_modelParams))
            
            _currentGpuLayerCount = actualGpuLayers
            _isLoaded = True
            
            ' Log actual device usage
            Debug.WriteLine("[LocalAIEngine] Model loaded. GpuLayerCount=" & _currentGpuLayerCount.ToString())
            
            progress?.Report("Ready (" & modeText & ")")
            Return True

        Catch ex As Exception
            progress?.Report("Error: " & ex.Message)
            Debug.WriteLine("[LocalAIEngine] Load error: " & ex.ToString())
            UnloadModel()
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Detects which backend is available (CPU, CUDA, etc)
    ''' </summary>
    Private Function DetectBackend() As String
        Try
            ' Check for CUDA DLLs
            Dim cudaDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native")
            Dim hasCudaDll = Directory.Exists(cudaDllPath) AndAlso 
                            (File.Exists(Path.Combine(cudaDllPath, "llama.dll")) OrElse
                             File.Exists(Path.Combine(cudaDllPath, "ggml-cuda.dll")))

            ' Check for cublas/cudart
            Dim hasCublas = File.Exists(Path.Combine(cudaDllPath, "cublas64_12.dll")) OrElse
                           File.Exists(Path.Combine(cudaDllPath, "cublasLt64_12.dll"))
            
            Dim hasCudart = File.Exists(Path.Combine(cudaDllPath, "cudart64_12.dll"))

            If hasCudaDll AndAlso hasCublas AndAlso hasCudart Then
                Return "CUDA 12 (DLLs found)"
            ElseIf hasCudaDll Then
                Return "CUDA (partial - missing cublas/cudart)"
            Else
                ' Check system PATH for CUDA
                Dim cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH")
                If Not String.IsNullOrEmpty(cudaPath) Then
                    Return "CUDA (system install: " & cudaPath & ")"
                End If
                Return "CPU only (no CUDA DLLs found)"
            End If
        Catch ex As Exception
            Return "Detection failed: " & ex.Message
        End Try
    End Function

    Public Async Function GenerateResponseAsync(
        prompt As String,
        Optional systemPrompt As String = Nothing,
        Optional ct As CancellationToken = Nothing,
        Optional maxTokens As Integer = 256
    ) As Task(Of String)

        If Not _isLoaded OrElse _model Is Nothing Then
            Return "[ERROR] Model not loaded."
        End If

        Try
            Dim fullPrompt = BuildLlama3ChatPrompt(prompt, systemPrompt)

            Return Await Task.Run(
                Function()
                    Return InferSimple(fullPrompt, maxTokens, ct)
                End Function
            )

        Catch ex As Exception
            Return "[ERROR] " & ex.Message
        End Try
    End Function

    Private Function BuildLlama3ChatPrompt(userMessage As String, Optional systemPrompt As String = Nothing) As String
        Dim sb As New Text.StringBuilder()

        sb.Append("<|begin_of_text|>")

        If Not String.IsNullOrWhiteSpace(systemPrompt) Then
            sb.Append("<|start_header_id|>system<|end_header_id|>")
            sb.AppendLine()
            sb.AppendLine(systemPrompt)
            sb.Append("<|eot_id|>")
        Else
            sb.Append("<|start_header_id|>system<|end_header_id|>")
            sb.AppendLine()
            sb.AppendLine("You are a helpful AI assistant. Provide clear, concise answers.")
            sb.Append("<|eot_id|>")
        End If

        sb.Append("<|start_header_id|>user<|end_header_id|>")
        sb.AppendLine()
        sb.AppendLine(userMessage)
        sb.Append("<|eot_id|>")

        sb.Append("<|start_header_id|>assistant<|end_header_id|>")
        sb.AppendLine()

        Return sb.ToString()
    End Function

    Private Function InferSimple(prompt As String, maxTokens As Integer, ct As CancellationToken) As String
        Dim result As New Text.StringBuilder()

        Dim executor = New StatelessExecutor(_model, _modelParams)

        Dim inferParams = New InferenceParams() With {
            .MaxTokens = maxTokens,
            .AntiPrompts = New List(Of String) From {
                "<|eot_id|>",
                "<|end_of_text|>",
                "<|start_header_id|>"
            }
        }

        For Each token In executor.InferAsync(prompt, inferParams, ct).ToBlockingEnumerable()
            If ct.IsCancellationRequested Then Exit For
            result.Append(token)
        Next

        Dim output = result.ToString()
        output = output.Replace("<|eot_id|>", "").Replace("<|end_of_text|>", "").Trim()

        Return output
    End Function

    Public Sub UnloadModel()
        Try
            If _model IsNot Nothing Then
                _model.Dispose()
                _model = Nothing
            End If
            _isLoaded = False
            _modelParams = Nothing
        Catch
        End Try
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _isDisposed Then
            If disposing Then UnloadModel()
            _isDisposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

End Class

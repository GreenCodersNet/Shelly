' ###  GpuDetection.vb - v1.0.0 ###

' ##########################################################
'  Shelly - Local AI Module - GPU Detection
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

' PURPOSE: Detects CUDA availability and GPU capabilities
' USAGE: Call before attempting GPU-accelerated inference
' NOTE: This module is ISOLATED - no dependencies on Shelly core

Imports System.IO
Imports System.Management
Imports Microsoft.Win32

''' <summary>
''' Provides GPU and CUDA detection capabilities for Local AI.
''' Implements industry-standard detection methods with graceful fallback.
''' </summary>
Public NotInheritable Class GpuDetection

    ' Cached detection results (lazy initialization)
    Private Shared _detectionPerformed As Boolean = False
    Private Shared _isCudaAvailable As Boolean = False
    Private Shared _isNvidiaGpuPresent As Boolean = False
    Private Shared _gpuName As String = "Unknown"
    Private Shared _vramMB As Long = 0
    Private Shared _cudaVersion As String = "Not installed"
    Private Shared _detectionError As String = Nothing
    Private Shared ReadOnly _lock As New Object()

    ' CUDA download URL
    Public Const CudaDownloadUrl As String = "https://developer.nvidia.com/cuda-downloads"

    ''' <summary>
    ''' Gets whether CUDA runtime is available on this system.
    ''' </summary>
    Public Shared ReadOnly Property IsCudaAvailable As Boolean
        Get
            EnsureDetectionPerformed()
            Return _isCudaAvailable
        End Get
    End Property

    ''' <summary>
    ''' Gets whether an NVIDIA GPU is present (even without CUDA).
    ''' </summary>
    Public Shared ReadOnly Property IsNvidiaGpuPresent As Boolean
        Get
            EnsureDetectionPerformed()
            Return _isNvidiaGpuPresent
        End Get
    End Property

    ''' <summary>
    ''' Gets the detected GPU name.
    ''' </summary>
    Public Shared ReadOnly Property GpuName As String
        Get
            EnsureDetectionPerformed()
            Return _gpuName
        End Get
    End Property

    ''' <summary>
    ''' Gets the detected VRAM in MB.
    ''' </summary>
    Public Shared ReadOnly Property VramMB As Long
        Get
            EnsureDetectionPerformed()
            Return _vramMB
        End Get
    End Property

    ''' <summary>
    ''' Gets the detected CUDA version string.
    ''' </summary>
    Public Shared ReadOnly Property CudaVersion As String
        Get
            EnsureDetectionPerformed()
            Return _cudaVersion
        End Get
    End Property

    ''' <summary>
    ''' Gets any error encountered during detection.
    ''' </summary>
    Public Shared ReadOnly Property DetectionError As String
        Get
            EnsureDetectionPerformed()
            Return _detectionError
        End Get
    End Property

    ''' <summary>
    ''' Gets a user-friendly status message about GPU availability.
    ''' </summary>
    Public Shared ReadOnly Property StatusMessage As String
        Get
            EnsureDetectionPerformed()
            If _isCudaAvailable Then
                Return $"GPU Ready: {_gpuName} ({_vramMB / 1024.0:F1} GB VRAM)"
            ElseIf _isNvidiaGpuPresent Then
                Return $"NVIDIA GPU found ({_gpuName}) but CUDA Toolkit not installed"
            Else
                Return "No NVIDIA GPU detected - CPU mode only"
            End If
        End Get
    End Property

    ''' <summary>
    ''' Performs GPU and CUDA detection. Thread-safe and cached.
    ''' </summary>
    Public Shared Sub EnsureDetectionPerformed()
        If _detectionPerformed Then Return

        SyncLock _lock
            If _detectionPerformed Then Return

            Try
                Debug.WriteLine("[GpuDetection] Starting GPU detection...")

                ' Step 1: Check for NVIDIA GPU via WMI
                DetectNvidiaGpu()

                ' Step 2: Check for CUDA installation
                DetectCudaInstallation()

                ' Step 3: Try to load CUDA runtime DLL (definitive test)
                If _isNvidiaGpuPresent Then
                    TestCudaRuntime()
                End If

                Debug.WriteLine($"[GpuDetection] Detection complete:")
                Debug.WriteLine($"  - NVIDIA GPU: {_isNvidiaGpuPresent} ({_gpuName})")
                Debug.WriteLine($"  - VRAM: {_vramMB} MB")
                Debug.WriteLine($"  - CUDA Version: {_cudaVersion}")
                Debug.WriteLine($"  - CUDA Available: {_isCudaAvailable}")

            Catch ex As Exception
                _detectionError = ex.Message
                Debug.WriteLine($"[GpuDetection] Detection error: {ex.Message}")
            Finally
                _detectionPerformed = True
            End Try
        End SyncLock
    End Sub

    ''' <summary>
    ''' Forces re-detection of GPU capabilities.
    ''' </summary>
    Public Shared Sub RefreshDetection()
        SyncLock _lock
            _detectionPerformed = False
            _isCudaAvailable = False
            _isNvidiaGpuPresent = False
            _gpuName = "Unknown"
            _vramMB = 0
            _cudaVersion = "Not installed"
            _detectionError = Nothing
        End SyncLock
        EnsureDetectionPerformed()
    End Sub

    ''' <summary>
    ''' Detects NVIDIA GPU using WMI (Win32_VideoController).
    ''' </summary>
    Private Shared Sub DetectNvidiaGpu()
        Try
            Using searcher As New ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController")
                For Each obj As ManagementObject In searcher.Get()
                    Dim name = obj("Name")?.ToString()
                    If Not String.IsNullOrEmpty(name) AndAlso name.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        _isNvidiaGpuPresent = True
                        _gpuName = name

                        ' Get VRAM (AdapterRAM is in bytes, may be capped at 4GB due to 32-bit limitation)
                        Dim vramBytes = obj("AdapterRAM")
                        If vramBytes IsNot Nothing Then
                            _vramMB = CLng(vramBytes) \ (1024 * 1024)
                            ' If reported as exactly 4GB or 0, it's likely capped - try registry
                            If _vramMB <= 4096 OrElse _vramMB = 0 Then
                                Dim actualVram = GetVramFromRegistry(name)
                                If actualVram > _vramMB Then
                                    _vramMB = actualVram
                                End If
                            End If
                        End If

                        Debug.WriteLine("[GpuDetection] Found NVIDIA GPU: " & name & ", VRAM: " & _vramMB.ToString() & " MB")
                        Exit For
                    End If
                Next
            End Using
        Catch ex As Exception
            Debug.WriteLine("[GpuDetection] WMI query failed: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Attempts to get accurate VRAM from registry (for GPUs > 4GB).
    ''' </summary>
    Private Shared Function GetVramFromRegistry(gpuName As String) As Long
        Try
            ' Check NVIDIA registry keys for accurate VRAM
            Using key = Registry.LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}")
                If key IsNot Nothing Then
                    For Each subKeyName In key.GetSubKeyNames()
                        If subKeyName.StartsWith("0") Then
                            Using subKey = key.OpenSubKey(subKeyName)
                                Dim driverDesc = subKey?.GetValue("DriverDesc")?.ToString()
                                If driverDesc IsNot Nothing AndAlso driverDesc.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 Then
                                    ' Try to get HardwareInformation.qwMemorySize (64-bit value)
                                    Dim memSize = subKey.GetValue("HardwareInformation.qwMemorySize")
                                    If memSize IsNot Nothing Then
                                        Return CLng(memSize) \ (1024 * 1024)
                                    End If
                                    ' Fallback to AdapterRAM
                                    Dim adapterRam = subKey.GetValue("HardwareInformation.MemorySize")
                                    If adapterRam IsNot Nothing Then
                                        Return CLng(CUInt(adapterRam)) \ (1024 * 1024)
                                    End If
                                End If
                            End Using
                        End If
                    Next
                End If
            End Using
        Catch ex As Exception
            Debug.WriteLine("[GpuDetection] Registry VRAM lookup failed: " & ex.Message)
        End Try
        Return 0
    End Function

    ''' <summary>
    ''' Detects CUDA installation via registry and environment.
    ''' </summary>
    Private Shared Sub DetectCudaInstallation()
        Try
            ' Method 1: Check CUDA_PATH environment variable
            Dim cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH")
            If Not String.IsNullOrEmpty(cudaPath) AndAlso Directory.Exists(cudaPath) Then
                ' Extract version from path (e.g., "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v12.1")
                Dim dirName = Path.GetFileName(cudaPath.TrimEnd(Path.DirectorySeparatorChar))
                If dirName.StartsWith("v") Then
                    _cudaVersion = dirName.Substring(1)
                    Debug.WriteLine("[GpuDetection] CUDA found via CUDA_PATH: v" & _cudaVersion)
                    Return
                End If
            End If

            ' Method 2: Check registry for CUDA installation
            Using key = Registry.LocalMachine.OpenSubKey("SOFTWARE\NVIDIA Corporation\GPU Computing Toolkit\CUDA")
                If key IsNot Nothing Then
                    Dim subKeys = key.GetSubKeyNames()
                    If subKeys.Length > 0 Then
                        ' Get the highest version
                        Dim versions = subKeys.OrderByDescending(Function(v) v).ToArray()
                        _cudaVersion = versions(0)
                        Debug.WriteLine("[GpuDetection] CUDA found via registry: v" & _cudaVersion)
                        Return
                    End If
                End If
            End Using

            ' Method 3: Check common installation paths
            Dim commonPaths = {
                "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA",
                "C:\Program Files (x86)\NVIDIA GPU Computing Toolkit\CUDA"
            }

            For Each basePath In commonPaths
                If Directory.Exists(basePath) Then
                    Dim versionDirs = Directory.GetDirectories(basePath, "v*")
                    If versionDirs.Length > 0 Then
                        Dim latestVersion = versionDirs.OrderByDescending(Function(d) d).First()
                        _cudaVersion = Path.GetFileName(latestVersion).Substring(1)
                        Debug.WriteLine("[GpuDetection] CUDA found via path scan: v" & _cudaVersion)
                        Return
                    End If
                End If
            Next

            Debug.WriteLine("[GpuDetection] CUDA installation not found")

        Catch ex As Exception
            Debug.WriteLine("[GpuDetection] CUDA detection error: " & ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Tests if CUDA runtime is actually functional by checking for DLL.
    ''' </summary>
    Private Shared Sub TestCudaRuntime()
        Try
            ' Check if cudart64_*.dll exists in PATH or system directories
            Dim systemPath = Environment.GetEnvironmentVariable("PATH")
            If String.IsNullOrEmpty(systemPath) Then
                Return
            End If

            Dim paths = systemPath.Split(";"c)
            For Each p In paths
                If String.IsNullOrWhiteSpace(p) Then Continue For
                Try
                    If Directory.Exists(p) Then
                        ' Look for CUDA runtime DLL (cudart64_12.dll, cudart64_110.dll, etc.)
                        Dim cudaDlls = Directory.GetFiles(p, "cudart64_*.dll")
                        If cudaDlls.Length > 0 Then
                            _isCudaAvailable = True
                            Debug.WriteLine($"[GpuDetection] CUDA runtime DLL found: {cudaDlls(0)}")
                            Return
                        End If
                    End If
                Catch
                    ' Skip inaccessible paths
                End Try
            Next

            ' Also check CUDA bin directory directly
            Dim cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH")
            If Not String.IsNullOrEmpty(cudaPath) Then
                Dim binPath = Path.Combine(cudaPath, "bin")
                If Directory.Exists(binPath) Then
                    Dim cudaDlls = Directory.GetFiles(binPath, "cudart64_*.dll")
                    If cudaDlls.Length > 0 Then
                        _isCudaAvailable = True
                        Debug.WriteLine($"[GpuDetection] CUDA runtime DLL found in CUDA_PATH: {cudaDlls(0)}")
                        Return
                    End If
                End If
            End If

            Debug.WriteLine("[GpuDetection] CUDA runtime DLL not found in PATH")

        Catch ex As Exception
            Debug.WriteLine($"[GpuDetection] CUDA runtime test failed: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Calculates recommended GPU layer count based on model size and available VRAM.
    ''' Returns 0 if GPU should not be used, -1 for full GPU, or a specific layer count.
    ''' </summary>
    ''' <param name="modelFileSizeBytes">Size of the GGUF model file in bytes</param>
    ''' <returns>Recommended GpuLayerCount value</returns>
    Public Shared Function GetRecommendedGpuLayers(modelFileSizeBytes As Long) As Integer
        If Not _isCudaAvailable OrElse _vramMB <= 0 Then
            Return 0 ' CPU only
        End If

        ' Estimate model memory requirement (Q4_K_M is roughly 0.5-0.6x file size in memory)
        Dim estimatedModelMB = modelFileSizeBytes \ (1024 * 1024)
        
        ' Reserve ~1GB for CUDA overhead and context
        Dim availableVramMB = _vramMB - 1024

        If availableVramMB <= 0 Then
            Return 0 ' Not enough VRAM
        End If

        ' If model fits comfortably in VRAM, use all GPU layers
        If estimatedModelMB < availableVramMB * 0.8 Then
            Return -1 ' All layers on GPU
        End If

        ' Calculate approximate layers that can fit
        ' Typical models have 32-80 layers, each taking roughly equal VRAM
        Dim approximateLayers = 40 ' Default assumption
        Dim layerSizeMB = estimatedModelMB / approximateLayers
        Dim recommendedLayers = CInt(availableVramMB / layerSizeMB)

        ' Return at least 1 layer if any VRAM available, capped at reasonable max
        Return Math.Max(1, Math.Min(recommendedLayers, 80))
    End Function

    ''' <summary>
    ''' Gets a user-friendly message explaining why GPU is not available.
    ''' </summary>
    Public Shared Function GetGpuUnavailableReason() As String
        EnsureDetectionPerformed()

        If Not _isNvidiaGpuPresent Then
            Return "No NVIDIA GPU detected. GPU acceleration requires an NVIDIA graphics card." & vbCrLf & vbCrLf &
                   "CPU mode will be used instead."
        End If

        If Not _isCudaAvailable Then
            Return $"NVIDIA GPU detected ({_gpuName}), but CUDA Toolkit is not installed." & vbCrLf & vbCrLf &
                   "To enable GPU acceleration:" & vbCrLf &
                   "1. Download CUDA Toolkit 12.x from nvidia.com/cuda-downloads" & vbCrLf &
                   "2. Install and restart the application" & vbCrLf & vbCrLf &
                   "CPU mode will be used for now."
        End If

        Return String.Empty ' GPU is available
    End Function

    ''' <summary>
    ''' Determines if model file size is suitable for GPU acceleration given available VRAM.
    ''' </summary>
    Public Shared Function CanModelFitInVram(modelFileSizeBytes As Long) As Boolean
        If Not _isCudaAvailable Then Return False
        
        Dim estimatedModelMB = modelFileSizeBytes \ (1024 * 1024)
        Dim availableVramMB = _vramMB - 512 ' Reserve 512MB for overhead
        
        Return estimatedModelMB < availableVramMB
    End Function

End Class

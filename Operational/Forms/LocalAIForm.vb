Imports System.IO
Imports Operational.Ops

Public Class LocalAIForm
    Private ReadOnly _aiEngine As New LocalAIEngine()
    Private ReadOnly _ttsEngine As New PiperTTSEngine()
    Private _isLoadingModel As Boolean = False
    Private _ttsInitialized As Boolean = False

    Private Async Sub LocalAIForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

        InitializePersistedState()
        InitializeGpuSelection()
        InitializeTTSCheckbox()
        InitializeVoices()
        Await InitializeModelsAsync()
    End Sub

    Private Sub InitializePersistedState()
        Globals.LoadLocalAISettings()
    End Sub

    Private Sub InitializeGpuSelection()
        ' Refresh GPU detection ONCE
        Globals.RefreshGpuDetection()

        ' FIX: Set fixed-width text for GPU to prevent overlapping Auto checkbox
        If Globals.LocalAICudaAvailable Then
            UseGPUchekbox.Enabled = True
            UseGPUchekbox.ForeColor = Color.FromArgb(0, 192, 0)
            ' Keep short text to not overlap Auto
            UseGPUchekbox.Text = "GPU"
        Else
            UseGPUchekbox.Enabled = False
            UseGPUchekbox.ForeColor = Color.Gray
            UseGPUchekbox.Text = "GPU"
        End If

        ' Set radio button based on persisted setting
        If Globals.LocalAIUseGpu AndAlso Globals.LocalAICudaAvailable Then
            UseGPUchekbox.Checked = True
        Else
            UseCPU_RAMcheckbox.Checked = True
        End If

        ' FIX: Ensure Auto checkbox is visible
    End Sub

    Private Sub InitializeTTSCheckbox()
        ' Load persisted setting
        Globals.LoadLocalAISettings()
        IncludeTTSCheckbox.Checked = Globals.LocalAIIncludeTTS
        IncludeTTSCheckbox.Text = "Include TTS"
    End Sub

    Private Async Function InitializeModelsAsync() As Task
        localAIModel.Items.Clear()
        For Each path In Globals.LocalAIModelPaths
            localAIModel.Items.Add(path)
        Next

        If Not String.IsNullOrWhiteSpace(Globals.LocalAISelectedModel) AndAlso localAIModel.Items.Contains(Globals.LocalAISelectedModel) Then
            localAIModel.SelectedItem = Globals.LocalAISelectedModel
            Await LoadModelAsync(Globals.LocalAISelectedModel)
        ElseIf localAIModel.Items.Count > 0 Then
            localAIModel.SelectedIndex = 0
            Await LoadModelAsync(localAIModel.SelectedItem.ToString())
        Else
            UpdateStatus("Status: No model selected", Color.Gray)
        End If
    End Function

    Private Sub InitializeVoices()
        voiceSelection.Items.Clear()

        Dim initialized = _ttsEngine.Initialize()
        _ttsInitialized = initialized

        If initialized Then
            Dim voices = _ttsEngine.GetAvailableVoices()
            Globals.LocalAIVoices = voices
            For Each voice In voices
                voiceSelection.Items.Add(voice)
            Next
            If voices.Count > 0 Then
                Dim targetVoice = Globals.LocalAISelectedVoice
                If Not String.IsNullOrWhiteSpace(targetVoice) AndAlso voiceSelection.Items.Contains(targetVoice) Then
                    voiceSelection.SelectedItem = targetVoice
                    _ttsEngine.SetVoice(targetVoice)
                Else
                    voiceSelection.SelectedIndex = 0
                    Globals.LocalAISelectedVoice = voiceSelection.SelectedItem.ToString()
                    _ttsEngine.SetVoice(Globals.LocalAISelectedVoice)
                End If
            Else
                voiceSelection.Items.Add("(No voices found)")
                voiceSelection.SelectedIndex = 0
            End If

            ' ✅ SHARE THE TTS ENGINE WITH GLOBALS
            Globals.SharedTTSEngine = _ttsEngine
            Debug.WriteLine("[LocalAIForm] Shared TTS engine set in Globals")
        Else
            voiceSelection.Items.Add("(Piper not found)")
            voiceSelection.SelectedIndex = 0
        End If

        Globals.SaveLocalAISettings()
    End Sub

    Private Async Function LoadModelAsync(modelPath As String) As Task
        If _isLoadingModel OrElse String.IsNullOrWhiteSpace(modelPath) Then Return
        If Not File.Exists(modelPath) Then
            UpdateStatus("Status: Model file missing", Color.Red)
            Return
        End If

        _isLoadingModel = True
        Dim modeText As String = If(Globals.LocalAIUseGpu AndAlso Globals.LocalAICudaAvailable, "GPU", "CPU")
        UpdateStatus("Status: Loading model (" & modeText & ")...", Color.Blue)

        Dim progress As New Progress(Of String)(Sub(msg) UpdateStatus("Status: " & msg, Color.Blue))

        Dim success = Await _aiEngine.LoadModelAsync(
            modelPath,
            progress,
            Globals.LocalAIUseGpu AndAlso Globals.LocalAICudaAvailable,
            Globals.LocalAIGpuLayerCount
        )

        If success Then
            Dim loadedMode As String = If(_aiEngine.IsUsingGpu, "GPU", "CPU")

            If _aiEngine.LastLoadWasFallback Then
                UpdateStatus("Status: Model loaded (" & loadedMode & ") [fallback]", Color.Orange)
            Else
                UpdateStatus("Status: Model loaded (" & loadedMode & ")", Color.LimeGreen)
            End If

            ' Update diagnostic label
            UpdateGpuDiagnostics()

            Globals.LocalAISelectedModel = modelPath
            Globals.SaveLocalAISettings()

            ' ✅ SHARE THE LOADED ENGINE WITH GLOBALS for use by HandleUserRequest
            Globals.SharedLocalAIEngine = _aiEngine
            Debug.WriteLine("[LocalAIForm] Shared engine set in Globals")
        Else
            UpdateStatus("Status: Load failed", Color.Red)
            LabelAIconfirimation.Text = "Load failed"
            LabelAIconfirimation.ForeColor = Color.Red
        End If

        _isLoadingModel = False
    End Function

    ''' <summary>
    ''' Updates the GPU diagnostic label with current backend info
    ''' </summary>
    Private Sub UpdateGpuDiagnostics()
        If _aiEngine.IsModelLoaded Then
            Dim gpuLayers = _aiEngine.CurrentGpuLayerCount
            Dim backend = _aiEngine.BackendInfo

            If gpuLayers = 0 Then
                LabelAIconfirimation.Text = "CPU Mode | " & backend
                LabelAIconfirimation.ForeColor = Color.Cyan
            ElseIf gpuLayers = -1 Then
                LabelAIconfirimation.Text = "GPU (ALL layers) | " & backend
                LabelAIconfirimation.ForeColor = Color.Lime
            Else
                LabelAIconfirimation.Text = "GPU (" & gpuLayers.ToString() & " layers) | " & backend
                LabelAIconfirimation.ForeColor = Color.Lime
            End If
        Else
            LabelAIconfirimation.Text = "No model loaded"
            LabelAIconfirimation.ForeColor = Color.Gray
        End If
    End Sub

    Private Sub UpdateStatus(message As String, foreColor As Color)
        _lblStatus.Text = message
        _lblStatus.ForeColor = foreColor
    End Sub

    ' +++++++++++++ SMOOTH FORM OPENING - REDESIGN +++++++++++++
    Public Sub ApplySmoothCustomTitleBar(form As Form)
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Me.UpdateStyles()
        Me.SuspendLayout()
        ApplyCustomTitleBar(form)
        Me.ResumeLayout()
        Me.Refresh()
    End Sub

    Private Sub voiceSelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles voiceSelection.SelectedIndexChanged
        If voiceSelection.SelectedItem Is Nothing Then Return
        Dim selectedVoice = voiceSelection.SelectedItem.ToString()
        Globals.LocalAISelectedVoice = selectedVoice
        Globals.SaveLocalAISettings()
        If _ttsInitialized Then
            _ttsEngine.SetVoice(selectedVoice)
        End If
    End Sub

    Private Sub OpenFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles OpenFileDialog1.FileOk
        GlobalApiKey.Text = OpenFileDialog1.FileName
    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click
    End Sub

    Private Sub browseMdoelButton_Click(sender As Object, e As EventArgs) Handles browseMdoelButton.Click
        OpenFileDialog1.Title = "Select GGUF Model File"
        OpenFileDialog1.Filter = "GGUF Models (*.gguf)|*.gguf|All Files (*.*)|*.*"
        OpenFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        OpenFileDialog1.Multiselect = False
        OpenFileDialog1.ShowDialog(Me)
    End Sub

    Private Async Sub saveModelButton_Click(sender As Object, e As EventArgs) Handles saveModelButton.Click
        Dim path = GlobalApiKey.Text.Trim()
        If String.IsNullOrWhiteSpace(path) Then
            MessageBox.Show("Please enter a model path.", "Missing Path", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If Not File.Exists(path) Then
            MessageBox.Show("Model file not found.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If Not Globals.LocalAIModelPaths.Contains(path) Then
            Globals.LocalAIModelPaths.Add(path)
        End If
        Globals.LocalAISelectedModel = path
        Globals.SaveLocalAISettings()

        Await InitializeModelsAsync()
        Await LoadModelAsync(path)
    End Sub

    Private Async Sub deleteModelButton_Click(sender As Object, e As EventArgs) Handles deleteModelButton.Click
        If localAIModel.SelectedItem Is Nothing Then Return
        Dim path = localAIModel.SelectedItem.ToString()
        Globals.LocalAIModelPaths.Remove(path)
        If Globals.LocalAISelectedModel = path Then Globals.LocalAISelectedModel = String.Empty
        Globals.SaveLocalAISettings()
        Await InitializeModelsAsync()
    End Sub

    Private Async Sub localAIModel_SelectedIndexChanged(sender As Object, e As EventArgs) Handles localAIModel.SelectedIndexChanged
        If localAIModel.SelectedItem Is Nothing Then Return
        Dim path = localAIModel.SelectedItem.ToString()
        Globals.LocalAISelectedModel = path
        Globals.SaveLocalAISettings()
        Await LoadModelAsync(path)
    End Sub

    Private Async Sub SubmitButton_Click(sender As Object, e As EventArgs) Handles SubmitButton.Click
        If Not _aiEngine.IsModelLoaded Then
            MessageBox.Show("Please load a model first.", "Model Not Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim promptText = userPrompt.Text
        If String.IsNullOrWhiteSpace(promptText) Then
            MessageBox.Show("Please enter a prompt.", "Empty Prompt", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        UpdateStatus("Status: Generating response...", Color.Yellow)
        aiPrompt.Clear()

        ' PERFORMANCE TEST: Measure inference time only
        Dim sw = Diagnostics.Stopwatch.StartNew()

        Try
            Dim response = Await _aiEngine.GenerateResponseAsync(prompt:=promptText, systemPrompt:=Nothing, maxTokens:=256)

            sw.Stop()
            Dim inferenceTimeMs = sw.ElapsedMilliseconds

            aiPrompt.Text = response
            UpdateStatus("Status: Complete (" & inferenceTimeMs.ToString() & "ms)", Color.LimeGreen)

            ' Speak the response if TTS checkbox is checked
            If IncludeTTSCheckbox.Checked AndAlso _ttsInitialized AndAlso _ttsEngine.IsReady Then
                If Not String.IsNullOrWhiteSpace(response) AndAlso Not response.StartsWith("[ERROR]") Then
                    UpdateStatus("Status: Speaking...", Color.Cyan)
                    Await _ttsEngine.SpeakAsync(response)
                    UpdateStatus("Status: Complete (" & inferenceTimeMs.ToString() & "ms)", Color.LimeGreen)
                End If
            End If

        Catch ex As Exception
            sw.Stop()
            aiPrompt.Text = "[ERROR] " & ex.Message
            UpdateStatus("Status: Error", Color.Red)
        End Try
    End Sub

    Private Sub LocalAIForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Don't dispose - keep engines alive for Shelly to use!
        ' Only clear shared references if user explicitly closes LocalAI form
        ' Globals.SharedLocalAIEngine = Nothing
        ' Globals.SharedTTSEngine = Nothing
        ' _aiEngine.Dispose()
        ' _ttsEngine.Dispose()

        ' Just hide instead of disposing - keep model loaded!
        e.Cancel = True
        Me.Hide()
    End Sub

    Private Async Sub UseCPU_RAMcheckbox_CheckedChanged(sender As Object, e As EventArgs) Handles UseCPU_RAMcheckbox.CheckedChanged
        If UseCPU_RAMcheckbox.Checked Then
            Globals.LocalAIUseGpu = False
            Globals.SaveLocalAISettings()

            If _aiEngine.IsModelLoaded AndAlso _aiEngine.IsUsingGpu Then
                Await ReloadModelWithCurrentSettings()
            End If
        End If
    End Sub

    Private Async Sub UseGPUchekbox_CheckedChanged(sender As Object, e As EventArgs) Handles UseGPUchekbox.CheckedChanged
        If UseGPUchekbox.Checked Then
            If Not Globals.LocalAICudaAvailable Then
                MessageBox.Show("CUDA is not available on this system.", "GPU Not Available", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                UseCPU_RAMcheckbox.Checked = True
                Return
            End If

            Globals.LocalAIUseGpu = True
            Globals.SaveLocalAISettings()

            If _aiEngine.IsModelLoaded AndAlso Not _aiEngine.IsUsingGpu Then
                Await ReloadModelWithCurrentSettings()
            End If
        End If
    End Sub

    Private Async Function ReloadModelWithCurrentSettings() As Task
        If String.IsNullOrWhiteSpace(Globals.LocalAISelectedModel) Then Return
        If Not File.Exists(Globals.LocalAISelectedModel) Then Return
        Await LoadModelAsync(Globals.LocalAISelectedModel)
    End Function

    Private Sub LabelAIconfirimation_Click(sender As Object, e As EventArgs) Handles LabelAIconfirimation.Click

    End Sub

    Private Sub IncludeTTSCheckbox_CheckedChanged(sender As Object, e As EventArgs) Handles IncludeTTSCheckbox.CheckedChanged
        ' Save TTS setting to Globals for use by other parts of Shelly
        Globals.LocalAIIncludeTTS = IncludeTTSCheckbox.Checked
        Globals.SaveLocalAISettings()
    End Sub

    Private Sub Autocheckbox_CheckedChanged(sender As Object, e As EventArgs)

    End Sub
End Class
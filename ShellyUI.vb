' ###  ShellyUIHelper.vb - v1.0.1 ###
'
' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.Web.WebView2.Core

Partial Public Class Shelly

    ' --------------------------------------------------------------------
    '   UI / DESIGN-RELATED CODE (now separated from main Shelly logic)
    ' --------------------------------------------------------------------

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowPos(hWnd As IntPtr,
                                          hWndInsertAfter As IntPtr,
                                          x As Integer,
                                          y As Integer,
                                          cx As Integer,
                                          cy As Integer,
                                          uFlags As UInteger) As Boolean
    End Function

    ' --- API declarations for drag behavior ---

    <DllImport("user32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    ' --- Variables for the form dragging and click differentiation ---
    Private isDraggingRobot As Boolean = False
    Private lastScreenPoint As Point

    ' Variables for differentiating a short click from a long click (drag) on pnlDrag
    Private clickStartTime As DateTime
    Private clickStartPosition As Point
    Private hasDragged As Boolean = False
    Private Const LONG_CLICK_THRESHOLD As Integer = 500  ' milliseconds

    Private isPotentialClick As Boolean = False
    Private mouseDownPoint As Point
    Private ReadOnly dragThreshold As Integer = 5  ' Pixel threshold

    ' --- New variables for WebView21 drag and click handling ---
    Private clickStartTimeWebView As DateTime
    Private isPotentialClickWebView As Boolean = False
    Private mouseDownPointWebView As Point
    Private Const LONG_CLICK_THRESHOLD_WEB As Integer = 500  ' milliseconds
    Private ReadOnly dragThresholdWebView As Integer = 5       ' Pixel threshold

    ' --------------------------------------------------------------------
    '   OVERRIDES & FORM LOAD/SHOWN EVENTS
    ' --------------------------------------------------------------------

    Protected Overrides Async Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)

        HintTime.Interval = 10000 ' 10 seconds (or adjust as needed)
        AddHandler HintTime.Tick, AddressOf ShellyOps.ShowNextHint
        HintTime.Start()

        Dim currentScreen As Screen = Screen.FromPoint(Cursor.Position)
        Me.StartPosition = FormStartPosition.Manual
        Me.WindowState = FormWindowState.Normal

        ' Setup tooltips for the UI elements
        ToolTip1.SetToolTip(Button1, "Send request")
        ToolTip1.SetToolTip(Button4, "New conversation")
        ToolTip1.SetToolTip(ButtonUseSpeech, "Use voice")
        ToolTip1.SetToolTip(CancelTaskButton, "Cancel task")
        ToolTip1.SetToolTip(ShowSettings, "Settings")
        ToolTip1.SetToolTip(ExitButton, "Close App")

        ' Improve drawing performance
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer, True)
        Me.UpdateStyles()

        ' Set the form to always be on top
        Me.TopMost = True

        ' Remove standard form border
        Me.FormBorderStyle = FormBorderStyle.None

        ' For transparency
        Me.SuspendLayout()
        Me.BackColor = Color.Black
        Me.TransparencyKey = Color.Black
        Me.ResumeLayout(False)

        ' Turn on double-buffering for panels
        Panel2.BorderStyle = BorderStyle.None
        AddHandler Panel2.Paint, AddressOf Panel_Paint

        Panel1.BorderStyle = BorderStyle.None
        AddHandler Panel1.Paint, AddressOf Panel_Paint

        ' Ensure WebView21 is ready, then set default color via JS
        Await ExecuteScriptSafeAsync("setColorDefault();")
        LabelStatusUpdate.Text = "Ready..."

        ' Check default audio device
        If Globals.UserAudioSelection < 0 Then
            Globals.SetDefaultAudioDevice()
        End If

        ' If not empty, set default device in Settings UI
        If Not String.IsNullOrEmpty(Globals.UserAudioSelection) Then
            If Settings.ComboBoxDevices.Items.Contains(Globals.UserAudioSelection) Then
                Settings.ComboBoxDevices.SelectedItem = Globals.UserAudioSelection
            End If
        End If

        ' If not empty, set default model selection in Settings UI
        If Not String.IsNullOrEmpty(Config.AssistantId) Then
            If Settings.aiSelection.Items.Contains(Config.AssistantId) Then
                Settings.aiSelection.SelectedItem = Config.AssistantId
            End If
        End If

        ' Initialize custom function registry
        CustomFunctionsEngine.InitializeFunctions(Me.functionRegistry)
    End Sub

    Private Async Sub Shelly_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Try
            EnsureShellyFolderExists()

            ' Additional form transparency and topmost config (redundant but safe)
            Me.FormBorderStyle = FormBorderStyle.None
            Me.BackColor = Color.Black
            Me.TransparencyKey = Color.Black
            Me.TopMost = True

            ' Right Click Menu Design
            ApplyDarkContextMenuStyle(PSFunctResultsContextMenu)
            ApplyYellowContextMenuStyle(PSFunctResultsContextMenu2)

            ' Configure WebView2 for transparency
            WebView21.DefaultBackgroundColor = Color.Transparent

            ' Initialize WebView2
            Await WebView21.EnsureCoreWebView2Async(Nothing)
            WebView21.CoreWebView2.Settings.IsWebMessageEnabled = True
            AddHandler WebView21.CoreWebView2.WebMessageReceived, AddressOf CoreWebView2_WebMessageReceived

            ' Attach mouse event handlers for drag/click functionality on WebView21
            AddHandler WebView21.MouseDown, AddressOf WebView21_MouseDown
            AddHandler WebView21.MouseMove, AddressOf WebView21_MouseMove
            AddHandler WebView21.MouseUp, AddressOf WebView21_MouseUp

            ' Attach handler for NavigationCompleted
            AddHandler WebView21.NavigationCompleted, AddressOf WebView21_NavigationCompleted

            Dim resourcesPath As String = Path.Combine(Application.StartupPath, "Resources")
            Dim folderPath As String = resourcesPath
            WebView21.CoreWebView2.SetVirtualHostNameToFolderMapping("appassets", folderPath, CoreWebView2HostResourceAccessKind.Allow)

            ' Load embedded HTML
            Dim resourceName As String = "ShellyAI.ShellyAnimation.html" ' Adjust if your project namespace differs
            Dim htmlContent As String = LoadHtmlFromResource(resourceName)

            If Not String.IsNullOrWhiteSpace(htmlContent) Then
                WebView21.NavigateToString(htmlContent)
            Else
                MessageBox.Show("Error: HTML content is empty or could not be loaded.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Error during initialization: " & ex.Message,
                            "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- WebView21 extended mouse events for drag and click functionality ---

    Private Sub WebView21_MouseDown(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            clickStartTimeWebView = DateTime.Now
            mouseDownPointWebView = e.Location
            isPotentialClickWebView = True
        End If
    End Sub

    Private Sub WebView21_MouseMove(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left AndAlso isPotentialClickWebView Then
            Dim dx As Integer = Math.Abs(e.X - mouseDownPointWebView.X)
            Dim dy As Integer = Math.Abs(e.Y - mouseDownPointWebView.Y)
            If dx > dragThresholdWebView OrElse dy > dragThresholdWebView Then
                isPotentialClickWebView = False
                ReleaseCapture()
                Dim sendResult As Integer = SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
            End If
        End If
    End Sub

    Private Sub WebView21_MouseUp(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Left Then
            ' If no significant drag occurred, treat as a click to toggle ChatPanel
            If isPotentialClickWebView Then
                ToggleChatPanel()
            End If
            isPotentialClickWebView = False
        End If
    End Sub

    Private Sub ToggleChatPanel()
        ChatPanel.Visible = Not ChatPanel.Visible

        If Not Globals.CheckBoxHintsState Then
            Panel5.Visible = ChatPanel.Visible
            Settings.CheckBoxHints.Text = "Hints OFF"
        Else
            Panel5.Visible = False
            Settings.CheckBoxHints.Text = "Hints ON "
        End If

        ChatPanel.Invalidate()
        Panel5.Invalidate()
        Me.Invalidate(True)
        Me.Update()
        Application.DoEvents()
        WebView21.Refresh()
    End Sub

    ' Fired after WebView2 finishes navigation
    Private Async Sub WebView21_NavigationCompleted(sender As Object,
                                                     e As CoreWebView2NavigationCompletedEventArgs)
        ' Small delay before executing wave animation
        Await Task.Delay(1000)
        Await ExecuteScriptSafeAsync("wave();")
        Await WebView21.CoreWebView2.ExecuteScriptAsync("startShellyRandom();")

        ' Optionally remove the handler if you only need it once
        RemoveHandler WebView21.NavigationCompleted, AddressOf WebView21_NavigationCompleted
    End Sub

    ' Load HTML from embedded resource
    Private Function LoadHtmlFromResource(resourceName As String) As String
        Try
            Dim asm As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()
            Using stream As IO.Stream = asm.GetManifestResourceStream(resourceName)
                If stream Is Nothing Then
                    Dim availableResources As String = String.Join(Environment.NewLine, asm.GetManifestResourceNames())
                    Throw New Exception($"Embedded resource '{resourceName}' not found. " &
                                        $"Available resources:{Environment.NewLine}{availableResources}")
                End If
                Using reader As New IO.StreamReader(stream)
                    Return reader.ReadToEnd()
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading HTML resource: " & ex.Message,
                            "Resource Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return String.Empty
        End Try
    End Function

    ' --------------------------------------------------------------------
    '   FORM SHOWN & CLOSING
    ' --------------------------------------------------------------------
    Private Sub Shelly_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Remove focus from all controls
        Me.ActiveControl = Nothing
    End Sub

    Private Sub Shelly_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            ' Cancel any running asynchronous tasks
            If cancellationTokenSource IsNot Nothing Then
                cancellationTokenSource.Cancel()
                cancellationTokenSource.Dispose()
                cancellationTokenSource = Nothing
            End If

            ' Dispose of the WebView2 control to free its associated processes
            If WebView21 IsNot Nothing Then
                WebView21.Dispose()
                WebView21 = Nothing
            End If
            If WebView2Google IsNot Nothing Then
                WebView2Google.Dispose()
                WebView2Google = Nothing
            End If

            ' Clear global caches to release memory
            Globals.conversationHistory.Clear()
            Globals.FileContents.Clear()
            Globals.GeneratedImages.Clear()
            Globals.TaskData.Clear()
            Globals.ConsoleForm = Nothing
            HintTime.Stop()
            HintTime.Dispose()
            ' Optionally force garbage collection
            GC.Collect()
            GC.WaitForPendingFinalizers()

            Debug.WriteLine("Application cleanup completed successfully.")
        Catch ex As Exception
            Debug.WriteLine("Error during form closing cleanup: " & ex.Message)
        End Try
    End Sub

    ' --------------------------------------------------------------------
    '   BUTTON & PANEL EVENT HANDLERS (purely UI)
    ' --------------------------------------------------------------------

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles ExitButton.Click

        ' Dispose WebView2 controls
        If WebView21 IsNot Nothing Then
            WebView21.Dispose()
            WebView21 = Nothing
        End If

        CleanupResources()
        ClearDebugLogs()
        Close()
    End Sub

    ' Dragging code for Panel3
    Private Sub Panel3_MouseDown(sender As Object, e As MouseEventArgs) Handles Panel3.MouseDown
        If e.Button = MouseButtons.Left Then
            ReleaseCapture()
            Dim sendResult As Integer = SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
        End If
    End Sub

    ' Dragging code for the top panel (pnlDrag) - collapses or expands ChatPanel on click
    Private Sub PnlDrag_MouseDown(sender As Object, e As MouseEventArgs) Handles pnlDrag.MouseDown
        If e.Button = MouseButtons.Left Then
            isPotentialClick = True
            mouseDownPoint = e.Location
        End If
    End Sub

    Private Sub PnlDrag_MouseMove(sender As Object, e As MouseEventArgs) Handles pnlDrag.MouseMove
        If e.Button = MouseButtons.Left AndAlso isPotentialClick Then
            Dim dx As Integer = Math.Abs(e.X - mouseDownPoint.X)
            Dim dy As Integer = Math.Abs(e.Y - mouseDownPoint.Y)
            If dx > dragThreshold OrElse dy > dragThreshold Then
                ' It's an actual drag. Move the form:
                isPotentialClick = False
                ReleaseCapture()
                Dim sendResult As Integer = SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
            End If
        End If
    End Sub

    Private Sub PnlDrag_MouseUp(sender As Object, e As MouseEventArgs) Handles pnlDrag.MouseUp
        If e.Button = MouseButtons.Left Then

            If isPotentialClick Then
                ToggleChatPanel()
            End If
        End If
    End Sub

    ' WebView2 click event (if needed for other UI interactions)
    Private Sub WebView21_Click(sender As Object, e As EventArgs) Handles WebView21.Click

    End Sub

    Private Sub ChatPanel_Paint(sender As Object, e As PaintEventArgs) Handles ChatPanel.Paint

    End Sub

    ' Panel paint event, used to reduce flicker or draw custom edges
    Private Sub Panel_Paint(sender As Object, e As PaintEventArgs)

    End Sub

    ' --------------------------------------------------------------------
    '   HELPER FOR RUNNING JAVASCRIPT IN WebView2
    ' --------------------------------------------------------------------
    Public Async Function ExecuteScriptSafeAsync(script As String) As Task
        ' If CoreWebView2 is not initialized, wait for it
        If WebView21.CoreWebView2 Is Nothing Then
            Await WebView21.EnsureCoreWebView2Async(Nothing)
        End If
        ' Now execute the script
        Await WebView21.CoreWebView2.ExecuteScriptAsync(script)
    End Function

    ' --------------------------------------------------------------------
    '   HINTS TIMER
    ' --------------------------------------------------------------------
    Public Sub StopHintRotation()
        HintTime.Stop()
    End Sub


    Private Sub CoreWebView2_WebMessageReceived(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Try
            Dim msgJson As String = e.TryGetWebMessageAsString()
            ' For simplicity, we can use basic string operations.
            ' In production, consider using a JSON parser.

            If msgJson.Contains("mousedown") Then
                isPotentialClickWebView = True
                Dim x As Integer = GetValue(msgJson, """x"":")
                Dim y As Integer = GetValue(msgJson, """y"":")
                mouseDownPointWebView = New Point(x, y)

            ElseIf msgJson.Contains("mousemove") AndAlso isPotentialClickWebView Then
                Dim x As Integer = GetValue(msgJson, """x"":")
                Dim y As Integer = GetValue(msgJson, """y"":")
                Dim dx = Math.Abs(x - mouseDownPointWebView.X)
                Dim dy = Math.Abs(y - mouseDownPointWebView.Y)
                If dx > dragThresholdWebView OrElse dy > dragThresholdWebView Then
                    isPotentialClickWebView = False
                    ' Start dragging the form:
                    ReleaseCapture()
                    Dim sendResult As Integer = SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
                End If

            ElseIf msgJson.Contains("mouseup") Then
                If isPotentialClickWebView Then
                    ' No significant drag => treat as a click (toggle ChatPanel)
                    ToggleChatPanel()
                End If
                isPotentialClickWebView = False
            End If
        Catch ex As Exception
            Debug.WriteLine("Error in WebMessageReceived: " & ex.Message)
        End Try
    End Sub

    Private Function GetValue(source As String, key As String) As Integer
        Dim idx = source.IndexOf(key)
        If idx < 0 Then Return 0
        Dim afterKey = source.Substring(idx + key.Length).Trim()
        Dim valStr = New String(afterKey.TakeWhile(Function(c) Char.IsDigit(c) OrElse c = "-"c).ToArray())
        Dim valInt As Integer = 0
        If Integer.TryParse(valStr, valInt) Then
            Return valInt
        Else
            ' Log or handle the parse failure (update)
            Return 0
        End If
    End Function


    'Buttons hover effects
    '=================================================================================================>
    Private Sub Button1_MouseEnter(sender As Object, e As EventArgs) Handles Button1.MouseEnter
        Button1.BackgroundImage = My.Resources.start2 ' Hover image
    End Sub

    Private Sub Button1_MouseLeave(sender As Object, e As EventArgs) Handles Button1.MouseLeave
        Button1.BackgroundImage = My.Resources.start ' Default image
    End Sub

    Private Sub Button4_MouseEnter(sender As Object, e As EventArgs) Handles Button4.MouseEnter
        Button4.BackgroundImage = My.Resources.new1 ' Hover image
    End Sub

    Private Sub Button4_MouseLeave(sender As Object, e As EventArgs) Handles Button4.MouseLeave
        Button4.BackgroundImage = My.Resources._new ' Default image
    End Sub

    Private Sub ButtonUseSpeech_MouseEnter(sender As Object, e As EventArgs) Handles ButtonUseSpeech.MouseEnter
        If Not isSpeechActive Then
            ButtonUseSpeech.BackgroundImage = My.Resources.mic2
        End If
    End Sub

    Private Sub ButtonUseSpeech_MouseLeave(sender As Object, e As EventArgs) Handles ButtonUseSpeech.MouseLeave
        If Not isSpeechActive Then
            ButtonUseSpeech.BackgroundImage = My.Resources.mic
        End If
    End Sub

    Private Sub CancelTaskButton_MouseEnter(sender As Object, e As EventArgs) Handles CancelTaskButton.MouseEnter
        CancelTaskButton.BackgroundImage = My.Resources.cancel2 ' Hover image
    End Sub

    Private Sub CancelTaskButton_MouseLeave(sender As Object, e As EventArgs) Handles CancelTaskButton.MouseLeave
        CancelTaskButton.BackgroundImage = My.Resources.cancel ' Default image
    End Sub

    Private Sub ShowSettings_MouseEnter(sender As Object, e As EventArgs) Handles ShowSettings.MouseEnter
        ShowSettings.BackgroundImage = My.Resources.settings2 ' Hover image
    End Sub

    Private Sub ShowSettings_MouseLeave(sender As Object, e As EventArgs) Handles ShowSettings.MouseLeave
        ShowSettings.BackgroundImage = My.Resources.settings ' Default image
    End Sub

    Private Sub ExitButton_MouseEnter(sender As Object, e As EventArgs) Handles ExitButton.MouseEnter
        ExitButton.BackgroundImage = My.Resources.exit2 ' Hover image
    End Sub

    Private Sub ExitButton_MouseLeave(sender As Object, e As EventArgs) Handles ExitButton.MouseLeave
        ExitButton.BackgroundImage = My.Resources.exit1 ' Default image
    End Sub

    '<=====================================================================================================

    Private Sub CleanupResources()
        Try
            ' Cancel running tasks
            If cancellationTokenSource IsNot Nothing Then
                cancellationTokenSource.Cancel()
                cancellationTokenSource.Dispose()
                cancellationTokenSource = Nothing
            End If

            If WebView2Google IsNot Nothing Then
                WebView2Google.Dispose()
                WebView2Google = Nothing
            End If

            ' Stop and dispose timers
            If HintTime IsNot Nothing Then
                HintTime.Stop()
                HintTime.Dispose()
            End If

            ' Clear UI elements
            UserInputBox.Clear()
            AIcommentBox.Clear()
            AIresponseErrorBox.Clear()
            PSFunctResultsBox.Clear()
            LabelStatusUpdate.Text = "Ready..."

            ' Reset global state
            Globals.ConsoleForm = Nothing
            conversationHistory.Clear()
            conversationHistory = New List(Of Dictionary(Of String, String))()
            FileContents.Clear()
            GeneratedImages.Clear()
            TaskData.Clear()
            totalTokens = 0
            isTrainingSent = False
            isVerificationDone = False
            systemPrompt = Nothing
            LastFileQuery = ""

            ' Optional: full GC cleanup
            GC.Collect()
            GC.WaitForPendingFinalizers()

            Debug.WriteLine("✅ CleanupResources: All resources cleaned up.")
        Catch ex As Exception
            Debug.WriteLine("❌ CleanupResources Exception: " & ex.Message)
        End Try
    End Sub

End Class

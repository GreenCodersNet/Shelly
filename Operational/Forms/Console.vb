' ###  Console.vb | v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################
Public Class Console
    Private Sub Console_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

        ' Set global reference to ConsoleForm
        Globals.ConsoleForm = Me
        UserInput.Focus()


        ShellyConsole.Font = New Font("Consolas", 12, FontStyle.Regular)
        ShellyConsole.WordWrap = False ' Disable word wrapping

        Dim asciiArt As String = "
=============================================================
=============================================================
 0101010  01    01  01010101  01        01      0101    0101
01        10    10  10        10        10       1001  0101 
01010101  01010101  01010101  01        01          0110
      01  10    10  10        10        10           10
01010101  01    01  01010101  01010101  01010101     01   
=============================================================
=========================== 1.0.1 ===========================

[_>] type [Help] for all options.     
"
        ShellyConsole.Text = asciiArt

    End Sub

    ' +++++++++++++ SMOOTH FORM OPENING - REDISGN +++++++++++++
    Public Sub ApplySmoothCustomTitleBar(form As Form)
        ' Enable double buffering to prevent flicker
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Me.UpdateStyles()

        ' Suspend layout to avoid redrawing until complete
        Me.SuspendLayout()

        ' Apply custom title bar logic here (add title bar panel, buttons, etc.)
        ApplyCustomTitleBar(form)

        ' Resume layout and force a refresh
        Me.ResumeLayout()
        Me.Refresh()
    End Sub

    Public Sub AppendDebugMessage(message As String)
        If InvokeRequired Then
            Invoke(New MethodInvoker(Sub() AppendDebugMessage(message)))
        Else
            ShellyConsole.AppendText(message & vbCrLf)
        End If
    End Sub

    Public Sub ClearConsole()
        If InvokeRequired Then
            Invoke(New MethodInvoker(Sub() ClearConsole()))
        Else
            ShellyConsole.Clear()
        End If
    End Sub

    ' When Console form closes, remove reference but keep logs stored
    Private Sub Console_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Globals.ConsoleForm = Nothing
        ' Stop timers
    End Sub

    Private Sub ShellyConsole_TextChanged(sender As Object, e As EventArgs) Handles ShellyConsole.TextChanged
        ShellyConsole.WordWrap = True
        ShellyConsole.ReadOnly = True
        ShellyConsole.ShortcutsEnabled = True ' Allow copy shortcuts (Ctrl+C)
        ShellyConsole.TabStop = False ' Prevent tab selection
    End Sub

    Private Sub UserInput_TextChanged(sender As Object, e As EventArgs) Handles UserInput.TextChanged

    End Sub

    Private Sub UserInput_KeyDown(sender As Object, e As KeyEventArgs) Handles UserInput.KeyDown
        ' Check if Enter key is pressed
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True ' Prevents adding a new line in the RichTextBox

            Dim input As String = UserInput.Text.Trim().ToLower() ' Normalize input

            Globals.AddToCommandHistory(input)

            If Globals.IsLiveLoggingEnabled Then
                Globals.IsLiveLoggingEnabled = False
                AppendToConsole(vbCrLf & "[_>] Live logging disabled.", Color.Red)
            End If

            Select Case input
                Case "clear logs"
                    Globals.ClearDebugLogs()
                    ShellyConsole.Clear()
                    AppendToConsole(vbCrLf & "[_>] Logs cleared successfully.", Color.Lime)
                Case "clear"
                    ShellyConsole.Clear()
                Case "show logs"
                    Dim storedLogs As String = Globals.GetStoredLogs()

                    If String.IsNullOrWhiteSpace(storedLogs) Then
                        AppendToConsole(vbCrLf & "[_>] No logs found.", Color.Orange)
                    Else
                        AppendToConsole(vbCrLf & storedLogs, Color.White)
                        Globals.IsLiveLoggingEnabled = True
                        AppendToConsole("[_>] Live logging enabled. Logs will update in real-time.", Color.Cyan)
                    End If

                Case "show history"

                    Dim sb As New System.Text.StringBuilder()

                    ' Loop through each dictionary (conversation) in the conversationHistory list
                    For Each conversation As Dictionary(Of String, String) In conversationHistory
                        ' Loop through each key-value pair in the current dictionary
                        For Each kvp As KeyValuePair(Of String, String) In conversation
                            ' Append the key and value in a readable format.
                            sb.AppendLine(kvp.Key & ": " & kvp.Value)
                        Next
                        ' Optionally, add a separator between conversation entries.
                        sb.AppendLine("-----")
                    Next

                    If String.IsNullOrWhiteSpace(sb.ToString()) Then
                        AppendToConsole(vbCrLf & "[_>] No logs found.", Color.Orange)
                    Else
                        AppendToConsole(vbCrLf & sb.ToString(), Color.White)
                    End If

                Case "show functions"
                    Dim functionsList As String = GetRecognizedFunctions()
                    AppendToConsole(vbCrLf & "[_>] Recognized Functions:", Color.Yellow)
                    AppendToConsole(functionsList, Color.LightGray)

                Case "exit"
                    Me.Close()
                    AppendToConsole(vbCrLf & "[_>] Console closed.", Color.Red)

                Case "help"
                    AppendToConsole(vbCrLf & "[_>] Available commands:", Color.LightBlue)
                    AppendToConsole("[show copilot] = Load Copilot window.", Color.DeepSkyBlue)
                    AppendToConsole("[show functions] = Show a list of all Custom Functions.", Color.DeepSkyBlue)
                    AppendToConsole("[show logs] = View all logs.", Color.DeepSkyBlue)
                    AppendToConsole("[show history] = View the conversation history sent to AI.", Color.DeepSkyBlue)
                    AppendToConsole("[clear] = Clear console output.", Color.DeepSkyBlue)
                    AppendToConsole("[clear logs] = Clear all logs from memory.", Color.DeepSkyBlue)
                    AppendToConsole("[exit] = Exist console mode.", Color.DeepSkyBlue)
                    AppendToConsole("[help] = Show the Help Menu.", Color.DeepSkyBlue)

                Case Else
                    AppendToConsole(vbCrLf & "[_>] Unknown command: " & UserInput.Text, Color.Orange)
                    AppendToConsole("Type 'help' for available commands. ", Color.White)
            End Select
            ' Clear input after processing
            UserInput.Clear()
        End If

        ' User Commands History

        If e.KeyCode = Keys.Up Then
            UserInput.Text = Globals.GetPreviousCommand()
            UserInput.SelectionStart = UserInput.Text.Length ' Move cursor to end
            e.SuppressKeyPress = True ' Prevents inserting control characters
            Return
        End If

        ' DOWN Arrow Key - Show Next Command
        If e.KeyCode = Keys.Down Then
            UserInput.Text = Globals.GetNextCommand()
            UserInput.SelectionStart = UserInput.Text.Length ' Move cursor to end
            e.SuppressKeyPress = True
            Return
        End If

    End Sub


    Public Sub AppendToConsole(message As String, color As Color)
        ' Check if form is disposed before updating UI
        If Me.IsDisposed OrElse Not Me.IsHandleCreated Then
            Return ' Exit if form is closed or disposed
        End If

        If InvokeRequired Then
            Invoke(New MethodInvoker(Sub() AppendToConsole(message, color)))
        Else
            ' Check if ShellyConsole is disposed before accessing it
            If ShellyConsole Is Nothing OrElse ShellyConsole.IsDisposed Then
                Return ' Exit if ShellyConsole is no longer accessible
            End If

            ' Move to the end of the text (ensures text is appended)
            ShellyConsole.SelectionStart = ShellyConsole.TextLength
            ShellyConsole.SelectionLength = 0

            ' Set the color for the new text
            ShellyConsole.SelectionColor = color
            ShellyConsole.AppendText(message & vbCrLf)

            ' Auto-scroll to last line
            ShellyConsole.SelectionStart = ShellyConsole.TextLength
            ShellyConsole.ScrollToCaret()

            ' Reset color to default
            ShellyConsole.SelectionColor = ShellyConsole.ForeColor

        End If
    End Sub

    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint

    End Sub
End Class

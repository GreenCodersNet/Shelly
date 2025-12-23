'==============================================
' HELPER FUNCTIONS FOR CUSTOM FUCTIONS
'==============================================

Imports System.Runtime.InteropServices
Imports NAudio.CoreAudioApi
Imports System.Threading
Imports System.IO
Imports FxResources.System
Imports System.Text.RegularExpressions
Imports System.Reflection
Imports System.Text
Imports System.Globalization


Public Module HelperFunctions

    Public Function ExtractActualUrl(url As String) As String
        Try
            If url.StartsWith("https://www.google.com/url?", StringComparison.OrdinalIgnoreCase) Then
                Dim uri As New Uri(url)
                Dim queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query)
                Dim actualUrl As String = queryParams("q")
                Return actualUrl
            End If
            Return url
        Catch ex As Exception
            Debug.WriteLine("ExtractActualUrl error: " & ex.Message)
            Return url
        End Try
    End Function

    Public Function ParseUrlsFromJson(json As String) As List(Of String)
        Try
            If json.StartsWith(""""c) AndAlso json.EndsWith(""""c) Then
                json = json.Substring(1, json.Length - 2)
                json = System.Text.RegularExpressions.Regex.Unescape(json)
            End If
            Dim urls As List(Of String) = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of String))(json)
            Return urls
        Catch ex As Exception
            Debug.WriteLine("ParseUrlsFromJson error: " & ex.Message)
            Return New List(Of String)()
        End Try
    End Function

    Public Function StripCodeFences(raw As String) As String
        If String.IsNullOrWhiteSpace(raw) Then Return String.Empty
        Dim text = raw.Trim()

        If text.StartsWith("```", StringComparison.Ordinal) Then
            Dim newlineIndex = text.IndexOfAny({ChrW(10), ChrW(13)})
            If newlineIndex >= 0 Then
                Dim nextIndex = newlineIndex + 1
                While nextIndex < text.Length AndAlso (text(nextIndex) = ChrW(10) OrElse text(nextIndex) = ChrW(13))
                    nextIndex += 1
                End While
                text = text.Substring(Math.Min(nextIndex, text.Length))
            Else
                text = text.Substring(3)
            End If
        End If

        If text.EndsWith("```", StringComparison.Ordinal) Then
            Dim lastFence = text.LastIndexOf("```", StringComparison.Ordinal)
            If lastFence >= 0 Then
                text = text.Substring(0, lastFence)
            End If
        End If

        Return text.Trim()
    End Function

    ' Helper to clear clipboard safely on STA thread
    Public Sub ClearClipboardSafe()
        Dim clearThread As New Thread(
            Sub()
                Try
                    Clipboard.Clear()
                Catch
                    ' Ignore
                End Try
            End Sub)
        clearThread.SetApartmentState(ApartmentState.STA)
        clearThread.Start()
        clearThread.Join(1000)
    End Sub



    ' Helper function to get window title
    <Runtime.InteropServices.DllImport("user32.dll", CharSet:=Runtime.InteropServices.CharSet.Auto)>
    Public Function GetWindowText(hWnd As IntPtr, lpString As Text.StringBuilder, nMaxCount As Integer) As Integer
    End Function

    Public Function GetWindowTitle(hWnd As IntPtr) As String
        Dim sb As New Text.StringBuilder(256)
        GetWindowText(hWnd, sb, sb.Capacity)
        Return sb.ToString()
    End Function

    ' Simplified and more reliable paste function
    Public Async Function PasteTextToWindow(targetWindow As IntPtr, textToPaste As String, ct As CancellationToken) As Task(Of Boolean)
        Const MAX_RETRIES As Integer = 3
        Const SW_SHOW As Integer = 5

        Dim needsRetryDelay As Boolean = False

        For attempt As Integer = 1 To MAX_RETRIES
            ' Handle retry delay from previous failed attempt (moved outside catch block)
            If needsRetryDelay Then
                Await Task.Delay(300, ct)
                needsRetryDelay = False
            End If

            If ct.IsCancellationRequested OrElse typingStopped Then
                Return False
            End If

            Dim attemptSucceeded As Boolean = False
            Dim shouldContinue As Boolean = False

            Try
                Debug.WriteLine($"[PasteTextToWindow] Attempt {attempt}/{MAX_RETRIES}")

                ' Step 1: Bring target window to foreground
                FileHandler.ShowWindow(targetWindow, SW_SHOW)
                FileHandler.BringWindowToTop(targetWindow)
                FileHandler.SetForegroundWindow(targetWindow)

                ' Wait for window to become active
                Await Task.Delay(150, ct)

                ' Step 2: Verify we have the right window
                Dim currentWindow As IntPtr = FileHandler.GetForegroundWindow()
                If currentWindow <> targetWindow Then
                    Debug.WriteLine($"[PasteTextToWindow] Window focus lost on attempt {attempt}")
                    shouldContinue = True
                End If

                If Not shouldContinue Then
                    ' Step 3: Set clipboard using STA thread (required for clipboard operations)
                    Dim clipboardSet As Boolean = False
                    Dim clipboardThread As New Thread(
                        Sub()
                            Try
                                For retryClip As Integer = 1 To 5
                                    Try
                                        Clipboard.Clear()
                                        Thread.Sleep(30)
                                        Clipboard.SetText(textToPaste, TextDataFormat.UnicodeText)
                                        clipboardSet = True
                                        Exit For
                                    Catch clipEx As Exception
                                        Debug.WriteLine($"[PasteTextToWindow] Clipboard retry {retryClip}: {clipEx.Message}")
                                        Thread.Sleep(100)
                                    End Try
                                Next
                            Catch ex As Exception
                                Debug.WriteLine($"[PasteTextToWindow] Clipboard thread error: {ex.Message}")
                            End Try
                        End Sub)
                    clipboardThread.SetApartmentState(ApartmentState.STA)
                    clipboardThread.Start()
                    clipboardThread.Join(2000) ' Wait up to 2 seconds

                    If Not clipboardSet Then
                        Debug.WriteLine($"[PasteTextToWindow] Failed to set clipboard on attempt {attempt}")
                        shouldContinue = True
                    End If
                End If

                If Not shouldContinue Then
                    Debug.WriteLine($"[PasteTextToWindow] Clipboard set successfully")

                    ' Step 4: Small delay before pasting
                    Await Task.Delay(50, ct)

                    ' Step 5: Simulate Ctrl+V using InputSimulator
                    Dim sim As New InputSimulatorStandard.InputSimulator()

                    ' Release any stuck modifier keys first
                    sim.Keyboard.KeyUp(InputSimulatorStandard.Native.VirtualKeyCode.CONTROL)
                    sim.Keyboard.KeyUp(InputSimulatorStandard.Native.VirtualKeyCode.SHIFT)
                    sim.Keyboard.KeyUp(InputSimulatorStandard.Native.VirtualKeyCode.MENU)
                    Await Task.Delay(30, ct)

                    ' Perform Ctrl+V
                    sim.Keyboard.ModifiedKeyStroke(
                        InputSimulatorStandard.Native.VirtualKeyCode.CONTROL,
                        InputSimulatorStandard.Native.VirtualKeyCode.VK_V)

                    ' Wait for paste to complete
                    Await Task.Delay(200, ct)

                    Debug.WriteLine($"[PasteTextToWindow] Paste command sent successfully on attempt {attempt}")
                    attemptSucceeded = True
                End If

            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                Debug.WriteLine($"[PasteTextToWindow] Attempt {attempt} failed: {ex.Message}")
                If attempt < MAX_RETRIES Then
                    needsRetryDelay = True
                End If
            End Try

            If attemptSucceeded Then
                Return True
            End If

            ' Add delay before next attempt if we need to continue
            If shouldContinue AndAlso attempt < MAX_RETRIES Then
                Await Task.Delay(200, ct)
            End If
        Next

        Debug.WriteLine("[PasteTextToWindow] All attempts failed")
        Return False
    End Function

    ''' <summary>
    ''' DEPRECATED: This function is no longer used by SearchForTextInsideFiles.
    ''' The new implementation uses FileContainsText in CustomFunctions_2.vb instead.
    ''' Kept for backward compatibility if needed elsewhere.
    ''' </summary>
    Public Sub AddMatchesFromFile(filePath As String, searchText As String, comparison As StringComparison, results As List(Of String))
        Try
            ' Only read plain text files - for other formats, use FileHandler.ReadFileContent
            Dim ext = Path.GetExtension(filePath).ToLowerInvariant()
            Dim plainTextExtensions = New HashSet(Of String) From {
                ".txt", ".log", ".csv", ".xml", ".html", ".htm", ".md", ".json", ".yaml", ".yml",
                ".ini", ".config", ".conf", ".properties", ".sql", ".bat", ".sh",
                ".java", ".c", ".cpp", ".py", ".js", ".php", ".css", ".scss",
                ".asp", ".aspx", ".jsp", ".pl", ".rb", ".vb", ".swift", ".kt"
            }

            If plainTextExtensions.Contains(ext) Then
                ' For plain text files, we can check line by line
                Dim lines = File.ReadAllLines(filePath)
                For i = 0 To lines.Length - 1
                    If lines(i).IndexOf(searchText, comparison) >= 0 Then
                        ' Only add file path (not content) - user can use ReadFileAndAnswer to get content
                        If Not results.Contains(filePath) Then
                            results.Add(filePath)
                        End If
                        Exit For ' Found match, no need to continue
                    End If
                Next
            Else
                ' For other file types (PDF, DOCX, etc.), use FileHandler
                Try
                    Dim content = FileHandler.ReadFileContent(filePath)
                    If content.IndexOf(searchText, comparison) >= 0 Then
                        results.Add(filePath)
                    End If
                Catch
                    ' Skip files that can't be read
                End Try
            End If
        Catch ex As Exception
            ' Don't add warnings to results for individual file failures
            Debug.WriteLine($"[AddMatchesFromFile] Could not read {filePath}: {ex.Message}")
        End Try
    End Sub

End Module


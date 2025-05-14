' ###  FileHandler.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports InputSimulatorStandard
Imports System.Windows.Forms  ' For Clipboard, IDataObject, etc.
Imports InputSimulatorStandard.Native
Imports Microsoft.Web.WebView2.Core

Module FileHandler

    ' Global variables to track the original window/control and typing state.
    Public originalWindow As IntPtr = IntPtr.Zero
    Public originalControl As IntPtr = IntPtr.Zero
    Public typingStopped As Boolean = False

    ' Win32 API declarations needed for focus and window management.
    <DllImport("user32.dll")>
    Public Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Public Function GetGUIThreadInfo(idThread As Integer, ByRef lpgui As GUITHREADINFO) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function BringWindowToTop(hWnd As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function SetForegroundWindow(hWnd As IntPtr) As Boolean
    End Function



    ' Constants for key events.
    Private Const WM_KEYDOWN As UInteger = &H100
    Private Const WM_KEYUP As UInteger = &H101
    Private Const VK_RETURN As Integer = 13

    Public Const SW_RESTORE As Integer = 9
    Public Const WM_CHAR As UInteger = &H102

    <StructLayout(LayoutKind.Sequential)>
    Public Structure GUITHREADINFO
        Public cbSize As Integer
        Public flags As Integer
        Public hwndActive As IntPtr
        Public hwndFocus As IntPtr
        Public hwndCapture As IntPtr
        Public hwndMenuOwner As IntPtr
        Public hwndMoveSize As IntPtr
        Public hwndCaret As IntPtr
        Public rcCaret As RECT
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    Public Function ReadFileContent(filePath As String) As String
        Dim extension As String = Path.GetExtension(filePath).ToLower()
        Debug.WriteLine($"[FileHandler] Reading file: {filePath} with extension: {extension}")

        Select Case extension
            Case ".pdf"
                Return ReadPdfFile(filePath)
            Case ".docx"
                Return ReadDocxFile(filePath)
            Case ".xlsx"
                Return ReadXlsxFile(filePath)
            Case ".pptx"
                Return ReadPptxFile(filePath)
            Case Else
                ' Define a list of extensions for files that can be read as plain text.
                Dim supportedTextExtensions As New List(Of String) From {
                ".txt", ".doc", ".odt", ".rtf", ".tex", ".wpd", ".log", ".csv",
                ".xml", ".html", ".htm", ".xhtml", ".md", ".json", ".yaml", ".yml",
                ".ini", ".config", ".conf", ".properties", ".sql", ".bat", ".sh",
                ".java", ".c", ".cpp", ".py", ".js", ".php", ".css", ".scss",
                ".asp", ".aspx", ".jsp", ".pl", ".rb", ".vb", ".swift", ".kt"
            }
                If supportedTextExtensions.Contains(extension) Then
                    Return ReadTextFile(filePath)
                Else
                    Debug.WriteLine($"[FileHandler] Unsupported file type: {extension}")
                    Throw New NotSupportedException($"Unsupported file type: {extension}")
                End If
        End Select
    End Function

    Private Function ReadTextFile(filePath As String) As String
        Try
            If Not File.Exists(filePath) Then
                Throw New FileNotFoundException($"The file '{filePath}' does not exist.")
            End If

            Dim content As String = File.ReadAllText(filePath)
            Debug.WriteLine($"[FileHandler] Successfully read .txt file: {filePath}")

            ' Trim and clean content
            content = content.Trim()

            If String.IsNullOrWhiteSpace(content) Then
                Debug.WriteLine($"[FileHandler] The .txt file '{filePath}' is empty.")
                Throw New IOException($"The .txt file '{filePath}' is empty.")
            End If

            Return content
        Catch ex As Exception
            Debug.WriteLine($"[FileHandler ERROR] Failed to read .txt file: {filePath}. Error: {ex.Message}")
            Throw New IOException($"Failed to read .txt file: {filePath}. {ex.Message}")
        End Try
    End Function

    Private Function ReadPdfFile(filePath As String) As String
        Dim text As New StringBuilder()
        Try
            Using document = UglyToad.PdfPig.PdfDocument.Open(filePath)
                For Each page In document.GetPages()
                    text.AppendLine(page.Text)
                Next
            End Using
            Debug.WriteLine($"[FileHandler] Successfully read .pdf file: {filePath}")
            Return text.ToString()
        Catch ex As Exception
            Debug.WriteLine($"[FileHandler ERROR] Failed to read .pdf file: {filePath}. Error: {ex.Message}")
            Throw New IOException($"Failed to read .pdf file: {filePath}. {ex.Message}")
        End Try
    End Function

    Private Function ReadDocxFile(filePath As String) As String
        Dim sb As New StringBuilder()
        Try
            Using wordDoc As DocumentFormat.OpenXml.Packaging.WordprocessingDocument = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, False)
                Dim body = wordDoc.MainDocumentPart.Document.Body
                For Each para In body.Elements(Of DocumentFormat.OpenXml.Wordprocessing.Paragraph)()
                    sb.AppendLine(para.InnerText)
                Next
            End Using
            Debug.WriteLine($"[FileHandler] Successfully read .docx file: {filePath}")
            Return sb.ToString()
        Catch ex As Exception
            Debug.WriteLine($"[FileHandler ERROR] Failed to read .docx file: {filePath}. Error: {ex.Message}")
            Throw New IOException($"Failed to read .docx file: {filePath}. {ex.Message}")
        End Try
    End Function

    Private Function ReadXlsxFile(filePath As String) As String
        Dim sb As New StringBuilder()
        Try
            Using workbook As DocumentFormat.OpenXml.Packaging.SpreadsheetDocument = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(filePath, False)
                Dim workbookPart As DocumentFormat.OpenXml.Packaging.WorkbookPart = workbook.WorkbookPart
                Dim sheets = workbookPart.Workbook.Sheets
                Dim sharedStringTable As DocumentFormat.OpenXml.Packaging.SharedStringTablePart = workbookPart.SharedStringTablePart

                For Each sheet As DocumentFormat.OpenXml.Spreadsheet.Sheet In sheets
                    Dim worksheetPart As DocumentFormat.OpenXml.Packaging.WorksheetPart = CType(workbookPart.GetPartById(sheet.Id.Value), DocumentFormat.OpenXml.Packaging.WorksheetPart)
                    Dim sheetData = worksheetPart.Worksheet.Elements(Of DocumentFormat.OpenXml.Spreadsheet.SheetData)().First()
                    For Each row As DocumentFormat.OpenXml.Spreadsheet.Row In sheetData.Elements(Of DocumentFormat.OpenXml.Spreadsheet.Row)()
                        For Each cell As DocumentFormat.OpenXml.Spreadsheet.Cell In row.Elements(Of DocumentFormat.OpenXml.Spreadsheet.Cell)()
                            Dim cellValue As String = GetCellValue(cell, sharedStringTable)
                            sb.Append(cellValue & " ")
                        Next
                        sb.AppendLine()
                    Next
                Next
            End Using
            Debug.WriteLine($"[FileHandler] Successfully read .xlsx file: {filePath}")
            Return sb.ToString()
        Catch ex As Exception
            Debug.WriteLine($"[FileHandler ERROR] Failed to read .xlsx file: {filePath}. Error: {ex.Message}")
            Throw New IOException($"Failed to read .xlsx file: {filePath}. {ex.Message}")
        End Try
    End Function

    Private Function GetCellValue(cell As DocumentFormat.OpenXml.Spreadsheet.Cell, sharedStringTable As DocumentFormat.OpenXml.Packaging.SharedStringTablePart) As String
        If cell.CellValue Is Nothing Then Return String.Empty

        Dim value As String = cell.CellValue.Text
        If cell.DataType IsNot Nothing AndAlso cell.DataType.Value = DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString Then
            If sharedStringTable IsNot Nothing AndAlso Integer.TryParse(value, New Integer()) Then
                Dim index As Integer
                If Integer.TryParse(value, index) Then
                    Dim ssItem = sharedStringTable.SharedStringTable.ElementAtOrDefault(index)
                    If ssItem IsNot Nothing Then
                        Return ssItem.InnerText
                    End If
                End If
            End If
        End If

        Return value
    End Function

    Private Function ReadPptxFile(filePath As String) As String
        Dim sb As New StringBuilder()
        Try
            Using presentationDocument As DocumentFormat.OpenXml.Packaging.PresentationDocument = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(filePath, False)
                Dim presentationPart = presentationDocument.PresentationPart
                If presentationPart IsNot Nothing AndAlso presentationPart.Presentation IsNot Nothing Then
                    For Each slidePart In presentationPart.SlideParts
                        ' Extract all drawing text (this covers text boxes, shapes, etc.).
                        Dim texts = slidePart.Slide.Descendants(Of DocumentFormat.OpenXml.Drawing.Text)()
                        For Each textElem In texts
                            sb.AppendLine(textElem.Text)
                        Next

                        ' Extract text from tables.
                        Dim tables = slidePart.Slide.Descendants(Of DocumentFormat.OpenXml.Drawing.Table)()
                        For Each tbl In tables
                            For Each cell In tbl.Descendants(Of DocumentFormat.OpenXml.Drawing.TableCell)()
                                Dim cellText = String.Join(" ", cell.Descendants(Of DocumentFormat.OpenXml.Drawing.Text).Select(Function(t) t.Text))
                                If Not String.IsNullOrWhiteSpace(cellText) Then
                                    sb.AppendLine(cellText)
                                End If
                            Next
                        Next

                        ' Extract chart data.
                        For Each chartPart In slidePart.ChartParts
                            Try
                                Dim chartSpace = chartPart.ChartSpace
                                Dim chart = chartSpace.GetFirstChild(Of DocumentFormat.OpenXml.Drawing.Charts.Chart)()
                                If chart IsNot Nothing Then
                                    ' Chart Title
                                    If chart.Title IsNot Nothing Then
                                        sb.AppendLine("Chart Title: " & chart.Title.InnerText)
                                    End If

                                    ' Category Axis Title
                                    Dim catAxis = chartPart.ChartSpace.Descendants(Of DocumentFormat.OpenXml.Drawing.Charts.CategoryAxis).FirstOrDefault()
                                    If catAxis IsNot Nothing Then
                                        ' Axis title may be in a ChartText element.
                                        Dim catAxisTitle = catAxis.Descendants(Of DocumentFormat.OpenXml.Drawing.Charts.ChartText).FirstOrDefault()
                                        If catAxisTitle IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(catAxisTitle.InnerText) Then
                                            sb.AppendLine("Category Axis Title: " & catAxisTitle.InnerText)
                                        End If
                                    End If

                                    ' Value Axis Title
                                    Dim valAxis = chartPart.ChartSpace.Descendants(Of DocumentFormat.OpenXml.Drawing.Charts.ValueAxis).FirstOrDefault()
                                    If valAxis IsNot Nothing Then
                                        Dim valAxisTitle = valAxis.Descendants(Of DocumentFormat.OpenXml.Drawing.Charts.ChartText).FirstOrDefault()
                                        If valAxisTitle IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(valAxisTitle.InnerText) Then
                                            sb.AppendLine("Value Axis Title: " & valAxisTitle.InnerText)
                                        End If
                                    End If

                                    ' Extract data labels from chart series, if any.
                                    Dim dataLabels = chartPart.ChartSpace.Descendants(Of DocumentFormat.OpenXml.Drawing.Charts.DataLabel)
                                    For Each dl In dataLabels
                                        Dim dlText = dl.InnerText
                                        If Not String.IsNullOrWhiteSpace(dlText) Then
                                            sb.AppendLine("Data Label: " & dlText)
                                        End If
                                    Next
                                End If
                            Catch ex As Exception
                                Debug.WriteLine("Error extracting chart data from slide: " & ex.Message)
                            End Try
                        Next
                    Next
                End If
            End Using

            Debug.WriteLine($"[FileHandler] Successfully read .pptx file: {filePath}")
            Return sb.ToString()
        Catch ex As Exception
            Debug.WriteLine($"[FileHandler ERROR] Failed to read .pptx file: {filePath}. Error: {ex.Message}")
            Throw New IOException($"Failed to read .pptx file: {filePath}. {ex.Message}")
        End Try
    End Function


    Public Async Function ProcessChunk(chunkText As String, userQuery As String) As Task(Of Tuple(Of String, String))
        Dim prompt As String = "Based on the following text, answer the question and then provide a one-sentence summary of the text." & Environment.NewLine &
                           "Question: " & userQuery & Environment.NewLine &
                           "Text:" & Environment.NewLine & chunkText & Environment.NewLine &
                           "Please format your response exactly as follows:" & Environment.NewLine &
                           "Answer: <your answer here>" & Environment.NewLine &
                           "Summary: <your summary here>"
        Dim messages As New List(Of Dictionary(Of String, String)) From {
        New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a helpful assistant."}},
        New Dictionary(Of String, String) From {{"role", "user"}, {"content", prompt}}
    }
        Dim response As String = Await AIcall.CallGPTCore(UserApiKey, AiModelSelection, messages, 0.5, CancellationToken.None)
        response = RemoveCustomFunctionCodeBlocks(response)
        If String.IsNullOrWhiteSpace(response) Then
            Debug.WriteLine("[ProcessChunk] Warning: Received an empty response from GPT.")
            Return New Tuple(Of String, String)("", "")
        End If
        Dim answerPart As String = ""
        Dim summaryPart As String = ""
        Dim lines() As String = response.Split({Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)
        For Each line In lines
            If line.Trim().StartsWith("Answer:", StringComparison.OrdinalIgnoreCase) Then
                answerPart = line.Substring(7).Trim()
            ElseIf line.Trim().StartsWith("Summary:", StringComparison.OrdinalIgnoreCase) Then
                summaryPart = line.Substring(8).Trim()
            End If
        Next
        Return New Tuple(Of String, String)(answerPart, summaryPart)
    End Function



    ' ProcessChunkUpdate: Processes a text chunk by asking GPT to update the text according to the user query.
    ' The prompt instructs GPT to output only the updated text with no commentary.
    Public Async Function ProcessChunkUpdate(chunkText As String, userQuery As String) As Task(Of String)
        Dim prompt As String = "Below is a portion of a file. Please update this text according to the following instruction:" & Environment.NewLine &
                           "Instruction: " & userQuery & " (If the chunk ends with an incomplete sentence, ignore that incomplete part.)" & Environment.NewLine &
                           "Text:" & Environment.NewLine & chunkText & Environment.NewLine &
                           "Output only the updated text exactly as it should appear, with no additional commentary."
        Dim messages As New List(Of Dictionary(Of String, String)) From {
        New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a skilled editor and programmer."}},
        New Dictionary(Of String, String) From {{"role", "user"}, {"content", prompt}}
    }
        Dim updatedChunk As String = Await AIcall.CallGPTCore(UserApiKey, AiModelSelection, messages, 0.7, CancellationToken.None)
        updatedChunk = RemoveCustomFunctionCodeBlocks(updatedChunk)
        Return updatedChunk.Trim()
    End Function


    ' ProcessChunk: Processes a text chunk by asking GPT to produce an answer and a summary.
    Public Async Function FileProcessChunk(chunkText As String, userQuery As String) As Task(Of Tuple(Of String, String))
        Dim prompt As String = "Based on the following text, answer the question and then provide a one-sentence summary of the text." & Environment.NewLine &
                           "Question: " & userQuery & Environment.NewLine &
                           "Text:" & Environment.NewLine & chunkText & Environment.NewLine &
                           "Please format your response exactly as follows:" & Environment.NewLine &
                           "Answer: <your answer here>" & Environment.NewLine &
                           "Summary: <your summary here>"
        Dim messages As New List(Of Dictionary(Of String, String)) From {
        New Dictionary(Of String, String) From {{"role", "system"}, {"content", "You are a helpful assistant."}},
        New Dictionary(Of String, String) From {{"role", "user"}, {"content", prompt}}
    }
        Dim response As String = Await AIcall.CallGPTCore(UserApiKey, AiModelSelection, messages, 0.5, CancellationToken.None)
        response = RemoveCustomFunctionCodeBlocks(response)
        If String.IsNullOrWhiteSpace(response) Then
            Debug.WriteLine("[ProcessChunk] Warning: Received an empty response from GPT.")
            Return New Tuple(Of String, String)("", "")
        End If
        Dim answerPart As String = ""
        Dim summaryPart As String = ""
        Dim lines() As String = response.Split({Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries)

        For Each line In lines
            If line.Trim().StartsWith("Answer:", StringComparison.OrdinalIgnoreCase) Then
                answerPart = line.Substring(7).Trim()
            ElseIf line.Trim().StartsWith("Summary:", StringComparison.OrdinalIgnoreCase) Then
                summaryPart = line.Substring(8).Trim()
            End If
        Next

        If String.IsNullOrWhiteSpace(answerPart) Then

            answerPart = response.Trim()
        End If

        Return New Tuple(Of String, String)(answerPart, summaryPart)
    End Function


    ' SplitTextIntoChunks: Splits the input text into chunks based on a maximum word count.
    ' Parameters:
    '   text      - The full text to be split.
    '   maxWords  - The maximum number of words per chunk.
    ' Returns:
    '   A List(Of String) where each string is a chunk containing up to maxWords words.
    Public Function SplitTextIntoChunks(text As String, maxWords As Integer) As List(Of String)
        Dim chunks As New List(Of String)()
        If String.IsNullOrWhiteSpace(text) Then Return chunks

        ' Split text into lines using both Environment.NewLine and vbLf as delimiters.
        Dim lines() As String = text.Split({Environment.NewLine, vbLf}, StringSplitOptions.None)

        Dim currentChunk As New List(Of String)()
        Dim currentWordCount As Integer = 0

        ' Calculate total word count for debugging.
        Dim totalWords As Integer = 0
        For Each line As String In lines
            totalWords += If(String.IsNullOrWhiteSpace(line), 0, line.Split({" "c}, StringSplitOptions.RemoveEmptyEntries).Length)
        Next
        Debug.WriteLine($"[SplitTextIntoChunks] Total word count: {totalWords}")

        For Each line As String In lines
            Dim wordsInLine As Integer = If(String.IsNullOrWhiteSpace(line), 0, line.Split({" "c}, StringSplitOptions.RemoveEmptyEntries).Length)
            ' If adding this line exceeds the limit and there is already some content, flush the current chunk.
            If currentWordCount + wordsInLine > maxWords AndAlso currentChunk.Count > 0 Then
                chunks.Add(String.Join(Environment.NewLine, currentChunk))
                currentChunk.Clear()
                currentWordCount = 0
            End If
            currentChunk.Add(line)
            currentWordCount += wordsInLine
        Next

        If currentChunk.Count > 0 Then
            chunks.Add(String.Join(Environment.NewLine, currentChunk))
        End If

        Debug.WriteLine($"[SplitTextIntoChunks] Created {chunks.Count} chunk(s).")
        Return chunks
    End Function


    ' Creates an empty Office file based on the file extension.
    Public Async Function CreateEmptyOfficeFile(filePath As String) As Task(Of Tuple(Of Boolean, String))
        Dim ext As String = Path.GetExtension(filePath).ToLowerInvariant()
        Dim psScript As String

        If ext = ".docx" OrElse ext = ".doc" Then
            psScript = $"
Function CreateEmptyWordFile {{
    param (
        [string]$FilePath
    )
    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    if (-not (Test-Path $directory)) {{
        New-Item -ItemType Directory -Path $directory | Out-Null
    }}
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $doc = $word.Documents.Add()
    # Save as a DOCX file using SaveAs2 with FileFormat 12 (wdFormatXMLDocument)
    $doc.SaveAs2($FilePath, 12)
    $doc.Close()
    $word.Quit()
    Write-Host 'Empty Word file created at: ' $FilePath
}}
CreateEmptyWordFile -FilePath '{filePath}'
"
        ElseIf ext = ".pptx" OrElse ext = ".ppt" Then
            psScript = $"
Function CreateEmptyPowerPointFile {{
    param (
        [string]$FilePath
    )
    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    if (-not (Test-Path $directory)) {{
        New-Item -ItemType Directory -Path $directory | Out-Null
    }}
    $powerpoint = New-Object -ComObject PowerPoint.Application
    $powerpoint.Visible = [Microsoft.Office.Core.MsoTriState]::msoTrue
    $presentation = $powerpoint.Presentations.Add()
    $slide = $presentation.Slides.Add(1, [Microsoft.Office.Interop.PowerPoint.PpSlideLayout]::ppLayoutBlank)
    $presentation.SaveAs($FilePath)
    $presentation.Close()
    $powerpoint.Quit()
    Write-Host 'Empty PowerPoint file created at: ' $FilePath
}}
CreateEmptyPowerPointFile -FilePath '{filePath}'
"
        ElseIf ext = ".xlsx" OrElse ext = ".xls" Then
            psScript = $"
Function CreateEmptyExcelFile {{
    param (
        [string]$FilePath
    )
    $directory = [System.IO.Path]::GetDirectoryName($FilePath)
    if (-not (Test-Path $directory)) {{
        New-Item -ItemType Directory -Path $directory | Out-Null
    }}
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $workbook = $excel.Workbooks.Add()
    $workbook.SaveAs($FilePath)
    $workbook.Close()
    $excel.Quit()
    Write-Host 'Empty Excel file created at: ' $FilePath
}}
CreateEmptyExcelFile -FilePath '{filePath}'
"
        Else
            Return New Tuple(Of Boolean, String)(True, "Not an Office extension; no special script needed.")
        End If

        Return Await Shelly.Instance.ExecutePowerShellScriptAsync(psScript, CancellationToken.None)
    End Function
    ' ==================  GenerateLargeFileWithTextOrCode =======================>


    Public Async Function AppendContentToFileUniversal(filePath As String, content As String) As Task(Of Tuple(Of Boolean, String))
        Try
            Dim ext As String = Path.GetExtension(filePath).ToLowerInvariant()

            Select Case ext
                Case ".doc", ".docx"
                    Return Await AppendContentToWordFileCOM(filePath, content)
                Case ".xlsx", ".xls"
                    Return Tuple.Create(False, $"Shelly Free Version: unsupported file format for {ext}. Please upgrade to Shelly PRO version.")
                Case ".pptx", ".ppt"
                    Return Tuple.Create(False, $"Shelly Free Version: unsupported file format for {ext}. Please upgrade to Shelly PRO version.")
                Case Else
                    Return AppendContentToTextFile(filePath, content)
            End Select
        Catch ex As Exception
            Return Tuple.Create(False, $"Error: {ex.Message}")
        End Try
    End Function


    Public Async Function AppendContentToWordFileCOM(filePath As String, content As String) As Task(Of Tuple(Of Boolean, String))
        Try
            Return Await Task.Run(Function()
                                      Try
                                          Dim wordApp = CreateObject("Word.Application")
                                          wordApp.Visible = False
                                          Dim doc = wordApp.Documents.Open(filePath, [ReadOnly]:=False)
                                          Dim range = doc.Content
                                          range.Collapse(0) ' Move cursor to the end
                                          range.InsertAfter(vbCrLf & content) ' Append text with a newline
                                          doc.Save()
                                          doc.Close()
                                          wordApp.Quit()
                                          Return Tuple.Create(True, "Content appended successfully.")
                                      Catch ex As Exception
                                          Return Tuple.Create(False, ex.Message)
                                      End Try
                                  End Function)
        Catch ex As Exception
            Return Tuple.Create(False, ex.Message)
        End Try
    End Function


    Public Function AppendContentToTextFile(filePath As String, content As String) As Tuple(Of Boolean, String)
        Try
            Using sw As StreamWriter = New StreamWriter(filePath, True, Encoding.UTF8)
                sw.WriteLine(content)
            End Using
            Return Tuple.Create(True, "Content appended successfully.")
        Catch ex As Exception
            Return Tuple.Create(False, ex.Message)
        End Try
    End Function

    ' ============================= Type To File ==========================================

    ' Returns the focused control for the specified window.
    Public Function GetFocusedControl(window As IntPtr) As IntPtr
        Dim guiInfo As New GUITHREADINFO With {.cbSize = Marshal.SizeOf(Of GUITHREADINFO)()}
        Dim threadId As Integer = GetWindowThreadProcessId(window, 0)
        If GetGUIThreadInfo(threadId, guiInfo) Then
            Return guiInfo.hwndFocus
        End If
        Return IntPtr.Zero
    End Function

    ' Ensures that typing remains in the originally selected window/control.
    Public Sub EnsureOriginalControl()
        Dim currentWindow As IntPtr = GetForegroundWindow()
        Dim currentControl As IntPtr = GetFocusedControl(currentWindow)
        If currentWindow <> originalWindow OrElse currentControl <> originalControl Then
            Debug.WriteLine("User switched tabs/windows! Redirecting typing to the original document.")
            ShowWindow(originalWindow, SW_RESTORE)
            BringWindowToTop(originalWindow)
            SetForegroundWindow(originalWindow)
            originalControl = GetFocusedControl(originalWindow)
        End If
    End Sub

    ' Call this when the app is closing to stop the typing immediately.
    Public Sub StopTyping()
        typingStopped = True
    End Sub

    Public Async Function PasteTextBulk(targetWindow As IntPtr, targetControl As IntPtr, textToPaste As String) As Task
        ' Backup the user’s existing clipboard content
        Dim backupData As IDataObject = Clipboard.GetDataObject()
        Dim SW_SHOW As Integer = 5

        Try
            ' 🪟 Restore window and bring to front
            ShowWindow(targetWindow, SW_SHOW)
            BringWindowToTop(targetWindow)
            SetForegroundWindow(targetWindow)

            ' 🔍 Confirm focus
            Dim currentControl As IntPtr = GetFocusedControl(targetWindow)
            If currentControl <> targetControl Then
                Debug.WriteLine("⚠️ Warning: Could not guarantee focus on original control.")
            End If

            Await Task.Delay(100) ' Slight delay for window to become active

            ' 📋 Set clipboard text safely
            SafeSetClipboardUnicode(textToPaste)

            ' ⌨ Simulate Ctrl+V paste
            Dim sim As New InputSimulator()
            sim.Keyboard.KeyDown(VirtualKeyCode.CONTROL)
            Await Task.Delay(20)
            sim.Keyboard.KeyPress(VirtualKeyCode.VK_V)
            Await Task.Delay(20)
            sim.Keyboard.KeyUp(VirtualKeyCode.CONTROL)

            Await Task.Delay(100) ' Give time for paste to complete
        Finally
            ' ♻️ Restore original clipboard
            If backupData IsNot Nothing Then
                Clipboard.SetDataObject(backupData, True)
            Else
                Clipboard.Clear()
            End If
        End Try
    End Function

    Private Sub SafeSetClipboardUnicode(text As String)
        Dim success As Boolean = False
        Dim retries As Integer = 5

        While Not success AndAlso retries > 0
            Try
                Clipboard.SetText(text, TextDataFormat.UnicodeText)
                success = True
            Catch ex As Exception
                Thread.Sleep(100)
                retries -= 1
                Debug.WriteLine($"[!] Clipboard retry due to lock: {ex.Message}")
            End Try
        End While
    End Sub

    Public Async Function WaitForNavAsync(
        browser As Microsoft.Web.WebView2.WinForms.WebView2,
       Optional ct As CancellationToken = Nothing) As Task(Of Boolean)

        Dim tcs As New TaskCompletionSource(Of Boolean)()

        ' ---- navigation‑completed handler ----
        Dim navHandler As EventHandler(Of CoreWebView2NavigationCompletedEventArgs) = Nothing
        navHandler =
        Sub(sender As Object, args As CoreWebView2NavigationCompletedEventArgs)
            tcs.TrySetResult(args.IsSuccess)
            RemoveHandler browser.NavigationCompleted, navHandler
        End Sub
        AddHandler browser.NavigationCompleted, navHandler

        ' ---- safety timeout (30 s) ----
        Dim completedTask = Await Task.WhenAny(tcs.Task, Task.Delay(30000, ct))
        If completedTask Is tcs.Task Then
            Return tcs.Task.Result          ' true = success, false = failed navigation
        Else
            RemoveHandler browser.NavigationCompleted, navHandler
            Return False                    ' timed‑out
        End If
    End Function

    Public Function GetFileContent(
   filePath As String,
   Optional forceReload As Boolean = False
) As String
        If Not forceReload AndAlso Globals.FileContents.ContainsKey(filePath) Then
            Return Globals.FileContents(filePath)
        End If

        ' Read from disk
        Dim text = ReadFileContent(filePath)
        ' Update cache
        Globals.FileContents(filePath) = text
        Return text
    End Function

End Module
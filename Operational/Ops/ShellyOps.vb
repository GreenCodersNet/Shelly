' ###  ShellyOps.vb - v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Threading
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports System.Text
Imports Newtonsoft.Json

Module ShellyOps

    ' ### Truncate Maximum Tokens
    Public Function TruncateTextToMaxTokens(inputText As String, maxTokens As Integer) As String
        If String.IsNullOrEmpty(inputText) Then Return inputText

        ' Approximate tokens by dividing the character count by 4
        Dim approxTokens As Integer = CInt(Math.Ceiling(inputText.Length / 4.0))

        If approxTokens <= maxTokens Then
            ' No truncation needed
            Return inputText
        End If

        ' Calculate how many characters correspond to 'maxTokens'
        Dim maxChars As Integer = maxTokens * 4

        If inputText.Length > maxChars Then
            ' Truncate and add a short note
            Return inputText.Substring(0, maxChars) & " ...[TRUNCATED FOR LENGTH]..."
        Else
            Return inputText
        End If
    End Function

    ' ### Revise the User prompt to best match ChatGPT
    Public Async Function ReviseUserQuestionAsync(originalQuestion As String, ct As CancellationToken) As Task(Of String)
        Try
            ' If the original prompt contains code blocks (```), skip revision to preserve exact content.
            If originalQuestion.Contains("```") Then
                Return originalQuestion
            End If

            ' Revised system prompt for clarity:
            Dim revisionPrompt As String =
"Please review the following user prompt. If it is already clear and well-structured, output it exactly as given, without any changes. 
If not, restructure it to clarify multiple tasks by listing them as bullet points, ensuring the logical order is maintained, 
and correct any grammatical issues. Do not add commentary or modify the meaning."

            Dim revizeMessages As New List(Of Dictionary(Of String, String)) From {
                New Dictionary(Of String, String) From {{"role", "system"}, {"content", revisionPrompt}},
                New Dictionary(Of String, String) From {{"role", "user"}, {"content", originalQuestion}}
            }

            Dim revisedQuestion As String = Await AIcall.CallGPTCore(Config.OpenAiApiKey, Config.AiModel, revizeMessages, 0.7, ct)

            ' Fallback: if revision returns an empty result or indicates no change, return original
            If String.IsNullOrWhiteSpace(revisedQuestion) OrElse revisedQuestion.Trim().ToLower().Contains("return the same exact prompt") Then
                Return originalQuestion
            End If

            Return revisedQuestion.Trim()
        Catch ex As OperationCanceledException
            Return originalQuestion
        Catch ex As Exception
            Debug.WriteLine($"ReviseUserQuestionAsync error: {ex.Message}")
            Return originalQuestion
        End Try
    End Function

    ' ============== Hints =================
    ' List of hints
    Private HintTitle As New List(Of String) From {
        "Use with caution:", '0
        "Not the results your were hopping for?", '1
        "Office files? ", '2
        "Remember: this is a FREE version!", '3
        "AI may have bad days:", '4
        "No results?", '5
        "Why Shelly?", '6
        "Sensitive data? Go PRO:", '7
        "Need more?", '8
        "Are you a developer?", '9
        "Remember: this is a FREE version!" '10
    }

    Private HintMessages As New List(Of String) From {
        "Do not share sensitive data!", '0
        "Try again with a diffrent prompt!", '1
        "Go PRO for advanced Office operations!", '2
        "Go PRO for advanced multitasking.", '3
        "Check the OpenAI (status.openai.com) regularly!", '4
        "Reinforce: PowerShell/Custom Functions.", '5
        "No Cloud, no extra accounts, always in control!", '6
        "Use 'Work profile' to include your Company AI.", '7
        "Visit GreenCoders.net and simply ask for it!", '8
        "Go PRO for generating complete projects.", '9
        "It comes with great limitations." '10
    }

    Private HintIndex As Integer = 0 ' Track which hint to show next

    ' Function to Show Next Hint (Called by Timer in Shelly.vb)
    Public Sub ShowNextHint()
        ' Check if HintsText exists before updating
        If Shelly.HintsText Is Nothing OrElse Shelly.HintsText.IsDisposed Then Return

        ' Update HintsText with the current hint
        Shelly.HintsText.Text = HintMessages(HintIndex)
        Shelly.HintsTitleText.Text = HintTitle(HintIndex)

        ' Move to the next hint (loop back if at the end)
        HintIndex = (HintIndex + 1) Mod HintMessages.Count
    End Sub


    Public ReadOnly DefaultPath As String = "C:\\Shelly"

    Public Sub EnsureShellyFolderExists()
        Try
            If Not Directory.Exists(DefaultPath) Then
                Directory.CreateDirectory(DefaultPath)
                Debug.WriteLine("Created folder: " & DefaultPath)
            Else
                Debug.WriteLine("Folder already exists: " & DefaultPath)
            End If
        Catch ex As UnauthorizedAccessException
            MessageBox.Show($"Access denied when trying to create {DefaultPath}.{vbCrLf}Please run the app as administrator.", "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As Exception
            MessageBox.Show("Unexpected error creating folder: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' READ FILES INSIDE THE RESOURCES (ShellyAI.[file_name])

    Public Function LoadTextResource(resourceName As String) As String
        Try
            Dim asm As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()
            Using stream As IO.Stream = asm.GetManifestResourceStream(resourceName)
                If stream Is Nothing Then
                    Dim availableResources As String = String.Join(Environment.NewLine,
                                                                   asm.GetManifestResourceNames())
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

    Public Sub AppendResultToBox(content As String,
                             Optional assistantIntro As String = Nothing,
                             Optional addToHistory As Boolean = True)

        Dim box = Shelly.Instance.PSFunctResultsBox

        If Not String.IsNullOrWhiteSpace(content) Then
            content = content.Trim()

            ' Check if the content contains the specific JSON marker: "tool":
            If content.Contains("""tool"":") Then
                ' Log the JSON to the debug console
                Debug.WriteLine("[DEBUG] JSON with 'tool' parameter detected in AppendResultToBox:")
                Debug.WriteLine(content)

                ' Show a user-friendly error message
                box.AppendText(Environment.NewLine)
                box.AppendText("-----------------------------" & Environment.NewLine)
                box.AppendText("Something went wrong: please check the Debug Console." & Environment.NewLine)
            Else
                ' Append the content to the results box
                box.AppendText(Environment.NewLine)
                box.AppendText("-----------------------------" & Environment.NewLine)

                If Not String.IsNullOrWhiteSpace(assistantIntro) Then
                    box.AppendText(assistantIntro.Trim() & Environment.NewLine)
                    box.AppendText("-----------------------------" & Environment.NewLine)
                End If

                box.AppendText(content & Environment.NewLine)
            End If

            ' ✅ Also add to history, unless disabled
            If addToHistory Then
                Dim message = CreateHistoryMessage("assistant", content)
                conversationHistory.Add(message)
            End If
        Else
            Debug.WriteLine("[DEBUG] AppendResultToBox received empty or null content.")
        End If
    End Sub


    ' ================================================================
    ' UPDATED FUNCTION: CreateHistoryMessage (Safe Role Assignment)
    ' ================================================================
    Public Function CreateHistoryMessage(role As String, content As String) As Dictionary(Of String, String)
        role = role.Trim().ToLower()
        If role <> "user" AndAlso role <> "assistant" AndAlso role <> "system" Then
            role = "assistant" ' fallback
        End If

        Dim msg As New Dictionary(Of String, String) From {
            {"role", role},
            {"content", content}
        }

        ' Mark as non-summarizable if the content contains code blocks or is very large (over 300 tokens)
        If content.Contains("```") OrElse CalculateTokenCount(content) > 300 Then
            msg.Add("summarizable", "false")
        Else
            msg.Add("summarizable", "true")
        End If

        Return msg
    End Function

    Public Sub RestoreWindow(form As Form)
        If form Is Nothing Then Return

        If form.Visible Then
            If form.WindowState = FormWindowState.Minimized Then
                form.WindowState = FormWindowState.Normal
            End If
            form.BringToFront()
            form.Activate()
        Else
            form.Show()
        End If
    End Sub

End Module

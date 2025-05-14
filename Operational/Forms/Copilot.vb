' ###  Copilot.vb | v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports Microsoft.Web.WebView2.Core
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Controls
Imports System.Windows.Forms.VisualStyles.VisualStyleElement

Public Class Copilot
    ' Ensure that a WebView2 control named "WebViewAiOne", a Button named "btnClearData", and
    ' a RichTextBox named "AiOnePrompt" are added to your form.

    Public Shared Property Instance As Copilot

    Public Sub New()
        InitializeComponent()
        ' Assign this instance to the shared property.
        Instance = Me
    End Sub

    Private Async Sub Copilot_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBarCo(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

        Await InitializeWebView2Async()
    End Sub

    Private Async Function InitializeWebView2Async() As Task
        Try
            ' Define a persistent folder for WebView2 user data.
            Dim userDataFolder As String = Path.Combine(Application.StartupPath, "WebView2Data")

            ' Create the WebView2 environment using the persistent data folder.
            Dim env As CoreWebView2Environment = Await CoreWebView2Environment.CreateAsync(Nothing, userDataFolder)
            Await WebViewAiOne.EnsureCoreWebView2Async(env)

            ' Ensure CoreWebView2 is available before proceeding
            If WebViewAiOne.CoreWebView2 Is Nothing Then
                MessageBox.Show("WebView2 failed to initialize!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Exit Function
            End If

            ' (Optional) Set a custom user agent to mimic a full browser.
            WebViewAiOne.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.60 Safari/537.36"

            ' Inject a script to setup a MutationObserver on the #app div
            Dim observerScript As String = "
            document.addEventListener('DOMContentLoaded', function() {
                var targetNode = document.getElementById('app');
                if(targetNode) {
                    var observer = new MutationObserver(function(mutations, obs) {
                        // Extract only the innerText from the #app div
                        var text = targetNode.innerText;
                        window.chrome.webview.postMessage(text);
                    });
                    observer.observe(targetNode, { childList: true, subtree: true, characterData: true });
                }
            });
        "
            Await WebViewAiOne.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(observerScript)

            ' Subscribe to messages from the web page
            AddHandler WebViewAiOne.CoreWebView2.WebMessageReceived, AddressOf CoreWebView2_WebMessageReceived

            ' Navigate directly to the chat page (user will log in if needed)
            WebViewAiOne.CoreWebView2.Navigate("https://copilot.microsoft.com/chats/")

            ' Wait a few seconds then auto-click the mic button if needed.
            Await Task.Delay(7000)

            ' Ensure CoreWebView2 is still available before executing script
            If WebViewAiOne.CoreWebView2 IsNot Nothing Then
                Dim clickScript As String = "var btn = document.querySelector('button[data-testid=""audio-call-button""]'); if(btn){ btn.click(); }"
                Await WebViewAiOne.CoreWebView2.ExecuteScriptAsync(clickScript)
            Else
                Debug.WriteLine("WebView2 was closed before script execution.")
            End If

        Catch ex As Exception
            MessageBox.Show("Error initializing WebView2: " & ex.Message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function


    ' This event handler receives messages from the web page (i.e. the text from the #app div)
    Private Sub CoreWebView2_WebMessageReceived(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim message As String = e.TryGetWebMessageAsString()
        ' Update the RichTextBox safely from the UI thread.
        If AiOnePrompt.InvokeRequired Then
            AiOnePrompt.Invoke(Sub() AiOnePrompt.Text = message)
        Else
            AiOnePrompt.Text = message
        End If
    End Sub

    ' Clear browser data by deleting all cookies via the WebView2 API.
    Private Sub ClearBrowserData()
        Try
            WebViewAiOne.CoreWebView2.CookieManager.DeleteAllCookies()
            MessageBox.Show("Browser cookies cleared. Restart the application if necessary.")
        Catch ex As Exception
            MessageBox.Show("Error clearing cookies: " & ex.Message)
        End Try
    End Sub

    ' Event handler for the Clear Data button.
    Private Sub btnClearData_Click(sender As Object, e As EventArgs) Handles btnClearData.Click
        WebViewAiOne.CoreWebView2.Reload()
    End Sub

    Private Sub CallShelly_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub WebViewAiOne_Click(sender As Object, e As EventArgs) Handles WebViewAiOne.Click

    End Sub

    Private Sub AiOnePrompt_TextChanged(sender As Object, e As EventArgs) Handles AiOnePrompt.TextChanged

    End Sub

    Private Async Sub NewChat_Click(sender As Object, e As EventArgs) Handles NewChat.Click

        ' First, click the parent element that reveals the dropdown menu.
        Dim newPlusScript As String = "var btn = document.querySelector('button[data-testid=""plus-button""]'); if(btn){ btn.click(); }"
        Await WebViewAiOne.CoreWebView2.ExecuteScriptAsync(newPlusScript)

        ' Wait for the dropdown menu to appear.
        Await Task.Delay(500)

        ' Then, click the new chat button.
        Dim newChatScript As String = "var btn = document.querySelector('button[data-testid=""new-chat-button""]'); if(btn){ btn.click(); }"
        Await WebViewAiOne.CoreWebView2.ExecuteScriptAsync(newChatScript)
    End Sub

    Private Sub CloseCopilot_Click(sender As Object, e As EventArgs) Handles CloseCopilot.Click
        Close()
    End Sub

    Private Async Sub AudioCall_Click(sender As Object, e As EventArgs) Handles AudioCall.Click
        Dim newPlusScript As String = "var btn = document.querySelector('button[data-testid=""audio-call-button""]'); if(btn){ btn.click(); }"
        Await WebViewAiOne.CoreWebView2.ExecuteScriptAsync(newPlusScript)
    End Sub

    ' +++++++++++++ SMOOTH FORM OPENING - REDISGN +++++++++++++
    Public Sub ApplySmoothCustomTitleBarCo(form As Form)
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

    Private Sub Copilot_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Remove focus from all controls
        Me.ActiveControl = Nothing
    End Sub

    Private Sub Copilot_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Globals.ConsoleForm = Nothing
        WebViewAiOne.Dispose()
        ' Stop timers
        Timer4.Stop()
        Timer4.Dispose()
        Timer1.Stop()
        Timer1.Dispose()
        idleTimer.Stop()
        idleTimer.Dispose()
    End Sub

    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

    End Sub

    Private Sub idleTimer_Tick(sender As Object, e As EventArgs) Handles idleTimer.Tick

    End Sub
End Class

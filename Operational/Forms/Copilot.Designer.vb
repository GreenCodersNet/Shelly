<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Copilot
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Copilot))
        WebViewAiOne = New Microsoft.Web.WebView2.WinForms.WebView2()
        btnClearData = New Button()
        Timer1 = New Timer(components)
        Panel1 = New Panel()
        AudioCall = New Button()
        NewChat = New Button()
        CloseCopilot = New Button()
        Timer4 = New Timer(components)
        AiOnePrompt = New RichTextBox()
        idleTimer = New Timer(components)
        CType(WebViewAiOne, ComponentModel.ISupportInitialize).BeginInit()
        Panel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' WebViewAiOne
        ' 
        WebViewAiOne.AllowExternalDrop = True
        WebViewAiOne.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        WebViewAiOne.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        WebViewAiOne.CreationProperties = Nothing
        WebViewAiOne.DefaultBackgroundColor = Color.White
        WebViewAiOne.Location = New Point(0, 40)
        WebViewAiOne.Name = "WebViewAiOne"
        WebViewAiOne.Size = New Size(517, 363)
        WebViewAiOne.TabIndex = 0
        WebViewAiOne.ZoomFactor = 1R
        ' 
        ' btnClearData
        ' 
        btnClearData.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        btnClearData.FlatAppearance.BorderColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        btnClearData.FlatAppearance.BorderSize = 0
        btnClearData.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        btnClearData.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        btnClearData.FlatStyle = FlatStyle.Flat
        btnClearData.Font = New Font("Segoe UI Semibold", 16F, FontStyle.Bold)
        btnClearData.ForeColor = Color.White
        btnClearData.Location = New Point(3, -3)
        btnClearData.Name = "btnClearData"
        btnClearData.Size = New Size(27, 39)
        btnClearData.TabIndex = 1
        btnClearData.Text = "⟲"
        btnClearData.UseVisualStyleBackColor = False
        ' 
        ' Timer1
        ' 
        ' 
        ' Panel1
        ' 
        Panel1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel1.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel1.Controls.Add(AudioCall)
        Panel1.Controls.Add(NewChat)
        Panel1.Controls.Add(CloseCopilot)
        Panel1.Controls.Add(btnClearData)
        Panel1.Location = New Point(12, 357)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(143, 34)
        Panel1.TabIndex = 5
        Panel1.Visible = False
        ' 
        ' AudioCall
        ' 
        AudioCall.BackColor = Color.Transparent
        AudioCall.FlatAppearance.BorderColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        AudioCall.FlatAppearance.BorderSize = 0
        AudioCall.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        AudioCall.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        AudioCall.FlatStyle = FlatStyle.Flat
        AudioCall.Font = New Font("Segoe UI Semibold", 13.25F, FontStyle.Bold)
        AudioCall.ForeColor = Color.White
        AudioCall.Location = New Point(74, -1)
        AudioCall.Name = "AudioCall"
        AudioCall.Size = New Size(31, 31)
        AudioCall.TabIndex = 4
        AudioCall.Text = "👄"
        AudioCall.UseVisualStyleBackColor = False
        ' 
        ' NewChat
        ' 
        NewChat.BackColor = Color.Transparent
        NewChat.FlatAppearance.BorderColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        NewChat.FlatAppearance.BorderSize = 0
        NewChat.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        NewChat.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        NewChat.FlatStyle = FlatStyle.Flat
        NewChat.Font = New Font("Segoe UI Semibold", 13.25F, FontStyle.Bold)
        NewChat.ForeColor = Color.White
        NewChat.Location = New Point(36, 1)
        NewChat.Name = "NewChat"
        NewChat.Size = New Size(29, 31)
        NewChat.TabIndex = 3
        NewChat.Text = "💬"
        NewChat.UseVisualStyleBackColor = False
        ' 
        ' CloseCopilot
        ' 
        CloseCopilot.BackColor = Color.Transparent
        CloseCopilot.FlatAppearance.BorderColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        CloseCopilot.FlatAppearance.BorderSize = 0
        CloseCopilot.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        CloseCopilot.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        CloseCopilot.FlatStyle = FlatStyle.Flat
        CloseCopilot.Font = New Font("Segoe UI Semibold", 13.25F, FontStyle.Bold)
        CloseCopilot.ForeColor = Color.White
        CloseCopilot.Location = New Point(108, 0)
        CloseCopilot.Name = "CloseCopilot"
        CloseCopilot.Size = New Size(31, 31)
        CloseCopilot.TabIndex = 2
        CloseCopilot.Text = "X"
        CloseCopilot.UseVisualStyleBackColor = False
        ' 
        ' Timer4
        ' 
        ' 
        ' AiOnePrompt
        ' 
        AiOnePrompt.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        AiOnePrompt.Location = New Point(35, 64)
        AiOnePrompt.Name = "AiOnePrompt"
        AiOnePrompt.Size = New Size(455, 53)
        AiOnePrompt.TabIndex = 8
        AiOnePrompt.Text = ""
        AiOnePrompt.Visible = False
        ' 
        ' idleTimer
        ' 
        ' 
        ' Copilot
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(515, 403)
        Controls.Add(AiOnePrompt)
        Controls.Add(Panel1)
        Controls.Add(WebViewAiOne)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "Copilot"
        StartPosition = FormStartPosition.WindowsDefaultBounds
        Text = "Copilot"
        CType(WebViewAiOne, ComponentModel.ISupportInitialize).EndInit()
        Panel1.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents WebViewAiOne As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents btnClearData As Button
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Panel1 As Panel
    Friend WithEvents CloseCopilot As Button
    Friend WithEvents NewChat As Button
    Friend WithEvents AudioCall As Button
    Friend WithEvents Timer2 As Timer
    Friend WithEvents Timer3 As Timer
    Friend WithEvents Timer4 As Timer
    Friend WithEvents AiOnePrompt As RichTextBox
    Friend WithEvents idleTimer As Timer
End Class

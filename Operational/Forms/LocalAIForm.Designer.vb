<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class LocalAIForm
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
        Label6 = New Label()
        Label5 = New Label()
        Label3 = New Label()
        localAIModel = New ComboBox()
        Label1 = New Label()
        voiceSelection = New ComboBox()
        userPrompt = New RichTextBox()
        Panel1 = New Panel()
        Label2 = New Label()
        aiPrompt = New RichTextBox()
        Panel2 = New Panel()
        Label4 = New Label()
        Label7 = New Label()
        Label8 = New Label()
        SubmitButton = New Button()
        Panel3 = New Panel()
        CheckBox1 = New CheckBox()
        GlobalApiKey = New TextBox()
        Label9 = New Label()
        OpenFileDialog1 = New OpenFileDialog()
        saveModelButton = New Button()
        deleteModelButton = New Button()
        browseMdoelButton = New Button()
        _lblStatus = New Label()
        UseCPU_RAMcheckbox = New RadioButton()
        UseGPUchekbox = New RadioButton()
        IncludeTTSCheckbox = New CheckBox()
        LabelAIconfirimation = New Label()
        Panel1.SuspendLayout()
        Panel2.SuspendLayout()
        Panel3.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.BackColor = Color.Transparent
        Label6.Font = New Font("Cascadia Code", 12F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(308, 27)
        Label6.Name = "Label6"
        Label6.Size = New Size(145, 21)
        Label6.TabIndex = 71
        Label6.Text = "Shelly Settings"
        ' 
        ' Label5
        ' 
        Label5.BackColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(293, 60)
        Label5.Name = "Label5"
        Label5.Size = New Size(250, 1)
        Label5.TabIndex = 70
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.BackColor = Color.Transparent
        Label3.Font = New Font("Cascadia Code", 9.75F)
        Label3.ForeColor = Color.White
        Label3.Location = New Point(36, 98)
        Label3.Name = "Label3"
        Label3.Size = New Size(120, 17)
        Label3.TabIndex = 72
        Label3.Text = "Local AI Model"
        ' 
        ' localAIModel
        ' 
        localAIModel.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        localAIModel.FlatStyle = FlatStyle.Flat
        localAIModel.Font = New Font("Bahnschrift SemiLight", 10F)
        localAIModel.ForeColor = Color.White
        localAIModel.FormattingEnabled = True
        localAIModel.Location = New Point(36, 121)
        localAIModel.Name = "localAIModel"
        localAIModel.Size = New Size(224, 24)
        localAIModel.TabIndex = 73
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.BackColor = Color.Transparent
        Label1.Font = New Font("Cascadia Code", 9.75F)
        Label1.ForeColor = Color.White
        Label1.Location = New Point(36, 164)
        Label1.Name = "Label1"
        Label1.Size = New Size(48, 17)
        Label1.TabIndex = 74
        Label1.Text = "Voice"
        ' 
        ' voiceSelection
        ' 
        voiceSelection.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        voiceSelection.FlatStyle = FlatStyle.Flat
        voiceSelection.Font = New Font("Bahnschrift SemiLight", 10F)
        voiceSelection.ForeColor = Color.White
        voiceSelection.FormattingEnabled = True
        voiceSelection.Location = New Point(36, 188)
        voiceSelection.Name = "voiceSelection"
        voiceSelection.Size = New Size(224, 24)
        voiceSelection.TabIndex = 75
        ' 
        ' userPrompt
        ' 
        userPrompt.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        userPrompt.BorderStyle = BorderStyle.None
        userPrompt.Location = New Point(45, 274)
        userPrompt.Name = "userPrompt"
        userPrompt.Size = New Size(877, 65)
        userPrompt.TabIndex = 78
        userPrompt.Text = ""
        ' 
        ' Panel1
        ' 
        Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Panel1.BackColor = SystemColors.Window
        Panel1.BorderStyle = BorderStyle.FixedSingle
        Panel1.Controls.Add(Label2)
        Panel1.Location = New Point(36, 268)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(895, 78)
        Panel1.TabIndex = 80
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.BackColor = Color.Transparent
        Label2.Font = New Font("Cascadia Code", 9.75F)
        Label2.ForeColor = Color.White
        Label2.Location = New Point(-1, -12)
        Label2.Name = "Label2"
        Label2.Size = New Size(192, 17)
        Label2.TabIndex = 81
        Label2.Text = "Path to Default Folder:"
        ' 
        ' aiPrompt
        ' 
        aiPrompt.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        aiPrompt.BorderStyle = BorderStyle.None
        aiPrompt.Location = New Point(45, 424)
        aiPrompt.Name = "aiPrompt"
        aiPrompt.Size = New Size(883, 151)
        aiPrompt.TabIndex = 81
        aiPrompt.Text = ""
        ' 
        ' Panel2
        ' 
        Panel2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Panel2.BackColor = SystemColors.Window
        Panel2.BorderStyle = BorderStyle.FixedSingle
        Panel2.Controls.Add(Label4)
        Panel2.Location = New Point(36, 411)
        Panel2.Name = "Panel2"
        Panel2.Size = New Size(901, 174)
        Panel2.TabIndex = 82
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.BackColor = Color.Transparent
        Label4.Font = New Font("Cascadia Code", 9.75F)
        Label4.ForeColor = Color.White
        Label4.Location = New Point(-1, -12)
        Label4.Name = "Label4"
        Label4.Size = New Size(192, 17)
        Label4.TabIndex = 81
        Label4.Text = "Path to Default Folder:"
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.BackColor = Color.Transparent
        Label7.Font = New Font("Cascadia Code", 9.75F)
        Label7.ForeColor = Color.White
        Label7.Location = New Point(36, 240)
        Label7.Name = "Label7"
        Label7.Size = New Size(96, 17)
        Label7.TabIndex = 83
        Label7.Text = "User Prompt"
        ' 
        ' Label8
        ' 
        Label8.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label8.AutoSize = True
        Label8.BackColor = Color.Transparent
        Label8.Font = New Font("Cascadia Code", 9.75F)
        Label8.ForeColor = Color.White
        Label8.Location = New Point(36, 379)
        Label8.Name = "Label8"
        Label8.Size = New Size(80, 17)
        Label8.TabIndex = 84
        Label8.Text = "AI Prompt"
        ' 
        ' SubmitButton
        ' 
        SubmitButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        SubmitButton.BackColor = Color.Transparent
        SubmitButton.Cursor = Cursors.Hand
        SubmitButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SubmitButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SubmitButton.FlatStyle = FlatStyle.Flat
        SubmitButton.Font = New Font("Bahnschrift SemiLight", 10F)
        SubmitButton.ForeColor = Color.Lime
        SubmitButton.Location = New Point(818, 362)
        SubmitButton.Name = "SubmitButton"
        SubmitButton.Size = New Size(119, 34)
        SubmitButton.TabIndex = 85
        SubmitButton.Text = "Submit"
        SubmitButton.UseVisualStyleBackColor = False
        ' 
        ' Panel3
        ' 
        Panel3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel3.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel3.BorderStyle = BorderStyle.FixedSingle
        Panel3.Controls.Add(CheckBox1)
        Panel3.Controls.Add(GlobalApiKey)
        Panel3.Location = New Point(348, 119)
        Panel3.Name = "Panel3"
        Panel3.Size = New Size(372, 30)
        Panel3.TabIndex = 86
        ' 
        ' CheckBox1
        ' 
        CheckBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        CheckBox1.Appearance = Appearance.Button
        CheckBox1.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        CheckBox1.CheckAlign = ContentAlignment.MiddleCenter
        CheckBox1.FlatAppearance.BorderColor = Color.Gray
        CheckBox1.FlatStyle = FlatStyle.Flat
        CheckBox1.Font = New Font("Arial Narrow", 15.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        CheckBox1.ForeColor = Color.White
        CheckBox1.Location = New Point(550, -4)
        CheckBox1.Margin = New Padding(0)
        CheckBox1.Name = "CheckBox1"
        CheckBox1.Size = New Size(38, 33)
        CheckBox1.TabIndex = 37
        CheckBox1.Text = "👁‍🗨"
        CheckBox1.TextAlign = ContentAlignment.MiddleCenter
        CheckBox1.UseVisualStyleBackColor = False
        ' 
        ' GlobalApiKey
        ' 
        GlobalApiKey.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        GlobalApiKey.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        GlobalApiKey.BorderStyle = BorderStyle.None
        GlobalApiKey.Font = New Font("Bahnschrift SemiLight", 10F)
        GlobalApiKey.ForeColor = Color.LightGreen
        GlobalApiKey.Location = New Point(5, 6)
        GlobalApiKey.Name = "GlobalApiKey"
        GlobalApiKey.PlaceholderText = "...guff"
        GlobalApiKey.Size = New Size(362, 17)
        GlobalApiKey.TabIndex = 5
        GlobalApiKey.Text = ".gguf"
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.BackColor = Color.Transparent
        Label9.Font = New Font("Cascadia Code", 9.75F)
        Label9.ForeColor = Color.White
        Label9.Location = New Point(348, 99)
        Label9.Name = "Label9"
        Label9.Size = New Size(80, 17)
        Label9.TabIndex = 87
        Label9.Text = "New Model"
        ' 
        ' OpenFileDialog1
        ' 
        OpenFileDialog1.FileName = "OpenFileDialog1"
        ' 
        ' saveModelButton
        ' 
        saveModelButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        saveModelButton.BackColor = Color.Transparent
        saveModelButton.Cursor = Cursors.Hand
        saveModelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        saveModelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        saveModelButton.FlatStyle = FlatStyle.Flat
        saveModelButton.Font = New Font("Bahnschrift SemiLight", 10F)
        saveModelButton.ForeColor = Color.Lime
        saveModelButton.Location = New Point(818, 117)
        saveModelButton.Name = "saveModelButton"
        saveModelButton.Size = New Size(119, 34)
        saveModelButton.TabIndex = 88
        saveModelButton.Text = "Save Model"
        saveModelButton.UseVisualStyleBackColor = False
        ' 
        ' deleteModelButton
        ' 
        deleteModelButton.BackColor = Color.Transparent
        deleteModelButton.Cursor = Cursors.Hand
        deleteModelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        deleteModelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        deleteModelButton.FlatStyle = FlatStyle.Flat
        deleteModelButton.Font = New Font("Bahnschrift SemiLight", 10F)
        deleteModelButton.ForeColor = Color.Red
        deleteModelButton.Location = New Point(269, 121)
        deleteModelButton.Name = "deleteModelButton"
        deleteModelButton.Size = New Size(25, 24)
        deleteModelButton.TabIndex = 89
        deleteModelButton.Text = "X"
        deleteModelButton.UseVisualStyleBackColor = False
        ' 
        ' browseMdoelButton
        ' 
        browseMdoelButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        browseMdoelButton.BackColor = Color.Transparent
        browseMdoelButton.Cursor = Cursors.Hand
        browseMdoelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        browseMdoelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        browseMdoelButton.FlatStyle = FlatStyle.Flat
        browseMdoelButton.Font = New Font("Bahnschrift SemiLight", 10F)
        browseMdoelButton.ForeColor = Color.DarkGray
        browseMdoelButton.Location = New Point(726, 117)
        browseMdoelButton.Name = "browseMdoelButton"
        browseMdoelButton.Size = New Size(86, 34)
        browseMdoelButton.TabIndex = 90
        browseMdoelButton.Text = "Browse"
        browseMdoelButton.UseVisualStyleBackColor = False
        ' 
        ' _lblStatus
        ' 
        _lblStatus.AutoSize = True
        _lblStatus.ForeColor = Color.Gray
        _lblStatus.Location = New Point(156, 100)
        _lblStatus.Name = "_lblStatus"
        _lblStatus.Size = New Size(104, 15)
        _lblStatus.TabIndex = 91
        _lblStatus.Text = "Status: Not loaded"
        ' 
        ' UseCPU_RAMcheckbox
        ' 
        UseCPU_RAMcheckbox.AutoSize = True
        UseCPU_RAMcheckbox.Font = New Font("Bahnschrift SemiLight", 10F)
        UseCPU_RAMcheckbox.ForeColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        UseCPU_RAMcheckbox.Location = New Point(348, 190)
        UseCPU_RAMcheckbox.Name = "UseCPU_RAMcheckbox"
        UseCPU_RAMcheckbox.Size = New Size(87, 21)
        UseCPU_RAMcheckbox.TabIndex = 92
        UseCPU_RAMcheckbox.TabStop = True
        UseCPU_RAMcheckbox.Text = "CPU/RAM"
        UseCPU_RAMcheckbox.UseVisualStyleBackColor = True
        ' 
        ' UseGPUchekbox
        ' 
        UseGPUchekbox.AutoSize = True
        UseGPUchekbox.Font = New Font("Bahnschrift SemiLight", 10F)
        UseGPUchekbox.ForeColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        UseGPUchekbox.Location = New Point(441, 190)
        UseGPUchekbox.Name = "UseGPUchekbox"
        UseGPUchekbox.Size = New Size(53, 21)
        UseGPUchekbox.TabIndex = 94
        UseGPUchekbox.TabStop = True
        UseGPUchekbox.Text = "GPU"
        UseGPUchekbox.UseVisualStyleBackColor = True
        ' 
        ' IncludeTTSCheckbox
        ' 
        IncludeTTSCheckbox.AutoSize = True
        IncludeTTSCheckbox.Font = New Font("Bahnschrift SemiLight", 10F)
        IncludeTTSCheckbox.ForeColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        IncludeTTSCheckbox.Location = New Point(500, 191)
        IncludeTTSCheckbox.Name = "IncludeTTSCheckbox"
        IncludeTTSCheckbox.Size = New Size(102, 21)
        IncludeTTSCheckbox.TabIndex = 95
        IncludeTTSCheckbox.Text = "Include TTS"
        IncludeTTSCheckbox.UseVisualStyleBackColor = True
        ' 
        ' LabelAIconfirimation
        ' 
        LabelAIconfirimation.AutoSize = True
        LabelAIconfirimation.ForeColor = Color.BlueViolet
        LabelAIconfirimation.Location = New Point(338, 230)
        LabelAIconfirimation.Name = "LabelAIconfirimation"
        LabelAIconfirimation.Size = New Size(47, 15)
        LabelAIconfirimation.TabIndex = 96
        LabelAIconfirimation.Text = "Label10"
        ' 
        ' LocalAIForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(968, 614)
        Controls.Add(LabelAIconfirimation)
        Controls.Add(IncludeTTSCheckbox)
        Controls.Add(UseGPUchekbox)
        Controls.Add(UseCPU_RAMcheckbox)
        Controls.Add(_lblStatus)
        Controls.Add(browseMdoelButton)
        Controls.Add(deleteModelButton)
        Controls.Add(saveModelButton)
        Controls.Add(Label9)
        Controls.Add(Panel3)
        Controls.Add(SubmitButton)
        Controls.Add(Label8)
        Controls.Add(Label7)
        Controls.Add(aiPrompt)
        Controls.Add(Panel2)
        Controls.Add(userPrompt)
        Controls.Add(voiceSelection)
        Controls.Add(Label1)
        Controls.Add(localAIModel)
        Controls.Add(Label3)
        Controls.Add(Label6)
        Controls.Add(Label5)
        Controls.Add(Panel1)
        MaximizeBox = False
        MinimumSize = New Size(816, 653)
        Name = "LocalAIForm"
        Text = "LocalAIForm"
        TopMost = True
        Panel1.ResumeLayout(False)
        Panel1.PerformLayout()
        Panel2.ResumeLayout(False)
        Panel2.PerformLayout()
        Panel3.ResumeLayout(False)
        Panel3.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents localAIModel As ComboBox
    Friend WithEvents Label1 As Label
    Friend WithEvents voiceSelection As ComboBox
    Friend WithEvents userPrompt As RichTextBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents Label2 As Label
    Friend WithEvents aiPrompt As RichTextBox
    Friend WithEvents Panel2 As Panel
    Friend WithEvents Label4 As Label
    Friend WithEvents Label7 As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents SubmitButton As Button
    Friend WithEvents Panel3 As Panel
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents GlobalApiKey As TextBox
    Friend WithEvents Label9 As Label
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents saveModelButton As Button
    Friend WithEvents deleteModelButton As Button
    Friend WithEvents browseMdoelButton As Button
    Private WithEvents _lblStatus As Label
    Friend WithEvents UseCPU_RAMcheckbox As RadioButton
    Friend WithEvents UseGPUchekbox As RadioButton
    Friend WithEvents IncludeTTSCheckbox As CheckBox
    Friend WithEvents LabelAIconfirimation As Label
End Class

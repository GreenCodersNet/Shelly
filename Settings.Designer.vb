<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Settings
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Settings))
        Label1 = New Label()
        Label2 = New Label()
        aiSelection = New ComboBox()
        ComboBoxDevices = New ComboBox()
        Label3 = New Label()
        GlobalApiKey = New TextBox()
        CheckBox1 = New CheckBox()
        Panel3 = New Panel()
        SaveSettings = New Button()
        Label4 = New Label()
        Label5 = New Label()
        Label6 = New Label()
        AssitantID = New TextBox()
        Panel4 = New Panel()
        Label7 = New Label()
        AboutButton = New Button()
        ProButton = New Button()
        IAmHumanButton = New Button()
        Timer1 = New Timer(components)
        ToolTip1 = New ToolTip(components)
        CheckBoxHints = New CheckBox()
        CheckBoxPromptRevision = New CheckBox()
        DefaultFolderWindow = New Button()
        Button1 = New Button()
        Label9 = New Label()
        Label8 = New Label()
        Panel3.SuspendLayout()
        Panel4.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label1.AutoSize = True
        Label1.BackColor = Color.Transparent
        Label1.Font = New Font("Cascadia Code", 9.75F)
        Label1.ForeColor = Color.White
        Label1.Location = New Point(25, 153)
        Label1.Name = "Label1"
        Label1.Size = New Size(88, 17)
        Label1.TabIndex = 1
        Label1.Text = "OpenAI API"
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label2.AutoSize = True
        Label2.BackColor = Color.Transparent
        Label2.Font = New Font("Cascadia Code", 9.75F)
        Label2.ForeColor = Color.White
        Label2.Location = New Point(26, 360)
        Label2.Name = "Label2"
        Label2.Size = New Size(64, 17)
        Label2.TabIndex = 2
        Label2.Text = "Options"
        ' 
        ' aiSelection
        ' 
        aiSelection.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        aiSelection.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        aiSelection.FlatStyle = FlatStyle.Flat
        aiSelection.Font = New Font("Bahnschrift SemiLight", 10F)
        aiSelection.ForeColor = Color.White
        aiSelection.FormattingEnabled = True
        aiSelection.Location = New Point(26, 105)
        aiSelection.Name = "aiSelection"
        aiSelection.Size = New Size(224, 24)
        aiSelection.TabIndex = 54
        ' 
        ' ComboBoxDevices
        ' 
        ComboBoxDevices.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ComboBoxDevices.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ComboBoxDevices.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxDevices.FlatStyle = FlatStyle.Flat
        ComboBoxDevices.Font = New Font("Bahnschrift SemiLight", 10F)
        ComboBoxDevices.ForeColor = Color.White
        ComboBoxDevices.FormattingEnabled = True
        ComboBoxDevices.Location = New Point(28, 304)
        ComboBoxDevices.Name = "ComboBoxDevices"
        ComboBoxDevices.Size = New Size(414, 24)
        ComboBoxDevices.TabIndex = 59
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label3.AutoSize = True
        Label3.BackColor = Color.Transparent
        Label3.Font = New Font("Cascadia Code", 9.75F)
        Label3.ForeColor = Color.White
        Label3.Location = New Point(24, 84)
        Label3.Name = "Label3"
        Label3.Size = New Size(72, 17)
        Label3.TabIndex = 60
        Label3.Text = "AI Model"
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
        GlobalApiKey.PasswordChar = "●"c
        GlobalApiKey.PlaceholderText = "E.g: sk-proj-i23w1jFtTOrP11..."
        GlobalApiKey.Size = New Size(372, 17)
        GlobalApiKey.TabIndex = 5
        GlobalApiKey.Text = "mypass"
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
        CheckBox1.Location = New Point(380, -4)
        CheckBox1.Margin = New Padding(0)
        CheckBox1.Name = "CheckBox1"
        CheckBox1.Size = New Size(38, 33)
        CheckBox1.TabIndex = 37
        CheckBox1.Text = "👁‍🗨"
        CheckBox1.TextAlign = ContentAlignment.MiddleCenter
        CheckBox1.UseVisualStyleBackColor = False
        ' 
        ' Panel3
        ' 
        Panel3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel3.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel3.BorderStyle = BorderStyle.FixedSingle
        Panel3.Controls.Add(CheckBox1)
        Panel3.Controls.Add(GlobalApiKey)
        Panel3.Location = New Point(26, 174)
        Panel3.Name = "Panel3"
        Panel3.Size = New Size(416, 30)
        Panel3.TabIndex = 62
        ' 
        ' SaveSettings
        ' 
        SaveSettings.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        SaveSettings.BackColor = Color.Transparent
        SaveSettings.Cursor = Cursors.Hand
        SaveSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SaveSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SaveSettings.FlatStyle = FlatStyle.Flat
        SaveSettings.Font = New Font("Bahnschrift SemiLight", 10F)
        SaveSettings.ForeColor = Color.Lime
        SaveSettings.Location = New Point(28, 468)
        SaveSettings.Name = "SaveSettings"
        SaveSettings.Size = New Size(479, 39)
        SaveSettings.TabIndex = 64
        SaveSettings.Text = "Save  Settings"
        SaveSettings.UseVisualStyleBackColor = False
        ' 
        ' Label4
        ' 
        Label4.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label4.AutoSize = True
        Label4.BackColor = Color.Transparent
        Label4.Font = New Font("Cascadia Code", 9.75F)
        Label4.ForeColor = Color.White
        Label4.Location = New Point(25, 283)
        Label4.Name = "Label4"
        Label4.Size = New Size(120, 17)
        Label4.TabIndex = 65
        Label4.Text = "Audio Settings"
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label5.BackColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(293, 60)
        Label5.Name = "Label5"
        Label5.Size = New Size(250, 1)
        Label5.TabIndex = 68
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label6.AutoSize = True
        Label6.BackColor = Color.Transparent
        Label6.Font = New Font("Cascadia Code", 12F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(308, 27)
        Label6.Name = "Label6"
        Label6.Size = New Size(145, 21)
        Label6.TabIndex = 69
        Label6.Text = "Shelly Settings"
        ' 
        ' AssitantID
        ' 
        AssitantID.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        AssitantID.BorderStyle = BorderStyle.None
        AssitantID.Font = New Font("Bahnschrift SemiLight", 10F)
        AssitantID.ForeColor = Color.White
        AssitantID.Location = New Point(5, 6)
        AssitantID.Name = "AssitantID"
        AssitantID.PlaceholderText = "E.g: asst_nVXv4Yx30LrJT"
        AssitantID.Size = New Size(405, 17)
        AssitantID.TabIndex = 70
        ' 
        ' Panel4
        ' 
        Panel4.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel4.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel4.BorderStyle = BorderStyle.FixedSingle
        Panel4.Controls.Add(AssitantID)
        Panel4.Location = New Point(26, 237)
        Panel4.Name = "Panel4"
        Panel4.Size = New Size(415, 32)
        Panel4.TabIndex = 71
        ' 
        ' Label7
        ' 
        Label7.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label7.AutoSize = True
        Label7.BackColor = Color.Transparent
        Label7.Font = New Font("Cascadia Code", 9.75F)
        Label7.ForeColor = Color.White
        Label7.Location = New Point(25, 217)
        Label7.Name = "Label7"
        Label7.Size = New Size(104, 17)
        Label7.TabIndex = 72
        Label7.Text = "Assistant ID"
        ' 
        ' AboutButton
        ' 
        AboutButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        AboutButton.BackColor = Color.Transparent
        AboutButton.Cursor = Cursors.Hand
        AboutButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        AboutButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        AboutButton.FlatStyle = FlatStyle.Flat
        AboutButton.Font = New Font("Bahnschrift SemiLight", 10F)
        AboutButton.ForeColor = Color.MediumOrchid
        AboutButton.Location = New Point(28, 386)
        AboutButton.Name = "AboutButton"
        AboutButton.Size = New Size(115, 32)
        AboutButton.TabIndex = 73
        AboutButton.Text = "About"
        AboutButton.UseVisualStyleBackColor = False
        ' 
        ' ProButton
        ' 
        ProButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ProButton.BackColor = Color.Transparent
        ProButton.Cursor = Cursors.Hand
        ProButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        ProButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        ProButton.FlatStyle = FlatStyle.Flat
        ProButton.Font = New Font("Bahnschrift SemiLight", 10F)
        ProButton.ForeColor = Color.MediumOrchid
        ProButton.Location = New Point(270, 386)
        ProButton.Name = "ProButton"
        ProButton.Size = New Size(115, 32)
        ProButton.TabIndex = 74
        ProButton.Text = " Shelly PRO"
        ProButton.UseVisualStyleBackColor = False
        ' 
        ' IAmHumanButton
        ' 
        IAmHumanButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        IAmHumanButton.BackColor = Color.Transparent
        IAmHumanButton.BackgroundImageLayout = ImageLayout.Stretch
        IAmHumanButton.Cursor = Cursors.Hand
        IAmHumanButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        IAmHumanButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        IAmHumanButton.FlatStyle = FlatStyle.Flat
        IAmHumanButton.Font = New Font("Bahnschrift SemiLight", 10F)
        IAmHumanButton.ForeColor = Color.Cyan
        IAmHumanButton.Location = New Point(538, 386)
        IAmHumanButton.Name = "IAmHumanButton"
        IAmHumanButton.Size = New Size(115, 32)
        IAmHumanButton.TabIndex = 75
        IAmHumanButton.Text = "I Am Human"
        IAmHumanButton.UseVisualStyleBackColor = False
        ' 
        ' Timer1
        ' 
        Timer1.Interval = 5000
        ' 
        ' CheckBoxHints
        ' 
        CheckBoxHints.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        CheckBoxHints.Appearance = Appearance.Button
        CheckBoxHints.AutoSize = True
        CheckBoxHints.BackColor = Color.Maroon
        CheckBoxHints.Cursor = Cursors.Hand
        CheckBoxHints.FlatAppearance.BorderColor = Color.White
        CheckBoxHints.FlatAppearance.CheckedBackColor = Color.FromArgb(CByte(0), CByte(64), CByte(0))
        CheckBoxHints.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(0), CByte(64), CByte(0))
        CheckBoxHints.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(0), CByte(64), CByte(0))
        CheckBoxHints.FlatStyle = FlatStyle.Flat
        CheckBoxHints.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxHints.ForeColor = Color.White
        CheckBoxHints.Location = New Point(676, 104)
        CheckBoxHints.Name = "CheckBoxHints"
        CheckBoxHints.Size = New Size(81, 27)
        CheckBoxHints.TabIndex = 77
        CheckBoxHints.Text = "Hints OFF"
        CheckBoxHints.UseVisualStyleBackColor = False
        ' 
        ' CheckBoxPromptRevision
        ' 
        CheckBoxPromptRevision.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        CheckBoxPromptRevision.Appearance = Appearance.Button
        CheckBoxPromptRevision.AutoSize = True
        CheckBoxPromptRevision.BackColor = Color.Maroon
        CheckBoxPromptRevision.Cursor = Cursors.Hand
        CheckBoxPromptRevision.FlatAppearance.BorderColor = Color.White
        CheckBoxPromptRevision.FlatAppearance.CheckedBackColor = Color.FromArgb(CByte(0), CByte(64), CByte(0))
        CheckBoxPromptRevision.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(0), CByte(64), CByte(0))
        CheckBoxPromptRevision.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(0), CByte(64), CByte(0))
        CheckBoxPromptRevision.FlatStyle = FlatStyle.Flat
        CheckBoxPromptRevision.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxPromptRevision.ForeColor = Color.White
        CheckBoxPromptRevision.Location = New Point(545, 104)
        CheckBoxPromptRevision.Name = "CheckBoxPromptRevision"
        CheckBoxPromptRevision.Size = New Size(118, 27)
        CheckBoxPromptRevision.TabIndex = 78
        CheckBoxPromptRevision.Text = "Rephrasing ON "
        CheckBoxPromptRevision.UseVisualStyleBackColor = False
        ' 
        ' DefaultFolderWindow
        ' 
        DefaultFolderWindow.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        DefaultFolderWindow.BackColor = Color.Transparent
        DefaultFolderWindow.Cursor = Cursors.Hand
        DefaultFolderWindow.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        DefaultFolderWindow.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        DefaultFolderWindow.FlatStyle = FlatStyle.Flat
        DefaultFolderWindow.Font = New Font("Bahnschrift SemiLight", 10F)
        DefaultFolderWindow.ForeColor = Color.MediumOrchid
        DefaultFolderWindow.Location = New Point(149, 386)
        DefaultFolderWindow.Name = "DefaultFolderWindow"
        DefaultFolderWindow.Size = New Size(115, 32)
        DefaultFolderWindow.TabIndex = 79
        DefaultFolderWindow.Text = "AI Training"
        DefaultFolderWindow.UseVisualStyleBackColor = False
        ' 
        ' Button1
        ' 
        Button1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Button1.BackColor = Color.Transparent
        Button1.BackgroundImageLayout = ImageLayout.Stretch
        Button1.Cursor = Cursors.Hand
        Button1.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        Button1.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        Button1.FlatStyle = FlatStyle.Flat
        Button1.Font = New Font("Bahnschrift SemiLight", 10F)
        Button1.ForeColor = Color.Cyan
        Button1.Location = New Point(659, 386)
        Button1.Name = "Button1"
        Button1.Size = New Size(115, 32)
        Button1.TabIndex = 80
        Button1.Text = "Digital Cells"
        Button1.UseVisualStyleBackColor = False
        ' 
        ' Label9
        ' 
        Label9.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Label9.AutoSize = True
        Label9.BackColor = Color.Transparent
        Label9.Font = New Font("Cascadia Code", 9.75F)
        Label9.ForeColor = Color.White
        Label9.Location = New Point(538, 360)
        Label9.Name = "Label9"
        Label9.Size = New Size(72, 17)
        Label9.TabIndex = 82
        Label9.Text = "Explore:"
        ' 
        ' Label8
        ' 
        Label8.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Label8.BackColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Label8.Font = New Font("Cascadia Code", 9.75F)
        Label8.ForeColor = Color.White
        Label8.Location = New Point(506, 350)
        Label8.Name = "Label8"
        Label8.Size = New Size(1, 100)
        Label8.TabIndex = 83
        ' 
        ' Settings
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        BackgroundImageLayout = ImageLayout.Stretch
        ClientSize = New Size(794, 526)
        Controls.Add(Label8)
        Controls.Add(Label9)
        Controls.Add(Button1)
        Controls.Add(DefaultFolderWindow)
        Controls.Add(CheckBoxPromptRevision)
        Controls.Add(CheckBoxHints)
        Controls.Add(IAmHumanButton)
        Controls.Add(ProButton)
        Controls.Add(AboutButton)
        Controls.Add(Label7)
        Controls.Add(Panel4)
        Controls.Add(Label6)
        Controls.Add(Label5)
        Controls.Add(Label4)
        Controls.Add(SaveSettings)
        Controls.Add(Panel3)
        Controls.Add(Label3)
        Controls.Add(ComboBoxDevices)
        Controls.Add(aiSelection)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Location = New Point(548, 629)
        MaximizeBox = False
        MaximumSize = New Size(810, 565)
        MinimizeBox = False
        MinimumSize = New Size(810, 565)
        Name = "Settings"
        StartPosition = FormStartPosition.WindowsDefaultBounds
        Text = "Shelly Settings"
        Panel3.ResumeLayout(False)
        Panel3.PerformLayout()
        Panel4.ResumeLayout(False)
        Panel4.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents aiSelection As ComboBox
    Friend WithEvents ComboBoxDevices As ComboBox
    Friend WithEvents Label3 As Label
    Friend WithEvents GlobalApiKey As TextBox
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents Panel3 As Panel
    Friend WithEvents SaveSettings As Button
    Friend WithEvents Label4 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents AssitantID As TextBox
    Friend WithEvents Panel4 As Panel
    Friend WithEvents Label7 As Label
    Friend WithEvents AboutButton As Button
    Friend WithEvents ProButton As Button
    Friend WithEvents IAmHumanButton As Button
    Friend WithEvents Timer1 As Timer
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents CheckBoxHints As CheckBox
    Friend WithEvents CheckBoxPromptRevision As CheckBox
    Friend WithEvents DefaultFolderWindow As Button
    Friend WithEvents Button1 As Button
    Friend WithEvents Label9 As Label
    Friend WithEvents Label8 As Label
End Class

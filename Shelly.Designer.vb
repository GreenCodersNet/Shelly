<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Shelly
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        components = New ComponentModel.Container()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Shelly))
        pnlDrag = New Panel()
        WebView21 = New Microsoft.Web.WebView2.WinForms.WebView2()
        AIcommentBox = New RichTextBox()
        AIresponseErrorBox = New RichTextBox()
        ToolTip1 = New ToolTip(components)
        WebView2Google = New Microsoft.Web.WebView2.WinForms.WebView2()
        PSFunctResultsContextMenu = New ContextMenuStrip(components)
        OpenInNewWindowToolStripMenuItem = New ToolStripMenuItem()
        PSFunctResultsContextMenu2 = New ContextMenuStrip(components)
        OpenCopilotToolStripMenuItem = New ToolStripMenuItem()
        ConsoleToolStripMenuItem = New ToolStripMenuItem()
        HintTime = New Timer(components)
        Panel4 = New Panel()
        HintsTitleText = New Label()
        HintsText = New Label()
        PictureBox1 = New PictureBox()
        Panel3 = New Panel()
        LabelStatusUpdate = New Label()
        ExitButton = New Button()
        Button1 = New Button()
        ShowSettings = New Button()
        ButtonUseSpeech = New Button()
        Button4 = New Button()
        CancelTaskButton = New Button()
        Panel1 = New Panel()
        UserInputBox = New RichTextBox()
        Panel2 = New Panel()
        PSFunctResultsBox = New RichTextBox()
        ChatPanel = New Panel()
        Panel5 = New Panel()
        pnlDrag.SuspendLayout()
        CType(WebView21, ComponentModel.ISupportInitialize).BeginInit()
        CType(WebView2Google, ComponentModel.ISupportInitialize).BeginInit()
        PSFunctResultsContextMenu.SuspendLayout()
        PSFunctResultsContextMenu2.SuspendLayout()
        Panel4.SuspendLayout()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        Panel3.SuspendLayout()
        Panel1.SuspendLayout()
        Panel2.SuspendLayout()
        ChatPanel.SuspendLayout()
        Panel5.SuspendLayout()
        SuspendLayout()
        ' 
        ' pnlDrag
        ' 
        pnlDrag.Anchor = AnchorStyles.Bottom
        pnlDrag.BackColor = Color.Transparent
        pnlDrag.BackgroundImage = My.Resources.Resources.circle_SHELLY_extra_small
        pnlDrag.BackgroundImageLayout = ImageLayout.Center
        pnlDrag.Controls.Add(WebView21)
        pnlDrag.Cursor = Cursors.Hand
        pnlDrag.Location = New Point(332, 117)
        pnlDrag.Name = "pnlDrag"
        pnlDrag.Size = New Size(90, 90)
        pnlDrag.TabIndex = 1
        ' 
        ' WebView21
        ' 
        WebView21.AllowExternalDrop = True
        WebView21.Anchor = AnchorStyles.Left Or AnchorStyles.Right
        WebView21.BackColor = SystemColors.ButtonFace
        WebView21.CreationProperties = Nothing
        WebView21.DefaultBackgroundColor = Color.Transparent
        WebView21.Enabled = False
        WebView21.Location = New Point(0, 0)
        WebView21.MaximumSize = New Size(90, 90)
        WebView21.MinimumSize = New Size(90, 90)
        WebView21.Name = "WebView21"
        WebView21.Size = New Size(90, 90)
        WebView21.TabIndex = 0
        WebView21.ZoomFactor = 1R
        ' 
        ' AIcommentBox
        ' 
        AIcommentBox.Location = New Point(519, 41)
        AIcommentBox.Name = "AIcommentBox"
        AIcommentBox.Size = New Size(100, 49)
        AIcommentBox.TabIndex = 5
        AIcommentBox.Text = ""
        AIcommentBox.Visible = False
        ' 
        ' AIresponseErrorBox
        ' 
        AIresponseErrorBox.Location = New Point(275, 14)
        AIresponseErrorBox.Name = "AIresponseErrorBox"
        AIresponseErrorBox.Size = New Size(73, 17)
        AIresponseErrorBox.TabIndex = 6
        AIresponseErrorBox.Text = ""
        AIresponseErrorBox.Visible = False
        ' 
        ' WebView2Google
        ' 
        WebView2Google.AllowExternalDrop = True
        WebView2Google.CreationProperties = Nothing
        WebView2Google.DefaultBackgroundColor = Color.White
        WebView2Google.Location = New Point(352, 12)
        WebView2Google.Name = "WebView2Google"
        WebView2Google.Size = New Size(75, 23)
        WebView2Google.TabIndex = 7
        WebView2Google.Visible = False
        WebView2Google.ZoomFactor = 1R
        ' 
        ' PSFunctResultsContextMenu
        ' 
        PSFunctResultsContextMenu.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        PSFunctResultsContextMenu.Font = New Font("Bahnschrift SemiLight", 9.75F)
        PSFunctResultsContextMenu.Items.AddRange(New ToolStripItem() {OpenInNewWindowToolStripMenuItem})
        PSFunctResultsContextMenu.Name = "PSFunctResultsContextMenu"
        PSFunctResultsContextMenu.Size = New Size(191, 26)
        ' 
        ' OpenInNewWindowToolStripMenuItem
        ' 
        OpenInNewWindowToolStripMenuItem.Name = "OpenInNewWindowToolStripMenuItem"
        OpenInNewWindowToolStripMenuItem.Size = New Size(190, 22)
        OpenInNewWindowToolStripMenuItem.Text = "Open in new window"
        ' 
        ' PSFunctResultsContextMenu2
        ' 
        PSFunctResultsContextMenu2.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        PSFunctResultsContextMenu2.Font = New Font("Bahnschrift SemiLight", 9.75F)
        PSFunctResultsContextMenu2.Items.AddRange(New ToolStripItem() {OpenCopilotToolStripMenuItem, ConsoleToolStripMenuItem})
        PSFunctResultsContextMenu2.Name = "PSFunctResultsContextMenu2"
        PSFunctResultsContextMenu2.Size = New Size(147, 48)
        ' 
        ' OpenCopilotToolStripMenuItem
        ' 
        OpenCopilotToolStripMenuItem.Name = "OpenCopilotToolStripMenuItem"
        OpenCopilotToolStripMenuItem.Size = New Size(146, 22)
        OpenCopilotToolStripMenuItem.Text = "Open Copilot"
        ' 
        ' ConsoleToolStripMenuItem
        ' 
        ConsoleToolStripMenuItem.Name = "ConsoleToolStripMenuItem"
        ConsoleToolStripMenuItem.Size = New Size(146, 22)
        ConsoleToolStripMenuItem.Text = "Console"
        ' 
        ' HintTime
        ' 
        ' 
        ' Panel4
        ' 
        Panel4.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel4.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel4.BackgroundImageLayout = ImageLayout.Center
        Panel4.Controls.Add(HintsTitleText)
        Panel4.Controls.Add(HintsText)
        Panel4.Controls.Add(PictureBox1)
        Panel4.Location = New Point(2, 3)
        Panel4.Name = "Panel4"
        Panel4.Size = New Size(318, 40)
        Panel4.TabIndex = 9
        ' 
        ' HintsTitleText
        ' 
        HintsTitleText.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        HintsTitleText.AutoSize = True
        HintsTitleText.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        HintsTitleText.Font = New Font("Bahnschrift", 11F)
        HintsTitleText.ForeColor = Color.Gold
        HintsTitleText.Location = New Point(32, 2)
        HintsTitleText.MaximumSize = New Size(283, 18)
        HintsTitleText.MinimumSize = New Size(283, 18)
        HintsTitleText.Name = "HintsTitleText"
        HintsTitleText.Size = New Size(283, 18)
        HintsTitleText.TabIndex = 11
        HintsTitleText.Text = "Note: this is a FREE version!"
        ' 
        ' HintsText
        ' 
        HintsText.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        HintsText.AutoSize = True
        HintsText.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        HintsText.Font = New Font("Bahnschrift", 9.75F)
        HintsText.ForeColor = Color.White
        HintsText.Location = New Point(32, 21)
        HintsText.MaximumSize = New Size(283, 16)
        HintsText.MinimumSize = New Size(283, 16)
        HintsText.Name = "HintsText"
        HintsText.Size = New Size(283, 16)
        HintsText.TabIndex = 10
        HintsText.Text = "It comes with great limitations."
        ' 
        ' PictureBox1
        ' 
        PictureBox1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        PictureBox1.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        PictureBox1.BackgroundImage = My.Resources.Resources.hint_ico2
        PictureBox1.BackgroundImageLayout = ImageLayout.Zoom
        PictureBox1.Location = New Point(3, 7)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(24, 24)
        PictureBox1.TabIndex = 8
        PictureBox1.TabStop = False
        ' 
        ' Panel3
        ' 
        Panel3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel3.BackColor = Color.Transparent
        Panel3.Controls.Add(LabelStatusUpdate)
        Panel3.Controls.Add(ExitButton)
        Panel3.Controls.Add(Button1)
        Panel3.Controls.Add(ShowSettings)
        Panel3.Controls.Add(ButtonUseSpeech)
        Panel3.Controls.Add(Button4)
        Panel3.Controls.Add(CancelTaskButton)
        Panel3.Cursor = Cursors.SizeAll
        Panel3.Location = New Point(3, 158)
        Panel3.Name = "Panel3"
        Panel3.Size = New Size(317, 66)
        Panel3.TabIndex = 10
        ' 
        ' LabelStatusUpdate
        ' 
        LabelStatusUpdate.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        LabelStatusUpdate.AutoSize = True
        LabelStatusUpdate.BackColor = Color.Transparent
        LabelStatusUpdate.Font = New Font("Bahnschrift SemiLight", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelStatusUpdate.ForeColor = Color.White
        LabelStatusUpdate.Location = New Point(3, 8)
        LabelStatusUpdate.Name = "LabelStatusUpdate"
        LabelStatusUpdate.Size = New Size(43, 16)
        LabelStatusUpdate.TabIndex = 5
        LabelStatusUpdate.Text = "Label1"
        ' 
        ' ExitButton
        ' 
        ExitButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ExitButton.BackColor = Color.Transparent
        ExitButton.BackgroundImage = My.Resources.Resources.exit1
        ExitButton.BackgroundImageLayout = ImageLayout.Stretch
        ExitButton.Cursor = Cursors.Hand
        ExitButton.FlatAppearance.BorderColor = Color.BlueViolet
        ExitButton.FlatAppearance.BorderSize = 0
        ExitButton.FlatAppearance.CheckedBackColor = Color.Transparent
        ExitButton.FlatAppearance.MouseDownBackColor = Color.Transparent
        ExitButton.FlatAppearance.MouseOverBackColor = Color.Transparent
        ExitButton.FlatStyle = FlatStyle.Flat
        ExitButton.Font = New Font("Arial Narrow", 16.75F)
        ExitButton.ForeColor = Color.Transparent
        ExitButton.Location = New Point(283, 38)
        ExitButton.Name = "ExitButton"
        ExitButton.Size = New Size(25, 21)
        ExitButton.TabIndex = 2
        ExitButton.UseVisualStyleBackColor = False
        ' 
        ' Button1
        ' 
        Button1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Button1.BackColor = Color.Transparent
        Button1.BackgroundImage = My.Resources.Resources.start
        Button1.BackgroundImageLayout = ImageLayout.Zoom
        Button1.Cursor = Cursors.Hand
        Button1.FlatAppearance.BorderColor = Color.BlueViolet
        Button1.FlatAppearance.BorderSize = 0
        Button1.FlatAppearance.CheckedBackColor = Color.Transparent
        Button1.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button1.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button1.FlatStyle = FlatStyle.Flat
        Button1.Font = New Font("Arial Narrow", 16.25F)
        Button1.ForeColor = Color.LimeGreen
        Button1.Location = New Point(6, 38)
        Button1.Name = "Button1"
        Button1.Size = New Size(25, 21)
        Button1.TabIndex = 8
        Button1.TextAlign = ContentAlignment.MiddleRight
        Button1.UseVisualStyleBackColor = False
        ' 
        ' ShowSettings
        ' 
        ShowSettings.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        ShowSettings.BackColor = Color.Transparent
        ShowSettings.BackgroundImage = My.Resources.Resources.settings
        ShowSettings.BackgroundImageLayout = ImageLayout.Zoom
        ShowSettings.Cursor = Cursors.Hand
        ShowSettings.FlatAppearance.BorderColor = Color.BlueViolet
        ShowSettings.FlatAppearance.BorderSize = 0
        ShowSettings.FlatAppearance.CheckedBackColor = Color.Transparent
        ShowSettings.FlatAppearance.MouseDownBackColor = Color.Transparent
        ShowSettings.FlatAppearance.MouseOverBackColor = Color.Transparent
        ShowSettings.FlatStyle = FlatStyle.Flat
        ShowSettings.Font = New Font("Arial Narrow", 16.75F)
        ShowSettings.ForeColor = SystemColors.ButtonFace
        ShowSettings.Location = New Point(194, 39)
        ShowSettings.Name = "ShowSettings"
        ShowSettings.Size = New Size(25, 21)
        ShowSettings.TabIndex = 9
        ShowSettings.UseVisualStyleBackColor = False
        ' 
        ' ButtonUseSpeech
        ' 
        ButtonUseSpeech.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        ButtonUseSpeech.BackColor = Color.Transparent
        ButtonUseSpeech.BackgroundImage = My.Resources.Resources.mic
        ButtonUseSpeech.BackgroundImageLayout = ImageLayout.Stretch
        ButtonUseSpeech.Cursor = Cursors.Hand
        ButtonUseSpeech.FlatAppearance.BorderColor = Color.BlueViolet
        ButtonUseSpeech.FlatAppearance.BorderSize = 0
        ButtonUseSpeech.FlatAppearance.CheckedBackColor = Color.Transparent
        ButtonUseSpeech.FlatAppearance.MouseDownBackColor = Color.Transparent
        ButtonUseSpeech.FlatAppearance.MouseOverBackColor = Color.Transparent
        ButtonUseSpeech.FlatStyle = FlatStyle.Flat
        ButtonUseSpeech.Font = New Font("Arial Narrow", 12.75F)
        ButtonUseSpeech.ForeColor = SystemColors.ButtonFace
        ButtonUseSpeech.ImageAlign = ContentAlignment.MiddleRight
        ButtonUseSpeech.Location = New Point(122, 38)
        ButtonUseSpeech.Name = "ButtonUseSpeech"
        ButtonUseSpeech.Size = New Size(25, 21)
        ButtonUseSpeech.TabIndex = 1
        ButtonUseSpeech.UseVisualStyleBackColor = False
        ' 
        ' Button4
        ' 
        Button4.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        Button4.BackColor = Color.Transparent
        Button4.BackgroundImage = My.Resources.Resources._new
        Button4.BackgroundImageLayout = ImageLayout.Zoom
        Button4.Cursor = Cursors.Hand
        Button4.FlatAppearance.BorderColor = Color.BlueViolet
        Button4.FlatAppearance.BorderSize = 0
        Button4.FlatAppearance.CheckedBackColor = Color.Transparent
        Button4.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button4.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button4.FlatStyle = FlatStyle.Flat
        Button4.Font = New Font("Arial Narrow", 16.25F)
        Button4.ForeColor = SystemColors.ButtonFace
        Button4.Location = New Point(41, 38)
        Button4.Name = "Button4"
        Button4.Size = New Size(25, 21)
        Button4.TabIndex = 7
        Button4.UseVisualStyleBackColor = False
        ' 
        ' CancelTaskButton
        ' 
        CancelTaskButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        CancelTaskButton.BackColor = Color.Transparent
        CancelTaskButton.BackgroundImage = My.Resources.Resources.cancel
        CancelTaskButton.BackgroundImageLayout = ImageLayout.Zoom
        CancelTaskButton.Cursor = Cursors.Hand
        CancelTaskButton.FlatAppearance.BorderColor = Color.BlueViolet
        CancelTaskButton.FlatAppearance.BorderSize = 0
        CancelTaskButton.FlatAppearance.CheckedBackColor = Color.Transparent
        CancelTaskButton.FlatAppearance.MouseDownBackColor = Color.Transparent
        CancelTaskButton.FlatAppearance.MouseOverBackColor = Color.Transparent
        CancelTaskButton.FlatStyle = FlatStyle.Flat
        CancelTaskButton.Font = New Font("Arial Narrow", 16.75F)
        CancelTaskButton.ForeColor = SystemColors.ButtonFace
        CancelTaskButton.Location = New Point(159, 39)
        CancelTaskButton.Name = "CancelTaskButton"
        CancelTaskButton.Size = New Size(25, 21)
        CancelTaskButton.TabIndex = 6
        CancelTaskButton.UseVisualStyleBackColor = False
        ' 
        ' Panel1
        ' 
        Panel1.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel1.Controls.Add(UserInputBox)
        Panel1.Location = New Point(2, 3)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(318, 65)
        Panel1.TabIndex = 0
        ' 
        ' UserInputBox
        ' 
        UserInputBox.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        UserInputBox.BorderStyle = BorderStyle.None
        UserInputBox.Font = New Font("Bahnschrift", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        UserInputBox.ForeColor = Color.White
        UserInputBox.Location = New Point(5, 4)
        UserInputBox.Name = "UserInputBox"
        UserInputBox.Size = New Size(308, 57)
        UserInputBox.TabIndex = 3
        UserInputBox.Text = ""
        ' 
        ' Panel2
        ' 
        Panel2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel2.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel2.Controls.Add(PSFunctResultsBox)
        Panel2.Location = New Point(2, 71)
        Panel2.Name = "Panel2"
        Panel2.Size = New Size(318, 91)
        Panel2.TabIndex = 4
        ' 
        ' PSFunctResultsBox
        ' 
        PSFunctResultsBox.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        PSFunctResultsBox.BorderStyle = BorderStyle.None
        PSFunctResultsBox.Font = New Font("Bahnschrift", 9.75F)
        PSFunctResultsBox.ForeColor = Color.Lime
        PSFunctResultsBox.Location = New Point(7, 5)
        PSFunctResultsBox.Name = "PSFunctResultsBox"
        PSFunctResultsBox.ReadOnly = True
        PSFunctResultsBox.Size = New Size(308, 79)
        PSFunctResultsBox.TabIndex = 4
        PSFunctResultsBox.Text = ""
        ' 
        ' ChatPanel
        ' 
        ChatPanel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ChatPanel.BackColor = SystemColors.Highlight
        ChatPanel.BackgroundImage = My.Resources.Resources.sc_background2
        ChatPanel.BackgroundImageLayout = ImageLayout.Stretch
        ChatPanel.Controls.Add(Panel2)
        ChatPanel.Controls.Add(Panel1)
        ChatPanel.Controls.Add(Panel3)
        ChatPanel.Font = New Font("Arial Narrow", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        ChatPanel.Location = New Point(4, 51)
        ChatPanel.Name = "ChatPanel"
        ChatPanel.Size = New Size(322, 227)
        ChatPanel.TabIndex = 5
        ChatPanel.Visible = False
        ' 
        ' Panel5
        ' 
        Panel5.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Panel5.BackgroundImage = My.Resources.Resources.sc_background2
        Panel5.Controls.Add(Panel4)
        Panel5.Location = New Point(4, 0)
        Panel5.Name = "Panel5"
        Panel5.Size = New Size(322, 46)
        Panel5.TabIndex = 10
        Panel5.Visible = False
        ' 
        ' Shelly
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(427, 279)
        Controls.Add(Panel5)
        Controls.Add(WebView2Google)
        Controls.Add(AIcommentBox)
        Controls.Add(ChatPanel)
        Controls.Add(AIresponseErrorBox)
        Controls.Add(pnlDrag)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximumSize = New Size(443, 318)
        MinimumSize = New Size(443, 318)
        Name = "Shelly"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Shelly"
        WindowState = FormWindowState.Minimized
        pnlDrag.ResumeLayout(False)
        CType(WebView21, ComponentModel.ISupportInitialize).EndInit()
        CType(WebView2Google, ComponentModel.ISupportInitialize).EndInit()
        PSFunctResultsContextMenu.ResumeLayout(False)
        PSFunctResultsContextMenu2.ResumeLayout(False)
        Panel4.ResumeLayout(False)
        Panel4.PerformLayout()
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        Panel3.ResumeLayout(False)
        Panel3.PerformLayout()
        Panel1.ResumeLayout(False)
        Panel2.ResumeLayout(False)
        ChatPanel.ResumeLayout(False)
        Panel5.ResumeLayout(False)
        ResumeLayout(False)
    End Sub
    Friend WithEvents pnlDrag As Panel
    Friend WithEvents AIcommentBox As RichTextBox
    Friend WithEvents AIresponseErrorBox As RichTextBox
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents WebView2Google As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents PSFunctResultsContextMenu As ContextMenuStrip
    Friend WithEvents OpenInNewWindowToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents PSFunctResultsContextMenu2 As ContextMenuStrip
    Friend WithEvents OpenCopilotToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ConsoleToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents HintTimer As Timer
    Friend WithEvents HintTime As Timer
    Friend WithEvents Panel4 As Panel
    Private WithEvents Panel3 As Panel
    Friend WithEvents ExitButton As Button
    Friend WithEvents Button1 As Button
    Friend WithEvents ShowSettings As Button
    Friend WithEvents ButtonUseSpeech As Button
    Friend WithEvents Button4 As Button
    Friend WithEvents CancelTaskButton As Button
    Friend WithEvents Panel1 As Panel
    Friend WithEvents UserInputBox As RichTextBox
    Friend WithEvents Panel2 As Panel
    Friend WithEvents PSFunctResultsBox As RichTextBox
    Friend WithEvents LabelStatusUpdate As Label
    Friend WithEvents ChatPanel As Panel
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Panel5 As Panel
    Friend WithEvents HintsText As Label
    Friend WithEvents HintsTitleText As Label
    Friend WithEvents WebView21 As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents PictureBox2 As PictureBox
End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Console
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Console))
        ShellyConsole = New RichTextBox()
        Panel1 = New Panel()
        Panel2 = New Panel()
        UserInput = New RichTextBox()
        Panel1.SuspendLayout()
        Panel2.SuspendLayout()
        SuspendLayout()
        ' 
        ' ShellyConsole
        ' 
        ShellyConsole.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        ShellyConsole.BackColor = Color.MidnightBlue
        ShellyConsole.BorderStyle = BorderStyle.None
        ShellyConsole.Font = New Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        ShellyConsole.ForeColor = SystemColors.HighlightText
        ShellyConsole.Location = New Point(27, 58)
        ShellyConsole.Name = "ShellyConsole"
        ShellyConsole.ScrollBars = RichTextBoxScrollBars.Vertical
        ShellyConsole.Size = New Size(657, 315)
        ShellyConsole.TabIndex = 4
        ShellyConsole.Text = ""
        ' 
        ' Panel1
        ' 
        Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Panel1.BackColor = Color.MidnightBlue
        Panel1.Controls.Add(Panel2)
        Panel1.Location = New Point(21, 51)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(669, 367)
        Panel1.TabIndex = 5
        ' 
        ' Panel2
        ' 
        Panel2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Panel2.BorderStyle = BorderStyle.FixedSingle
        Panel2.Controls.Add(UserInput)
        Panel2.Location = New Point(6, 331)
        Panel2.Name = "Panel2"
        Panel2.Size = New Size(657, 31)
        Panel2.TabIndex = 6
        ' 
        ' UserInput
        ' 
        UserInput.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        UserInput.BackColor = Color.MidnightBlue
        UserInput.BorderStyle = BorderStyle.None
        UserInput.Font = New Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        UserInput.ForeColor = SystemColors.HighlightText
        UserInput.Location = New Point(6, 4)
        UserInput.Name = "UserInput"
        UserInput.Size = New Size(655, 22)
        UserInput.TabIndex = 6
        UserInput.Text = ""
        ' 
        ' Console
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.MidnightBlue
        ClientSize = New Size(711, 430)
        Controls.Add(ShellyConsole)
        Controls.Add(Panel1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MinimumSize = New Size(727, 469)
        Name = "Console"
        Text = "Console | v 1.0"
        Panel1.ResumeLayout(False)
        Panel2.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents ShellyConsole As RichTextBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents UserInput As RichTextBox
    Friend WithEvents Panel2 As Panel
End Class

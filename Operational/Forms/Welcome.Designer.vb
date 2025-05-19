<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Welcome
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Welcome))
        Label2 = New Label()
        CloseButton = New Button()
        Label1 = New Label()
        LearnMoreButton = New Button()
        Label3 = New Label()
        SuspendLayout()
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.None
        Label2.AutoSize = True
        Label2.Font = New Font("Segoe UI", 26.25F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label2.ForeColor = SystemColors.ButtonFace
        Label2.Location = New Point(208, 131)
        Label2.Name = "Label2"
        Label2.Size = New Size(537, 47)
        Label2.TabIndex = 1
        Label2.Text = "Thank you for test driving Shelly. "
        Label2.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' CloseButton
        ' 
        CloseButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        CloseButton.BackColor = Color.Transparent
        CloseButton.BackgroundImageLayout = ImageLayout.Stretch
        CloseButton.Cursor = Cursors.Hand
        CloseButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        CloseButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        CloseButton.FlatStyle = FlatStyle.Flat
        CloseButton.Font = New Font("Bahnschrift SemiLight", 10F)
        CloseButton.ForeColor = SystemColors.ButtonFace
        CloseButton.Location = New Point(771, 392)
        CloseButton.Name = "CloseButton"
        CloseButton.Size = New Size(115, 54)
        CloseButton.TabIndex = 85
        CloseButton.Text = "Continue"
        CloseButton.UseVisualStyleBackColor = False
        ' 
        ' Label1
        ' 
        Label1.Anchor = AnchorStyles.None
        Label1.AutoSize = True
        Label1.Font = New Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label1.ForeColor = SystemColors.ButtonFace
        Label1.ImageAlign = ContentAlignment.TopLeft
        Label1.Location = New Point(56, 196)
        Label1.Name = "Label1"
        Label1.Size = New Size(804, 126)
        Label1.TabIndex = 86
        Label1.Text = resources.GetString("Label1.Text")
        Label1.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' LearnMoreButton
        ' 
        LearnMoreButton.Anchor = AnchorStyles.None
        LearnMoreButton.BackColor = Color.Transparent
        LearnMoreButton.BackgroundImageLayout = ImageLayout.Stretch
        LearnMoreButton.Cursor = Cursors.Hand
        LearnMoreButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        LearnMoreButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        LearnMoreButton.FlatStyle = FlatStyle.Flat
        LearnMoreButton.Font = New Font("Bahnschrift SemiLight", 10F)
        LearnMoreButton.ForeColor = Color.Turquoise
        LearnMoreButton.Location = New Point(337, 341)
        LearnMoreButton.Name = "LearnMoreButton"
        LearnMoreButton.Size = New Size(250, 31)
        LearnMoreButton.TabIndex = 87
        LearnMoreButton.Text = "Visit the official Shelly page"
        LearnMoreButton.UseVisualStyleBackColor = False
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.None
        Label3.AutoSize = True
        Label3.Font = New Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label3.ForeColor = Color.White
        Label3.Location = New Point(689, 105)
        Label3.Name = "Label3"
        Label3.Size = New Size(56, 30)
        Label3.TabIndex = 88
        Label3.Text = "1.0.1"
        Label3.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Welcome
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(910, 470)
        Controls.Add(Label3)
        Controls.Add(LearnMoreButton)
        Controls.Add(Label1)
        Controls.Add(CloseButton)
        Controls.Add(Label2)
        Name = "Welcome"
        Text = "Welcome"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label2 As Label
    Friend WithEvents CloseButton As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents LearnMoreButton As Button
    Friend WithEvents Label3 As Label
End Class

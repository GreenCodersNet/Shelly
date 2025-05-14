<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class RebootForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(RebootForm))
        Label6 = New Label()
        Label5 = New Label()
        Label3 = New Label()
        YesButton = New Button()
        NoButton = New Button()
        PictureBox1 = New PictureBox()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label6.AutoSize = True
        Label6.Font = New Font("Segoe UI", 16F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(256, 19)
        Label6.Name = "Label6"
        Label6.Size = New Size(168, 30)
        Label6.TabIndex = 73
        Label6.Text = "Restart required"
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label5.BackColor = Color.Red
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(256, 54)
        Label5.Name = "Label5"
        Label5.Size = New Size(250, 1)
        Label5.TabIndex = 72
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label3.AutoSize = True
        Label3.BackColor = Color.Transparent
        Label3.Font = New Font("Cascadia Code", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label3.ForeColor = Color.PaleGoldenrod
        Label3.Location = New Point(114, 75)
        Label3.MaximumSize = New Size(500, 55)
        Label3.Name = "Label3"
        Label3.Size = New Size(496, 42)
        Label3.TabIndex = 74
        Label3.Text = "A restart is required for the changes to take effect. Do you wish to restart the application now?"
        ' 
        ' YesButton
        ' 
        YesButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        YesButton.BackColor = Color.Transparent
        YesButton.Cursor = Cursors.Hand
        YesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        YesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        YesButton.FlatStyle = FlatStyle.Flat
        YesButton.Font = New Font("Bahnschrift SemiLight", 10F)
        YesButton.ForeColor = Color.LawnGreen
        YesButton.Location = New Point(189, 144)
        YesButton.Name = "YesButton"
        YesButton.Size = New Size(115, 32)
        YesButton.TabIndex = 76
        YesButton.Text = "Yes"
        YesButton.UseVisualStyleBackColor = False
        ' 
        ' NoButton
        ' 
        NoButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        NoButton.BackColor = Color.Transparent
        NoButton.Cursor = Cursors.Hand
        NoButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        NoButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        NoButton.FlatStyle = FlatStyle.Flat
        NoButton.Font = New Font("Bahnschrift SemiLight", 10F)
        NoButton.ForeColor = Color.Coral
        NoButton.Location = New Point(340, 144)
        NoButton.Name = "NoButton"
        NoButton.Size = New Size(115, 32)
        NoButton.TabIndex = 77
        NoButton.Text = "Later"
        NoButton.UseVisualStyleBackColor = False
        ' 
        ' PictureBox1
        ' 
        PictureBox1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), Image)
        PictureBox1.Location = New Point(8, 73)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(90, 82)
        PictureBox1.SizeMode = PictureBoxSizeMode.CenterImage
        PictureBox1.TabIndex = 78
        PictureBox1.TabStop = False
        ' 
        ' RebootForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.MidnightBlue
        ClientSize = New Size(657, 192)
        Controls.Add(PictureBox1)
        Controls.Add(NoButton)
        Controls.Add(YesButton)
        Controls.Add(Label3)
        Controls.Add(Label6)
        Controls.Add(Label5)
        MaximumSize = New Size(673, 249)
        MinimumSize = New Size(673, 231)
        Name = "RebootForm"
        Text = "RebootForm"
        TopMost = True
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents YesButton As Button
    Friend WithEvents NoButton As Button
    Friend WithEvents PictureBox1 As PictureBox
End Class

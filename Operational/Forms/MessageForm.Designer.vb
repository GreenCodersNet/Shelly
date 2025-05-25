<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MessageForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MessageForm))
        LabelMessage = New Label()
        PictureBox1 = New PictureBox()
        ButtonOK = New Button()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' LabelMessage
        ' 
        LabelMessage.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        LabelMessage.BackColor = Color.Transparent
        LabelMessage.Font = New Font("Cascadia Code", 11.25F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelMessage.ForeColor = Color.Gold
        LabelMessage.Location = New Point(108, 22)
        LabelMessage.MaximumSize = New Size(620, 125)
        LabelMessage.MinimumSize = New Size(580, 125)
        LabelMessage.Name = "LabelMessage"
        LabelMessage.Padding = New Padding(15, 0, 0, 0)
        LabelMessage.Size = New Size(580, 125)
        LabelMessage.TabIndex = 75
        LabelMessage.Text = "A restart is required for the changes to take effect. Do you wish to restart the application now?"
        LabelMessage.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' PictureBox1
        ' 
        PictureBox1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), Image)
        PictureBox1.Location = New Point(12, 65)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(90, 82)
        PictureBox1.SizeMode = PictureBoxSizeMode.CenterImage
        PictureBox1.TabIndex = 79
        PictureBox1.TabStop = False
        ' 
        ' ButtonOK
        ' 
        ButtonOK.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ButtonOK.BackColor = Color.Transparent
        ButtonOK.Cursor = Cursors.Hand
        ButtonOK.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        ButtonOK.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        ButtonOK.FlatStyle = FlatStyle.Flat
        ButtonOK.Font = New Font("Bahnschrift SemiLight", 10F)
        ButtonOK.ForeColor = Color.LawnGreen
        ButtonOK.Location = New Point(301, 165)
        ButtonOK.Name = "ButtonOK"
        ButtonOK.Size = New Size(115, 32)
        ButtonOK.TabIndex = 80
        ButtonOK.Text = "O.K."
        ButtonOK.UseVisualStyleBackColor = False
        ' 
        ' MessageForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.MidnightBlue
        ClientSize = New Size(713, 209)
        Controls.Add(ButtonOK)
        Controls.Add(PictureBox1)
        Controls.Add(LabelMessage)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximumSize = New Size(729, 248)
        MinimumSize = New Size(729, 248)
        Name = "MessageForm"
        Text = "MessageForm"
        TopMost = True
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents LabelMessage As Label
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents ButtonOK As Button
End Class

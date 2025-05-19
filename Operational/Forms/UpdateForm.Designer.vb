<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UpdateForm
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(UpdateForm))
        PictureBox1 = New PictureBox()
        NoButton = New Button()
        YesButton = New Button()
        Label3 = New Label()
        Label6 = New Label()
        Label5 = New Label()
        LabelURL = New Label()
        LinkLabel1 = New LinkLabel()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' PictureBox1
        ' 
        PictureBox1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        PictureBox1.Image = My.Resources.Resources.lab
        PictureBox1.Location = New Point(12, 93)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(90, 82)
        PictureBox1.SizeMode = PictureBoxSizeMode.Zoom
        PictureBox1.TabIndex = 84
        PictureBox1.TabStop = False
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
        NoButton.Location = New Point(346, 209)
        NoButton.Name = "NoButton"
        NoButton.Size = New Size(115, 32)
        NoButton.TabIndex = 83
        NoButton.Text = "Later"
        NoButton.UseVisualStyleBackColor = False
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
        YesButton.Location = New Point(193, 209)
        YesButton.Name = "YesButton"
        YesButton.Size = New Size(115, 32)
        YesButton.TabIndex = 82
        YesButton.Text = "Yes"
        YesButton.UseVisualStyleBackColor = False
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label3.AutoSize = True
        Label3.BackColor = Color.Transparent
        Label3.Font = New Font("Cascadia Code", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label3.ForeColor = Color.PaleGoldenrod
        Label3.Location = New Point(124, 97)
        Label3.MaximumSize = New Size(500, 55)
        Label3.Name = "Label3"
        Label3.Size = New Size(496, 42)
        Label3.TabIndex = 81
        Label3.Text = "A restart is required for the changes to take effect. Do you wish to restart the application now?"
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label6.AutoSize = True
        Label6.Font = New Font("Segoe UI", 16F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(194, 19)
        Label6.Name = "Label6"
        Label6.Size = New Size(269, 30)
        Label6.TabIndex = 80
        Label6.Text = "We got an update for you!"
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label5.BackColor = Color.Red
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(202, 54)
        Label5.Name = "Label5"
        Label5.Size = New Size(270, 1)
        Label5.TabIndex = 79
        ' 
        ' LabelURL
        ' 
        LabelURL.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        LabelURL.AutoSize = True
        LabelURL.BackColor = Color.Transparent
        LabelURL.Font = New Font("Cascadia Code", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelURL.ForeColor = Color.White
        LabelURL.Location = New Point(124, 154)
        LabelURL.MaximumSize = New Size(500, 55)
        LabelURL.Name = "LabelURL"
        LabelURL.Size = New Size(200, 17)
        LabelURL.TabIndex = 85
        LabelURL.Text = "Here is what you'll get:"
        ' 
        ' LinkLabel1
        ' 
        LinkLabel1.ActiveLinkColor = Color.BlueViolet
        LinkLabel1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        LinkLabel1.AutoSize = True
        LinkLabel1.Font = New Font("Cascadia Code", 9.75F)
        LinkLabel1.LinkColor = Color.Turquoise
        LinkLabel1.Location = New Point(322, 154)
        LinkLabel1.Name = "LinkLabel1"
        LinkLabel1.Size = New Size(128, 17)
        LinkLabel1.TabIndex = 86
        LinkLabel1.TabStop = True
        LinkLabel1.Text = "Read Change Log"
        ' 
        ' UpdateForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(657, 257)
        Controls.Add(LinkLabel1)
        Controls.Add(LabelURL)
        Controls.Add(PictureBox1)
        Controls.Add(NoButton)
        Controls.Add(YesButton)
        Controls.Add(Label3)
        Controls.Add(Label6)
        Controls.Add(Label5)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximumSize = New Size(673, 296)
        MinimumSize = New Size(673, 296)
        Name = "UpdateForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Is time for some cool updates!"
        TopMost = True
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents NoButton As Button
    Friend WithEvents YesButton As Button
    Friend WithEvents Label3 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents LabelURL As Label
    Friend WithEvents LinkLabel1 As LinkLabel
End Class

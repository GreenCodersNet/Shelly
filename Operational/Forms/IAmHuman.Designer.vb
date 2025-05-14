<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class IAmHuman
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(IAmHuman))
        Label1 = New Label()
        Label6 = New Label()
        Label5 = New Label()
        Timer1 = New Timer(components)
        PictureBox1 = New PictureBox()
        Label2 = New Label()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label1.AutoSize = True
        Label1.Font = New Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label1.ForeColor = Color.White
        Label1.Location = New Point(38, 83)
        Label1.MaximumSize = New Size(788, 0)
        Label1.MinimumSize = New Size(306, 0)
        Label1.Name = "Label1"
        Label1.Size = New Size(306, 21)
        Label1.TabIndex = 0
        Label1.Text = "..."
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label6.AutoSize = True
        Label6.Font = New Font("Segoe UI", 16F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(348, 18)
        Label6.Name = "Label6"
        Label6.Size = New Size(134, 30)
        Label6.TabIndex = 71
        Label6.Text = "I Am Human"
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label5.BackColor = Color.Red
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(348, 54)
        Label5.Name = "Label5"
        Label5.Size = New Size(250, 1)
        Label5.TabIndex = 70
        ' 
        ' Timer1
        ' 
        ' 
        ' PictureBox1
        ' 
        PictureBox1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        PictureBox1.Image = My.Resources.Resources.ai_face
        PictureBox1.Location = New Point(488, 1)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(343, 259)
        PictureBox1.SizeMode = PictureBoxSizeMode.StretchImage
        PictureBox1.TabIndex = 72
        PictureBox1.TabStop = False
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label2.AutoSize = True
        Label2.BackColor = Color.Black
        Label2.Cursor = Cursors.Hand
        Label2.Font = New Font("Segoe UI", 18F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label2.ForeColor = Color.LawnGreen
        Label2.Location = New Point(389, 209)
        Label2.Name = "Label2"
        Label2.Size = New Size(48, 32)
        Label2.TabIndex = 73
        Label2.Text = "⟶"
        ' 
        ' IAmHuman
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.Black
        ClientSize = New Size(827, 261)
        Controls.Add(Label2)
        Controls.Add(Label6)
        Controls.Add(Label5)
        Controls.Add(Label1)
        Controls.Add(PictureBox1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Margin = New Padding(3, 2, 3, 2)
        MaximumSize = New Size(843, 300)
        MinimumSize = New Size(843, 300)
        Name = "IAmHuman"
        StartPosition = FormStartPosition.WindowsDefaultBounds
        Text = "I Am Human"
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Timer1 As Timer
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents Label2 As Label
End Class

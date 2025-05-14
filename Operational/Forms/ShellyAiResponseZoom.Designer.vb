<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ShellyAiResponseZoom
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ShellyAiResponseZoom))
        RichTextBox1 = New RichTextBox()
        Panel1 = New Panel()
        SuspendLayout()
        ' 
        ' RichTextBox1
        ' 
        RichTextBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        RichTextBox1.BackColor = Color.FromArgb(CByte(64), CByte(64), CByte(64))
        RichTextBox1.BorderStyle = BorderStyle.None
        RichTextBox1.Font = New Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        RichTextBox1.ForeColor = SystemColors.HighlightText
        RichTextBox1.Location = New Point(27, 66)
        RichTextBox1.Name = "RichTextBox1"
        RichTextBox1.ReadOnly = True
        RichTextBox1.Size = New Size(746, 362)
        RichTextBox1.TabIndex = 2
        RichTextBox1.Text = ""
        ' 
        ' Panel1
        ' 
        Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Panel1.BackColor = Color.FromArgb(CByte(64), CByte(64), CByte(64))
        Panel1.Location = New Point(21, 57)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(758, 371)
        Panel1.TabIndex = 3
        ' 
        ' ShellyAiResponseZoom
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(800, 450)
        Controls.Add(RichTextBox1)
        Controls.Add(Panel1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "ShellyAiResponseZoom"
        Text = "Shelly AI Reply"
        ResumeLayout(False)
    End Sub

    Friend WithEvents RichTextBox1 As RichTextBox
    Friend WithEvents Panel1 As Panel
End Class

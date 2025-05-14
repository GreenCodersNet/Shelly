<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class DefaultFolder
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(DefaultFolder))
        FolderBrowserDialog1 = New FolderBrowserDialog()
        Label7 = New Label()
        Panel4 = New Panel()
        FolderPath = New TextBox()
        SelectFolderButton = New Button()
        Label6 = New Label()
        Label5 = New Label()
        RichTextBox1 = New RichTextBox()
        Panel1 = New Panel()
        Label1 = New Label()
        UpdateTrainingButton = New Button()
        Label2 = New Label()
        SaveChangesButton = New Button()
        SaveFileDialog1 = New SaveFileDialog()
        LoadTrainingButton = New Button()
        Panel4.SuspendLayout()
        Panel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' FolderBrowserDialog1
        ' 
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.BackColor = Color.Transparent
        Label7.Font = New Font("Cascadia Code", 9.75F)
        Label7.ForeColor = Color.White
        Label7.Location = New Point(30, 109)
        Label7.Name = "Label7"
        Label7.Size = New Size(192, 17)
        Label7.TabIndex = 74
        Label7.Text = "Path to Default Folder:"
        ' 
        ' Panel4
        ' 
        Panel4.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        Panel4.BorderStyle = BorderStyle.FixedSingle
        Panel4.Controls.Add(FolderPath)
        Panel4.Location = New Point(31, 129)
        Panel4.Name = "Panel4"
        Panel4.Size = New Size(415, 32)
        Panel4.TabIndex = 73
        ' 
        ' FolderPath
        ' 
        FolderPath.BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        FolderPath.BorderStyle = BorderStyle.None
        FolderPath.Font = New Font("Bahnschrift SemiLight", 10F)
        FolderPath.ForeColor = Color.White
        FolderPath.Location = New Point(5, 5)
        FolderPath.Name = "FolderPath"
        FolderPath.PlaceholderText = "E.g: D:\Shelly"
        FolderPath.Size = New Size(405, 17)
        FolderPath.TabIndex = 70
        ' 
        ' SelectFolderButton
        ' 
        SelectFolderButton.BackColor = Color.Transparent
        SelectFolderButton.Cursor = Cursors.Hand
        SelectFolderButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SelectFolderButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SelectFolderButton.FlatStyle = FlatStyle.Flat
        SelectFolderButton.Font = New Font("Bahnschrift SemiLight", 10F)
        SelectFolderButton.ForeColor = Color.MediumOrchid
        SelectFolderButton.Location = New Point(452, 128)
        SelectFolderButton.Name = "SelectFolderButton"
        SelectFolderButton.Size = New Size(93, 34)
        SelectFolderButton.TabIndex = 75
        SelectFolderButton.Text = "Browse"
        SelectFolderButton.UseVisualStyleBackColor = False
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.Top
        Label6.AutoSize = True
        Label6.BackColor = Color.Transparent
        Label6.Font = New Font("Cascadia Code", 12F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(295, 47)
        Label6.Name = "Label6"
        Label6.Size = New Size(181, 21)
        Label6.TabIndex = 77
        Label6.Text = "Default Folder Path"
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Top
        Label5.BackColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(295, 77)
        Label5.Name = "Label5"
        Label5.Size = New Size(250, 1)
        Label5.TabIndex = 76
        ' 
        ' RichTextBox1
        ' 
        RichTextBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        RichTextBox1.BorderStyle = BorderStyle.None
        RichTextBox1.Location = New Point(9, 8)
        RichTextBox1.Name = "RichTextBox1"
        RichTextBox1.Size = New Size(724, 184)
        RichTextBox1.TabIndex = 78
        RichTextBox1.Text = ""
        ' 
        ' Panel1
        ' 
        Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Panel1.BackColor = SystemColors.Window
        Panel1.BorderStyle = BorderStyle.FixedSingle
        Panel1.Controls.Add(Label1)
        Panel1.Controls.Add(RichTextBox1)
        Panel1.Location = New Point(30, 207)
        Panel1.Name = "Panel1"
        Panel1.Size = New Size(745, 202)
        Panel1.TabIndex = 79
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.BackColor = Color.Transparent
        Label1.Font = New Font("Cascadia Code", 9.75F)
        Label1.ForeColor = Color.White
        Label1.Location = New Point(-1, -12)
        Label1.Name = "Label1"
        Label1.Size = New Size(192, 17)
        Label1.TabIndex = 81
        Label1.Text = "Path to Default Folder:"
        ' 
        ' UpdateTrainingButton
        ' 
        UpdateTrainingButton.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        UpdateTrainingButton.BackColor = Color.Transparent
        UpdateTrainingButton.Cursor = Cursors.Hand
        UpdateTrainingButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        UpdateTrainingButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        UpdateTrainingButton.FlatStyle = FlatStyle.Flat
        UpdateTrainingButton.Font = New Font("Bahnschrift SemiLight", 10F)
        UpdateTrainingButton.ForeColor = Color.Lime
        UpdateTrainingButton.Location = New Point(625, 147)
        UpdateTrainingButton.Name = "UpdateTrainingButton"
        UpdateTrainingButton.Size = New Size(150, 54)
        UpdateTrainingButton.TabIndex = 80
        UpdateTrainingButton.Text = "Update System Training Message"
        UpdateTrainingButton.UseVisualStyleBackColor = False
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.BackColor = Color.Transparent
        Label2.Font = New Font("Cascadia Code", 9.75F)
        Label2.ForeColor = Color.White
        Label2.Location = New Point(30, 184)
        Label2.Name = "Label2"
        Label2.Size = New Size(312, 17)
        Label2.TabIndex = 81
        Label2.Text = "Paste your 'System Training Message': "
        ' 
        ' SaveChangesButton
        ' 
        SaveChangesButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        SaveChangesButton.BackColor = Color.Transparent
        SaveChangesButton.Cursor = Cursors.Hand
        SaveChangesButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SaveChangesButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        SaveChangesButton.FlatStyle = FlatStyle.Flat
        SaveChangesButton.Font = New Font("Bahnschrift SemiLight", 10F)
        SaveChangesButton.ForeColor = Color.Lime
        SaveChangesButton.Location = New Point(656, 419)
        SaveChangesButton.Name = "SaveChangesButton"
        SaveChangesButton.Size = New Size(119, 34)
        SaveChangesButton.TabIndex = 82
        SaveChangesButton.Text = "Save File"
        SaveChangesButton.UseVisualStyleBackColor = False
        ' 
        ' SaveFileDialog1
        ' 
        ' 
        ' LoadTrainingButton
        ' 
        LoadTrainingButton.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        LoadTrainingButton.BackColor = Color.Transparent
        LoadTrainingButton.Cursor = Cursors.Hand
        LoadTrainingButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        LoadTrainingButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(CByte(20), CByte(20), CByte(20))
        LoadTrainingButton.FlatStyle = FlatStyle.Flat
        LoadTrainingButton.Font = New Font("Bahnschrift SemiLight", 10F)
        LoadTrainingButton.ForeColor = Color.Orange
        LoadTrainingButton.Location = New Point(31, 419)
        LoadTrainingButton.Name = "LoadTrainingButton"
        LoadTrainingButton.Size = New Size(119, 34)
        LoadTrainingButton.TabIndex = 83
        LoadTrainingButton.Text = "Load Training"
        LoadTrainingButton.UseVisualStyleBackColor = False
        ' 
        ' DefaultFolder
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(808, 462)
        Controls.Add(LoadTrainingButton)
        Controls.Add(SaveChangesButton)
        Controls.Add(Label2)
        Controls.Add(UpdateTrainingButton)
        Controls.Add(Label6)
        Controls.Add(Label5)
        Controls.Add(SelectFolderButton)
        Controls.Add(Label7)
        Controls.Add(Panel4)
        Controls.Add(Panel1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MinimumSize = New Size(757, 419)
        Name = "DefaultFolder"
        Text = "DefaultFolder"
        Panel4.ResumeLayout(False)
        Panel4.PerformLayout()
        Panel1.ResumeLayout(False)
        Panel1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents FolderBrowserDialog1 As FolderBrowserDialog
    Friend WithEvents Label7 As Label
    Friend WithEvents Panel4 As Panel
    Friend WithEvents FolderPath As TextBox
    Friend WithEvents SelectFolderButton As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents RichTextBox1 As RichTextBox
    Friend WithEvents Panel1 As Panel
    Friend WithEvents UpdateTrainingButton As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents SaveChangesButton As Button
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents LoadTrainingButton As Button
End Class

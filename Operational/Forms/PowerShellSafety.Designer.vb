<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class PowerShellSafety
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(PowerShellSafety))
        CheckBoxConstrainedMode = New CheckBox()
        CheckBoxBlockNetwork = New CheckBox()
        CheckBoxBlockEnv = New CheckBox()
        CheckBoxBlockJobs = New CheckBox()
        ButtonSave = New Button()
        Label6 = New Label()
        Label5 = New Label()
        Timer1 = New Timer(components)
        ButtonResetDefaults = New Button()
        Label2 = New Label()
        LabelWarning = New Label()
        ToolTipSecurity = New ToolTip(components)
        CheckBoxBlockSystemC = New CheckBox()
        SuspendLayout()
        ' 
        ' CheckBoxConstrainedMode
        ' 
        CheckBoxConstrainedMode.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        CheckBoxConstrainedMode.AutoSize = True
        CheckBoxConstrainedMode.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxConstrainedMode.ForeColor = Color.White
        CheckBoxConstrainedMode.Location = New Point(69, 129)
        CheckBoxConstrainedMode.Name = "CheckBoxConstrainedMode"
        CheckBoxConstrainedMode.Size = New Size(257, 21)
        CheckBoxConstrainedMode.TabIndex = 0
        CheckBoxConstrainedMode.Text = "Enable Constrained Language Mode"
        CheckBoxConstrainedMode.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxBlockNetwork
        ' 
        CheckBoxBlockNetwork.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        CheckBoxBlockNetwork.AutoSize = True
        CheckBoxBlockNetwork.Checked = True
        CheckBoxBlockNetwork.CheckState = CheckState.Checked
        CheckBoxBlockNetwork.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxBlockNetwork.ForeColor = Color.White
        CheckBoxBlockNetwork.Location = New Point(69, 183)
        CheckBoxBlockNetwork.Name = "CheckBoxBlockNetwork"
        CheckBoxBlockNetwork.Size = New Size(156, 21)
        CheckBoxBlockNetwork.TabIndex = 1
        CheckBoxBlockNetwork.Text = "Block Network Calls"
        CheckBoxBlockNetwork.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxBlockEnv
        ' 
        CheckBoxBlockEnv.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        CheckBoxBlockEnv.AutoSize = True
        CheckBoxBlockEnv.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxBlockEnv.ForeColor = Color.White
        CheckBoxBlockEnv.Location = New Point(69, 208)
        CheckBoxBlockEnv.Name = "CheckBoxBlockEnv"
        CheckBoxBlockEnv.Size = New Size(253, 21)
        CheckBoxBlockEnv.TabIndex = 2
        CheckBoxBlockEnv.Text = "Block Environment Variable Access"
        CheckBoxBlockEnv.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxBlockJobs
        ' 
        CheckBoxBlockJobs.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        CheckBoxBlockJobs.AutoSize = True
        CheckBoxBlockJobs.Checked = True
        CheckBoxBlockJobs.CheckState = CheckState.Checked
        CheckBoxBlockJobs.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxBlockJobs.ForeColor = Color.White
        CheckBoxBlockJobs.Location = New Point(69, 233)
        CheckBoxBlockJobs.Name = "CheckBoxBlockJobs"
        CheckBoxBlockJobs.Size = New Size(176, 21)
        CheckBoxBlockJobs.TabIndex = 3
        CheckBoxBlockJobs.Text = "Block Background Jobs"
        CheckBoxBlockJobs.UseVisualStyleBackColor = True
        ' 
        ' ButtonSave
        ' 
        ButtonSave.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        ButtonSave.FlatStyle = FlatStyle.Flat
        ButtonSave.Font = New Font("Bahnschrift SemiLight", 10F)
        ButtonSave.ForeColor = Color.Lime
        ButtonSave.Location = New Point(293, 283)
        ButtonSave.Name = "ButtonSave"
        ButtonSave.Size = New Size(235, 36)
        ButtonSave.TabIndex = 4
        ButtonSave.Text = "Save Changes"
        ButtonSave.UseVisualStyleBackColor = True
        ' 
        ' Label6
        ' 
        Label6.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label6.AutoSize = True
        Label6.BackColor = Color.Transparent
        Label6.Font = New Font("Cascadia Code", 12F)
        Label6.ForeColor = Color.White
        Label6.Location = New Point(321, 48)
        Label6.Name = "Label6"
        Label6.Size = New Size(262, 21)
        Label6.TabIndex = 79
        Label6.Text = "PowerShell Security Settings"
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label5.BackColor = Color.FromArgb(CByte(0), CByte(192), CByte(0))
        Label5.Font = New Font("Cascadia Code", 9.75F)
        Label5.ForeColor = Color.White
        Label5.Location = New Point(321, 78)
        Label5.Name = "Label5"
        Label5.Size = New Size(250, 1)
        Label5.TabIndex = 78
        ' 
        ' Timer1
        ' 
        Timer1.Interval = 2000
        ' 
        ' ButtonResetDefaults
        ' 
        ButtonResetDefaults.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        ButtonResetDefaults.FlatStyle = FlatStyle.Flat
        ButtonResetDefaults.Font = New Font("Bahnschrift SemiLight", 10F)
        ButtonResetDefaults.ForeColor = Color.DodgerBlue
        ButtonResetDefaults.Location = New Point(633, 283)
        ButtonResetDefaults.Name = "ButtonResetDefaults"
        ButtonResetDefaults.Size = New Size(172, 36)
        ButtonResetDefaults.TabIndex = 80
        ButtonResetDefaults.Text = "Reset to Defaults"
        ButtonResetDefaults.UseVisualStyleBackColor = True
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Label2.AutoSize = True
        Label2.BackColor = Color.Transparent
        Label2.Font = New Font("Cascadia Code", 9.75F)
        Label2.ForeColor = Color.White
        Label2.Location = New Point(33, 98)
        Label2.Name = "Label2"
        Label2.Size = New Size(64, 17)
        Label2.TabIndex = 81
        Label2.Text = "Options"
        ' 
        ' LabelWarning
        ' 
        LabelWarning.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        LabelWarning.AutoSize = True
        LabelWarning.BackColor = Color.Transparent
        LabelWarning.Font = New Font("Cascadia Code", 9.75F)
        LabelWarning.ForeColor = Color.White
        LabelWarning.Location = New Point(375, 129)
        LabelWarning.MaximumSize = New Size(392, 98)
        LabelWarning.MinimumSize = New Size(392, 98)
        LabelWarning.Name = "LabelWarning"
        LabelWarning.Size = New Size(392, 98)
        LabelWarning.TabIndex = 82
        LabelWarning.Text = "Changing this Settings will allow/restrict AI from accessing files or make changes to your System. " & vbCrLf & "Use ""Defaults"" settings for increase Security."
        ' 
        ' ToolTipSecurity
        ' 
        ' 
        ' CheckBoxBlockSystemC
        ' 
        CheckBoxBlockSystemC.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        CheckBoxBlockSystemC.AutoSize = True
        CheckBoxBlockSystemC.Checked = True
        CheckBoxBlockSystemC.CheckState = CheckState.Checked
        CheckBoxBlockSystemC.Font = New Font("Bahnschrift SemiLight", 10F)
        CheckBoxBlockSystemC.ForeColor = Color.Crimson
        CheckBoxBlockSystemC.Location = New Point(69, 156)
        CheckBoxBlockSystemC.Name = "CheckBoxBlockSystemC"
        CheckBoxBlockSystemC.Size = New Size(225, 21)
        CheckBoxBlockSystemC.TabIndex = 83
        CheckBoxBlockSystemC.Text = "Block Sytem File Changes (C:\)"
        CheckBoxBlockSystemC.UseVisualStyleBackColor = True
        ' 
        ' PowerShellSafety
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(32), CByte(32), CByte(32))
        ClientSize = New Size(817, 331)
        Controls.Add(CheckBoxBlockSystemC)
        Controls.Add(LabelWarning)
        Controls.Add(Label2)
        Controls.Add(ButtonResetDefaults)
        Controls.Add(Label6)
        Controls.Add(Label5)
        Controls.Add(ButtonSave)
        Controls.Add(CheckBoxBlockJobs)
        Controls.Add(CheckBoxBlockEnv)
        Controls.Add(CheckBoxBlockNetwork)
        Controls.Add(CheckBoxConstrainedMode)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximumSize = New Size(833, 370)
        MinimumSize = New Size(833, 370)
        Name = "PowerShellSafety"
        StartPosition = FormStartPosition.CenterScreen
        Text = "PowerShell Safety"
        TopMost = True
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents CheckBoxConstrainedMode As CheckBox
    Friend WithEvents CheckBoxBlockNetwork As CheckBox
    Friend WithEvents CheckBoxBlockEnv As CheckBox
    Friend WithEvents CheckBoxBlockJobs As CheckBox
    Friend WithEvents ButtonSave As Button
    Friend WithEvents Label6 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Timer1 As Timer
    Friend WithEvents ButtonResetDefaults As Button
    Friend WithEvents Label2 As Label
    Friend WithEvents LabelWarning As Label
    Friend WithEvents ToolTipSecurity As ToolTip
    Friend WithEvents CheckBoxBlockSystemC As CheckBox
End Class

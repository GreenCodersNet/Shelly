' ###  PowerShellSafety.vb | v1.0.2 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Public Class PowerShellSafety
    Private Sub PowerShellSafety_Load(sender As Object, e As EventArgs) Handles MyBase.Load


        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

        CheckBoxConstrainedMode.Checked = My.Settings.PowerShell_UseConstrainedMode
        CheckBoxBlockSystemC.Checked = My.Settings.PowerShell_BlockSystemC
        CheckBoxBlockNetwork.Checked = My.Settings.PowerShell_BlockNetworkCalls
        CheckBoxBlockEnv.Checked = My.Settings.PowerShell_BlockEnvVariables
        CheckBoxBlockJobs.Checked = My.Settings.PowerShell_BlockBackgroundJobs

        ' Add a styled warning label
        LabelWarning.Text = "⚠ Warning: Changing these settings may allow the AI to access files or modify system behavior." &
                            vbCrLf & "For maximum protection, use the default settings."
        LabelWarning.ForeColor = Color.Gold
        LabelWarning.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold)
        LabelWarning.TextAlign = ContentAlignment.MiddleCenter
        LabelWarning.BackColor = Color.FromArgb(40, 40, 40)
        LabelWarning.BorderStyle = BorderStyle.FixedSingle
        LabelWarning.Padding = New Padding(8)

        '===================================================

        ToolTipSecurity.SetToolTip(CheckBoxConstrainedMode,
        "Limits PowerShell capabilities to basic commands only." + vbCrLf +
        "Prevents access to .NET, COM, and other advanced features." + vbCrLf +
        "This can block reading emails or creating Office Documents.")

        ToolTipSecurity.SetToolTip(CheckBoxBlockSystemC,
        "Block all file changes on the system drive (C:\)." & vbCrLf &
        "To allow edits, keep your files under 'C:\Shelly'.")

        ToolTipSecurity.SetToolTip(CheckBoxBlockNetwork,
            "Blocks any PowerShell command that tries to connect to the internet " + vbCrLf +
            "or download files (e.g., Invoke-WebRequest).")

        ToolTipSecurity.SetToolTip(CheckBoxBlockEnv,
            "Prevents access to environment variables like username, computer name, " + vbCrLf +
            "system paths, etc.")

        ToolTipSecurity.SetToolTip(CheckBoxBlockJobs,
            "Disables commands that execute code in the background (e.g., Start-Job, Invoke-Command). " + vbCrLf +
            "Prevents hidden script execution.")

    End Sub

    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        ' Update SecurityFlags
        SecurityFlags.ConstrainedLanguageMode = CheckBoxConstrainedMode.Checked
        SecurityFlags.BlockSystemC = CheckBoxBlockSystemC.Checked
        SecurityFlags.BlockNetworkCalls = CheckBoxBlockNetwork.Checked
        SecurityFlags.BlockEnvVariableAccess = CheckBoxBlockEnv.Checked
        SecurityFlags.BlockBackgroundJobs = CheckBoxBlockJobs.Checked

        ' Save to My.Settings
        Globals.SavePowerShellSecuritySettings()

        ButtonSave.Text = "Changes saved!"
        ButtonSave.ForeColor = Color.Gold
        Timer1.Start()
    End Sub

    Public Sub ApplySmoothCustomTitleBar(form As Form)
        ' Enable double buffering to prevent flicker
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.UserPaint, True)
        Me.UpdateStyles()

        ' Suspend layout to avoid redrawing until complete
        Me.SuspendLayout()

        ' Apply custom title bar logic here (add title bar panel, buttons, etc.)
        ApplyCustomTitleBar(form)

        ' Resume layout and force a refresh
        Me.ResumeLayout()
        Me.Refresh()
    End Sub



    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        ButtonSave.Text = "Save Settings"
        ButtonSave.ForeColor = Color.Lime

        ButtonResetDefaults.Text = "Reset to Defaults"
        ButtonResetDefaults.ForeColor = Color.DodgerBlue
        Timer1.Stop()
    End Sub

    Private Sub ButtonResetDefaults_Click(sender As Object, e As EventArgs) Handles ButtonResetDefaults.Click
        ' Set checkbox states back to default values
        CheckBoxConstrainedMode.Checked = True
        CheckBoxBlockSystemC.Checked = True
        CheckBoxBlockNetwork.Checked = True
        CheckBoxBlockEnv.Checked = False
        CheckBoxBlockJobs.Checked = True

        ' Apply defaults to SecurityFlags and Settings
        SecurityFlags.ConstrainedLanguageMode = False
        SecurityFlags.BlockSystemC = True
        SecurityFlags.BlockNetworkCalls = True
        SecurityFlags.BlockEnvVariableAccess = False
        SecurityFlags.BlockBackgroundJobs = True

        ' Save defaults to My.Settings
        Globals.SavePowerShellSecuritySettings()

        ButtonResetDefaults.Text = "Defaults Restored!"
        ButtonResetDefaults.ForeColor = Color.Gold
        Timer1.Start()
    End Sub

    Private Sub ToolTipSecurity_Popup(sender As Object, e As PopupEventArgs) Handles ToolTipSecurity.Popup

    End Sub


End Class
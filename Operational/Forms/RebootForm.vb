
' ###  RebootForm.vb | v1.0.1 ### 

' ##########################################################
'  Shelly.vb - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Public Class RebootForm
    Private Sub RebootForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================
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

    Private Sub NoButton_Click(sender As Object, e As EventArgs) Handles NoButton.Click
        Me.Close()
    End Sub

    Private Sub YesButton_Click(sender As Object, e As EventArgs) Handles YesButton.Click
        RestartApp()
    End Sub


    Private Sub RestartApp()
        Try
            ' Get the current application's executable path
            Dim exePath As String = Application.ExecutablePath

            ' Start a new instance of the application
            Process.Start(exePath)

            ' Close the current application
            Application.Exit()
        Catch ex As Exception
            MessageBox.Show("Failed to restart the application: " & ex.Message,
                            "Restart Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
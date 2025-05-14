' ###  About.vb | v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################
Public Class About
    Private Sub About_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================
    End Sub

    ' +++++++++++++ SMOOTH FORM OPENING - REDISGN +++++++++++++
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

    Private Sub RichTextBox1_TextChanged(sender As Object, e As EventArgs) Handles RichTextBox1.TextChanged

    End Sub

    Private Sub About_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Remove focus from all controls
        Me.ActiveControl = Nothing
    End Sub

    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint

    End Sub
End Class
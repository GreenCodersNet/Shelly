Public Class MessageForm
    Public Sub New(messageText As String, Optional title As String = "Message")
        InitializeComponent()
        Me.Text = title
        LabelMessage.Text = messageText
    End Sub

    Private Sub ButtonOK_Click(sender As Object, e As EventArgs) Handles ButtonOK.Click
        Me.Close()
    End Sub

    Private Sub MessageForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FormStyler.ApplyBorder(Me, Color.White, 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.TopMost = True

    End Sub

    Private Sub LabelMessage_Click(sender As Object, e As EventArgs) Handles LabelMessage.Click

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
End Class

Public Class UpdateForm

    Public Property MessageText As String
        Get
            Return Label3.Text
        End Get
        Set(value As String)
            Label3.Text = value
        End Set
    End Property

    Public Sub New()
        InitializeComponent()
        ' Make Enter = Yes, Escape = No
        Me.AcceptButton = YesButton
        Me.CancelButton = NoButton
        YesButton.DialogResult = DialogResult.Yes
        NoButton.DialogResult = DialogResult.No
    End Sub

    Private Sub UpdateForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

    End Sub

    Private Sub YesButton_Click(sender As Object, e As EventArgs) _
            Handles YesButton.Click
        ' DialogResult is already set; just close
        Me.Close()
    End Sub

    Private Sub NoButton_Click(sender As Object, e As EventArgs) _
            Handles NoButton.Click
        Me.Close()
    End Sub

    ' +++++++++++++ SMOOTH FORM OPENING - REDESIGN +++++++++++++
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

    Private Sub LabelURL_Click(sender As Object, e As EventArgs) Handles LabelURL.Click

    End Sub

    Private Sub LinkLabel1_LinkClicked(sender As Object, e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
        Process.Start(New ProcessStartInfo("https://greencoders.net/greencoders-labs/shelly-ai/") With {.UseShellExecute = True})
    End Sub
End Class
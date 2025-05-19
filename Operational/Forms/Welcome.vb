Public Class Welcome
    Private Sub Welcome_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.FormBorderStyle = FormBorderStyle.None
        Me.WindowState = FormWindowState.Maximized
        Me.TopMost = True
        Me.ShowInTaskbar = False
    End Sub

    Private Sub CloseButton_Click(sender As Object, e As EventArgs) Handles CloseButton.Click
        Close()
    End Sub

    Private Sub LearnMoreButton_Click(sender As Object, e As EventArgs) Handles LearnMoreButton.Click
        Dim shellyUrl As String = "https://greencoders.net/greencoders-labs/shelly-ai/"
        Try
            Process.Start(New ProcessStartInfo With {
                .FileName = shellyUrl,
                .UseShellExecute = True
            })
        Catch ex As Exception
            MessageBox.Show("Unable to open the web page. Please check your internet connection.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As System.Windows.Forms.Message, keyData As System.Windows.Forms.Keys) As Boolean
        If keyData = Keys.Escape OrElse keyData = Keys.Enter OrElse keyData = Keys.Space Then
            Me.Close()
            Return True
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
End Class
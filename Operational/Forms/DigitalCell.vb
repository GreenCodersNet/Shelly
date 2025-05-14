
' ###  DigitalCell.vb | v1.0.1 ### 

' ##########################################################
'  Shelly.vb - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Public Class DigitalCell

    Private currentIndex As Integer = 0
    Private textToShow As String = """Digital Cell"" is an experimental primordial-life simulator designed to showcase" & vbCrLf &
                    "the complexity of a self-evolving digital ecosystem." & vbCrLf &
                    "Watch virtual organisms eat, reproduce, die, and adapt in real time."

    Private Sub DigitalCell_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Remove focus from all controls
        Me.ActiveControl = Nothing
    End Sub

    Private Sub DigitalCell_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

        Label1.Text = "" ' Start with an empty label
        Timer1.Interval = 40 ' Adjust for speed (lower is faster)
        Timer1.Enabled = True ' Start the timer
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
        If currentIndex < textToShow.Length Then
            Label1.Text &= textToShow(currentIndex) ' Append one character at a time
            currentIndex += 1
        Else
            Timer1.Enabled = False ' Stop the timer when done
            Me.ActiveControl = Nothing
        End If
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        Try
            Process.Start(New ProcessStartInfo With {
                .FileName = "https://greencoders.net/greencoders-labs/ai-xploring/digital-cell-simulator/",
                .UseShellExecute = True
            })
        Catch ex As Exception

        End Try
    End Sub

    Private Sub PictureBox2_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

    End Sub


End Class
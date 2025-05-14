' ###  DefaultFolder.vb | v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.IO
Imports System.Reflection
Public Class DefaultFolder
    Private Sub DefaultFolder_Load(sender As Object, e As EventArgs) Handles MyBase.Load

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

    Private Sub DefaultFolder_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Remove focus from all controls
        Me.ActiveControl = Nothing
    End Sub

    Private Sub FolderPath_TextChanged(sender As Object, e As EventArgs) Handles FolderPath.TextChanged

    End Sub

    Private Sub RichTextBox1_TextChanged(sender As Object, e As EventArgs) Handles RichTextBox1.TextChanged

    End Sub


    Private Sub UpdateTrainingButton_Click(sender As Object, e As EventArgs) Handles UpdateTrainingButton.Click
        If String.IsNullOrWhiteSpace(FolderPath.Text) Then
            MessageBox.Show("Please select a folder first.", "Missing Folder Path", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        Dim resourceName = "ShellyAI.TrainingMessage.txt" ' Use correct namespace + filename
        Dim SystemTrainingMessage = LoadTextResource(resourceName)
        RichTextBox1.Text = SystemTrainingMessage.Replace("C:\Shelly", FolderPath.Text)
    End Sub

    Private Sub SaveChangesButton_Click(sender As Object, e As EventArgs) Handles SaveChangesButton.Click
        ' Check if RichTextBox1 is not empty
        If Not String.IsNullOrWhiteSpace(RichTextBox1.Text) Then
            ' Configure SaveFileDialog
            SaveFileDialog1.Title = "Save Text File"
            SaveFileDialog1.Filter = "Text Files (*.txt)|*.txt"
            SaveFileDialog1.DefaultExt = "txt"
            SaveFileDialog1.FileName = "SystemTrainingMessage.txt"

            ' Show dialog
            If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
                Try
                    ' Save text to selected file path
                    IO.File.WriteAllText(SaveFileDialog1.FileName, RichTextBox1.Text)
                    MessageBox.Show("File saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Error saving file: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        Else
            MessageBox.Show("There is no text to save.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub


    Private Sub FolderBrowserDialog1_HelpRequest(sender As Object, e As EventArgs) Handles FolderBrowserDialog1.HelpRequest

    End Sub

    Private Sub SaveFileDialog1_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles SaveFileDialog1.FileOk

    End Sub

    Private Sub SelectFolderButton_Click(sender As Object, e As EventArgs) Handles SelectFolderButton.Click
        ' Optional: Set a default path
        FolderBrowserDialog1.Description = "Select a folder"
        FolderBrowserDialog1.RootFolder = Environment.SpecialFolder.MyComputer
        FolderBrowserDialog1.ShowNewFolderButton = True

        ' Show the dialog
        If FolderBrowserDialog1.ShowDialog() = DialogResult.OK Then
            ' Set the selected path in the FolderPath TextBox
            FolderPath.Text = FolderBrowserDialog1.SelectedPath
        End If
    End Sub

    Private Sub LoadTrainingButton_Click(sender As Object, e As EventArgs) Handles LoadTrainingButton.Click
        Dim resourceName = "ShellyAI.TrainingMessage.txt" ' Use correct namespace + filename
        Dim SystemTrainingMessage = LoadTextResource(resourceName)
        RichTextBox1.Text = SystemTrainingMessage
    End Sub
End Class
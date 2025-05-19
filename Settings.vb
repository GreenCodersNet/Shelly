' ###  Settings.vb | v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Public Class Settings

    Private Sub Settings_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        FormStyler.ApplyBorder(Me, Color.FromArgb(28, 28, 28), 1)

        Me.ActiveControl = Nothing
        '================== REDESIGN FORM ==================
        Me.Opacity = 0 ' Start with form fully transparent
        ApplySmoothCustomTitleBar(Me)
        Me.Opacity = 1 ' Set opacity back to fully visible after customization
        '===================================================

        ' 1️⃣ Load the API key from Config (which reads My.Settings.UserApiKey)
        GlobalApiKey.Text = Config.OpenAiApiKey

        ' 2️⃣ Load the assistant ID from Config (My.Settings.assistantId)
        AssitantID.Text = Config.AssistantId

        ' 3️⃣ Populate and select the AI model
        PopulateAiModels()
        GetAudioDevices()
        PopulateAudioDevices()

        If Not String.IsNullOrEmpty(Config.AiModel) AndAlso
           aiSelection.Items.Contains(Config.AiModel) Then
            aiSelection.SelectedItem = Config.AiModel
        Else
            aiSelection.SelectedIndex = 0
        End If

        ' … the rest of your Load (audio devices, checkboxes, etc.) …
    End Sub

    Private Sub SaveSettings_Click(sender As Object, e As EventArgs) Handles SaveSettings.Click
        ' 1️⃣ Save the API key (encrypted) via Globals.UserApiKey
        Globals.UserApiKey = GlobalApiKey.Text.Trim()

        ' 2️⃣ Save the assistant ID into user settings
        My.Settings.assistantId = AssitantID.Text.Trim()

        ' 3️⃣ Save the AI model choice
        If aiSelection.SelectedItem IsNot Nothing Then
            My.Settings.AiModelSelection = aiSelection.SelectedItem.ToString()
            Globals.AiModelSelection = aiSelection.SelectedItem.ToString()
        End If

        ' 4️⃣ Persist all user settings
        My.Settings.Save()

        ' UI feedback
        SaveSettings.Text = "Changes saved!"
        SaveSettings.ForeColor = Color.Gold
        Timer1.Start()

        RebootForm.Show()
        Me.ActiveControl = Nothing
    End Sub

    Public Sub PopulateAiModels()
        aiSelection.Items.Clear()
        aiSelection.Items.Add("gpt-4o-mini")
        aiSelection.Items.Add("gpt-4o")
        aiSelection.Items.Add("gpt-4.1")
        aiSelection.Items.Add("o3-mini")
    End Sub

    Public Shared Sub InitializeControls()
        If Shelly.Instance IsNot Nothing Then
            Shelly.Instance.LabelStatusUpdate.Text = "Ready"
        End If
    End Sub

    ' Populate ComboBoxDevices with the system audio inputs
    Public Sub PopulateAudioDevices()
        ComboBoxDevices.Items.Clear()
        Dim devices = Globals.GetAudioDevices()

        For Each device In devices
            ComboBoxDevices.Items.Add(device)
        Next

        If ComboBoxDevices.Items.Count > 0 Then
            ' Make sure we select the saved index
            If Globals.UserAudioSelection >= 0 AndAlso Globals.UserAudioSelection < ComboBoxDevices.Items.Count Then
                ComboBoxDevices.SelectedIndex = Globals.UserAudioSelection
            Else
                ComboBoxDevices.SelectedIndex = 0
                Globals.SaveAudioSelection(0)
            End If
        Else
            MessageBox.Show("No recording devices found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ' If no devices, disable the speech button in Shelly
            Shelly.ButtonUseSpeech.Enabled = False
        End If
    End Sub

    ' Toggle masking of the API key field
    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox1.CheckedChanged
        If CheckBox1.Checked Then
            GlobalApiKey.PasswordChar = ControlChars.NullChar ' Show actual input
        Else
            GlobalApiKey.PasswordChar = "●" ' Mask the input
        End If
    End Sub

    ' Event handler for the new Prompt Revision checkbox


    ' User changed audio device in the combo
    Private Sub ComboBoxDevices_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxDevices.SelectedIndexChanged
        If ComboBoxDevices.SelectedIndex >= 0 Then
            ' Immediately save to My.Settings
            Globals.SaveAudioSelection(ComboBoxDevices.SelectedIndex)
        End If
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

    ' These remain unchanged
    Private Sub AboutButton_Click(sender As Object, e As EventArgs) Handles AboutButton.Click
        RestoreWindow(About)
        Me.ActiveControl = Nothing
    End Sub


    Private Sub IAmHumanButton_Click(sender As Object, e As EventArgs) Handles IAmHumanButton.Click
        RestoreWindow(IAmHuman)
        Me.ActiveControl = Nothing
    End Sub

    Private Sub ProButton_Click(sender As Object, e As EventArgs) Handles ProButton.Click
        Try
            Process.Start(New ProcessStartInfo With {
                .FileName = "https://greencoders.net/greencoders-labs/shelly-ai",
                .UseShellExecute = True
            })
        Catch ex As Exception
            Debug.WriteLine("[ERROR] Opening URL: " & ex.Message)
        End Try
        Me.ActiveControl = Nothing
    End Sub

    Private Sub Settings_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Remove focus from all controls
        Me.ActiveControl = Nothing
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        SaveSettings.Text = "Save Settings"
        SaveSettings.ForeColor = Color.Lime
        Timer1.Stop()
    End Sub

    Private Sub CheckBoxHints_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxHints.CheckedChanged
        Globals.CheckBoxHintsState = CheckBoxHints.Checked

        ' Apply the visibility rule for Panel5
        If CheckBoxHints.Checked Then
            Shelly.Panel5.Visible = False
            CheckBoxHints.Text = "Hints ON "
        Else
            If Shelly.ChatPanel.Visible Then
                Shelly.Panel5.Visible = True
                CheckBoxHints.Text = "Hints OFF"
            End If
        End If
        Me.ActiveControl = Nothing
    End Sub


    Private Sub CheckBoxPromptRevision_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPromptRevision.CheckedChanged
        Globals.UsePromptRevision = CheckBoxPromptRevision.Checked
        If CheckBoxPromptRevision.Checked Then
            CheckBoxPromptRevision.Text = "Rephrasing ON "
        Else
            CheckBoxPromptRevision.Text = "Rephrasing OFF"
        End If
        Me.ActiveControl = Nothing
    End Sub

    Private Sub DefaultFolderWindow_Click(sender As Object, e As EventArgs) Handles DefaultFolderWindow.Click
        RestoreWindow(DefaultFolder)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        RestoreWindow(DigitalCell)
        Me.ActiveControl = Nothing
    End Sub

    Private Sub AISelection_SelectedIndexChanged(sender As Object, e As EventArgs) Handles aiSelection.SelectedIndexChanged

    End Sub

    Private Sub GlobalApiKey_TextChanged(sender As Object, e As EventArgs) Handles GlobalApiKey.TextChanged

    End Sub

    Private Sub AssitantID_TextChanged(sender As Object, e As EventArgs) Handles AssitantID.TextChanged

    End Sub


End Class

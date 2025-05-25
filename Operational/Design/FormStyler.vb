' ###  FormStyler.vb | v1.0.1 ### 

' ##########################################################
'  Shelly - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Drawing.Drawing2D
Imports System.Runtime.CompilerServices

Module FormStyler
    ' Constants for resizing
    Private Const HTLEFT As Integer = 10
    Private Const HTRIGHT As Integer = 11
    Private Const HTTOP As Integer = 12
    Private Const HTTOPLEFT As Integer = 13
    Private Const HTTOPRIGHT As Integer = 14
    Private Const HTBOTTOM As Integer = 15
    Private Const HTBOTTOMLEFT As Integer = 16
    Private Const HTBOTTOMRIGHT As Integer = 17

    Private ReadOnly BorderColor As Color = Color.MediumPurple
    Private ReadOnly BorderWidth As Integer = 2

    ' -------------------- DESIGN ---------------------->
    Public Sub ApplyBorder(frm As Form, Optional color As Color = Nothing, Optional width As Integer = -1)
        Dim finalColor = If(color = Nothing, BorderColor, color)
        Dim finalWidth = If(width <= 0, BorderWidth, width)

        ' Set DoubleBuffered via reflection
        Dim doubleBufferProp = GetType(Control).GetProperty("DoubleBuffered", BindingFlags.Instance Or BindingFlags.NonPublic)
        doubleBufferProp?.SetValue(frm, True)

        ' Set ResizeRedraw style via reflection
        Dim setStyleMethod = GetType(Control).GetMethod("SetStyle", BindingFlags.Instance Or BindingFlags.NonPublic)
        setStyleMethod?.Invoke(frm, New Object() {ControlStyles.ResizeRedraw, True})

        ' Prevent duplicate handlers
        RemoveHandler frm.Paint, AddressOf PaintHandler
        AddHandler frm.Paint, AddressOf PaintHandler

        ' Store border parameters in Tag (optional for multi-form setup)
        frm.Tag = New BorderSettings With {.color = finalColor, .width = finalWidth}
    End Sub


    Private Sub PaintHandler(sender As Object, e As PaintEventArgs)
        Dim frm = CType(sender, Form)
        Dim settings = TryCast(frm.Tag, BorderSettings)
        If settings Is Nothing Then Return

        Using pen As New Pen(settings.Color, settings.Width)
            pen.Alignment = PenAlignment.Inset
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
            e.Graphics.DrawRectangle(pen, 0, 0, frm.ClientSize.Width - 1, frm.ClientSize.Height - 1)
        End Using
    End Sub

    Private Class BorderSettings
        Public Property Color As Color
        Public Property Width As Integer
    End Class

    ' <-----------------------------------------------

    ' Windows API for handling resizing
    <DllImport("user32.dll")>
    Private Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function SendMessage(hwnd As IntPtr, wMsg As Integer, wParam As Integer, lParam As Integer) As Boolean
    End Function

    ' Enable double buffering to reduce flicker
    Private Sub EnableDoubleBuffering(form As Form)
        Dim doubleBufferPropertyInfo As PropertyInfo = form.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance Or BindingFlags.NonPublic)
        If doubleBufferPropertyInfo IsNot Nothing Then
            doubleBufferPropertyInfo.SetValue(form, True, Nothing)
        End If
    End Sub

    ' Apply custom title bar and resizing functionality
    Public Sub ApplyCustomTitleBar(form As Form)
        ' Enable double buffering to reduce flicker
        EnableDoubleBuffering(form)

        ' Prevent the form from drawing during control updates
        form.SuspendLayout()

        ' Set form properties to reduce initial flicker
        form.FormBorderStyle = FormBorderStyle.None
        form.StartPosition = FormStartPosition.CenterScreen

        ' Create the title bar panel with custom styling
        Dim titleBarPanel As New Panel With {
            .Dock = DockStyle.Top,
            .Height = 40,
            .BackColor = Color.FromArgb(28, 28, 28)
        }
        form.Controls.Add(titleBarPanel)

        ' Add a Paint event handler for the border
        AddHandler form.Paint, Sub(sender, e)
                                   Using borderPen As New Pen(Color.FromArgb(28, 28, 28), 3)
                                       e.Graphics.DrawRectangle(borderPen, 0, 0, form.ClientSize.Width - 1, form.ClientSize.Height - 1)
                                   End Using
                               End Sub

        ' Add Resize event handler to refresh the border when resized
        AddHandler form.Resize, Sub(sender, e)
                                    form.Invalidate() ' Force repaint to redraw the border
                                End Sub

        ' Attach MouseDown and MouseMove for resizing
        AddHandler form.MouseDown, AddressOf Form_MouseDown
        AddHandler form.MouseMove, AddressOf Form_MouseMove

        ' Add title label
        Dim titleLabel As New Label With {
            .Text = form.Text,
            .ForeColor = Color.White,
            .Font = New Font("Bahnschrift Light", 10, FontStyle.Regular),
            .AutoSize = True,
            .Dock = DockStyle.Left,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Padding = New Padding(5, 10, 0, 0)
        }
        titleBarPanel.Controls.Add(titleLabel)

        ' Add buttons based on the form properties (ControlBox, MinimizeBox, MaximizeBox)
        If form.ControlBox Then
            ' Add Minimize button if MinimizeBox is True
            If form.MinimizeBox Then
                Dim btnMinimize As New Button With {
                    .Text = "━",
                    .ForeColor = Color.White,
                    .FlatStyle = FlatStyle.Flat,
                    .Dock = DockStyle.Right,
                    .Width = 40
                }
                btnMinimize.FlatAppearance.BorderSize = 0
                AddHandler btnMinimize.Click, Sub(sender, e) form.WindowState = FormWindowState.Minimized
                titleBarPanel.Controls.Add(btnMinimize)
            End If

            ' Add Maximize/Restore button if MaximizeBox is True
            If form.MaximizeBox Then
                Dim btnMultiview As New Button With {
                    .Text = "🗖",
                    .ForeColor = Color.White,
                    .FlatStyle = FlatStyle.Flat,
                    .Dock = DockStyle.Right,
                    .Width = 40
                }
                btnMultiview.FlatAppearance.BorderSize = 0
                AddHandler btnMultiview.Click, Sub(sender, e)
                                                   If form.WindowState = FormWindowState.Maximized Then
                                                       form.WindowState = FormWindowState.Normal
                                                   Else
                                                       form.WindowState = FormWindowState.Maximized
                                                   End If
                                               End Sub
                titleBarPanel.Controls.Add(btnMultiview)
            End If

            ' Add Close button
            Dim btnClose As New Button With {
                .Text = "❌",
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat,
                .Dock = DockStyle.Right,
                .Width = 40,
                .BackColor = Color.Red
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub(sender, e) form.Close()
            titleBarPanel.Controls.Add(btnClose)
        End If

        ' Enable dragging for the custom title bar
        AddHandler titleBarPanel.MouseDown, AddressOf StartDrag
        AddHandler titleBarPanel.MouseMove, AddressOf PerformDrag
        AddHandler titleBarPanel.MouseUp, AddressOf StopDrag

        ' Resume layout updates after all elements have been added
        form.ResumeLayout(False)
    End Sub

    ' Resizing logic
    Private Sub Form_MouseDown(sender As Object, e As MouseEventArgs)
        Dim form As Form = CType(sender, Form)

        If e.Button = MouseButtons.Left Then
            Dim resizeDir As Integer = GetResizeDirection(form, e.Location)
            If resizeDir <> -1 Then
                ReleaseCapture()
                SendMessage(form.Handle, &HA1, resizeDir, 0)
            End If
        End If
    End Sub

    Private Sub Form_MouseMove(sender As Object, e As MouseEventArgs)
        Dim form As Form = CType(sender, Form)
        Dim resizeDir As Integer = GetResizeDirection(form, e.Location)

        ' Change cursor based on the resizing direction
        Select Case resizeDir
            Case HTTOP, HTBOTTOM
                form.Cursor = Cursors.SizeNS
            Case HTLEFT, HTRIGHT
                form.Cursor = Cursors.SizeWE
            Case HTTOPLEFT, HTBOTTOMRIGHT
                form.Cursor = Cursors.SizeNWSE
            Case HTTOPRIGHT, HTBOTTOMLEFT
                form.Cursor = Cursors.SizeNESW
            Case Else
                form.Cursor = Cursors.Default
        End Select
    End Sub

    Private Function GetResizeDirection(form As Form, location As Point) As Integer
        Const RESIZE_BORDER As Integer = 5 ' Adjust for wider detection area
        If location.X <= RESIZE_BORDER AndAlso location.Y <= RESIZE_BORDER Then
            Return HTTOPLEFT
        ElseIf location.X >= form.ClientSize.Width - RESIZE_BORDER AndAlso location.Y <= RESIZE_BORDER Then
            Return HTTOPRIGHT
        ElseIf location.X <= RESIZE_BORDER AndAlso location.Y >= form.ClientSize.Height - RESIZE_BORDER Then
            Return HTBOTTOMLEFT
        ElseIf location.X >= form.ClientSize.Width - RESIZE_BORDER AndAlso location.Y >= form.ClientSize.Height - RESIZE_BORDER Then
            Return HTBOTTOMRIGHT
        ElseIf location.X <= RESIZE_BORDER Then
            Return HTLEFT
        ElseIf location.X >= form.ClientSize.Width - RESIZE_BORDER Then
            Return HTRIGHT
        ElseIf location.Y <= RESIZE_BORDER Then
            Return HTTOP
        ElseIf location.Y >= form.ClientSize.Height - RESIZE_BORDER Then
            Return HTBOTTOM
        Else
            Return -1
        End If
    End Function

    ' Dragging Variables
    Private isFormDragging As Boolean
    Private dragStartPoint As Point
    Private dragForm As Form

    ' Dragging Functions
    Private Sub StartDrag(sender As Object, e As MouseEventArgs)
        If TypeOf sender Is Panel Then
            dragForm = CType(CType(sender, Panel).Parent, Form) ' Get the form
            isFormDragging = True
            dragStartPoint = e.Location
        End If
    End Sub

    Private Sub PerformDrag(sender As Object, e As MouseEventArgs)
        If isFormDragging AndAlso dragForm IsNot Nothing Then
            Dim newPoint As Point = dragForm.PointToScreen(e.Location)
            newPoint.Offset(-dragStartPoint.X, -dragStartPoint.Y)
            dragForm.Location = newPoint
        End If
    End Sub

    Private Sub StopDrag(sender As Object, e As MouseEventArgs)
        isFormDragging = False
    End Sub


    Public Sub Panel_Paint(sender As Object, e As PaintEventArgs)
        Dim panel As Panel = CType(sender, Panel)
        Dim borderColor As Color = Color.DarkViolet ' Set your desired border color here.
        Dim borderWidth As Integer = 1 ' Set the border width here.

        ' Draw the border around the panel.
        ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle,
                                borderColor, borderWidth, ButtonBorderStyle.Solid,
                                borderColor, borderWidth, ButtonBorderStyle.Solid,
                                borderColor, borderWidth, ButtonBorderStyle.Solid,
                                borderColor, borderWidth, ButtonBorderStyle.Solid)
    End Sub

    ' ================================== HTML & WINDOW =========================================

    ' Reads an embedded resource (HTML) as a string.
    Public Function LoadHtmlFromResource(resourceName As String) As String
        Try
            Dim asm As Reflection.Assembly = Reflection.Assembly.GetExecutingAssembly()
            Using stream As IO.Stream = asm.GetManifestResourceStream(resourceName)
                If stream Is Nothing Then
                    Dim availableResources As String = String.Join(Environment.NewLine, asm.GetManifestResourceNames())
                    Throw New Exception($"Embedded resource '{resourceName}' not found. Available resources:{Environment.NewLine}{availableResources}")
                End If
                Using reader As New IO.StreamReader(stream)
                    Return reader.ReadToEnd()
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading HTML resource: " & ex.Message, "Resource Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return String.Empty
        End Try
    End Function



End Module

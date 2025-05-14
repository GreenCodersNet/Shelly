' ###  FormRightMenu.vb | v1.0.1 ### 

' ##########################################################
'  Shelly App - v1.0.1
'  License: Creative Commons Attribution-NonCommercial (CC BY-NC)
'  https://creativecommons.org/licenses/by-nc/4.0/
'  © 2025 Vlad Stefanescu | GreenCoders.net. Attribution required.
' ##########################################################

Module FormRightMenu
    Public Class DarkMenuRenderer
        Inherits ToolStripProfessionalRenderer

        Protected Overrides Sub OnRenderMenuItemBackground(e As ToolStripItemRenderEventArgs)
            Dim menuItem As ToolStripMenuItem = TryCast(e.Item, ToolStripMenuItem)

            If menuItem IsNot Nothing Then
                Dim g As Graphics = e.Graphics
                Dim rect As Rectangle = New Rectangle(Point.Empty, menuItem.Bounds.Size)

                ' Use Using statements to ensure SolidBrush objects are disposed of
                If menuItem.Selected Then
                    Using brush As New SolidBrush(Color.FromArgb(22, 22, 22)) ' Hover color
                        g.FillRectangle(brush, rect)
                    End Using
                Else
                    Using brush As New SolidBrush(Color.FromArgb(32, 32, 32)) ' Default color
                        g.FillRectangle(brush, rect)
                    End Using
                End If
            Else
                MyBase.OnRenderMenuItemBackground(e)
            End If
        End Sub

        Protected Overrides Sub OnRenderToolStripBorder(e As ToolStripRenderEventArgs)
            ' Remove the default border of the context menu
        End Sub

        ' Change Text Color to White
        Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
            e.TextColor = Color.White ' Set text color to white
            MyBase.OnRenderItemText(e)
        End Sub
    End Class

    ' Function to Apply Dark Styling to Any ContextMenuStrip
    Public Sub ApplyDarkContextMenuStyle(menu As ContextMenuStrip)
        If menu IsNot Nothing Then
            menu.Renderer = New DarkMenuRenderer()
            menu.ShowImageMargin = False ' Removes the icon margin
        End If
    End Sub

    Public Class YellowMenuRenderer
        Inherits ToolStripProfessionalRenderer

        Protected Overrides Sub OnRenderMenuItemBackground(e As ToolStripItemRenderEventArgs)
            Dim menuItem2 As ToolStripMenuItem = TryCast(e.Item, ToolStripMenuItem)

            If menuItem2 IsNot Nothing Then
                Dim g As Graphics = e.Graphics
                Dim rect As Rectangle = New Rectangle(Point.Empty, menuItem2.Bounds.Size)

                ' Use Using statements to ensure SolidBrush objects are disposed of
                If menuItem2.Selected Then
                    Using brush As New SolidBrush(Color.FromArgb(9, 194, 240)) ' Hover color
                        g.FillRectangle(brush, rect)
                    End Using
                Else
                    Using brush As New SolidBrush(Color.FromArgb(10, 67, 149)) ' Default color
                        g.FillRectangle(brush, rect)
                    End Using
                End If
            Else
                MyBase.OnRenderMenuItemBackground(e)
            End If
        End Sub

        Protected Overrides Sub OnRenderToolStripBorder(e As ToolStripRenderEventArgs)
            ' Remove the default border of the context menu
        End Sub

        ' Change Text Color to White
        Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
            e.TextColor = Color.White ' Set text color to white
            MyBase.OnRenderItemText(e)
        End Sub
    End Class

    ' Function to Apply Dark Styling to Any ContextMenuStrip
    Public Sub ApplyYellowContextMenuStyle(menu As ContextMenuStrip)
        If menu IsNot Nothing Then
            menu.Renderer = New YellowMenuRenderer()
            menu.ShowImageMargin = False ' Removes the icon margin
        End If
    End Sub
End Module

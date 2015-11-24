﻿' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports System.Windows.Forms
Imports System.IO
Imports EmberAPI
Imports NLog
Imports System.Drawing

Public Class frmSettingsHolder

#Region "Fields"

    Shared logger As Logger = LogManager.GetCurrentClassLogger()
    Private tmpGenreXML As clsXMLGenres

#End Region 'Fields

    Public Event ModuleSettingsChanged()

    Sub New()
        ' This call is required by the Windows Form Designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
        SetUp()
        tmpGenreXML = CType(APIXML.GenreXML.CloneDeep, clsXMLGenres)
        GetGenres()
    End Sub

    Public Sub SaveChanges()
        APIXML.GenreXML = tmpGenreXML
        APIXML.GenreXML.Save()
    End Sub

    Private Sub GetGenres()
        cbMappingFilter.Items.Clear()
        dgvGenres.Rows.Clear()
        cbMappingFilter.Items.Add(Master.eLang.GetString(639, "< All >"))
        For Each tGenre As genreProperty In tmpGenreXML.Genres
            cbMappingFilter.Items.Add(tGenre.Name)
            Dim iRow As Integer = dgvGenres.Rows.Add(New Object() {False, tGenre.Name})
            dgvGenres.Rows(iRow).Tag = tGenre
        Next
        cbMappingFilter.SelectedIndex = 0
        dgvGenres.ClearSelection()
    End Sub

    Private Sub PopulateMappings()
        dgvMappings.Rows.Clear()
        GenreClearSelection()
        If cbMappingFilter.SelectedItem.ToString = Master.eLang.GetString(639, "< All >") Then
            For Each gMapping As genreMapping In tmpGenreXML.MappingTable
                Dim iRow As Integer = dgvMappings.Rows.Add(New Object() {gMapping.SearchString})
                dgvMappings.Rows(iRow).Tag = gMapping
            Next
        Else
            btnMappingRemove.Enabled = False
            For Each gMapping As genreMapping In tmpGenreXML.MappingTable.Where(Function(f) f.Mappings.Contains(cbMappingFilter.SelectedItem.ToString))
                Dim i As Integer = dgvMappings.Rows.Add(New Object() {gMapping.SearchString})
                dgvMappings.Rows(i).Tag = gMapping
            Next
        End If
    End Sub

    Private Sub GenreClearSelection()
        For Each dRow As DataGridViewRow In dgvGenres.Rows
            dRow.Cells(0).Value = False
        Next
    End Sub

    Private Sub dgvMappings_CurrentCellDirtyStateChanged(ByVal sender As Object, ByVal e As EventArgs) Handles dgvMappings.CurrentCellDirtyStateChanged
        Dim gMapping As genreMapping = DirectCast(dgvMappings.CurrentRow.Tag, genreMapping)
        If Not gMapping Is Nothing Then
            dgvMappings.CommitEdit(DataGridViewDataErrorContexts.Commit)
            gMapping.SearchString = dgvMappings.CurrentRow.Cells(0).Value.ToString
            RaiseEvent ModuleSettingsChanged()
        End If
    End Sub

    Private Sub dgvMappings_KeyDown(ByVal sender As Object, ByVal e As KeyEventArgs) Handles dgvMappings.KeyDown
        e.Handled = (e.KeyCode = Keys.Enter)
    End Sub

    Private Sub dgvMappings_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs) Handles dgvMappings.SelectionChanged
        btnMappingRemove.Enabled = False
        dgvGenres.Enabled = False
        dgvGenres.ClearSelection()

        If dgvMappings.SelectedCells.Count > 0 Then
            Dim gMapping As genreMapping = DirectCast(dgvMappings.Rows(dgvMappings.SelectedCells(0).RowIndex).Tag, genreMapping)
            If Not gMapping Is Nothing Then
                GenreClearSelection()
                For Each dRow As DataGridViewRow In dgvGenres.Rows
                    For Each tGenre As String In gMapping.Mappings
                        dRow.Cells(0).Value = If(dRow.Cells(1).Value.ToString = tGenre, True, dRow.Cells(0).Value)
                    Next
                Next
                btnMappingRemove.Enabled = True
                dgvGenres.Enabled = True
            Else
                If dgvMappings.Rows.Count > 0 Then
                    dgvMappings.ClearSelection()
                End If
            End If
        Else
            GenreClearSelection()
        End If
    End Sub

    Private Sub dgvGenres_CurrentCellDirtyStateChanged(ByVal sender As Object, ByVal e As EventArgs) Handles dgvGenres.CurrentCellDirtyStateChanged
        Dim gMapping As genreMapping = DirectCast(dgvMappings.CurrentRow.Tag, genreMapping)
        If Not gMapping Is Nothing Then
            dgvGenres.CommitEdit(DataGridViewDataErrorContexts.Commit)
            If Convert.ToBoolean(dgvGenres.CurrentRow.Cells(0).Value) Then
                If Not gMapping.Mappings.Contains(dgvGenres.CurrentRow.Cells(1).Value.ToString) Then
                    gMapping.Mappings.Add(dgvGenres.CurrentRow.Cells(1).Value.ToString)
                    gMapping.isNew = False
                End If
            Else
                If gMapping.Mappings.Contains(dgvGenres.CurrentRow.Cells(1).Value.ToString) Then
                    gMapping.Mappings.Remove(dgvGenres.CurrentRow.Cells(1).Value.ToString)
                    gMapping.isNew = False
                End If
            End If
            dgvMappings.Refresh()
            RaiseEvent ModuleSettingsChanged()
        End If
    End Sub

    Private Sub dgvGenres_SelectionChanged(ByVal sender As Object, ByVal e As EventArgs) Handles dgvGenres.SelectionChanged
        If dgvGenres.SelectedRows.Count > 0 Then 'AndAlso Not dgvGenres.CurrentRow.Cells(1).Value Is Nothing Then
            Dim tGenre As genreProperty = DirectCast(dgvGenres.CurrentRow.Tag, genreProperty)
            If Not String.IsNullOrEmpty(tGenre.Image) Then
                Dim imgPath As String = Path.Combine(Functions.AppPath, String.Format("Images{0}Genres{0}{1}", Path.DirectorySeparatorChar, tGenre.Image))
                If File.Exists(imgPath) Then
                    pbIcon.Load(imgPath)
                End If
            Else
                pbIcon.Image = Nothing
            End If
            btnGenreRemove.Enabled = True
            btnChangeImg.Enabled = True
        Else
            btnGenreRemove.Enabled = False
            btnChangeImg.Enabled = False
        End If
    End Sub

    Private Sub cbMappingFilter_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles cbMappingFilter.SelectedIndexChanged
        PopulateMappings()
    End Sub

    Private Sub btnMappingAdd_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnMappingAdd.Click
        Dim strSearchString As String = InputBox(Master.eLang.GetString(640, "Enter the new Language"), Master.eLang.GetString(641, "New Language"))
        If Not String.IsNullOrEmpty(strSearchString) Then
            Dim nMapping As New genreMapping With {.SearchString = strSearchString}
            tmpGenreXML.MappingTable.Add(nMapping)
            Dim iRow As Integer = dgvMappings.Rows.Add(New Object() {strSearchString})
            dgvMappings.Rows(iRow).Tag = nMapping
            dgvMappings.CurrentCell = dgvMappings.Rows(iRow).Cells(0)
            RaiseEvent ModuleSettingsChanged()
        End If
    End Sub

    Private Sub btnGenreAdd_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnGenreAdd.Click
        Dim strGenre As String = InputBox(Master.eLang.GetString(640, "Enter the new Language"), Master.eLang.GetString(641, "New Language"))
        If Not String.IsNullOrEmpty(strGenre) Then
            Dim nGenre As New genreProperty With {.isNew = False, .Name = strGenre}
            tmpGenreXML.Genres.Add(nGenre)
            Dim iRow As Integer = dgvGenres.Rows.Add(New Object() {False, strGenre})
            dgvGenres.Rows(iRow).Tag = nGenre
            dgvGenres.CurrentCell = dgvGenres.Rows(iRow).Cells(1)

            RaiseEvent ModuleSettingsChanged()
        End If
    End Sub

    Private Sub btnMappingRemove_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnMappingRemove.Click
        If dgvMappings.SelectedCells.Count > 0 Then
            tmpGenreXML.MappingTable.Remove(DirectCast(dgvMappings.SelectedRows(0).Tag, genreMapping))
            dgvMappings.Rows.RemoveAt(dgvMappings.SelectedCells(0).RowIndex)
            RaiseEvent ModuleSettingsChanged()
        End If
    End Sub

    Private Sub btnGenreRemove_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnGenreRemove.Click
        If MsgBox(Master.eLang.GetString(661, "This will remove the Language from all Genres. Are you sure?"), MsgBoxStyle.YesNo, Master.eLang.GetString(662, "Remove Language")) = MsgBoxResult.Yes Then
            If dgvGenres.SelectedRows.Count > 0 Then 'AndAlso Not dgvLang.CurrentRow.Cells(1).Value Is Nothing Then
                Dim tGenre As genreProperty = DirectCast(dgvGenres.SelectedRows(0).Tag, genreProperty)
                tmpGenreXML.Genres.Remove(tGenre)
                For Each gMapping As genreMapping In tmpGenreXML.MappingTable
                    If gMapping.Mappings.Contains(tGenre.Name) Then
                        gMapping.Mappings.Remove(tGenre.Name)
                    End If
                Next
                dgvGenres.Rows.Remove(dgvGenres.SelectedRows(0))
                cbMappingFilter.Items.Remove(tGenre.Name)
                dgvMappings.Refresh()
                RaiseEvent ModuleSettingsChanged()
            End If
        End If
    End Sub

    Private Sub btnChangeImg_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnChangeImg.Click
        Using fbImages As New OpenFileDialog
            fbImages.InitialDirectory = Path.Combine(Functions.AppPath, String.Format("Images{0}Genres{0}", Path.DirectorySeparatorChar))
            fbImages.Filter = Master.eLang.GetString(497, "Images") + "|*.jpg;*.png"
            fbImages.ShowDialog()
            Dim tGenre As genreProperty = DirectCast(dgvGenres.CurrentRow.Tag, genreProperty)
            tGenre.Image = Path.GetFileName(fbImages.FileName)
            pbIcon.Load(Path.Combine(Functions.AppPath, String.Format("Images{0}Genres{0}{1}", Path.DirectorySeparatorChar, tGenre.Image)))
            RaiseEvent ModuleSettingsChanged()
        End Using
    End Sub

    Private Sub SetUp()
        btnMappingAdd.Text = Master.eLang.GetString(28, "Add")
        btnGenreAdd.Text = Master.eLang.GetString(28, "Add")
        btnMappingRemove.Text = Master.eLang.GetString(30, "Remove")
        btnGenreRemove.Text = Master.eLang.GetString(30, "Remove")
        btnChangeImg.Text = Master.eLang.GetString(702, "Change")
        GroupBox1.Text = Master.eLang.GetString(497, "Images")
        Label1.Text = Master.eLang.GetString(704, "Genres Filter")
        dgvMappings.Columns(0).HeaderText = Master.eLang.GetString(20, "Genre")
        dgvGenres.Columns(1).HeaderText = Master.eLang.GetString(707, "Languages")
    End Sub

    Private Sub dgvMappings_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles dgvMappings.CellPainting
        If Master.isWindows AndAlso e.RowIndex >= 0 AndAlso Not dgvMappings.Item(e.ColumnIndex, e.RowIndex).Displayed Then
            e.Handled = True
            Return
        End If

        If e.RowIndex >= 0 Then

            Dim gMapping As genreMapping = DirectCast(dgvMappings.Rows(e.RowIndex).Tag, genreMapping)

            'text 
            If gMapping.isNew Then
                e.CellStyle.ForeColor = Color.Green
                e.CellStyle.Font = New Font("Segoe UI", 9, FontStyle.Bold)
                e.CellStyle.SelectionForeColor = Color.Green
            Else
                e.CellStyle.ForeColor = Color.Black
                e.CellStyle.Font = New Font("Segoe UI", 8.25, FontStyle.Regular)
                e.CellStyle.SelectionForeColor = Color.FromKnownColor(KnownColor.HighlightText)
            End If

            'background
            If gMapping.Mappings.Count = 0 Then
                e.CellStyle.BackColor = Color.LightSteelBlue
                e.CellStyle.SelectionBackColor = Color.DarkTurquoise
            Else
                e.CellStyle.BackColor = Color.White
                e.CellStyle.SelectionBackColor = Color.FromKnownColor(KnownColor.Highlight)
            End If
        End If
    End Sub

#Region "Nested Types"

#End Region 'Nested Types

End Class

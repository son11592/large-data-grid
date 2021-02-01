using LargeDataGrid.Source.Controls.Columns;
using LargeDataGrid.Source.Controls.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LargeDataGrid.Source.Controls
{
    public partial class TableContent : UserControl
    {
        #region Public Variables
        public bool IsFixedHeaderWidth = false;
        public double FixedHeaderWidth = 100;
        public readonly List<ColumnModel> ResColumns = new List<ColumnModel>();
        public readonly List<RowModel> ResRows = new List<RowModel>();
        private readonly DataTable Source = new DataTable();
        #endregion

        #region Private Variables
        private ColumnModel SortColumn;
        private int SortDirection;
        private bool IsEditing = false;
        private bool IsModified = false;
        #endregion

        #region Filtered Columns
        private List<ColumnModel> FilteredColumns = new List<ColumnModel>();

        public void UpdateFilteredColumns()
        {
            FilteredColumns = ResColumns;
        }
        #endregion

        public TableContent()
        {
            InitializeComponent();
            TableData.ItemsSource = Source.DefaultView;

            RenderData();
        }

        private void RenderData()
        {
            Task.Run(() =>
            {
                var cols = new List<ColumnModel>();
                var rows = new List<RowModel>();
                for (int i = 0; i < 100; i++)
                {
                    cols.Add(new ColumnModel
                    {
                        Index = i,
                        Name = $"Column {i}",
                        IsString = i % 3 == 0,
                        IsBool = i % 3 == 1,
                        IsNumeric = i % 3 == 2,
                        Values = i % 6 == 0 ? new List<string> { "Value 1", "Value 2", "Value 3" } : null
                    });
                }

                var random = new Random();
                var names = new List<string> { "Aaren", "Aarika", "Abagael", "Abagail", "Abbe", "Abbey", "Abbi", "Abbie", "Abby", "Abbye", "Abigael", "Abigail", "Abigale", "Abra", "Ada", "Adah", "Adaline", "Adan", "Adara", "Adda", "Addi", "Addia", "Addie", "Addy", "Adel", "Adela", "Adelaida", "Adelaide", "Adele", "Adelheid", "Adelice", "Adelina", "Adelind", "Adeline", "Adella", "Adelle", "Adena", "Adey", "Adi", "Adiana", "Adina", "Adora", "Adore", "Adoree", "Adorne", "Adrea", "Adria" };
                for (int j = 0; j < 1000; j++)
                {
                    var fields = new List<FieldModel>();
                    for (int k = 0; k < cols.Count; k++)
                    {
                        string value;
                        if (k % 6 == 0)
                        {
                            value = "Value 1";
                        }
                        else if (k % 3 == 1)
                        {
                            value = random.Next(0, 1) == 1 ? "TRUE" : "FALSE";
                        }
                        else if (k % 3 == 2)
                        {
                            value = random.Next(0, 5000).ToString();
                        }
                        else
                        {
                            value = names[random.Next(0, names.Count - 1)];
                        }
                        fields.Add(new FieldModel { Value = value });
                    }
                    rows.Add(new RowModel { Columns = cols, Fields = fields, WillInsert = j % 3 == 1, WillDelete = j % 3 == 2 });
                }
                Dispatcher.Invoke(() => AppendData(cols, rows));
            });
        }

        #region DataGrid Events
        private void Keydown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Tab && e.Key != Key.Return && e.Key != Key.Enter)
            {
                IsModified = true;
            }

            // Handle other keys
            if (e.Key == Key.Delete && !IsEditing)
            {
                DeleteRows();
            }
            else if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                if (sender is DataGrid grid)
                {
                    if ((grid.SelectionUnit == DataGridSelectionUnit.Cell || grid.SelectionUnit == DataGridSelectionUnit.CellOrRowHeader) && Keyboard.FocusedElement is UIElement focusedElement)
                    {
                        focusedElement.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                    }
                    if (IsEditing)
                    {
                        grid.CommitEdit();
                    }
                    else
                    {
                        grid.BeginEdit();
                    }
                    e.Handled = true;
                }
            }
        }

        void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            DataGridColumn column = e.Column;
            ColumnModel col = null;

            if (column is TemplateColumn textColumn)
            {
                col = textColumn.Column;
            }
            if (col == null)
            {
                return;
            }

            // prevent the built-in sort from sorting
            e.Handled = true;

            // Ascending -> Descending -> null
            if (column.SortDirection == ListSortDirection.Descending)
            {
                column.SortDirection = null;
                column.Header = col.Name.Replace("_", "__");
            }
            else
            {
                // Clean all other sorts except the current column
                foreach (var c in TableData.Columns)
                {
                    if (c is TemplateColumn t && t != column)
                    {
                        t.SortDirection = null;
                        t.Header = t.Column.Name.Replace("_", "__");
                    }
                }

                // set the sort order on the column
                ListSortDirection direction = (column.SortDirection != ListSortDirection.Ascending) ? ListSortDirection.Ascending : ListSortDirection.Descending;
                column.SortDirection = direction;
                if (direction == ListSortDirection.Ascending)
                {
                    column.Header = col.Name.Replace("_", "__") + " ↑";
                }
                else
                {
                    column.Header = col.Name.Replace("_", "__") + " ↓";
                }
            }

            if (column.SortDirection == ListSortDirection.Descending)
            {
                SortDirection = -1;
            }
            else if (column.SortDirection == ListSortDirection.Ascending)
            {
                SortDirection = 1;
            }
            else
            {
                SortDirection = 0;
            }
            SortColumn = col;
        }

        private void RowDidChange(object sender, EventArgs e)
        {
            RowDidChange();
        }

        private void CellDidBeginditing(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            var col = GetColumnDataAtGridColumn(e.Column);
            var row = GetRowDataAtGridRow(e.Row);
            if (row == null || col == null) return;

            IsEditing = true;
            IsModified = false;
            var lastValue = row.LastRaw(col.Index);
            if (GetTextBox(e.EditingElement) is TextBox textBox)
            {
                if (string.IsNullOrEmpty(lastValue))
                {
                    textBox.Text = "";
                }
                else
                {
                    textBox.Text = lastValue;
                }
                textBox.Focus();
                textBox.SelectAll();
                textBox.AcceptsReturn = true;
                textBox.PreviewKeyDown -= ValueTextKeyDown;
                textBox.PreviewKeyDown += ValueTextKeyDown;
            }
        }

        private void CellDidEndEditing(object sender, DataGridCellEditEndingEventArgs e)
        {
            IsEditing = false;

            int rowIndex = ((DataGrid)sender).ItemContainerGenerator.IndexFromContainer(e.Row);

            var col = GetColumnDataAtGridColumn(e.Column);
            var row = GetRowDataAtRowIndex(rowIndex);

            if (row == null || col == null) return;

            if (GetTextBox(e.EditingElement) is TextBox textBox)
            {
                if (e.EditAction == DataGridEditAction.Cancel) return;
                var newValue = textBox.Text;
                row.Update(newValue, col.Index);
            }
            if (rowIndex >= 0)
            {
                ReloadRow(rowIndex);
                RowDidChange();
            }
        }
        #endregion

        #region Public
        public void DeleteRows()
        {
            var willRemoveRows = new List<int>();
            foreach (var item in TableData.SelectedItems)
            {
                var rowIndex = TableData.Items.IndexOf(item);
                var row = GetRowDataAtRowIndex(rowIndex);
                if (row == null) return;
                if (row.WillInsert)
                {
                    willRemoveRows.Add(rowIndex);
                }
                else
                {
                    row.WillDelete = true;
                    ReloadRow(rowIndex);
                }
            }

            if (willRemoveRows.Count > 0)
            {
                // Foreach is not respect the priority
                for (var i = willRemoveRows.Count - 1; i >= 0; i--)
                {
                    var rowIndex = willRemoveRows[i];
                    var row = GetRowDataAtRowIndex(rowIndex);
                    if (row == null) continue;
                    if (rowIndex >= 0 && rowIndex < Source.Rows.Count)
                    {
                        Source.Rows.RemoveAt(rowIndex);
                    }
                }
            }
        }

        public void RowDidChange() { }
        #endregion

        #region Private
        private void ValueTextKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textbox)
            {
                if (e.Key == Key.Tab)
                {
                    textbox.AcceptsTab = false;
                }
                else
                {
                    textbox.AcceptsTab = true;
                }
            }
        }

        private TextBox GetTextBox(DependencyObject o)
        {
            if (VisualTreeHelper.GetChildrenCount(o) > 0 && VisualTreeHelper.GetChild(o, 0) is TextBox textBox)
            {
                return textBox;
            }
            return null;
        }

        private int ActualRowHeight()
        {
            return (int)(TableData.FontSize + 12);
        }
        #endregion

        #region Templates
        private DataGridColumn TextColumn(ColumnModel column)
        {
            if (column.Values != null && column.Values.Count > 0)
            {
                var gridColumn = new DataTypeColumn(column, SortDirection, SortColumn);
                gridColumn.ColumnTypeChanged += ColumnTypeChanged;
                return gridColumn;
            }
            return new TextColumn(column, SortDirection, SortColumn);
        }

        private void ColumnTypeChanged(DataGridRow gridRow, ColumnModel column, string newValue)
        {
            int rowIndex = TableData.ItemContainerGenerator.IndexFromContainer(gridRow);
            var col = column;
            var row = GetRowDataAtRowIndex(rowIndex);

            if (row == null || col == null) return;
            row.Update(newValue, col.Index);
            if (rowIndex >= 0) ReloadRow(rowIndex);
        }

        #endregion

        #region Data Loading
        public void AppendData(List<ColumnModel> cols, List<RowModel> rows)
        {
            // Load Rows
            ResRows.AddRange(rows);

            // Reload column if needed
            ReloadColumn(cols);

            // Load row
            foreach (RowModel row in rows)
            {
                var dic = Source.NewRow();
                foreach (ColumnModel column in FilteredColumns)
                {
                    SetRowValue(row, column, dic);
                }

                Source.Rows.Add(dic);
            }

            // Update height if needed
            var actualRowHeight = ActualRowHeight();
            if ((int)TableData.RowHeight < actualRowHeight)
            {
                TableData.RowHeight = actualRowHeight;
            }
        }

        public void ReloadRow(int index)
        {
            if (GetRowDataAtRowIndex(index) is RowModel row && index < Source.Rows.Count)
            {
                var dic = Source.Rows[index];
                foreach (ColumnModel column in FilteredColumns)
                {
                    SetRowValue(row, column, dic);
                }
            }
        }

        private void ReloadColumn(List<ColumnModel> columns)
        {
            if (ResColumns.Count != columns.Count)
            {
                // Cleanup old column
                ResColumns.Clear();
                TableData.Columns.Clear();
                Source.Columns.Clear();

                // Appnd columns if empty
                ResColumns.AddRange(columns);
                UpdateFilteredColumns();

                // Load columns
                LoadColumns(FilteredColumns);
            }
            else
            {
                bool isChanged = false;
                for (var i = 0; i < columns.Count; i++)
                {
                    if (columns[i].Name != ResColumns[i].Name)
                    {
                        isChanged = true;
                        break;
                    }
                }
                if (isChanged)
                {
                    // Cleanup old column
                    ResColumns.Clear();
                    TableData.Columns.Clear();
                    Source.Columns.Clear();

                    // Appnd columns if empty
                    ResColumns.AddRange(columns);
                    UpdateFilteredColumns();

                    // Load columns
                    LoadColumns(FilteredColumns);
                }
            }
        }

        private void LoadColumns(List<ColumnModel> columns)
        {
            var sizeCached = false;
            var ColumnToAdd = new List<DataGridColumn>();

            foreach (ColumnModel column in columns)
            {
                DataGridColumn dataColumn;
                var key = column.Index.ToString();
                dataColumn = TextColumn(column);
                Source.Columns.Add(key + "_color");
                Source.Columns.Add(key + "_background");
                Source.Columns.Add(key);
                ColumnToAdd.Add(dataColumn);
            }

            // Add Column
            foreach (var dataColumn in ColumnToAdd)
            {
                TableData.Columns.Add(dataColumn);
            }
        }

        private void SetRowValue(RowModel row, ColumnModel column, DataRow dic)
        {
            string val;
            var key = column.Index.ToString();
            var raw = row.LastRaw(column.Index);

            // Get first index of new line
            if (raw == null)
            {
                val = null;
            }
            else
            {
                if (raw.Length > 500)
                {
                    raw = raw.Substring(0, 500) + "...";
                }
                var newLine = raw.IndexOf('\n');
                if (newLine == 0)
                {
                    val = "...";
                }
                else if (newLine > 0)
                {
                    val = raw.Substring(0, newLine) + "...";
                }
                else
                {
                    val = raw;
                }
            }

            if (row.WillInsert)
            {
                dic[key + "_background"] = "New";
            }
            else if (row.WillDelete)
            {
                dic[key + "_background"] = "Deleted";
            }
            else if (row.IsModified(column.Index))
            {
                dic[key + "_background"] = "Modified";
            }
            else
            {
                dic[key + "_background"] = null;
            }

            if (val == null)
            {
                dic[key] = "NULL";
                dic[key + "_color"] = "SecondaryTextBrush";
            }
            else
            {
                dic[key] = val;
                dic[key + "_color"] = "CommonTextBrush";
            }
        }
        #endregion


        #region Data Retrieve
        private int GetRowIndexFromGridRow(DataGridRow gridRow)
        {
            return TableData.ItemContainerGenerator.IndexFromContainer(gridRow);
        }

        private ColumnModel GetColumnDataAtGridColumn(DataGridColumn gridColumn)
        {
            if (gridColumn is TemplateColumn textColumn)
            {
                return textColumn.Column;
            }
            return null;
        }

        private RowModel GetRowDataAtGridRow(DataGridRow gridRow)
        {
            int rowIndex = GetRowIndexFromGridRow(gridRow);
            return GetRowDataAtRowIndex(rowIndex);
        }

        private RowModel GetRowDataAtRowIndex(int index)
        {
            if (index >= 0)
            {
                var RowCount = ResRows.Count;
                if (index < RowCount)
                    return ResRows[index];
            }
            return null;
        }
        #endregion

        public static T VisualDownwardSearch<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = VisualDownwardSearch<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}

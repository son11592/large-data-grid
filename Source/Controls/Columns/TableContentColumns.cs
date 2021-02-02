using LargeDataGrid.Source.Controls.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace LargeDataGrid.Source.Controls.Columns
{
    public class TemplateColumn : DataGridTemplateColumn
    {
        public ColumnModel Column { get; set; }

        public void UpdateCellStyle()
        {
            var key = Column.Index.ToString();
            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, null));
            var newTrigger = new DataTrigger { Binding = new Binding($"{key}_background"), Value = "New" };
            newTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Green));
            var deletedTrigger = new DataTrigger { Binding = new Binding($"{key}_background"), Value = "Deleted" };
            deletedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Red));
            var modifiedTrigger = new DataTrigger { Binding = new Binding($"{key}_background"), Value = "Modified" };
            modifiedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Yellow));
            var focusedTrigger = new Trigger { Property = UIElement.IsFocusedProperty, Value = true };
            focusedTrigger.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.DarkRed));
            cellStyle.Triggers.Add(newTrigger);
            cellStyle.Triggers.Add(deletedTrigger);
            cellStyle.Triggers.Add(modifiedTrigger);
            cellStyle.Triggers.Add(focusedTrigger);
            CellStyle = cellStyle;
        }

        internal double WidthForColumn(string name)
        {
            var width = name.Length * 7 + 10;
            return width;
        }

        internal FrameworkElementFactory CreateButtonFactory(string iconName, double rotate, Thickness margin)
        {
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            var imageFactory = new FrameworkElementFactory(typeof(Image));
            var transformGroup = new TransformGroup();
            var rotateTransform = new RotateTransform(rotate);
            transformGroup.Children.Add(rotateTransform);
            imageFactory.SetValue(Image.SourceProperty, (DrawingImage)(new FrameworkElement()).FindResource(iconName));
            imageFactory.SetValue(FrameworkElement.MarginProperty, margin);
            imageFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            imageFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            imageFactory.SetValue(FrameworkElement.RenderTransformProperty, transformGroup);
            buttonFactory.AppendChild(imageFactory);
            buttonFactory.SetValue(Grid.ColumnProperty, 1);
            buttonFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
            buttonFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            buttonFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            buttonFactory.SetValue(Button.BackgroundProperty, Brushes.Transparent);
            buttonFactory.SetValue(Button.BorderThicknessProperty, new Thickness(0));
            buttonFactory.SetValue(FrameworkElement.HeightProperty, 22d);
            return buttonFactory;
        }

        internal DataTemplate EditingCellTemplateFor(ColumnModel column)
        {
            var tBlockFactory = new FrameworkElementFactory(typeof(TextBox));
            tBlockFactory.SetValue(Grid.ColumnProperty, 0);
            tBlockFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            tBlockFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 0, 0, 0));
            if (column.IsNumeric) tBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            else if (column.IsBool) tBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
            return new DataTemplate { VisualTree = tBlockFactory };
        }
    }

    public class TextColumn : TemplateColumn
    {
        public TextColumn(ColumnModel column, int sortDirection, ColumnModel sortColumn)
        {
            var columnName = column.Name.Replace("_", "__");
            if (sortColumn == column)
            {
                if (sortDirection == 1) columnName += " ↑";
                else if (sortDirection == -1) columnName += " ↓";
            }

            Column = column;
            Header = columnName;
            SortMemberPath = column.Index.ToString();
            Width = WidthForColumn(columnName);
            CellTemplate = CellTemplateFor(column);
            CellEditingTemplate = EditingCellTemplateFor(column);
            UpdateCellStyle();
        }

        private DataTemplate CellTemplateFor(ColumnModel column)
        {
            var key = column.Index.ToString();

            var tBlockStyleTriggerForeground = new DataTrigger { Binding = new Binding($"{key}_color"), Value = "SecondaryTextBrush" };
            tBlockStyleTriggerForeground.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.LightGray));

            var tBlockStyle = new Style(typeof(TextBlock));
            tBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            tBlockStyle.Triggers.Add(tBlockStyleTriggerForeground);

            var tBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            tBlockFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            tBlockFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(3, 0, 3, 2));
            tBlockFactory.SetValue(TextBlock.TextProperty, new Binding(key));
            tBlockFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            tBlockFactory.SetValue(FrameworkElement.StyleProperty, tBlockStyle);
            if (column.IsNumeric) tBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            else if (column.IsBool) tBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);

            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            gridFactory.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
            gridFactory.AppendChild(tBlockFactory);

            return new DataTemplate { VisualTree = gridFactory };
        }
    }

    public class DataTypeColumn : TemplateColumn
    {
        public delegate void ColumnTypeChangedEventHandler(DataGridRow gridRow, ColumnModel column, string newValue);
        public event ColumnTypeChangedEventHandler ColumnTypeChanged;

        public DataTypeColumn(ColumnModel column, int sortDirection, ColumnModel sortColumn)
        {
            var columnName = column.Name.Replace("_", "__");
            if (sortColumn == column)
            {
                if (sortDirection == 1) columnName += " ↑";
                else if (sortDirection == -1) columnName += " ↓";
            }

            Column = column;
            Header = columnName;
            SortMemberPath = column.Index.ToString();
            Width = WidthForColumn(columnName);
            CellTemplate = CellTemplateFor(column);
            CellEditingTemplate = EditingCellTemplateFor(column);
            UpdateCellStyle();
        }

        private DataTemplate CellTemplateFor(ColumnModel column)
        {
            var key = column.Index.ToString();
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var col2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col2.SetValue(ColumnDefinition.WidthProperty, new GridLength(16));

            var tBlockStyleTriggerForeground = new DataTrigger { Binding = new Binding($"{key}_color"), Value = "SecondaryTextBrush" };
            tBlockStyleTriggerForeground.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.LightGray));

            var tBlockStyle = new Style(typeof(TextBlock));
            tBlockStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            tBlockStyle.Triggers.Add(tBlockStyleTriggerForeground);

            var tBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
            tBlockFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            tBlockFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(3, 0, 0, 2));
            tBlockFactory.SetValue(TextBlock.TextProperty, new Binding(key));
            tBlockFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            tBlockFactory.SetValue(FrameworkElement.StyleProperty, tBlockStyle);
            if (column.IsNumeric) tBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Right);
            else if (column.IsBool) tBlockFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);

            var databaseTypes = column.Values ?? new List<string>();
            databaseTypes.Sort();
            var dataTypes = new List<object>();
            dataTypes.AddRange(databaseTypes);

            var cbbFactory = new FrameworkElementFactory(typeof(ComboBox));
            cbbFactory.SetValue(Grid.ColumnProperty, 0);
            cbbFactory.SetValue(Grid.ColumnSpanProperty, 2);
            cbbFactory.SetValue(ComboBox.ItemsSourceProperty, dataTypes);
            cbbFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            cbbFactory.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 22, 0, 0));
            cbbFactory.SetValue(TextBlock.TextProperty, new Binding(key) { Mode = BindingMode.TwoWay });
            cbbFactory.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
            cbbFactory.SetValue(ComboBox.BackgroundProperty, Brushes.Transparent);
            cbbFactory.SetValue(ComboBox.BorderThicknessProperty, new Thickness(0));
            cbbFactory.SetValue(ComboBox.BorderBrushProperty, Brushes.Transparent);
            cbbFactory.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(ColumnTypeComboBoxSelectionChanged));

            var arrowButtonFactory = CreateButtonFactory("ComboBoxRightArrowIcon", 45d, new Thickness(6, -4, 0, 0));
            arrowButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(ArrowButtonClicked));

            var gridFactory = new FrameworkElementFactory(typeof(Grid));
            gridFactory.SetValue(Panel.BackgroundProperty, Brushes.Transparent);
            gridFactory.AppendChild(col1);
            gridFactory.AppendChild(col2);
            gridFactory.AppendChild(cbbFactory);
            gridFactory.AppendChild(tBlockFactory);
            gridFactory.AppendChild(arrowButtonFactory);

            return new DataTemplate { VisualTree = gridFactory };
        }

        public void ColumnTypeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbb && cbb.BindingGroup.Owner is DataGridRow gridRow && cbb.SelectedItem is string newValue)
            {
                ColumnTypeChanged?.Invoke(gridRow, Column, newValue);
                cbb.SelectedItem = null; // Clear the selection to avoid reselect not working.
            }
        }

        private void ArrowButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.Parent is Grid parent)
            {
                foreach (object child in parent.Children)
                {
                    if (child is ComboBox cbb)
                    {
                        cbb.IsDropDownOpen = true;
                        break;
                    }
                }
            }
        }
    }
}

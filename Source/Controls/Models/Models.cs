using System.Collections.Generic;

namespace LargeDataGrid.Source.Controls.Models
{
    public class ColumnModel
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool IsString { get; set; }
        public bool IsBool { get; set; }
        public bool IsNumeric { get; set; }
        public List<string> Values { get; set; }
    }

    public class RowModel
    {
        public List<ColumnModel> Columns { get; set; }
        public List<FieldModel> Fields { get; set; }
        public string LastRaw(int index)
        {
            if (Fields[index].EditedValue != null)
            {
                return Fields[index].EditedValue;
            }
            return Fields[index].Value;
        }
        public void Update(string value, int index)
        {
            Fields[index].EditedValue = value;
        }

        public bool WillDelete { get; set; }
        public bool WillInsert { get; set; }
        public bool IsModified(int index)
        {
            return Fields[index].EditedValue != null;
        }
    }

    public class FieldModel
    {
        public string Value { set; get; }
        public string EditedValue { get; set; }
    }
}

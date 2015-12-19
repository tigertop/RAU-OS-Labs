using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication2
{
    public class ExcelHelper
    {
        public static WorksheetPart GetWorksheetPart(WorkbookPart workbookPart, string sheetName)
        {
            string relId = "";
            var relIdGeter = workbookPart.Workbook.Descendants<Sheet>().Where(x => x.Name == sheetName).FirstOrDefault();
            if (relIdGeter != null)
            {
                relId = relIdGeter.Id;
                return (WorksheetPart)workbookPart.GetPartById(relId);
            }
            return null;

        }

        public static SharedStringItem GetSharedStringItemById(WorkbookPart workbookPart, int id)
        {
            return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(id);
        }
        public static Cell GetCell(Worksheet worksheet, string columnName, int rowIndex)
        {
            Row row = GetRow(worksheet, rowIndex);

            if (row == null)
                return null;

            return row.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, columnName +
                   rowIndex, true) == 0).First();
        }

        public static Cell GetCell(Row row, string columnName)
        {
            if (row == null)
                return null;

            return row.Elements<Cell>().Where(c => string.Compare
                   (c.CellReference.Value, columnName +
                   row.RowIndex, true) == 0).First();
        }


        // Given a worksheet and a row index, return the row.
        public static Row GetRow(Worksheet worksheet, int rowIndex)
        {
            return worksheet.GetFirstChild<SheetData>().
              Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
        }

        public static string GetCellValue(WorkbookPart workbookPart, Cell cell)
        {
            if (workbookPart == null || cell == null)
            {
                return null;
            }

            if (cell.DataType != null)
            {
                if (cell.DataType.Value == CellValues.SharedString)
                {
                    int id = -1;

                    if (Int32.TryParse(cell.CellValue.InnerText, out id))
                    {
                        SharedStringItem item = GetSharedStringItemById(workbookPart, id);

                        if (item.Text != null)
                        {
                            return item.Text.Text;
                        }
                        else if (item.InnerText != null)
                        {
                            return item.InnerText;
                        }
                        else if (item.InnerXml != null)
                        {
                            return item.InnerXml;
                        }
                    }
                }

            }
            if (true)
            {

            }
            return cell.CellValue != null ? cell.CellValue.InnerXml : null;
        }
    }
}

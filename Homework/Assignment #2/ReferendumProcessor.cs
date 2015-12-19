using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace WindowsFormsApplication2
{
    class ReferendumProcessor
    {
        public ReferendumProcessor()
        {

        }

        public ConcurrentQueue<Exception> Errors
        {
            get { return _errors; }
            set { }
        }

        public ConcurrentQueue<Exception> _errors = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        private List<Citizen> ProcessFile(string file)
        {
            List<Citizen> returnValues = new List<Citizen>();

            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(file, false))
            {
                //Load workbook and get sheet with specified name
                WorkbookPart workbookPart = doc.WorkbookPart;
                WorksheetPart workSheetPart = ExcelHelper.GetWorksheetPart(workbookPart, "Sheet1");

                Worksheet sheet = workSheetPart.Worksheet;
                foreach (var row in sheet.Descendants<Row>())
                {
                    if (row.RowIndex < 8)
                    {
                        continue;
                    }

                    try
                    {
                        Citizen newHay = new Citizen();
                        newHay.Firstname = ArmenianUnicodeConverter.Current.ConvertStringFrom1252ToUnicode(
                            ExcelHelper.GetCellValue(workbookPart, ExcelHelper.GetCell(row, "C")));
                        if (string.IsNullOrEmpty(newHay.Firstname))
                        {
                            continue;
                        }
                        newHay.Lastname = ArmenianUnicodeConverter.Current.ConvertStringFrom1252ToUnicode(
                            ExcelHelper.GetCellValue(workbookPart, ExcelHelper.GetCell(row, "B")));
                        newHay.Middlename = ArmenianUnicodeConverter.Current.ConvertStringFrom1252ToUnicode(
                            ExcelHelper.GetCellValue(workbookPart, ExcelHelper.GetCell(row, "D")));
                        newHay.Address = ArmenianUnicodeConverter.Current.ConvertStringFrom1252ToUnicode(
                            ExcelHelper.GetCellValue(workbookPart, ExcelHelper.GetCell(row, "F")));
                        newHay.Tec =
                            ExcelHelper.GetCellValue(workbookPart, ExcelHelper.GetCell(row, "H"));

                        var bday = ExcelHelper.GetCellValue(workbookPart, ExcelHelper.GetCell(row, "E"));
                        if (bday.StartsWith("00/00"))
                        {
                            newHay.Birthday = new DateTime(Convert.ToInt32(bday.Substring(6, 4)), 1, 1);
                        }
                        else
                        {
                            newHay.Birthday = DateTime.ParseExact(bday, "d/M/yyyy", CultureInfo.InvariantCulture);
                        }


                        newHay.State = "N/A";
                        newHay.Community = "N/A";


                        returnValues.Add(newHay);
                    }
                    catch (Exception ex)
                    {

                        errors.Enqueue(new Exception(string.Format("Filename: {0} Row: {1}", file, row.RowIndex.ToString()), ex));
                    }

                }
            }
            return returnValues;
        }
    }
}

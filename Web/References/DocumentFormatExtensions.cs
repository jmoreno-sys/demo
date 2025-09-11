using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Web.References
{
    public static class DocumentFormatExtensions
    {
        public static void ToExcelStream(this DataSet ds, Stream stream, Dictionary<string, uint> headers = null, List<dynamic> totales = null, Stylesheet style = null, Dictionary<string, uint> styleMap = null)
        {
            var columnConverter = new Func<int, string>((contador) =>
            {
                var letras = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z".Split(',');
                if (contador <= letras.Length)
                    return letras[contador - 1];
                else
                {
                    var result = new System.Text.StringBuilder();
                    var index = ((int)Math.Round((decimal)(contador / letras.Length), 0, MidpointRounding.ToEven));
                    var diff = contador - (index * letras.Length);
                    result.Append(letras[index - 1]);
                    result.Append(letras[diff]);
                    return result.ToString();
                }
            });

            if (styleMap == null) styleMap = new Dictionary<string, uint>();

            using (var workbook = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
            {
                var workbookpart = workbook.AddWorkbookPart();
                workbookpart.Workbook = new Workbook();

                var stylePart = workbookpart.AddNewPart<WorkbookStylesPart>();
                if (style != null)
                {
                    stylePart.Stylesheet = style;
                }
                else
                {
                    stylePart.Stylesheet = new Stylesheet(
                        new Fonts(new Font(new FontSize())),
                        new Fills(new Fill(new PatternFill())),
                        new Borders(new Border()),
                        new CellFormats(new CellFormat()));
                }
                stylePart.Stylesheet.Save();

                workbookpart.Workbook.Sheets = new Sheets();

                foreach (var table in ds.Tables.OfType<DataTable>())
                {
                    var sheetPart = workbook.WorkbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    sheetPart.Worksheet = new Worksheet(sheetData);

                    var sheets = workbook.WorkbookPart.Workbook.GetFirstChild<Sheets>();
                    var relationshipId = workbook.WorkbookPart.GetIdOfPart(sheetPart);

                    uint sheetId = 1;
                    if (sheets.Elements<Sheet>().Count() > 0)
                        sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;

                    var sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = "Datos" };
                    sheets.Append(sheet);

                    var headerRow = new Row();
                    if (headers != null)
                    {
                        var hIndex = 1;
                        var mergeCells = new MergeCells();

                        foreach (var item in headers)
                        {
                            var newRow = new Row();
                            newRow.Append(new Cell()
                            {
                                CellValue = new CellValue(item.Key),
                                DataType = new EnumValue<CellValues>(CellValues.String),
                                StyleIndex = item.Value
                            });
                            sheetData.AppendChild(newRow);

                            var l1 = $"A{hIndex}:{columnConverter(table.Columns.Count)}{hIndex}";
                            mergeCells.Append(new MergeCell() { Reference = new StringValue(l1) });
                            hIndex++;
                        }
                        sheetPart.Worksheet.InsertAfter(mergeCells, sheetData);
                    }
                    var colHeaders = table.Columns.OfType<DataColumn>().Select(m => new Cell()
                    {
                        CellValue = new CellValue(m.ColumnName.ToUpper()),
                        DataType = new EnumValue<CellValues>(CellValues.String),
                        StyleIndex = styleMap != null && styleMap.ContainsKey("Header") ? styleMap["Header"] : default(uint)
                    });
                    headerRow.Append(colHeaders);
                    sheetData.AppendChild(headerRow);

                    foreach (DataRow dsrow in table.Rows)
                    {
                        var newRow = new Row();
                        foreach (var column in table.Columns.OfType<DataColumn>())
                        {
                            var cell = new Cell();
                            var value = Convert.ToString(dsrow[column], CultureInfo.InvariantCulture);

                            if (column.DataType == typeof(Decimal) || column.DataType == typeof(decimal) || column.DataType == typeof(int) || column.DataType == typeof(Single) || column.DataType == typeof(double))
                            {
                                cell.DataType = CellValues.Number;
                                cell.CellValue = new CellValue(value);
                            }
                            else if (column.DataType == typeof(DateTime) || column.DataType == typeof(TimeSpan) || column.DataType == typeof(DateTimeOffset))
                            {
                                cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                                if (column.DataType == typeof(DateTime))
                                {
                                    if (!string.IsNullOrEmpty(dsrow[column].ToString()))
                                    {
                                        var vp = ((DateTime)dsrow[column]).ToOADate().ToString(CultureInfo.InvariantCulture);
                                        cell.CellValue = new CellValue(vp);
                                    }
                                }
                                else if (column.DataType == typeof(TimeSpan))
                                {
                                    var dt = new DateTime((dsrow[column] as TimeSpan?).Value.Ticks);
                                    var vp = dt.ToOADate().ToString(CultureInfo.InvariantCulture);
                                    cell.CellValue = new CellValue(vp);
                                }
                            }
                            else if (column.DataType == typeof(bool))
                            {
                                cell.DataType = CellValues.Boolean;
                                cell.CellValue = new CellValue(((bool)dsrow[column] ? 1 : 0).ToString());
                            }
                            else
                            {
                                cell.DataType = CellValues.String;
                                cell.CellValue = new CellValue(value);
                            }

                            if (!styleMap.ContainsKey(column.ColumnName))
                                cell.StyleIndex = styleMap.ContainsKey(column.DataType.Name) ? styleMap[column.DataType.Name] : default(uint);
                            else
                                cell.StyleIndex = styleMap[column.ColumnName];

                            newRow.AppendChild(cell);
                        }
                        sheetData.AppendChild(newRow);
                    }

                    if (totales != null)
                    {
                        var newRowTotales = new Row();
                        var cellString = new Cell();
                        cellString.DataType = CellValues.String;
                        cellString.CellValue = new CellValue("TOTALES");
                        cellString.StyleIndex = 2;
                        newRowTotales.AppendChild(cellString);
                        foreach (var item in totales)
                        {
                            var cell = new Cell();
                            var value = Convert.ToString(item.key, CultureInfo.InvariantCulture);
                            if (item.value == 0)
                            {
                                cell.DataType = CellValues.String;
                                value = "";
                            }
                            else
                                cell.DataType = CellValues.Number;
                            cell.CellValue = new CellValue(value);
                            cell.StyleIndex = item.value;

                            newRowTotales.AppendChild(cell);
                        }
                        sheetData.AppendChild(newRowTotales);
                    }

                    Columns columns = AutoSize(sheetData, headers);
                    sheetPart.Worksheet.InsertAt(columns, 0);
                }
                workbook.WorkbookPart.Workbook.Save();
            }
        }

        public static void ToExcelStream(this DataTable dt, Stream stream, Dictionary<string, uint> headers = null, List<dynamic> totales = null, Stylesheet style = null, Dictionary<string, uint> styleMap = null)
        {
            (new DataSet() { Tables = { dt } }).ToExcelStream(stream, headers, totales, style, styleMap);
        }

        /// <summary>
        /// METODO PARA OBTENER LOS ESTILOS
        /// TODO: cargar desde archivo xml en filestorage
        /// </summary>
        /// <returns></returns>
        public static Stylesheet GenerateStylesheet()
        {
            Fonts fonts = new Fonts(
                new Font( // Index 0 - default
                    new FontSize() { Val = 11 }
                ),
                new Font( // Index 1 - header
                    new FontSize() { Val = 12 },
                    new Bold(),
                    new Color() { Rgb = "#808080" }
                ),
                 new Font
                 ( // Index 1 - header
                    new FontSize() { Val = 13 },
                    new Bold(),
                    //new Color() { Rgb = new HexBinaryValue() { Value = "123c51" } }
                    new Color() { Rgb = new HexBinaryValue() { Value = "000000" } }
                ));

            Fills fills = new Fills(
                    new Fill(new PatternFill() { PatternType = PatternValues.None }), // Index 0 - default
                    new Fill(new PatternFill() { PatternType = PatternValues.Solid }), // Index 1 - default
                    new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue() { Value = "FFFFFF"/*"4fbde8"*/ } })
                    { PatternType = PatternValues.Solid }), // Index 2 - header
                    new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue() { Value = "000000"/*"157688"*/ } }) { PatternType = PatternValues.Solid })
                );

            Borders borders = new Borders(
                    new Border(), // index 0 default
                    new Border( // index 1 black border
                        new LeftBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new RightBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new TopBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new BottomBorder(new Color() { Auto = true }) { Style = BorderStyleValues.Thin },
                        new DiagonalBorder())
                );

            CellFormats cellFormats = new CellFormats(
                    new CellFormat(), // default
                    new CellFormat { FontId = 0, FillId = 0, BorderId = 1, ApplyBorder = true }, // body
                    new CellFormat { FontId = 1, FillId = 0, BorderId = 0, ApplyFill = true, Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center }, ApplyAlignment = true },// header
                    new CellFormat { FontId = 2, FillId = 0, BorderId = 0, ApplyFill = true, Alignment = new Alignment() { Horizontal = HorizontalAlignmentValues.Center, WrapText = true }, ApplyAlignment = true }, //cabecera
                    new CellFormat { NumberFormatId = 14, ApplyNumberFormat = true },
                    new CellFormat { NumberFormatId = 21, ApplyNumberFormat = true },
                    new CellFormat { NumberFormatId = 22, ApplyNumberFormat = true }
                );

            var styleSheet = new Stylesheet(fonts, fills, borders, cellFormats);

            //System.IO.File.WriteAllText(@"demo.xml", styleSheet.InnerXml);

            //var xs = new XmlSerializer(typeof(HashSet<Stylesheet>));
            //using (var sm = new StreamWriter(@"D:\demo.xml"))
            //{
            //    xs.Serialize(sm, new HashSet<Stylesheet>() { styleSheet });
            //}
            //var data = Newtonsoft.Json.JsonConvert.SerializeObject(styleSheet);

            return styleSheet;
        }

        private static Columns AutoSize(SheetData sheetData, Dictionary<string, uint> headers = null)
        {
            var maxColWidth = GetMaxCharacterWidth(sheetData, headers);

            Columns columns = new Columns();
            //this is the width of my font - yours may be different
            double maxWidth = 7;
            foreach (var item in maxColWidth)
            {
                //width = Truncate([{Number of Characters} * {Maximum Digit Width} + {5 pixel padding}]/{Maximum Digit Width}*256)/256
                double width = Math.Truncate((item.Value * maxWidth + 5) / maxWidth * 256) / 256;

                //pixels=Truncate(((256 * {width} + Truncate(128/{Maximum Digit Width}))/256)*{Maximum Digit Width})
                double pixels = Math.Truncate(((256 * width + Math.Truncate(128 / maxWidth)) / 256) * maxWidth);

                //character width=Truncate(({pixels}-5)/{Maximum Digit Width} * 100+0.5)/100
                double charWidth = Math.Truncate((pixels - 5) / maxWidth * 100 + 0.5) / 100;

                Column col = new Column() { BestFit = true, Min = (UInt32)(item.Key + 1), Max = (UInt32)(item.Key + 1), CustomWidth = true, Width = (DoubleValue)width };
                columns.Append(col);
            }

            return columns;
        }

        private static Dictionary<int, int> GetMaxCharacterWidth(SheetData sheetData, Dictionary<string, uint> headers = null)
        {
            //iterate over all cells getting a max char value for each column
            Dictionary<int, int> maxColWidth = new Dictionary<int, int>();
            var rows = sheetData.Elements<Row>().ToList();
            if (headers != null)
                rows.RemoveRange(0, 2);
            UInt32[] numberStyles = new UInt32[] { 5, 6, 7, 8 }; //styles that will add extra chars
            UInt32[] boldStyles = new UInt32[] { 1, 2, 3, 4, 6, 7, 8 }; //styles that will bold
            foreach (var r in rows)
            {
                var cells = r.Elements<Cell>().ToArray();

                //using cell index as my column
                for (int i = 0; i < cells.Length; i++)
                {
                    var cell = cells[i];
                    var cellValue = cell.CellValue == null ? string.Empty : cell.CellValue.InnerText;
                    var bandera = false;
                    double retNum;
                    if (cellValue.Contains(".") && Double.TryParse(cellValue, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out retNum))
                    {
                        var temp = cellValue.Split(".");
                        if (temp[1].Length > 5)
                            bandera = true;
                    }

                    var cellTextLength = cellValue.Length;

                    if (cell.StyleIndex != null && numberStyles.Contains(cell.StyleIndex))
                    {
                        int thousandCount = (int)Math.Truncate((double)cellTextLength / 4);

                        //add 3 for '.00'
                        cellTextLength += (3 + thousandCount);
                    }

                    if (cell.StyleIndex != null && boldStyles.Contains(cell.StyleIndex) && bandera == false)
                    {
                        //add an extra char for bold - not 100% acurate but good enough for what i need.
                        if (cellValue == "NÚMERO DOCUMENTO")
                            cellTextLength += 8;
                        else
                            cellTextLength += 5;
                    }

                    if (maxColWidth.ContainsKey(i))
                    {
                        if (bandera == false)
                        {
                            var current = maxColWidth[i];
                            if (cellTextLength > current)
                            {
                                maxColWidth[i] = cellTextLength + 5;
                            }
                        }
                    }
                    else
                    {
                        maxColWidth.Add(i, cellTextLength);
                    }
                }
            }

            return maxColWidth;
        }
    }
}

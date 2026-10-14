using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;

namespace GestorComunicacionesDNET;

internal static class PdfReportGenerator
{
	private const string PdfPrinterName = "Microsoft Print to PDF";

	internal static bool IsAvailable => PrinterSettings.InstalledPrinters.Cast<string>().Any(name => string.Equals(name, PdfPrinterName, StringComparison.OrdinalIgnoreCase));

	internal static void Create(string path, IReadOnlyList<JsonObject> contacts, IReadOnlyList<PdfReportColumn> columns, string title, string filterSummary)
	{
		if (!IsAvailable)
		{
			throw new InvalidOperationException("No está instalada la impresora 'Microsoft Print to PDF' de Windows.");
		}
		if (contacts.Count == 0)
		{
			throw new InvalidOperationException("No hay contactos para incluir en el informe.");
		}
		if (columns.Count == 0)
		{
			throw new InvalidOperationException("No se ha seleccionado ninguna columna.");
		}

		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
		using PrintDocument document = new();
		document.DocumentName = title;
		document.PrintController = new StandardPrintController();
		document.PrinterSettings.PrinterName = PdfPrinterName;
		document.PrinterSettings.PrintToFile = true;
		document.PrinterSettings.PrintFileName = Path.GetFullPath(path);
		document.DefaultPageSettings.Landscape = true;
		document.DefaultPageSettings.Margins = new Margins(32, 32, 35, 35);

		int contactIndex = 0;
		int pageNumber = 0;
		using Font brandFont = new("Segoe UI", 8.5f, FontStyle.Bold);
		using Font titleFont = new("Segoe UI", 15f, FontStyle.Bold);
		using Font infoFont = new("Segoe UI", 8f);
		using Font headerFont = new("Segoe UI", 7.5f, FontStyle.Bold);
		using Font cellFont = new("Segoe UI", 7.5f);
		using Pen gridPen = new(Color.FromArgb(190, 198, 210), 0.6f);
		using SolidBrush headerBrush = new(Color.FromArgb(36, 67, 112));
		using SolidBrush alternateBrush = new(Color.FromArgb(241, 245, 250));
		using SolidBrush whiteBrush = new(Color.White);
		using StringFormat cellFormat = new() { Trimming = StringTrimming.EllipsisWord, FormatFlags = StringFormatFlags.LineLimit };

		document.PrintPage += (_, e) =>
		{
			Graphics graphics = e.Graphics ?? throw new InvalidOperationException("No se pudo preparar la página del informe.");
			pageNumber++;
			Rectangle bounds = e.MarginBounds;
			float x = bounds.Left;
			float y = bounds.Top;
			float width = bounds.Width;

			graphics.DrawString("Gestor de Comunicaciones DNET", brandFont, Brushes.DimGray, x, y);
			y += 19f;
			graphics.DrawString(title, titleFont, Brushes.Black, new RectangleF(x, y, width, 30f), cellFormat);
			y += 31f;
			string information = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}    ·    Contactos: {contacts.Count}    ·    {filterSummary}";
			graphics.DrawString(information, infoFont, Brushes.DimGray, new RectangleF(x, y, width, 28f), cellFormat);
			y += 31f;

			float totalWeight = columns.Sum(column => column.Weight);
			float[] widths = columns.Select(column => width * column.Weight / totalWeight).ToArray();
			const float headerHeight = 29f;
			DrawHeader(graphics, columns, widths, x, y, headerHeight, headerFont, headerBrush, whiteBrush, gridPen, cellFormat);
			y += headerHeight;

			float footerTop = bounds.Bottom - 22f;
			while (contactIndex < contacts.Count)
			{
				JsonObject contact = contacts[contactIndex];
				float rowHeight = MeasureRowHeight(graphics, contact, columns, widths, cellFont);
				if (y + rowHeight > footerTop && y > bounds.Top + 110f)
				{
					break;
				}
				DrawRow(graphics, contact, columns, widths, x, y, rowHeight, cellFont, contactIndex % 2 == 1 ? alternateBrush : null, gridPen, cellFormat);
				y += rowHeight;
				contactIndex++;
			}

			using StringFormat footerFormat = new() { Alignment = StringAlignment.Far };
			graphics.DrawString($"Página {pageNumber}", infoFont, Brushes.DimGray, new RectangleF(x, bounds.Bottom - 16f, width, 16f), footerFormat);
			e.HasMorePages = contactIndex < contacts.Count;
		};

		document.Print();
		if (!File.Exists(path) || new FileInfo(path).Length == 0)
		{
			throw new IOException("Windows no pudo crear el archivo PDF.");
		}
	}

	private static float MeasureRowHeight(Graphics graphics, JsonObject contact, IReadOnlyList<PdfReportColumn> columns, float[] widths, Font font)
	{
		float height = 24f;
		for (int index = 0; index < columns.Count; index++)
		{
			string value = Clean(ContactStore.Get(contact, columns[index].Field));
			SizeF measured = graphics.MeasureString(value, font, Math.Max(20, (int)widths[index] - 7));
			height = Math.Max(height, Math.Min(52f, measured.Height + 7f));
		}
		return height;
	}

	private static void DrawHeader(Graphics graphics, IReadOnlyList<PdfReportColumn> columns, float[] widths, float x, float y, float height, Font font, Brush background, Brush foreground, Pen pen, StringFormat format)
	{
		for (int index = 0; index < columns.Count; index++)
		{
			RectangleF cell = new(x, y, widths[index], height);
			graphics.FillRectangle(background, cell);
			graphics.DrawRectangle(pen, cell.X, cell.Y, cell.Width, cell.Height);
			graphics.DrawString(columns[index].Header, font, foreground, new RectangleF(cell.X + 3f, cell.Y + 5f, cell.Width - 6f, cell.Height - 8f), format);
			x += widths[index];
		}
	}

	private static void DrawRow(Graphics graphics, JsonObject contact, IReadOnlyList<PdfReportColumn> columns, float[] widths, float x, float y, float height, Font font, Brush? background, Pen pen, StringFormat format)
	{
		for (int index = 0; index < columns.Count; index++)
		{
			RectangleF cell = new(x, y, widths[index], height);
			if (background != null)
			{
				graphics.FillRectangle(background, cell);
			}
			graphics.DrawRectangle(pen, cell.X, cell.Y, cell.Width, cell.Height);
			graphics.DrawString(Clean(ContactStore.Get(contact, columns[index].Field)), font, Brushes.Black, new RectangleF(cell.X + 3f, cell.Y + 4f, cell.Width - 6f, cell.Height - 7f), format);
			x += widths[index];
		}
	}

	private static string Clean(string value)
	{
		string cleaned = string.Join(" ", value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
		return cleaned.Length <= 260 ? cleaned : cleaned[..257] + "...";
	}
}

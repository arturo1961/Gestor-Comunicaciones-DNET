using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed record ChartField(string Key, string Caption, Func<JsonObject, string>? Reader = null)
{
	public string Read(JsonObject contact) => Reader?.Invoke(contact) ?? ContactStore.Get(contact, Key);
	public override string ToString() => UiText.Get(Caption);
}

internal sealed record ChartKindOption(ChartKind Kind, string Caption)
{
	public override string ToString() => UiText.Get(Caption);
}

internal sealed record ChartPoint(string Label, int Value);

internal enum ChartKind
{
	Columns,
	HorizontalBars,
	Pie,
	Doughnut,
	Line,
	Area
}

internal sealed class ChartsForm : DnetForm
{
	private static readonly ChartField[] Fields =
	[
		new("nombre", "Nombre / entidad"),
		new("correo", "Correo electrónico"),
		new("dominio_correo", "Dominio del correo", contact => EmailDomain(ContactStore.Get(contact, "correo"))),
		new("pais", "País"),
		new("region", "Región"),
		new("idioma", "Idioma"),
		new("tipo", "Tipo de contacto"),
		new("naturaleza", "Organización"),
		new("canal", "Canal"),
		new("persona_contacto", "Persona de contacto"),
		new("uso_del_correo", "Uso del correo"),
		new("verificacion", "Verificación / fuente"),
		new("estado", "Estado"),
		new("ultima_version_comunicada", "Última versión enviada"),
		new("fecha_ultimo_envio", "Mes del último envío", contact => Month(ContactStore.Get(contact, "fecha_ultimo_envio"))),
		new("no_contactar", "No contactar", contact => ContactStore.MarkedNoContact(contact) ? UiText.Get("Sí") : UiText.Get("No")),
		new("origen_csv", "Origen del registro")
	];

	private readonly List<ContactRow> _rows;
	private readonly string _mainFilterSummary;
	private readonly ComboBox _groupField = DropDown(190);
	private readonly ComboBox _filterField = DropDown(190);
	private readonly ComboBox _filterValue = DropDown(210);
	private readonly ComboBox _chartKind = DropDown(180);
	private readonly ComboBox _scope = DropDown(155);
	private readonly NumericUpDown _maximum = new()
	{
		Minimum = 3,
		Maximum = 50,
		Value = 15,
		Width = 72,
		Margin = new Padding(5, 5, 14, 4)
	};
	private readonly CheckBox _includeEmpty = new()
	{
		Text = "Incluir valores vacíos",
		AutoSize = true,
		Margin = new Padding(8, 10, 14, 0)
	};
	private readonly ChartCanvas _chart = new() { Dock = DockStyle.Fill };
	private readonly DataGridView _summaryGrid = new();
	private readonly Label _summary = new()
	{
		Dock = DockStyle.Fill,
		TextAlign = ContentAlignment.MiddleLeft,
		ForeColor = Color.FromArgb(63, 78, 102)
	};

	public ChartsForm(IEnumerable<ContactRow> visibleRows, string mainFilterSummary)
	{
		_rows = visibleRows.ToList();
		_mainFilterSummary = mainFilterSummary;
		Text = "Gráficos de contactos";
		StartPosition = FormStartPosition.CenterParent;
		Size = new Size(1280, 820);
		MinimumSize = new Size(960, 650);
		Font = new Font("Segoe UI", 10f);
		BackColor = Color.FromArgb(244, 247, 251);

		var root = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			Padding = new Padding(12)
		};
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
		Controls.Add(root);

		var header = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = Color.FromArgb(31, 55, 88),
			Padding = new Padding(18, 7, 18, 7),
			Margin = new Padding(0, 0, 0, 8)
		};
		header.Controls.Add(new Label
		{
			Text = "Crea gráficos con los contactos visibles y elige libremente el campo de agrupación y filtro.",
			Dock = DockStyle.Bottom,
			Height = 23,
			ForeColor = Color.FromArgb(216, 228, 244)
		});
		header.Controls.Add(new Label
		{
			Text = "Gráficos de contactos",
			Dock = DockStyle.Top,
			Height = 30,
			Font = new Font("Segoe UI Semibold", 16f),
			ForeColor = Color.White
		});
		root.Controls.Add(header, 0, 0);

		var selectors = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			WrapContents = true,
			AutoScroll = true,
			BackColor = Color.White,
			Padding = new Padding(8, 5, 8, 4),
			Margin = new Padding(0, 0, 0, 8)
		};
		AddLabeled(selectors, "Agrupar por", _groupField);
		AddLabeled(selectors, "Tipo de gráfico", _chartKind);
		AddLabeled(selectors, "Datos", _scope);
		AddLabeled(selectors, "Filtrar por", _filterField);
		AddLabeled(selectors, "Valor", _filterValue);
		AddLabeled(selectors, "Máximo de categorías", _maximum);
		selectors.Controls.Add(_includeEmpty);
		root.Controls.Add(selectors, 0, 1);

		var split = new SplitContainer
		{
			Dock = DockStyle.Fill,
			Orientation = Orientation.Vertical,
			SplitterDistance = 880,
			SplitterWidth = 7,
			Panel1MinSize = 560,
			Panel2MinSize = 240
		};
		split.Panel1.Padding = new Padding(0, 0, 5, 0);
		split.Panel2.Padding = new Padding(5, 0, 0, 0);
		split.Panel1.Controls.Add(_chart);
		SetupSummaryGrid();
		split.Panel2.Controls.Add(_summaryGrid);
		root.Controls.Add(split, 0, 2);

		var footer = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1,
			BackColor = Color.White,
			Padding = new Padding(8, 6, 8, 4),
			Margin = new Padding(0, 8, 0, 0)
		};
		footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		footer.Controls.Add(_summary, 0, 0);
		var actions = new FlowLayoutPanel { AutoSize = true, WrapContents = false, FlowDirection = FlowDirection.RightToLeft };
		actions.Controls.Add(ActionButton("Cerrar", (_, _) => Close(), 95));
		actions.Controls.Add(ActionButton("Copiar imagen", CopyImage, 135));
		actions.Controls.Add(ActionButton("Guardar PNG...", SavePng, 140));
		footer.Controls.Add(actions, 1, 0);
		root.Controls.Add(footer, 0, 3);

		_groupField.Items.AddRange(Fields.Cast<object>().ToArray());
		_filterField.Items.Add(UiText.Get("Sin filtro adicional"));
		_filterField.Items.AddRange(Fields.Cast<object>().ToArray());
		_chartKind.Items.AddRange(new object[]
		{
			new ChartKindOption(ChartKind.Columns, "Columnas"),
			new ChartKindOption(ChartKind.HorizontalBars, "Barras horizontales"),
			new ChartKindOption(ChartKind.Pie, "Circular"),
			new ChartKindOption(ChartKind.Doughnut, "Anillo"),
			new ChartKindOption(ChartKind.Line, "Líneas"),
			new ChartKindOption(ChartKind.Area, "Área")
		});
		_scope.Items.AddRange(new object[] { UiText.Get("Contactos visibles"), UiText.Get("Sólo marcados") });
		_groupField.SelectedIndex = 3;
		_filterField.SelectedIndex = 0;
		_chartKind.SelectedIndex = 0;
		_scope.SelectedIndex = 0;

		_groupField.SelectedIndexChanged += (_, _) => UpdateChart();
		_chartKind.SelectedIndexChanged += (_, _) => UpdateChart();
		_scope.SelectedIndexChanged += (_, _) => { PopulateFilterValues(); UpdateChart(); };
		_filterField.SelectedIndexChanged += (_, _) => { PopulateFilterValues(); UpdateChart(); };
		_filterValue.SelectedIndexChanged += (_, _) => UpdateChart();
		_maximum.ValueChanged += (_, _) => UpdateChart();
		_includeEmpty.CheckedChanged += (_, _) => { PopulateFilterValues(); UpdateChart(); };

		PopulateFilterValues();
		UpdateChart();
		UiText.Apply(this);
	}

	private static ComboBox DropDown(int width) => new()
	{
		DropDownStyle = ComboBoxStyle.DropDownList,
		Width = width,
		Margin = new Padding(5, 4, 12, 4)
	};

	private static Button ActionButton(string text, EventHandler action, int width)
	{
		var button = new Button
		{
			Text = text,
			AutoSize = true,
			MinimumSize = new Size(width, 35),
			Margin = new Padding(5, 0, 0, 0),
			Padding = new Padding(8, 1, 8, 1)
		};
		button.Click += action;
		return button;
	}

	private static void AddLabeled(FlowLayoutPanel parent, string text, Control input)
	{
		var group = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 5, 2) };
		group.Controls.Add(new Label { Text = text + ":", AutoSize = true, Margin = new Padding(4, 11, 1, 0) });
		group.Controls.Add(input);
		parent.Controls.Add(group);
	}

	private void SetupSummaryGrid()
	{
		_summaryGrid.Dock = DockStyle.Fill;
		_summaryGrid.ReadOnly = true;
		_summaryGrid.AllowUserToAddRows = false;
		_summaryGrid.AllowUserToDeleteRows = false;
		_summaryGrid.RowHeadersVisible = false;
		_summaryGrid.AutoGenerateColumns = false;
		_summaryGrid.BackgroundColor = Color.White;
		_summaryGrid.BorderStyle = BorderStyle.FixedSingle;
		_summaryGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_summaryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "category", HeaderText = "Categoría", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 120 });
		_summaryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "count", HeaderText = "Cantidad", Width = 84 });
		_summaryGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "percentage", HeaderText = "%", Width = 68 });
	}

	private IEnumerable<ContactRow> ScopedRows() => _scope.SelectedIndex == 1 ? _rows.Where(row => row.Elegido) : _rows;

	private void PopulateFilterValues()
	{
		string? previous = _filterValue.SelectedItem?.ToString();
		_filterValue.Items.Clear();
		_filterValue.Items.Add(UiText.Get("Todos"));
		if (_filterField.SelectedItem is ChartField field)
		{
			foreach (string value in ScopedRows().Select(row => Normalize(field.Read(row.Data)))
				.Where(value => _includeEmpty.Checked || value.Length > 0)
				.Select(value => value.Length == 0 ? UiText.Get("Sin datos") : value)
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase))
			{
				_filterValue.Items.Add(value);
			}
			_filterValue.Enabled = true;
		}
		else
		{
			_filterValue.Enabled = false;
		}
		_filterValue.SelectedItem = previous is not null && _filterValue.Items.Contains(previous) ? previous : UiText.Get("Todos");
	}

	private void UpdateChart()
	{
		if (_groupField.SelectedItem is not ChartField groupField || _chartKind.SelectedItem is not ChartKindOption kind) return;
		IEnumerable<ContactRow> selected = ScopedRows();
		if (_filterField.SelectedItem is ChartField filterField && _filterValue.SelectedItem?.ToString() is string filterValue && filterValue != UiText.Get("Todos"))
		{
			selected = selected.Where(row =>
			{
				string value = Normalize(filterField.Read(row.Data));
				if (value.Length == 0) value = UiText.Get("Sin datos");
				return string.Equals(value, filterValue, StringComparison.CurrentCultureIgnoreCase);
			});
		}

		List<ContactRow> contacts = selected.ToList();
		List<ChartPoint> all = contacts
			.Select(row => Normalize(groupField.Read(row.Data)))
			.Where(value => _includeEmpty.Checked || value.Length > 0)
			.Select(value => value.Length == 0 ? UiText.Get("Sin datos") : value)
			.GroupBy(value => value, StringComparer.CurrentCultureIgnoreCase)
			.Select(group => new ChartPoint(group.Key, group.Count()))
			.OrderByDescending(point => point.Value)
			.ThenBy(point => point.Label, StringComparer.CurrentCultureIgnoreCase)
			.ToList();

		int maximum = Decimal.ToInt32(_maximum.Value);
		List<ChartPoint> points = all.Take(maximum).ToList();
		if (all.Count > maximum)
		{
			points.Add(new ChartPoint(UiText.Get("Otros"), all.Skip(maximum).Sum(point => point.Value)));
		}

		string title = UiText.Format("Contactos por {0}", UiText.Get(groupField.Caption));
		_chart.SetData(points, kind.Kind, title);
		FillSummaryGrid(points);
		_summary.Text = UiText.Format("{0} contactos analizados · {1} categorías · Filtros principales: {2}", contacts.Count, all.Count, _mainFilterSummary);
	}

	private void FillSummaryGrid(IReadOnlyList<ChartPoint> points)
	{
		_summaryGrid.Rows.Clear();
		int total = points.Sum(point => point.Value);
		foreach (ChartPoint point in points)
		{
			double percentage = total == 0 ? 0 : point.Value * 100d / total;
			_summaryGrid.Rows.Add(point.Label, point.Value.ToString("N0", CultureInfo.CurrentCulture), percentage.ToString("N1", CultureInfo.CurrentCulture));
		}
	}

	private void SavePng(object? sender, EventArgs e)
	{
		using var dialog = new SaveFileDialog
		{
			Title = UiText.Get("Guardar gráfico"),
			Filter = "PNG (*.png)|*.png",
			FileName = "grafico_contactos.png"
		};
		if (dialog.ShowDialog(this) != DialogResult.OK) return;
		using Bitmap image = _chart.Render(new Size(1600, 900));
		image.Save(dialog.FileName, ImageFormat.Png);
		MessageBox.Show(this, UiText.Format("Gráfico guardado correctamente.\n\n{0}", dialog.FileName), UiText.Get("Guardar gráfico"), MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void CopyImage(object? sender, EventArgs e)
	{
		using Bitmap image = _chart.Render(new Size(1400, 800));
		Clipboard.SetImage(new Bitmap(image));
		MessageBox.Show(this, UiText.Get("El gráfico se ha copiado al portapapeles."), UiText.Get("Copiar imagen"), MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private static string Normalize(string value)
	{
		string normalized = string.Join(" ", value.Replace('\r', ' ').Replace('\n', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries));
		return normalized.Length <= 80 ? normalized : normalized[..77] + "...";
	}

	private static string EmailDomain(string email)
	{
		int at = email.LastIndexOf('@');
		return at >= 0 && at < email.Length - 1 ? email[(at + 1)..].Trim().ToLowerInvariant() : "";
	}

	private static string Month(string value)
	{
		return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset date)
			? date.ToString("yyyy-MM", CultureInfo.InvariantCulture)
			: value;
	}
}

internal sealed class ChartCanvas : Control
{
	private static readonly Color[] Palette =
	[
		Color.FromArgb(31, 111, 235), Color.FromArgb(0, 166, 153), Color.FromArgb(245, 166, 35),
		Color.FromArgb(218, 73, 107), Color.FromArgb(116, 89, 217), Color.FromArgb(42, 157, 143),
		Color.FromArgb(230, 126, 34), Color.FromArgb(64, 120, 192), Color.FromArgb(143, 188, 143),
		Color.FromArgb(192, 90, 135), Color.FromArgb(87, 117, 144), Color.FromArgb(233, 196, 106)
	];

	private IReadOnlyList<ChartPoint> _points = Array.Empty<ChartPoint>();
	private ChartKind _kind;
	private string _title = "";

	public ChartCanvas()
	{
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		BackColor = Color.White;
	}

	public void SetData(IReadOnlyList<ChartPoint> points, ChartKind kind, string title)
	{
		_points = points;
		_kind = kind;
		_title = title;
		Invalidate();
	}

	public Bitmap Render(Size size)
	{
		var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		Draw(graphics, new Rectangle(Point.Empty, size));
		return bitmap;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Draw(e.Graphics, ClientRectangle);
	}

	private void Draw(Graphics graphics, Rectangle bounds)
	{
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
		graphics.Clear(Color.White);
		if (bounds.Width < 200 || bounds.Height < 160) return;

		using var titleFont = new Font("Segoe UI Semibold", Math.Max(12, Math.Min(19, bounds.Width / 55f)));
		using var textFont = new Font("Segoe UI", Math.Max(8, Math.Min(10, bounds.Width / 100f)));
		using var smallFont = new Font("Segoe UI", Math.Max(7, Math.Min(9, bounds.Width / 115f)));
		using var titleBrush = new SolidBrush(Color.FromArgb(28, 51, 82));
		graphics.DrawString(_title, titleFont, titleBrush, new RectangleF(24, 17, bounds.Width - 48, 36), new StringFormat { Alignment = StringAlignment.Center });

		Rectangle plot = Rectangle.FromLTRB(62, 65, bounds.Width - 24, bounds.Height - 48);
		if (_points.Count == 0)
		{
			using var emptyBrush = new SolidBrush(Color.FromArgb(100, 112, 132));
			graphics.DrawString(UiText.Get("No hay datos para representar."), textFont, emptyBrush, plot, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
			return;
		}

		switch (_kind)
		{
			case ChartKind.HorizontalBars: DrawHorizontalBars(graphics, plot, textFont, smallFont); break;
			case ChartKind.Pie: DrawPie(graphics, plot, textFont, doughnut: false); break;
			case ChartKind.Doughnut: DrawPie(graphics, plot, textFont, doughnut: true); break;
			case ChartKind.Line: DrawLineOrArea(graphics, plot, textFont, smallFont, area: false); break;
			case ChartKind.Area: DrawLineOrArea(graphics, plot, textFont, smallFont, area: true); break;
			default: DrawColumns(graphics, plot, textFont, smallFont); break;
		}
	}

	private void DrawColumns(Graphics graphics, Rectangle plot, Font textFont, Font smallFont)
	{
		int maximum = Math.Max(1, _points.Max(point => point.Value));
		DrawGrid(graphics, plot, maximum, textFont);
		float slot = plot.Width / (float)_points.Count;
		float barWidth = Math.Max(7, slot * 0.62f);
		for (int index = 0; index < _points.Count; index++)
		{
			ChartPoint point = _points[index];
			float height = point.Value / (float)maximum * (plot.Height - 28);
			float x = plot.Left + index * slot + (slot - barWidth) / 2f;
			float y = plot.Bottom - height;
			using var brush = new SolidBrush(Palette[index % Palette.Length]);
			graphics.FillRectangle(brush, x, y, barWidth, height);
			DrawCentered(graphics, point.Value.ToString("N0"), smallFont, Brushes.DimGray, new RectangleF(x - 5, y - 21, barWidth + 10, 20));
			DrawCentered(graphics, Ellipsize(point.Label, 15), smallFont, Brushes.DimGray, new RectangleF(plot.Left + index * slot, plot.Bottom + 5, slot, 35));
		}
	}

	private void DrawHorizontalBars(Graphics graphics, Rectangle plot, Font textFont, Font smallFont)
	{
		int maximum = Math.Max(1, _points.Max(point => point.Value));
		int labelWidth = Math.Min(240, Math.Max(120, plot.Width / 3));
		Rectangle bars = Rectangle.FromLTRB(plot.Left + labelWidth, plot.Top, plot.Right - 30, plot.Bottom);
		float slot = bars.Height / (float)_points.Count;
		float barHeight = Math.Max(7, slot * 0.62f);
		for (int index = 0; index < _points.Count; index++)
		{
			ChartPoint point = _points[index];
			float width = point.Value / (float)maximum * bars.Width;
			float y = bars.Top + index * slot + (slot - barHeight) / 2f;
			using var brush = new SolidBrush(Palette[index % Palette.Length]);
			graphics.FillRectangle(brush, bars.Left, y, width, barHeight);
			using var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
			graphics.DrawString(point.Label, smallFont, Brushes.DimGray, new RectangleF(plot.Left, y - 3, labelWidth - 10, barHeight + 6), format);
			graphics.DrawString(point.Value.ToString("N0"), smallFont, Brushes.DimGray, bars.Left + width + 5, y + Math.Max(0, (barHeight - smallFont.Height) / 2f));
		}
	}

	private void DrawPie(Graphics graphics, Rectangle plot, Font textFont, bool doughnut)
	{
		int total = _points.Sum(point => point.Value);
		int legendWidth = Math.Min(300, Math.Max(210, plot.Width / 3));
		Rectangle pieArea = Rectangle.FromLTRB(plot.Left, plot.Top, plot.Right - legendWidth - 15, plot.Bottom);
		int diameter = Math.Min(pieArea.Width, pieArea.Height) - 20;
		var circle = new Rectangle(pieArea.Left + (pieArea.Width - diameter) / 2, pieArea.Top + (pieArea.Height - diameter) / 2, diameter, diameter);
		float start = -90;
		for (int index = 0; index < _points.Count; index++)
		{
			float sweep = total == 0 ? 0 : _points[index].Value * 360f / total;
			using var brush = new SolidBrush(Palette[index % Palette.Length]);
			graphics.FillPie(brush, circle, start, sweep);
			start += sweep;
		}
		if (doughnut)
		{
			int inner = (int)(diameter * 0.52);
			graphics.FillEllipse(Brushes.White, circle.Left + (diameter - inner) / 2, circle.Top + (diameter - inner) / 2, inner, inner);
			using var totalFont = new Font("Segoe UI Semibold", 16f);
			DrawCentered(graphics, total.ToString("N0"), totalFont, Brushes.DimGray, new RectangleF(circle.Left, circle.Top + diameter / 2f - 18, diameter, 36));
		}

		float legendX = plot.Right - legendWidth + 8;
		float rowHeight = Math.Max(21, Math.Min(30, plot.Height / (float)Math.Max(1, _points.Count)));
		for (int index = 0; index < _points.Count; index++)
		{
			float y = plot.Top + index * rowHeight;
			using var brush = new SolidBrush(Palette[index % Palette.Length]);
			graphics.FillRectangle(brush, legendX, y + 4, 14, 14);
			double percentage = total == 0 ? 0 : _points[index].Value * 100d / total;
			string text = $"{Ellipsize(_points[index].Label, 22)}  {_points[index].Value:N0} ({percentage:N1} %)";
			graphics.DrawString(text, textFont, Brushes.DimGray, legendX + 21, y);
		}
	}

	private void DrawLineOrArea(Graphics graphics, Rectangle plot, Font textFont, Font smallFont, bool area)
	{
		int maximum = Math.Max(1, _points.Max(point => point.Value));
		DrawGrid(graphics, plot, maximum, textFont);
		float step = _points.Count == 1 ? 0 : plot.Width / (float)(_points.Count - 1);
		PointF[] points = _points.Select((point, index) => new PointF(plot.Left + index * step, plot.Bottom - point.Value / (float)maximum * (plot.Height - 28))).ToArray();
		if (area && points.Length > 0)
		{
			PointF[] polygon = [new(plot.Left, plot.Bottom), .. points, new(plot.Right, plot.Bottom)];
			using var fill = new SolidBrush(Color.FromArgb(75, 31, 111, 235));
			graphics.FillPolygon(fill, polygon);
		}
		using var pen = new Pen(Color.FromArgb(31, 111, 235), 3f) { LineJoin = LineJoin.Round };
		if (points.Length > 1) graphics.DrawLines(pen, points);
		foreach ((PointF point, int index) in points.Select((point, index) => (point, index)))
		{
			graphics.FillEllipse(Brushes.White, point.X - 5, point.Y - 5, 10, 10);
			graphics.DrawEllipse(pen, point.X - 5, point.Y - 5, 10, 10);
			DrawCentered(graphics, _points[index].Value.ToString("N0"), smallFont, Brushes.DimGray, new RectangleF(point.X - 22, point.Y - 25, 44, 18));
			float slot = _points.Count == 1 ? plot.Width : Math.Max(35, step);
			DrawCentered(graphics, Ellipsize(_points[index].Label, 15), smallFont, Brushes.DimGray, new RectangleF(point.X - slot / 2, plot.Bottom + 5, slot, 35));
		}
	}

	private static void DrawGrid(Graphics graphics, Rectangle plot, int maximum, Font font)
	{
		using var gridPen = new Pen(Color.FromArgb(228, 233, 240), 1f);
		using var axisPen = new Pen(Color.FromArgb(150, 160, 175), 1f);
		for (int step = 0; step <= 5; step++)
		{
			float y = plot.Bottom - step * plot.Height / 5f;
			graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);
			string label = Math.Round(maximum * step / 5d).ToString("N0");
			using var format = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
			graphics.DrawString(label, font, Brushes.Gray, new RectangleF(0, y - 11, plot.Left - 8, 22), format);
		}
		graphics.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
		graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
	}

	private static void DrawCentered(Graphics graphics, string text, Font font, Brush brush, RectangleF rectangle)
	{
		using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
		graphics.DrawString(text, font, brush, rectangle, format);
	}

	private static string Ellipsize(string text, int maximum) => text.Length <= maximum ? text : text[..Math.Max(1, maximum - 1)] + "…";
}

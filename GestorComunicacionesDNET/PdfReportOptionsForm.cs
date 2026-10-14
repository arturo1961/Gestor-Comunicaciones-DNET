using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed record PdfReportColumn(string Field, string Header, float Weight)
{
	public override string ToString() => Header;
}

internal sealed class PdfReportOptionsForm : Form
{
	private readonly RadioButton _visible;
	private readonly RadioButton _marked;
	private readonly TextBox _title;
	private readonly CheckedListBox _columns;

	internal static IReadOnlyList<PdfReportColumn> AvailableColumns { get; } = new List<PdfReportColumn>
	{
		new("nombre", "Nombre", 1.55f),
		new("correo", "Correo electrónico", 1.75f),
		new("pais", "País", 0.8f),
		new("region", "Estado / región", 1.15f),
		new("estado", "Situación", 1.1f),
		new("naturaleza", "Organización / clase", 1.25f),
		new("idioma", "Idioma", 0.7f),
		new("tipo", "Tipo", 0.8f),
		new("persona_contacto", "Persona de contacto", 1.2f),
		new("web_contacto", "Web", 1.45f),
		new("uso_del_correo", "Uso del correo", 1.25f),
		new("ultima_version_comunicada", "Última versión", 0.95f),
		new("fecha_ultimo_envio", "Último envío", 0.9f),
		new("notas", "Notas", 1.8f)
	};

	internal bool MarkedOnly => _marked.Checked;

	internal string ReportTitle => string.IsNullOrWhiteSpace(_title.Text) ? "Listado de contactos" : _title.Text.Trim();

	internal IReadOnlyList<PdfReportColumn> SelectedColumns => _columns.CheckedItems.Cast<PdfReportColumn>().ToList();

	internal PdfReportOptionsForm(int visibleCount, int markedCount)
	{
		Text = "Crear informe PDF";
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(520, 610);
		Size = new Size(560, 660);
		Font = new Font("Segoe UI", 10f);
		BackColor = Color.FromArgb(245, 247, 251);
		AutoScaleMode = AutoScaleMode.Dpi;

		TableLayoutPanel layout = new()
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 7,
			Padding = new Padding(18)
		};
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 105f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		Controls.Add(layout);

		layout.Controls.Add(new Label
		{
			Text = "El informe utiliza los filtros aplicados en la pantalla principal. Elige el alcance y las columnas que deseas incluir.",
			Dock = DockStyle.Fill,
			AutoSize = false
		}, 0, 0);

		GroupBox scope = new() { Text = "Contactos que se incluirán", Dock = DockStyle.Fill, Padding = new Padding(12) };
		FlowLayoutPanel scopeFlow = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
		_visible = new RadioButton { Text = $"Todos los visibles después de filtrar ({visibleCount})", AutoSize = true, Checked = true };
		_marked = new RadioButton { Text = $"Solamente los marcados ({markedCount})", AutoSize = true, Enabled = markedCount > 0 };
		scopeFlow.Controls.Add(_visible);
		scopeFlow.Controls.Add(_marked);
		scope.Controls.Add(scopeFlow);
		layout.Controls.Add(scope, 0, 1);

		layout.Controls.Add(new Label { Text = "Título del informe:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 2);
		_title = new TextBox { Text = "Listado de contactos", Dock = DockStyle.Fill, Margin = new Padding(0, 5, 0, 5) };
		layout.Controls.Add(_title, 0, 3);
		layout.Controls.Add(new Label { Text = "Columnas del PDF:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 4);

		_columns = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
		string[] defaults = { "nombre", "correo", "pais", "region", "estado", "naturaleza" };
		foreach (PdfReportColumn column in AvailableColumns)
		{
			_columns.Items.Add(column, defaults.Contains(column.Field, StringComparer.OrdinalIgnoreCase));
		}
		layout.Controls.Add(_columns, 0, 5);

		FlowLayoutPanel buttons = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0) };
		Button cancel = new() { Text = "Cancelar", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(105, 38) };
		Button create = new() { Text = "Crear PDF", AutoSize = true, MinimumSize = new Size(115, 38) };
		create.Click += (_, _) =>
		{
			if (_columns.CheckedItems.Count == 0)
			{
				MessageBox.Show(this, "Selecciona al menos una columna para el informe.", "Crear informe PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			if (_columns.CheckedItems.Count > 8)
			{
				MessageBox.Show(this, "Para que el PDF sea legible, selecciona un máximo de ocho columnas.", "Crear informe PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			DialogResult = DialogResult.OK;
			Close();
		};
		buttons.Controls.Add(cancel);
		buttons.Controls.Add(create);
		layout.Controls.Add(buttons, 0, 6);
		AcceptButton = create;
		CancelButton = cancel;
		UiText.Apply(this);
	}
}

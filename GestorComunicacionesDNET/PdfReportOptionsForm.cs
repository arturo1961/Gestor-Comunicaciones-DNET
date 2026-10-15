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

internal sealed class PdfReportOptionsForm : DnetForm
{
	private readonly RadioButton _visible;
	private readonly RadioButton _marked;
	private readonly TextBox _title;
	private readonly CheckedListBox _columns;

	internal static IReadOnlyList<PdfReportColumn> AvailableColumns => new List<PdfReportColumn>
	{
		new("nombre", UiText.Get("Nombre"), 1.55f),
		new("correo", UiText.Get("Correo"), 1.75f),
		new("pais", UiText.Get("País"), 0.8f),
		new("region", UiText.Get("Región"), 1.15f),
		new("estado", UiText.Get("Estado"), 1.1f),
		new("naturaleza", UiText.Get("Organización"), 1.25f),
		new("idioma", UiText.Get("Idioma"), 0.7f),
		new("tipo", UiText.Get("Tipo"), 0.8f),
		new("persona_contacto", UiText.Get("Persona de contacto:").TrimEnd(':', ' '), 1.2f),
		new("web_contacto", UiText.Get("Web de contacto:").TrimEnd(':', ' '), 1.45f),
		new("uso_del_correo", UiText.Get("Uso del correo:").TrimEnd(':', ' '), 1.25f),
		new("ultima_version_comunicada", UiText.Get("Última versión"), 0.95f),
		new("fecha_ultimo_envio", UiText.Get("Fecha último envío:").TrimEnd(':', ' '), 0.9f),
		new("notas", UiText.Get("Notas:").TrimEnd(':', ' '), 1.8f)
	};

	internal bool MarkedOnly => _marked.Checked;

	internal string ReportTitle => string.IsNullOrWhiteSpace(_title.Text) ? UiText.Get("Listado de contactos") : _title.Text.Trim();

	internal IReadOnlyList<PdfReportColumn> SelectedColumns => _columns.CheckedItems.Cast<PdfReportColumn>().ToList();

	internal PdfReportOptionsForm(int visibleCount, int markedCount)
	{
		Text = UiText.Get("Crear informe PDF");
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
			Text = UiText.Get("El informe utiliza los filtros aplicados en la pantalla principal. Elige el alcance y las columnas que deseas incluir."),
			Dock = DockStyle.Fill,
			AutoSize = false
		}, 0, 0);

		GroupBox scope = new() { Text = UiText.Get("Contactos que se incluirán"), Dock = DockStyle.Fill, Padding = new Padding(12) };
		FlowLayoutPanel scopeFlow = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
		_visible = new RadioButton { Text = UiText.Format("Todos los visibles después de filtrar ({0})", visibleCount), AutoSize = true, Checked = true };
		_marked = new RadioButton { Text = UiText.Format("Solamente los marcados ({0})", markedCount), AutoSize = true, Enabled = markedCount > 0 };
		scopeFlow.Controls.Add(_visible);
		scopeFlow.Controls.Add(_marked);
		scope.Controls.Add(scopeFlow);
		layout.Controls.Add(scope, 0, 1);

		layout.Controls.Add(new Label { Text = UiText.Get("Título del informe:"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 2);
		_title = new TextBox { Text = UiText.Get("Listado de contactos"), Dock = DockStyle.Fill, Margin = new Padding(0, 5, 0, 5) };
		layout.Controls.Add(_title, 0, 3);
		layout.Controls.Add(new Label { Text = UiText.Get("Columnas del PDF:"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft }, 0, 4);

		_columns = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
		string[] defaults = { "nombre", "correo", "pais", "region", "estado", "naturaleza" };
		foreach (PdfReportColumn column in AvailableColumns)
		{
			_columns.Items.Add(column, defaults.Contains(column.Field, StringComparer.OrdinalIgnoreCase));
		}
		layout.Controls.Add(_columns, 0, 5);

		FlowLayoutPanel buttons = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 9, 0, 0) };
		Button cancel = new() { Text = UiText.Get("Cancelar"), DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(105, 38) };
		Button create = new() { Text = UiText.Get("Crear PDF"), AutoSize = true, MinimumSize = new Size(115, 38) };
		create.Click += (_, _) =>
		{
			if (_columns.CheckedItems.Count == 0)
			{
				MessageBox.Show(this, UiText.Get("Selecciona al menos una columna para el informe."), UiText.Get("Crear informe PDF"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			if (_columns.CheckedItems.Count > 8)
			{
				MessageBox.Show(this, UiText.Get("Para que el PDF sea legible, selecciona un máximo de ocho columnas."), UiText.Get("Crear informe PDF"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
	}
}

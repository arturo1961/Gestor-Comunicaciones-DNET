using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class RecycleBinForm : DnetForm
{
	private sealed class DeletedRow
	{
		public required JsonObject Data { get; init; }
		public string Nombre => ContactStore.Get(Data, "nombre");
		public string Correo => ContactStore.Get(Data, "correo");
		public string Pais => ContactStore.Get(Data, "pais");
		public string Fecha => ContactStore.Get(Data, "fecha_eliminacion_del_gestor");
	}

	private readonly ContactStore _store;
	private readonly DataGridView _grid = new DataGridView();
	private readonly Label _status = new Label();
	public bool DataChanged { get; private set; }

	public RecycleBinForm(ContactStore store)
	{
		_store = store;
		Text = "Papelera de contactos";
		StartPosition = FormStartPosition.CenterParent;
		Size = new Size(950, 570);
		MinimumSize = new Size(720, 420);
		Font = new Font("Segoe UI", 10f);

		TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));
		Controls.Add(layout);
		_status.Dock = DockStyle.Fill;
		_status.TextAlign = ContentAlignment.MiddleLeft;
		layout.Controls.Add(_status, 0, 0);

		_grid.Dock = DockStyle.Fill;
		_grid.AutoGenerateColumns = false;
		_grid.ReadOnly = true;
		_grid.AllowUserToAddRows = false;
		_grid.AllowUserToDeleteRows = false;
		_grid.MultiSelect = true;
		_grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_grid.RowHeadersVisible = false;
		_grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Nombre", DataPropertyName = "Nombre", Width = 245 });
		_grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Correo", DataPropertyName = "Correo", Width = 260 });
		_grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "País", DataPropertyName = "Pais", Width = 110 });
		_grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Fecha de eliminación", DataPropertyName = "Fecha", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
		layout.Controls.Add(_grid, 0, 1);

		FlowLayoutPanel buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
		Button close = NewButton("Cerrar", (_, _) => Close(), 105);
		Button delete = NewButton("Eliminar definitivamente", DeletePermanently, 190);
		Button restore = NewButton("Recuperar seleccionados", RestoreSelected, 195);
		buttons.Controls.Add(close);
		buttons.Controls.Add(delete);
		buttons.Controls.Add(restore);
		layout.Controls.Add(buttons, 0, 2);
		Reload();
		UiText.Apply(this);
	}

	private static Button NewButton(string text, EventHandler click, int width)
	{
		Button button = new Button { Text = text, Width = width, Height = 38 };
		button.Click += click;
		return button;
	}

	private List<JsonObject> SelectedContacts()
	{
		return _grid.SelectedRows.Cast<DataGridViewRow>()
			.Select(row => row.DataBoundItem as DeletedRow)
			.Where(row => row != null)
			.Select(row => row!.Data)
			.Distinct()
			.ToList();
	}

	private void Reload()
	{
		List<DeletedRow> rows = _store.DeletedContacts
			.OrderByDescending(contact => ContactStore.Get(contact, "fecha_eliminacion_del_gestor"), StringComparer.OrdinalIgnoreCase)
			.Select(contact => new DeletedRow { Data = contact })
			.ToList();
		_grid.DataSource = new BindingList<DeletedRow>(rows);
		_status.Text = rows.Count == 0
			? UiText.Get("La papelera está vacía.")
			: UiText.Format("Hay {0} contactos eliminados. Puedes seleccionar varios manteniendo pulsada la tecla Ctrl.", rows.Count);
	}

	private void RestoreSelected(object? sender, EventArgs e)
	{
		List<JsonObject> selected = SelectedContacts();
		if (selected.Count == 0)
		{
			MessageBox.Show(this, UiText.Get("Selecciona uno o varios contactos para recuperarlos."), UiText.Get("Papelera"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		var result = _store.RestoreDeletedContacts(selected);
		DataChanged |= result.restored > 0;
		Reload();
		MessageBox.Show(this, UiText.Format("Recuperados: {0}\nNo recuperados por coincidencia de ID o correo: {1}", result.restored, result.skipped), UiText.Get("Resultado"));
	}

	private void DeletePermanently(object? sender, EventArgs e)
	{
		List<JsonObject> selected = SelectedContacts();
		if (selected.Count == 0)
		{
			MessageBox.Show(this, UiText.Get("Selecciona uno o varios contactos."), UiText.Get("Papelera"), MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		if (MessageBox.Show(this, UiText.Format("¿Eliminar definitivamente {0} contactos de la papelera?\n\nEsta operación no se puede deshacer salvo que tengas una copia de seguridad.", selected.Count), UiText.Get("Confirmar eliminación definitiva"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		int removed = _store.PermanentlyDeleteArchivedContacts(selected);
		DataChanged |= removed > 0;
		Reload();
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class HistoryForm : Form
{
	private sealed record HistoryRow(string Fecha, string Nombre, string Correo, string Version, string Estado, string Medio, string Observaciones);

	public HistoryForm(ContactStore store)
	{
		Text = "Historial de comunicaciones";
		StartPosition = FormStartPosition.CenterParent;
		Size = new Size(1180, 620);
		MinimumSize = new Size(800, 440);
		Font = new Font("Segoe UI", 10f);

		Dictionary<string, JsonObject> contacts = store.Contacts
			.Concat(store.DeletedContacts)
			.Where(contact => ContactStore.Get(contact, "id").Length > 0)
			.GroupBy(contact => ContactStore.Get(contact, "id"), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
		List<HistoryRow> rows = store.HistoryEntries
			.Select(entry => CreateRow(entry, contacts))
			.OrderByDescending(row => row.Fecha, StringComparer.OrdinalIgnoreCase)
			.ToList();

		TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		Controls.Add(layout);
		layout.Controls.Add(new Label { Text = rows.Count == 0 ? "Todavía no hay comunicaciones registradas." : $"Comunicaciones registradas: {rows.Count}", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);

		DataGridView grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
		AddColumn(grid, "Fecha", "Fecha", 165);
		AddColumn(grid, "Contacto", "Nombre", 210);
		AddColumn(grid, "Correo", "Correo", 220);
		AddColumn(grid, "Versión", "Version", 105);
		AddColumn(grid, "Estado", "Estado", 195);
		AddColumn(grid, "Medio", "Medio", 125);
		AddColumn(grid, "Observaciones", "Observaciones", 260, true);
		grid.DataSource = new BindingList<HistoryRow>(rows);
		layout.Controls.Add(grid, 0, 1);

		FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
		Button close = new Button { Text = "Cerrar", Width = 110, Height = 36 };
		close.Click += (_, _) => Close();
		footer.Controls.Add(close);
		layout.Controls.Add(footer, 0, 2);
	}

	private static HistoryRow CreateRow(JsonObject entry, IReadOnlyDictionary<string, JsonObject> contacts)
	{
		contacts.TryGetValue(ContactStore.Get(entry, "contacto_id"), out JsonObject? contact);
		return new HistoryRow(
			ContactStore.Get(entry, "fecha"),
			contact == null ? "Contacto no disponible" : ContactStore.Get(contact, "nombre"),
			contact == null ? "" : ContactStore.Get(contact, "correo"),
			ContactStore.Get(entry, "version"),
			ContactStore.Get(entry, "estado"),
			ContactStore.Get(entry, "medio"),
			ContactStore.Get(entry, "observaciones"));
	}

	private static void AddColumn(DataGridView grid, string header, string property, int width, bool fill = false)
	{
		grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, Width = width, AutoSizeMode = fill ? DataGridViewAutoSizeColumnMode.Fill : DataGridViewAutoSizeColumnMode.None });
	}
}

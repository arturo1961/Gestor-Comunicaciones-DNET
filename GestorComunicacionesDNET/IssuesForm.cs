using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class IssuesForm : DnetForm
{
	public IssuesForm(ContactStore store)
	{
		var issues = store.FindContactIssues();
		Text = "Revisión de contactos";
		StartPosition = FormStartPosition.CenterParent;
		Size = new Size(980, 560);
		MinimumSize = new Size(720, 410);
		Font = new Font("Segoe UI", 10f);

		TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));
		Controls.Add(layout);
		layout.Controls.Add(new Label
		{
			Text = issues.Count == 0 ? UiText.Get("No se han encontrado correos incorrectos ni posibles duplicados.") : UiText.Format("Se han encontrado {0} avisos. Revísalos y modifica el contacto correspondiente desde la ventana principal.", issues.Count),
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft
		}, 0, 0);

		DataGridView grid = new DataGridView { Dock = DockStyle.Fill, AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
		grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Aviso", DataPropertyName = "Type", Width = 215 });
		grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Contacto(s)", DataPropertyName = "Contacts", Width = 300 });
		grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Detalle", DataPropertyName = "Detail", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
		grid.DataSource = new BindingList<ContactIssue>(new System.Collections.Generic.List<ContactIssue>(issues));
		layout.Controls.Add(grid, 0, 1);

		FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 7, 0, 0) };
		Button close = new Button { Text = "Cerrar", Width = 110, Height = 36 };
		close.Click += (_, _) => Close();
		footer.Controls.Add(close);
		layout.Controls.Add(footer, 0, 2);
		UiText.Apply(this);
	}
}

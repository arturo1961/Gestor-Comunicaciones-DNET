using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class MainForm : Form
{
	private readonly ContactStore _store;

	private BindingList<ContactRow> _visibleRows = new BindingList<ContactRow>();

	private readonly DataGridView _grid = new DataGridView();

	private readonly ComboBox _country = Combo();

	private readonly ComboBox _region = Combo();

	private readonly ComboBox _lang = Combo();

	private readonly ComboBox _type = Combo();

	private readonly ComboBox _classFilter = Combo();

	private readonly ComboBox _statusFilter = Combo();

	private readonly ComboBox _classifyAs = Combo();

	private readonly TextBox _version = new TextBox();

	private readonly Label _status = new Label();

	private readonly CheckBox _skipInformed = new CheckBox
	{
		Text = "Omitir quienes ya recibieron la versión",
		AutoSize = true,
		Checked = true
	};

	private List<JsonObject> _lastPrepared = new List<JsonObject>();

	private string _lastPreparedVersion = "";

	private static ComboBox Combo()
	{
		return new ComboBox
		{
			DropDownStyle = ComboBoxStyle.DropDownList,
			Width = 166,
			Margin = new Padding(5, 4, 12, 4)
		};
	}

	private static Button Btn(string title, EventHandler callback, int width = 150)
	{
		Button button = new Button();
		button.Text = title;
		button.AutoSize = true;
		button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		button.MinimumSize = new Size(width, 40);
		button.Margin = new Padding(4, 3, 8, 3);
		button.Padding = new Padding(8, 2, 8, 2);
		button.FlatStyle = FlatStyle.System;
		button.Click += callback;
		return button;
	}

	public MainForm()
	{
		Text = "Gestor de Comunicaciones DNET — Uso personal";
		base.StartPosition = FormStartPosition.CenterScreen;
		MinimumSize = new Size(1080, 780);
		base.Size = new Size(1450, 900);
		base.AutoScaleMode = AutoScaleMode.Dpi;
		Font = new Font("Segoe UI", 10f);
		BackColor = Color.FromArgb(245, 247, 251);
		try
		{
			_store = new ContactStore();
		}
		catch (Exception ex)
		{
			MessageBox.Show("No se pudieron leer los archivos JSON.\n" + ex.Message, "Error de inicio", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			throw;
		}
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 4,
			Padding = new Padding(13)
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 218f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42f));
		MenuStrip menuStrip = new MenuStrip
		{
			Dock = DockStyle.Top
		};
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Copias de seguridad");
		toolStripMenuItem.DropDownItems.Add("Hacer copia de seguridad...", null, CreateBackup);
		toolStripMenuItem.DropDownItems.Add("Recuperar copia de seguridad...", null, RestoreBackup);
		menuStrip.Items.Add(toolStripMenuItem);
		base.MainMenuStrip = menuStrip;
		base.Controls.Add(tableLayoutPanel);
		base.Controls.Add(menuStrip);
		Label control = new Label
		{
			Text = "Gestor de Comunicaciones DNET   ·   Contactos y preparación de correos",
			Font = new Font("Segoe UI", 17f, FontStyle.Bold),
			Dock = DockStyle.Fill,
			ForeColor = Color.FromArgb(25, 40, 65)
		};
		tableLayoutPanel.Controls.Add(control, 0, 0);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			RowCount = 2,
			ColumnCount = 1
		};
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 96f));
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 1);
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoScroll = false,
			WrapContents = true,
			Padding = new Padding(0, 2, 0, 2)
		};
		tableLayoutPanel2.Controls.Add(flowLayoutPanel, 0, 0);
		AddLabeled(flowLayoutPanel, "País", _country);
		AddLabeled(flowLayoutPanel, "Región", _region);
		AddLabeled(flowLayoutPanel, "Idioma", _lang);
		AddLabeled(flowLayoutPanel, "Tipo", _type);
		AddLabeled(flowLayoutPanel, "Organización", _classFilter);
		AddLabeled(flowLayoutPanel, "Estado", _statusFilter);
		ComboBox[] array = new ComboBox[6] { _country, _region, _lang, _type, _classFilter, _statusFilter };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SelectedIndexChanged += delegate
			{
				RefreshList();
			};
		}
		FlowLayoutPanel flowLayoutPanel2 = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			WrapContents = true,
			Padding = new Padding(0, 2, 0, 2)
		};
		tableLayoutPanel2.Controls.Add(flowLayoutPanel2, 0, 1);
		flowLayoutPanel2.Controls.Add(Btn("Marcar visibles", delegate
		{
			MarkVisible(chosen: true);
		}, 133));
		flowLayoutPanel2.Controls.Add(Btn("Desmarcar todo", delegate
		{
			MarkVisible(chosen: false);
		}, 133));
		flowLayoutPanel2.Controls.Add(Btn("Nuevo contacto", AddContact, 136));
		flowLayoutPanel2.Controls.Add(Btn("Modificar contacto", EditContact, 160));
		flowLayoutPanel2.Controls.Add(Btn("Borrar contacto", DeleteContact, 145));
		flowLayoutPanel2.Controls.Add(Btn("Importar JSON", ImportJson, 130));
		flowLayoutPanel2.Controls.Add(Btn("Exportar JSON", ExportJson, 132));
		_classifyAs.Items.AddRange(new object[5] { "empresa_medio", "organismo_publico", "red_publica", "entidad_civil", "sin_clasificar" });
		_classifyAs.SelectedIndex = 0;
		_classifyAs.Width = 165;
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "  Clasificar seleccionados:",
			AutoSize = true,
			Margin = new Padding(6, 9, 4, 0)
		});
		flowLayoutPanel2.Controls.Add(_classifyAs);
		flowLayoutPanel2.Controls.Add(Btn("Aplicar clase", ApplyClassification, 114));
		flowLayoutPanel2.Controls.Add(Btn("Abrir carpeta JSON", OpenDataFolder, 169));
		SplitContainer split = new SplitContainer
		{
			Dock = DockStyle.Fill,
			Orientation = Orientation.Horizontal,
			SplitterWidth = 8
		};
		tableLayoutPanel.Controls.Add(split, 0, 2);
		GroupBox groupBox = new GroupBox
		{
			Text = "Contactos (marca sólo los destinatarios que quieras incluir en CCO)",
			Dock = DockStyle.Fill,
			Padding = new Padding(8)
		};
		split.Panel1.Controls.Add(groupBox);
		SetupGrid();
		groupBox.Controls.Add(_grid);
		GroupBox groupBox2 = new GroupBox
		{
			Text = "Preparar correo — se abrirán únicamente los contactos seleccionados en CCO",
			Dock = DockStyle.Fill,
			Padding = new Padding(8)
		};
		split.Panel2.Controls.Add(groupBox2);
		TableLayoutPanel tableLayoutPanel3 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 3
		};
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245f));
		tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Absolute, 90f));
		groupBox2.Controls.Add(tableLayoutPanel3);
		tableLayoutPanel3.Controls.Add(new Label
		{
			Text = "Versión que deseas registrar (opcional):",
			TextAlign = ContentAlignment.MiddleLeft,
			Dock = DockStyle.Fill
		}, 0, 0);
		_version.Dock = DockStyle.Fill;
		_version.Margin = new Padding(3, 6, 3, 6);
		tableLayoutPanel3.Controls.Add(_version, 1, 0);
		Label control2 = new Label
		{
			Text = "El gestor abrirá un único correo nuevo y colocará todos los contactos seleccionados en CCO. El asunto, el mensaje, los adjuntos y el envío los completarás personalmente en tu aplicación de correo.",
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Padding = new Padding(8)
		};
		tableLayoutPanel3.Controls.Add(control2, 0, 1);
		tableLayoutPanel3.SetColumnSpan(control2, 2);
		FlowLayoutPanel flowLayoutPanel3 = new FlowLayoutPanel
		{
			Dock = DockStyle.Fill,
			WrapContents = true,
			AutoScroll = true,
			Padding = new Padding(0, 3, 0, 3)
		};
		tableLayoutPanel3.Controls.Add(flowLayoutPanel3, 0, 2);
		tableLayoutPanel3.SetColumnSpan(flowLayoutPanel3, 2);
		flowLayoutPanel3.Controls.Add(_skipInformed);
		_skipInformed.Margin = new Padding(4, 10, 13, 0);
		flowLayoutPanel3.Controls.Add(Btn("Copiar CCO", CopyBcc, 137));
		flowLayoutPanel3.Controls.Add(Btn("Preparar con mailto", PrepareMail, 180));
		flowLayoutPanel3.Controls.Add(Btn("Confirmar envío REAL", ConfirmSent, 215));
		flowLayoutPanel3.Controls.Add(Btn("Ayuda", Help, 88));
		_status.Dock = DockStyle.Fill;
		_status.TextAlign = ContentAlignment.MiddleLeft;
		_status.ForeColor = Color.FromArgb(70, 82, 105);
		tableLayoutPanel.Controls.Add(_status, 0, 3);
		PopulateFilters();
		RefreshList();
		base.Shown += delegate
		{
			split.Panel1MinSize = 190;
			split.Panel2MinSize = 230;
			int max = Math.Max(split.Panel1MinSize, split.Height - split.SplitterWidth - split.Panel2MinSize);
			split.SplitterDistance = Math.Clamp((int)((double)split.Height * 0.58), split.Panel1MinSize, max);
		};
	}

	private static void AddLabeled(FlowLayoutPanel parent, string name, ComboBox combo)
	{
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			AutoSize = true,
			AutoSizeMode = AutoSizeMode.GrowAndShrink,
			WrapContents = false,
			Margin = new Padding(0, 0, 5, 2)
		};
		flowLayoutPanel.Controls.Add(new Label
		{
			Text = name + ":",
			AutoSize = true,
			Margin = new Padding(4, 11, 1, 0)
		});
		flowLayoutPanel.Controls.Add(combo);
		parent.Controls.Add(flowLayoutPanel);
	}

	private void SetupGrid()
	{
		_grid.Dock = DockStyle.Fill;
		_grid.AutoGenerateColumns = false;
		_grid.AllowUserToAddRows = false;
		_grid.AllowUserToDeleteRows = false;
		_grid.MultiSelect = false;
		_grid.RowHeadersVisible = false;
		_grid.RowTemplate.Height = 30;
		_grid.ColumnHeadersHeight = 33;
		_grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		_grid.BackgroundColor = Color.White;
		_grid.GridColor = Color.FromArgb(225, 228, 235);
		_grid.EditMode = DataGridViewEditMode.EditOnEnter;
		_grid.Columns.Add(new DataGridViewCheckBoxColumn
		{
			DataPropertyName = "Elegido",
			HeaderText = "✓",
			Width = 43
		});
		AddGridCol("Nombre", "Nombre", 210);
		AddGridCol("Correo", "Correo", 215);
		AddGridCol("País", "Pais", 92);
		AddGridCol("Región", "Region", 155);
		AddGridCol("Idioma", "Idioma", 67);
		AddGridCol("Organización", "Clase", 150);
		AddGridCol("Tipo", "Tipo", 102);
		AddGridCol("Última versión", "UltimaVersion", 113);
		AddGridCol("Estado", "Estado", 210);
		_grid.CurrentCellDirtyStateChanged += delegate
		{
			if (_grid.IsCurrentCellDirty)
			{
				_grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
		};
		_grid.CellValueChanged += delegate(object? _, DataGridViewCellEventArgs e)
		{
			if (e.ColumnIndex == 0)
			{
				_lastPrepared.Clear();
				UpdateStatus();
			}
		};
	}

	private void AddGridCol(string header, string field, int width)
	{
		_grid.Columns.Add(new DataGridViewTextBoxColumn
		{
			HeaderText = header,
			DataPropertyName = field,
			Width = width,
			ReadOnly = true
		});
	}

	private void PopulateFilters()
	{
		Fill(_country, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "pais")));
		Fill(_region, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "region")));
		Fill(_lang, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "idioma")));
		Fill(_type, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "tipo")));
		Fill(_classFilter, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "naturaleza")));
		Fill(_statusFilter, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "estado")));
		static void Fill(ComboBox combo, IEnumerable<string> values)
		{
			string text = combo.SelectedItem?.ToString();
			combo.Items.Clear();
			combo.Items.Add("Todos");
			foreach (string item in from v in values.Where((string v) => !string.IsNullOrWhiteSpace(v)).Distinct<string>(StringComparer.OrdinalIgnoreCase)
				orderby v
				select v)
			{
				combo.Items.Add(item);
			}
			combo.SelectedItem = ((text != null && combo.Items.Contains(text)) ? text : "Todos");
		}
	}

	private void RefreshList()
	{
		if (_country.SelectedItem != null)
		{
			IOrderedEnumerable<JsonObject> source = _store.Contacts.Where((JsonObject c) => Keep(c, _country, "pais") && Keep(c, _region, "region") && Keep(c, _lang, "idioma") && Keep(c, _type, "tipo") && Keep(c, _classFilter, "naturaleza") && Keep(c, _statusFilter, "estado")).OrderBy<JsonObject, string>((JsonObject c) => ContactStore.Get(c, "nombre"), StringComparer.CurrentCultureIgnoreCase);
			_visibleRows = new BindingList<ContactRow>(source.Select((JsonObject c) => new ContactRow
			{
				Data = c
			}).ToList());
			_grid.DataSource = _visibleRows;
			_lastPrepared.Clear();
			UpdateStatus();
		}
		static bool Keep(JsonObject c, ComboBox f, string name)
		{
			string text = f.SelectedItem?.ToString();
			if (text != null && !(text == "Todos"))
			{
				return string.Equals(ContactStore.Get(c, name), text, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	private void MarkVisible(bool chosen)
	{
		foreach (ContactRow visibleRow in _visibleRows)
		{
			visibleRow.Elegido = chosen && Eligible(visibleRow.Data);
		}
		_grid.Refresh();
		_lastPrepared.Clear();
		UpdateStatus();
	}

	private static bool Eligible(JsonObject c)
	{
		if (ContactStore.Get(c, "canal") == "email" && !string.IsNullOrWhiteSpace(ContactStore.Get(c, "correo")) && !ContactStore.Flag(c, "no_contactar"))
		{
			return ContactStore.Get(c, "estado") != "no_contactar_por_ahora";
		}
		return false;
	}

	private List<ContactRow> SelectedRows()
	{
		return _visibleRows.Where((ContactRow c) => c.Elegido).ToList();
	}

	private void UpdateStatus()
	{
		List<ContactRow> list = SelectedRows();
		_status.Text = $"Contactos en base: {_store.Contacts.Count}    ·    Visibles: {_visibleRows.Count}    ·    Marcados: {list.Count}    ·    Dirección CCO oculta para otros destinatarios.";
	}

	private void ApplyClassification(object? sender, EventArgs e)
	{
		List<ContactRow> list = SelectedRows();
		if (list.Count == 0)
		{
			Warn("Primero marca una o varias filas.");
			return;
		}
		string text = _classifyAs.SelectedItem?.ToString() ?? "sin_clasificar";
		foreach (ContactRow item in list)
		{
			item.Data["naturaleza"] = text;
		}
		_store.SaveContacts();
		PopulateFilters();
		RefreshList();
		MessageBox.Show($"Clasificación '{text}' aplicada a {list.Count} contactos y guardada. Vuelve a marcarlos antes de preparar correo.", "Guardado");
	}

	private void ImportJson(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Importar contactos JSON",
			Filter = "JSON (*.json)|*.json"
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			var (value, value2) = _store.ImportContacts(openFileDialog.FileName);
			PopulateFilters();
			RefreshList();
			MessageBox.Show($"Importados: {value}\nOmitidos por ID/correo repetido o sin nombre: {value2}\nLos contactos existentes NO se han sobrescrito.", "Importación");
		}
		catch (Exception ex)
		{
			Warn("No se pudo importar el archivo:\n" + ex.Message);
		}
	}

	private void AddContact(object? sender, EventArgs e)
	{
		using ContactEditorForm contactEditorForm = new ContactEditorForm();
		if (contactEditorForm.ShowDialog(this) != DialogResult.OK || contactEditorForm.EditedContact == null)
		{
			return;
		}
		try
		{
			_store.AddContact(contactEditorForm.EditedContact);
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, "Contacto añadido y guardado en contactos.json.", "Guardado");
		}
		catch (Exception ex)
		{
			Warn("No se pudo añadir el contacto:\n" + ex.Message);
		}
	}

	private ContactRow? CurrentContact()
	{
		return _grid.CurrentRow?.DataBoundItem as ContactRow;
	}

	private void EditContact(object? sender, EventArgs e)
	{
		ContactRow contactRow = CurrentContact();
		if (contactRow == null)
		{
			Warn("Selecciona una fila para modificarla (no hace falta marcar su casilla). ");
			return;
		}
		using ContactEditorForm contactEditorForm = new ContactEditorForm(contactRow.Data);
		if (contactEditorForm.ShowDialog(this) != DialogResult.OK || contactEditorForm.EditedContact == null)
		{
			return;
		}
		try
		{
			_store.UpdateContact(contactRow.Data, contactEditorForm.EditedContact);
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, "Contacto modificado y guardado. Se conserva el historial anterior.", "Guardado");
		}
		catch (Exception ex)
		{
			Warn("No se pudo modificar el contacto:\n" + ex.Message);
		}
	}

	private void DeleteContact(object? sender, EventArgs e)
	{
		List<ContactRow> list = SelectedRows();
		if (list.Count == 0)
		{
			ContactRow contactRow = CurrentContact();
			if (contactRow == null)
			{
				Warn("Marca uno o varios contactos, o selecciona una fila, para borrarlos.");
				return;
			}
			list.Add(contactRow);
		}
		string text = ((list.Count == 1) ? ("'" + ContactStore.Get(list[0].Data, "nombre") + "'") : $"los {list.Count} contactos marcados");
		if (MessageBox.Show(this, "¿Eliminar de la lista activa a " + text + "?\n\nNo aparecerán en futuras campañas. Se guardará una copia de cada registro en contactos_eliminados y el historial de envíos seguirá intacto.", (list.Count == 1) ? "Confirmar eliminación" : "Confirmar eliminación múltiple", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			_store.DeleteContacts(list.Select((ContactRow row) => row.Data));
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, $"Eliminados {list.Count} contactos de la lista activa y conservados en el archivo histórico.", "Eliminación completada");
		}
		catch (Exception ex)
		{
			Warn("No se pudieron eliminar los contactos:\n" + ex.Message);
		}
	}

	private void ExportJson(object? sender, EventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Exportar todos los contactos (para conservar o compartir con ChatGPT)",
			Filter = "JSON (*.json)|*.json",
			FileName = "contactos_DNET_exportados.json",
			OverwritePrompt = true
		};
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			_store.ExportContacts(saveFileDialog.FileName);
			MessageBox.Show(this, "Contactos exportados. Puedes compartir este JSON aquí para que preparemos nuevos registros.", "Exportación");
		}
		catch (Exception ex)
		{
			Warn("No se pudo exportar el JSON:\n" + ex.Message);
		}
	}

	private void CreateBackup(object? sender, EventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = "Hacer copia de seguridad completa",
			Filter = "Copia de seguridad ZIP (*.zip)|*.zip",
			FileName = "Gestor_Comunicaciones_DNET_copia_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip",
			InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			OverwritePrompt = true
		};
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			_store.CreateBackup(saveFileDialog.FileName);
			MessageBox.Show(this, "La copia de seguridad se ha creado correctamente.\n\nIncluye todos los contactos, los contactos eliminados y el historial de envíos.\n\n" + saveFileDialog.FileName, "Copia completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			Warn("No se pudo crear la copia de seguridad:\n" + ex.Message);
		}
	}

	private void RestoreBackup(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = "Recuperar copia de seguridad completa",
			Filter = "Copia de seguridad ZIP (*.zip)|*.zip"
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		if (MessageBox.Show(this, "La copia seleccionada sustituirá los contactos y el historial actuales.\n\nAntes de recuperar, el programa guardará automáticamente una copia de seguridad del estado actual.\n\n¿Deseas continuar?", "Confirmar recuperación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			var result = _store.RestoreBackup(openFileDialog.FileName);
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, $"Copia recuperada correctamente.\n\nContactos activos: {result.contacts}\nEnvíos en el historial: {result.shipments}\n\nCopia automática del estado anterior:\n{result.safetyBackupPath}", "Recuperación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			Warn("No se pudo recuperar la copia. Los datos actuales se han conservado.\n\n" + ex.Message);
		}
	}

	private List<ContactRow> ValidSelection(out string message)
	{
		List<ContactRow> list = SelectedRows();
		List<ContactRow> list2 = list.Where((ContactRow c) => !Eligible(c.Data)).ToList();
		if (list2.Count > 0)
		{
			message = $"Hay {list2.Count} contactos sin correo autorizado o marcados como no contactar. Desmárcalos.";
			return new List<ContactRow>();
		}
		if (_skipInformed.Checked)
		{
			list = list.Where((ContactRow c) => !_store.AlreadyInformed(c.Data, _version.Text.Trim())).ToList();
		}
		if ((from g in list.GroupBy<ContactRow, string>((ContactRow c) => c.Correo.Trim(), StringComparer.OrdinalIgnoreCase)
			where g.Count() > 1
			select g).ToList().Count > 0)
		{
			message = "Hay direcciones repetidas entre las seleccionadas. Corrige la lista antes de continuar.";
			return new List<ContactRow>();
		}
		message = ((list.Count == 0) ? "No hay destinatarios válidos para la selección y versión actual." : "");
		return list;
	}

	private void CopyBcc(object? sender, EventArgs e)
	{
		string message;
		List<ContactRow> list = ValidSelection(out message);
		if (message.Length != 0)
		{
			Warn(message);
			return;
		}
		Clipboard.SetText(string.Join(", ", list.Select((ContactRow c) => c.Correo)));
		MessageBox.Show($"{list.Count} correos copiados. Pégalos en CCO, NO en CC.", "CCO copiado");
	}

	private void PrepareMail(object? sender, EventArgs e)
	{
		string message;
		List<ContactRow> list = ValidSelection(out message);
		if (message.Length > 0)
		{
			Warn(message);
			return;
		}
		string stringToEscape = string.Join(",", list.Select((ContactRow c) => c.Correo.Trim()));
		string fileName = "mailto:?bcc=" + Uri.EscapeDataString(stringToEscape);
		if (MessageBox.Show($"Preparar correo para {list.Count} destinatarios en CCO.\n\n" + "Se abrirá la aplicación de correo predeterminada, NO se enviará nada automáticamente.\nEl asunto, el mensaje y los adjuntos quedarán en blanco para que los completes personalmente.\nRevisa especialmente el campo CCO antes de enviar.\n\n¿Continuar?", "Revisar destinatarios", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			Process.Start(new ProcessStartInfo(fileName)
			{
				UseShellExecute = true
			});
			_lastPrepared = list.Select((ContactRow c) => c.Data).ToList();
			_lastPreparedVersion = _version.Text.Trim();
		}
		catch (Exception ex)
		{
			_lastPrepared.Clear();
			Warn("No se pudo abrir el cliente de correo. Puede que mailto no esté configurado. Usa 'Copiar CCO' y pega las direcciones en Gmail manualmente.\n\n" + ex.Message);
		}
	}

	private void ConfirmSent(object? sender, EventArgs e)
	{
		if (_lastPrepared.Count == 0)
		{
			Warn("Primero prepara el correo con 'Preparar con mailto'. Esto NO confirma un envío.");
			return;
		}
		string value = ((_lastPreparedVersion.Length == 0) ? "sin indicar versión" : ("de la versión " + _lastPreparedVersion));
		if (MessageBox.Show($"¿CONFIRMAS que tú has pulsado ENVIAR en tu cliente de correo para los {_lastPrepared.Count} destinatarios ({value})?\n\nSólo entonces anotaremos 'enviado confirmado por usuario'.", "Confirmar envío real", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			_store.ConfirmSend(_lastPrepared, _lastPreparedVersion);
			MessageBox.Show($"Registrados {_lastPrepared.Count} envíos declarados por ti. No equivale a confirmar entrega.", "Registro actualizado");
			_lastPrepared.Clear();
			RefreshList();
		}
		catch (Exception ex)
		{
			Warn("No se pudo guardar el historial:\n" + ex.Message);
		}
	}

	private void OpenDataFolder(object? sender, EventArgs e)
	{
		try
		{
			Process.Start(new ProcessStartInfo("explorer.exe", "\"" + _store.DataDirectory + "\"")
			{
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			Warn(ex.Message);
		}
	}

	private void Help(object? sender, EventArgs e)
	{
		MessageBox.Show("1) Añade, modifica o borra contactos; también puedes importar y exportar JSON.\n2) Filtra y marca los destinatarios que quieras incluir.\n3) La versión es opcional y sólo sirve para el historial interno.\n4) 'Preparar con mailto' abre un único correo con los seleccionados en CCO.\n5) El asunto, el mensaje y los adjuntos los completas personalmente en tu aplicación de correo.\n6) Después de enviarlo, vuelve aquí y confirma el envío real para registrarlo.\n7) En el menú 'Copias de seguridad' puedes guardar o recuperar juntos los contactos y el historial.\n\nIMPORTANTE: el gestor NO envía correos, no guarda contraseñas y no incluye asunto ni mensaje en el enlace mailto. Los JSON privados se guardan en AppData local.", "Cómo utilizar el gestor");
	}

	private void Warn(string message)
	{
		MessageBox.Show(message, "Gestor DNET", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}
}

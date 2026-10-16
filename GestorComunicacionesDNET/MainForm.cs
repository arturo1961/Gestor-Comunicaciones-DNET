using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class MainForm : DnetForm
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

	private readonly TextBox _search = new TextBox
	{
		Width = 245,
		PlaceholderText = "Nombre, correo, país, notas...",
		Margin = new Padding(5, 5, 12, 4)
	};

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

	private bool _updatingFilters;

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
		: this(null)
	{
	}

	internal MainForm(ContactStore? store)
	{
		Text = "Gestor de Comunicaciones DNET — Uso personal";
		base.StartPosition = FormStartPosition.CenterScreen;
		MinimumSize = new Size(1120, 900);
		base.Size = new Size(1520, 940);
		base.AutoScaleMode = AutoScaleMode.Dpi;
		Font = new Font("Segoe UI", 10f);
		BackColor = Color.FromArgb(245, 247, 251);
		try
		{
			_store = store ?? new ContactStore();
		}
		catch (Exception ex)
		{
			MessageBox.Show(UiText.Get("No se pudieron leer los archivos JSON.\n") + ex.Message, UiText.Get("Error de inicio"), MessageBoxButtons.OK, MessageBoxIcon.Hand);
			throw;
		}
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 1,
			RowCount = 3,
			Padding = new Padding(13)
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 260f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		MenuStrip menuStrip = new MenuStrip
		{
			Dock = DockStyle.Top
		};
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Copias de seguridad");
		toolStripMenuItem.DropDownItems.Add("Hacer copia de seguridad...", null, CreateBackup);
		toolStripMenuItem.DropDownItems.Add("Recuperar copia de seguridad...", null, RestoreBackup);
		menuStrip.Items.Add(toolStripMenuItem);
		ToolStripMenuItem toolsMenu = new ToolStripMenuItem("Herramientas");
		toolsMenu.DropDownItems.Add("Papelera de contactos...", null, ShowRecycleBin);
		toolsMenu.DropDownItems.Add("Historial de comunicaciones...", null, ShowHistory);
		toolsMenu.DropDownItems.Add("Revisar duplicados y correos...", null, ShowIssues);
		menuStrip.Items.Add(toolsMenu);
		ToolStripMenuItem languageMenu = new ToolStripMenuItem("Idioma");
		foreach (LanguageOption option in UiText.Languages)
		{
			ToolStripMenuItem languageItem = new ToolStripMenuItem(option.DisplayName) { Checked = option.Code == UiText.CurrentLanguage, Tag = option.Code };
			languageItem.Click += ChangeLanguage;
			languageMenu.DropDownItems.Add(languageItem);
		}
		menuStrip.Items.Add(languageMenu);
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
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 164f));
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
		AddLabeled(flowLayoutPanel, "Buscar", _search);
		_search.TextChanged += delegate
		{
			RefreshList();
		};
		_country.SelectedIndexChanged += delegate
		{
			if (_updatingFilters)
			{
				return;
			}
			PopulateRegionFilter();
			RefreshList();
		};
		ComboBox[] array = new ComboBox[5] { _region, _lang, _type, _classFilter, _statusFilter };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SelectedIndexChanged += delegate
			{
				if (_updatingFilters)
				{
					return;
				}
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
		Button noContact = Btn("Marcar NO CONTACTAR", MarkNoContact, 188);
		noContact.BackColor = Color.FromArgb(180, 32, 45);
		flowLayoutPanel2.Controls.Add(noContact);
		flowLayoutPanel2.Controls.Add(Btn("Borrar contacto", DeleteContact, 145));
		flowLayoutPanel2.Controls.Add(Btn("Importar JSON/CSV/VCF", ImportJson, 188));
		flowLayoutPanel2.Controls.Add(Btn("Exportar JSON", ExportJson, 132));
		flowLayoutPanel2.Controls.Add(Btn("Crear informe PDF", ExportPdf, 157));
		_classifyAs.Items.AddRange(new object[5] { "empresa_medio", "organismo_publico", "red_publica", "entidad_civil", "sin_clasificar" });
		_classifyAs.SelectedIndex = 0;
		_classifyAs.Width = 165;
		flowLayoutPanel2.Controls.Add(new Label
		{
			Text = "Clasificar seleccionados:",
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
		flowLayoutPanel3.Controls.Add(Btn("Campaña individual...", OpenIndividualCampaign, 180));
		flowLayoutPanel3.Controls.Add(Btn("Ayuda", Help, 88));
		_status.Dock = DockStyle.Fill;
		_status.TextAlign = ContentAlignment.MiddleLeft;
		_status.ForeColor = Color.FromArgb(70, 82, 105);
		Panel statusPanel = new Panel
		{
			Dock = DockStyle.Bottom,
			Height = 42,
			Padding = new Padding(13, 0, 13, 0),
			BackColor = BackColor
		};
		statusPanel.Controls.Add(_status);
		base.Controls.Add(statusPanel);
		PopulateFilters();
		RefreshList();
		base.Shown += delegate
		{
			split.Panel1MinSize = 175;
			split.Panel2MinSize = 190;
			int max = Math.Max(split.Panel1MinSize, split.Height - split.SplitterWidth - split.Panel2MinSize);
			split.SplitterDistance = Math.Clamp((int)((double)split.Height * 0.58), split.Panel1MinSize, max);
		};
		UiText.Apply(this);
	}

	private void ChangeLanguage(object? sender, EventArgs e)
	{
		if (sender is not ToolStripMenuItem item || item.Tag is not string code) return;
		UiText.SetLanguage(code);
		foreach (ToolStripMenuItem sibling in item.Owner!.Items.OfType<ToolStripMenuItem>()) sibling.Checked = ReferenceEquals(sibling, item);
		PopulateFilters();
		UiText.ApplyAllOpenForms();
		UpdateStatus();
	}

	private static void AddLabeled(FlowLayoutPanel parent, string name, Control input)
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
		flowLayoutPanel.Controls.Add(input);
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
		AddGridCol("NO CONTACTAR", "NoContactar", 105);
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
		_updatingFilters = true;
		try
		{
			FillFilter(_country, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "pais")));
			PopulateRegionFilter();
			FillFilter(_lang, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "idioma")));
			FillFilter(_type, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "tipo")));
			FillFilter(_classFilter, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "naturaleza")));
			FillFilter(_statusFilter, _store.Contacts.Select((JsonObject c) => ContactStore.Get(c, "estado")));
		}
		finally
		{
			_updatingFilters = false;
		}
	}

	private void PopulateRegionFilter()
	{
		string? selectedCountry = _country.SelectedItem?.ToString();
		if (selectedCountry == UiText.Get("Todos"))
		{
			selectedCountry = null;
		}
		FillFilter(_region, GetRegionsForCountry(_store.Contacts, selectedCountry));
	}

	internal static IReadOnlyList<string> GetRegionsForCountry(IEnumerable<JsonObject> contacts, string? selectedCountry)
	{
		return contacts
			.Where((JsonObject contact) => string.IsNullOrWhiteSpace(selectedCountry) || string.Equals(ContactStore.Get(contact, "pais"), selectedCountry, StringComparison.OrdinalIgnoreCase))
			.Select((JsonObject contact) => ContactStore.Get(contact, "region"))
			.Where((string region) => !string.IsNullOrWhiteSpace(region))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy((string region) => region, StringComparer.CurrentCultureIgnoreCase)
			.ToList();
	}

	private static void FillFilter(ComboBox combo, IEnumerable<string> values)
	{
		string? selectedValue = combo.SelectedItem?.ToString();
		combo.Items.Clear();
		combo.Items.Add(UiText.Get("Todos"));
		foreach (string value in values.Where((string value) => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string value) => value, StringComparer.CurrentCultureIgnoreCase))
		{
			combo.Items.Add(value);
		}
		combo.SelectedItem = selectedValue != null && combo.Items.Contains(selectedValue) ? selectedValue : UiText.Get("Todos");
	}

	private void RefreshList()
	{
		if (_country.SelectedItem != null)
		{
			IOrderedEnumerable<JsonObject> source = _store.Contacts.Where((JsonObject c) => Keep(c, _country, "pais") && Keep(c, _region, "region") && Keep(c, _lang, "idioma") && Keep(c, _type, "tipo") && Keep(c, _classFilter, "naturaleza") && Keep(c, _statusFilter, "estado") && MatchesSearch(c)).OrderBy<JsonObject, string>((JsonObject c) => ContactStore.Get(c, "nombre"), StringComparer.CurrentCultureIgnoreCase);
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
			if (text != null && !(text == UiText.Get("Todos")))
			{
				return string.Equals(ContactStore.Get(c, name), text, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		bool MatchesSearch(JsonObject contact)
		{
			string term = _search.Text.Trim();
			if (term.Length == 0)
			{
				return true;
			}
			string[] fields = new string[11] { "nombre", "correo", "pais", "region", "idioma", "tipo", "naturaleza", "estado", "persona_contacto", "web_contacto", "notas" };
			return fields.Any(field => ContactStore.Get(contact, field).Contains(term, StringComparison.CurrentCultureIgnoreCase));
		}
	}

	private void ShowRecycleBin(object? sender, EventArgs e)
	{
		using RecycleBinForm form = new RecycleBinForm(_store);
		form.ShowDialog(this);
		if (form.DataChanged)
		{
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
		}
	}

	private void ShowHistory(object? sender, EventArgs e)
	{
		using HistoryForm form = new HistoryForm(_store);
		form.ShowDialog(this);
	}

	private void ShowIssues(object? sender, EventArgs e)
	{
		using IssuesForm form = new IssuesForm(_store);
		form.ShowDialog(this);
	}

	private void MarkVisible(bool chosen)
	{
		_grid.EndEdit();
		foreach (ContactRow visibleRow in _visibleRows)
		{
			visibleRow.Elegido = chosen && Eligible(visibleRow.Data);
		}
		_visibleRows.ResetBindings();
		_grid.InvalidateColumn(0);
		_lastPrepared.Clear();
		UpdateStatus();
	}

	private bool Eligible(JsonObject c)
	{
		if (string.Equals(ContactStore.Get(c, "canal"), "email", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ContactStore.Get(c, "correo")) && !_store.IsSuppressed(c))
		{
			return !string.Equals(ContactStore.Get(c, "estado"), "no_contactar_por_ahora", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private void MarkNoContact(object? sender, EventArgs e)
	{
		List<ContactRow> rows = SelectedRows();
		if (rows.Count == 0)
		{
			ContactRow? current = CurrentContact();
			if (current != null) rows.Add(current);
		}
		if (rows.Count == 0)
		{
			Warn(UiText.Get("Selecciona uno o varios contactos."));
			return;
		}
		if (MessageBox.Show(this, UiText.Get("Los contactos seleccionados quedarán bloqueados para futuras comunicaciones e importaciones.\n\n¿Marcar como NO CONTACTAR?"), UiText.Get("Marcar NO CONTACTAR"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}

		try
		{
			int changed = _store.MarkNoContact(rows.Select(row => row.Data));
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, UiText.Format("Contactos añadidos a NO CONTACTAR: {0}.", changed), UiText.Get("Lista de exclusión actualizada"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo actualizar la lista de exclusión:\n") + ex.Message);
		}
	}

	private List<ContactRow> SelectedRows()
	{
		return _visibleRows.Where((ContactRow c) => c.Elegido).ToList();
	}

	private void UpdateStatus()
	{
		List<ContactRow> list = SelectedRows();
		int noContact = _store.Contacts.Count(contact => _store.IsSuppressed(contact));
		_status.Text = UiText.Format("Contactos en base: {0}    ·    Visibles: {1}    ·    Marcados: {2}    ·    NO CONTACTAR: {3}    ·    Dirección CCO oculta para otros destinatarios.", _store.Contacts.Count, _visibleRows.Count, list.Count, noContact);
	}

	private void ApplyClassification(object? sender, EventArgs e)
	{
		List<ContactRow> list = SelectedRows();
		if (list.Count == 0)
		{
			Warn(UiText.Get("Primero marca una o varias filas."));
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
		MessageBox.Show(UiText.Format("Clasificación '{0}' aplicada a {1} contactos y guardada. Vuelve a marcarlos antes de preparar correo.", text, list.Count), UiText.Get("Guardado"));
	}

	private void ImportJson(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = UiText.Get("Importar contactos"),
			Filter = UiText.Get("Contactos compatibles") + " (*.json;*.csv;*.vcf)|*.json;*.csv;*.vcf|Google/Outlook CSV (*.csv)|*.csv|vCard (*.vcf)|*.vcf|JSON (*.json)|*.json"
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		try
		{
			ContactImportPlan plan = _store.PrepareImport(openFileDialog.FileName);
			string sample = string.Join("\n", plan.Contacts.Take(8).Select(contact => "• " + ContactStore.Get(contact, "nombre") + FormatEmail(ContactStore.Get(contact, "correo"))));
			if (plan.Contacts.Count > 8)
			{
				sample += "\n" + UiText.Format("… y {0} más", plan.Contacts.Count - 8);
			}
			string preview = UiText.Format("Archivo: {0}\nFormato: {1}\n\nRegistros detectados: {2}\nListos para importar: {3}\nRepetidos omitidos: {4}\nSin nombre omitidos: {5}", Path.GetFileName(openFileDialog.FileName), plan.Format, plan.SourceRows, plan.Contacts.Count, plan.SkippedDuplicate, plan.SkippedMissingName);
			preview += "\n" + UiText.Format("NO CONTACTAR omitidos: {0}", plan.SkippedNoContact);
			if (sample.Length > 0)
			{
				preview += "\n\n" + UiText.Get("Vista previa:\n") + sample;
			}
			if (plan.Contacts.Count == 0)
			{
				MessageBox.Show(this, preview + "\n\n" + UiText.Get("No hay contactos nuevos que importar."), UiText.Get("Importación"), MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			if (MessageBox.Show(this, preview + "\n\n" + UiText.Get("Antes de importar se creará automáticamente una copia de seguridad.\nLos contactos existentes no se sobrescribirán.\n\n¿Continuar?"), UiText.Get("Revisar datos antes de importar"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
			{
				return;
			}
			ContactImportResult result = _store.CommitImport(plan);
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, UiText.Format("Importados: {0}\nOmitidos: {1}\n\nCopia de seguridad creada:\n{2}\n\nLos contactos existentes NO se han sobrescrito.", result.Added, result.Skipped, result.SafetyBackupPath), UiText.Get("Importación"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo importar el archivo:\n") + ex.Message);
		}

		static string FormatEmail(string email)
		{
			return email.Length == 0 ? " (" + UiText.Get("sin correo") + ")" : " — " + email;
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
			MessageBox.Show(this, UiText.Get("Contacto añadido y guardado en contactos.json."), UiText.Get("Guardado"));
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo añadir el contacto:\n") + ex.Message);
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
			Warn(UiText.Get("Selecciona una fila para modificarla (no hace falta marcar su casilla)."));
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
			MessageBox.Show(this, UiText.Get("Contacto modificado y guardado. Se conserva el historial anterior."), UiText.Get("Guardado"));
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo modificar el contacto:\n") + ex.Message);
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
				Warn(UiText.Get("Marca uno o varios contactos, o selecciona una fila, para borrarlos."));
				return;
			}
			list.Add(contactRow);
		}
		string text = ((list.Count == 1) ? ("'" + ContactStore.Get(list[0].Data, "nombre") + "'") : $"los {list.Count} contactos marcados");
		if (MessageBox.Show(this, UiText.Format("¿Eliminar de la lista activa a {0}?\n\nNo aparecerán en futuras campañas. Se guardará una copia de cada registro en contactos_eliminados y el historial de envíos seguirá intacto.", text), UiText.Get((list.Count == 1) ? "Confirmar eliminación" : "Confirmar eliminación múltiple"), MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			_store.DeleteContacts(list.Select((ContactRow row) => row.Data));
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, UiText.Format("Eliminados {0} contactos de la lista activa y conservados en el archivo histórico.", list.Count), UiText.Get("Eliminación completada"));
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudieron eliminar los contactos:\n") + ex.Message);
		}
	}

	private void ExportJson(object? sender, EventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = UiText.Get("Exportar todos los contactos"),
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
			MessageBox.Show(this, UiText.Get("Contactos exportados correctamente."), UiText.Get("Exportación"));
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo exportar el JSON:\n") + ex.Message);
		}
	}

	private void ExportPdf(object? sender, EventArgs e)
	{
		_grid.EndEdit();
		List<ContactRow> markedRows = SelectedRows();
		using PdfReportOptionsForm options = new(_visibleRows.Count, markedRows.Count);
		if (options.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		List<JsonObject> contacts = (options.MarkedOnly ? markedRows : _visibleRows.ToList()).Select(row => row.Data).ToList();
		if (contacts.Count == 0)
		{
			Warn(UiText.Get("No hay contactos para incluir en el informe."));
			return;
		}
		if (!PdfReportGenerator.IsAvailable)
		{
			Warn(UiText.Get("No está instalada la impresora 'Microsoft Print to PDF' de Windows."));
			return;
		}

		using SaveFileDialog saveFileDialog = new()
		{
			Title = UiText.Get("Guardar informe PDF"),
			Filter = "PDF (*.pdf)|*.pdf",
			FileName = "informe_contactos_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".pdf",
			InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			OverwritePrompt = true,
			AddExtension = true,
			DefaultExt = "pdf"
		};
		if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}

		try
		{
			PdfReportGenerator.Create(saveFileDialog.FileName, contacts, options.SelectedColumns, options.ReportTitle, CurrentFilterSummary(options.MarkedOnly));
			if (MessageBox.Show(this, UiText.Format("Informe PDF creado correctamente con {0} contactos.\n\n{1}\n\n¿Deseas abrirlo ahora?", contacts.Count, saveFileDialog.FileName), UiText.Get("Informe PDF completado"), MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
			{
				Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
			}
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo crear el informe PDF:\n") + ex.Message);
		}
	}

	private string CurrentFilterSummary(bool markedOnly)
	{
		List<string> filters = new();
		AddFilter(UiText.Get("País"), _country);
		AddFilter(UiText.Get("Región"), _region);
		AddFilter(UiText.Get("Idioma"), _lang);
		AddFilter(UiText.Get("Tipo"), _type);
		AddFilter(UiText.Get("Organización"), _classFilter);
		AddFilter(UiText.Get("Estado"), _statusFilter);
		if (!string.IsNullOrWhiteSpace(_search.Text))
		{
			filters.Add(UiText.Format("Búsqueda: {0}", _search.Text.Trim()));
		}
		string scope = markedOnly ? UiText.Get("Sólo marcados") : UiText.Get("Visibles");
		return scope + " · " + (filters.Count == 0 ? UiText.Get("Sin filtros") : string.Join(" · ", filters));

		void AddFilter(string label, ComboBox combo)
		{
			string? value = combo.SelectedItem?.ToString();
			if (!string.IsNullOrWhiteSpace(value) && value != UiText.Get("Todos"))
			{
				filters.Add(label + ": " + value);
			}
		}
	}

	private void CreateBackup(object? sender, EventArgs e)
	{
		using SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			Title = UiText.Get("Hacer copia de seguridad completa"),
			Filter = UiText.Get("Copia de seguridad ZIP (*.zip)|*.zip"),
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
			MessageBox.Show(this, UiText.Format("La copia de seguridad se ha creado correctamente.\n\nIncluye todos los contactos, los contactos eliminados y el historial de envíos.\n\n{0}", saveFileDialog.FileName), UiText.Get("Copia completada"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo crear la copia de seguridad:\n") + ex.Message);
		}
	}

	private void RestoreBackup(object? sender, EventArgs e)
	{
		using OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Title = UiText.Get("Recuperar copia de seguridad completa"),
			Filter = UiText.Get("Copia de seguridad ZIP (*.zip)|*.zip")
		};
		if (openFileDialog.ShowDialog(this) != DialogResult.OK)
		{
			return;
		}
		if (MessageBox.Show(this, UiText.Get("La copia seleccionada sustituirá los contactos y el historial actuales.\n\nAntes de recuperar, el programa guardará automáticamente una copia de seguridad del estado actual.\n\n¿Deseas continuar?"), UiText.Get("Confirmar recuperación"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			var result = _store.RestoreBackup(openFileDialog.FileName);
			_lastPrepared.Clear();
			PopulateFilters();
			RefreshList();
			MessageBox.Show(this, UiText.Format("Copia recuperada correctamente.\n\nContactos activos: {0}\nEnvíos en el historial: {1}\n\nCopia automática del estado anterior:\n{2}", result.contacts, result.shipments, result.safetyBackupPath), UiText.Get("Recuperación completada"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo recuperar la copia. Los datos actuales se han conservado.\n\n") + ex.Message);
		}
	}

	private List<ContactRow> ValidSelection(out string message)
	{
		List<ContactRow> list = SelectedRows();
		List<ContactRow> list2 = list.Where((ContactRow c) => !Eligible(c.Data)).ToList();
		if (list2.Count > 0)
		{
			message = UiText.Format("Hay {0} contactos sin correo autorizado o marcados como no contactar. Desmárcalos.", list2.Count);
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
			message = UiText.Get("Hay direcciones repetidas entre las seleccionadas. Corrige la lista antes de continuar.");
			return new List<ContactRow>();
		}
		message = ((list.Count == 0) ? UiText.Get("No hay destinatarios válidos para la selección y versión actual.") : "");
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
		MessageBox.Show(UiText.Format("{0} correos copiados. Pégalos en CCO, NO en CC.", list.Count), UiText.Get("CCO copiado"));
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
		if (MessageBox.Show(UiText.Format("Preparar correo para {0} destinatarios en CCO.\n\nSe abrirá la aplicación de correo predeterminada, NO se enviará nada automáticamente.\nEl asunto, el mensaje y los adjuntos quedarán en blanco para que los completes personalmente.\nRevisa especialmente el campo CCO antes de enviar.\n\n¿Continuar?", list.Count), UiText.Get("Revisar destinatarios"), MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) != DialogResult.Yes)
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
			Warn(UiText.Get("No se pudo abrir el cliente de correo. Puede que mailto no esté configurado. Usa 'Copiar CCO' y pega las direcciones manualmente.\n\n") + ex.Message);
		}
	}

	private void ConfirmSent(object? sender, EventArgs e)
	{
		if (_lastPrepared.Count == 0)
		{
			Warn(UiText.Get("Primero prepara el correo con 'Preparar con mailto'. Esto NO confirma un envío."));
			return;
		}
		string value = ((_lastPreparedVersion.Length == 0) ? UiText.Get("sin indicar versión") : UiText.Format("de la versión {0}", _lastPreparedVersion));
		if (MessageBox.Show(UiText.Format("¿CONFIRMAS que tú has pulsado ENVIAR en tu cliente de correo para los {0} destinatarios ({1})?\n\nSólo entonces anotaremos 'enviado confirmado por usuario'.", _lastPrepared.Count, value), UiText.Get("Confirmar envío REAL"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
		{
			return;
		}
		try
		{
			_store.ConfirmSend(_lastPrepared, _lastPreparedVersion);
			MessageBox.Show(UiText.Format("Registrados {0} envíos declarados por ti. No equivale a confirmar entrega.", _lastPrepared.Count), UiText.Get("Registro actualizado"));
			_lastPrepared.Clear();
			RefreshList();
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo guardar el historial:\n") + ex.Message);
		}
	}

	private void OpenIndividualCampaign(object? sender, EventArgs e)
	{
		_grid.EndEdit();
		try
		{
			using CampaignForm form = new CampaignForm(_store, SelectedRows().Select(row => row.Data).ToList(), _version.Text.Trim());
			form.ShowDialog(this);
			if (form.HistoryChanged)
			{
				_lastPrepared.Clear();
				PopulateFilters();
				RefreshList();
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
		{
			Warn(UiText.Get("No se pudo abrir la campaña individual. No se ha modificado ningún envío.\n\n") + ex.Message);
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
		MessageBox.Show(UiText.Get("1) Busca, filtra y marca los contactos que necesites.\n2) Para un grupo puedes mantener el sistema CCO existente.\n3) Para enviar uno a uno, abre 'Campaña individual', escribe el asunto y el cuerpo, añade los archivos y crea la cola.\n4) 'Preparar siguiente' abre un solo borrador en la aplicación y cuenta predeterminadas del usuario.\n5) Revisa el mensaje y pulsa Enviar personalmente. Después confirma el envío en el gestor.\n6) El siguiente se habilita tras 20 segundos y el gestor se detiene al llegar a 45 confirmados en el día.\n7) Puedes pausar, cerrar y continuar la cola posteriormente sin repetir destinatarios.\n8) NO CONTACTAR, la papelera y quienes ya recibieron la versión quedan excluidos.\n9) Las copias de seguridad incluyen los contactos, el historial y la campaña guardada.\n\nIMPORTANTE: el gestor no envía automáticamente ni guarda contraseñas. Abrir un borrador nunca lo marca como enviado."), UiText.Get("Cómo utilizar el gestor"));
	}

	private void Warn(string message)
	{
		MessageBox.Show(message, UiText.Get("Gestor DNET"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}
}

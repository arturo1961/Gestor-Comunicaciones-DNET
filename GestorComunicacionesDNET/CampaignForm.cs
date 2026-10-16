using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class CampaignForm : DnetForm
{
	private const int DailyLimit = 45;
	private const int DelaySeconds = 20;
	private readonly ContactStore _store;
	private readonly CampaignStateStore _stateStore;
	private readonly List<JsonObject> _selectedContacts;
	private CampaignState _state;
	private readonly TextBox _subject = new() { Dock = DockStyle.Fill };
	private readonly TextBox _version = new() { Dock = DockStyle.Fill };
	private readonly TextBox _body = new() { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
	private readonly ListBox _attachments = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
	private readonly Label _summary = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
	private readonly Label _current = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
	private readonly Button _prepare;
	private readonly Button _confirm;
	private readonly Button _notSent;
	private readonly Timer _timer = new() { Interval = 500 };

	public bool HistoryChanged { get; private set; }

	public CampaignForm(ContactStore store, IReadOnlyList<JsonObject> selectedContacts, string initialVersion)
	{
		_store = store;
		_selectedContacts = selectedContacts.ToList();
		_stateStore = new CampaignStateStore(store.DataDirectory);
		_state = _stateStore.Load();
		if (_state.Queue.Count == 0 && _state.Version.Length == 0) _state.Version = initialVersion;

		Text = "Campaña individual — preparación supervisada";
		StartPosition = FormStartPosition.CenterParent;
		MinimumSize = new Size(900, 720);
		Size = new Size(1120, 860);
		Font = new Font("Segoe UI", 10f);
		BackColor = Color.FromArgb(245, 247, 251);

		TableLayoutPanel root = new()
		{
			Dock = DockStyle.Fill,
			Padding = new Padding(14),
			ColumnCount = 1,
			RowCount = 5
		};
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
		Controls.Add(root);

		root.Controls.Add(new Label
		{
			Text = "Campaña individual: un destinatario cada vez",
			Dock = DockStyle.Fill,
			Font = new Font("Segoe UI", 16f, FontStyle.Bold),
			ForeColor = Color.FromArgb(25, 40, 65),
			TextAlign = ContentAlignment.MiddleLeft
		}, 0, 0);

		TableLayoutPanel header = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
		header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
		header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		header.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		header.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
		AddField(header, "Asunto:", _subject, 0);
		AddField(header, "Versión:", _version, 1);
		root.Controls.Add(header, 0, 1);

		GroupBox bodyGroup = new() { Text = "Cuerpo del mensaje", Dock = DockStyle.Fill, Padding = new Padding(9) };
		bodyGroup.Controls.Add(_body);
		root.Controls.Add(bodyGroup, 0, 2);

		GroupBox attachmentsGroup = new() { Text = "Archivos adjuntos", Dock = DockStyle.Fill, Padding = new Padding(9) };
		TableLayoutPanel attachmentLayout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
		attachmentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		attachmentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
		attachmentLayout.Controls.Add(_attachments, 0, 0);
		FlowLayoutPanel attachmentButtons = new() { Dock = DockStyle.Fill, WrapContents = false };
		attachmentButtons.Controls.Add(NewButton("Añadir archivos...", AddAttachments, 150));
		attachmentButtons.Controls.Add(NewButton("Quitar seleccionado", RemoveAttachment, 165));
		attachmentButtons.Controls.Add(new Label
		{
			Text = "Puedes usar {nombre}, {organizacion}, {correo}, {pais} y {region} en el asunto o el cuerpo.",
			AutoSize = true,
			Margin = new Padding(15, 12, 0, 0)
		});
		attachmentLayout.Controls.Add(attachmentButtons, 0, 1);
		attachmentsGroup.Controls.Add(attachmentLayout);
		root.Controls.Add(attachmentsGroup, 0, 3);

		TableLayoutPanel footer = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
		footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
		footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 27));
		footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		footer.Controls.Add(_summary, 0, 0);
		footer.Controls.Add(_current, 0, 1);
		FlowLayoutPanel actions = new() { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
		actions.Controls.Add(NewButton("Crear cola con los marcados", CreateQueue, 210));
		_prepare = NewButton("Preparar siguiente", PrepareNext, 165);
		_confirm = NewButton("Confirmar enviado", ConfirmSent, 165);
		_notSent = NewButton("No enviado", ReturnToPending, 120);
		actions.Controls.Add(_prepare);
		actions.Controls.Add(_confirm);
		actions.Controls.Add(_notSent);
		actions.Controls.Add(NewButton("Omitir", SkipCurrent, 95));
		actions.Controls.Add(NewButton("Pausar y cerrar", PauseAndClose, 150));
		footer.Controls.Add(actions, 0, 2);
		root.Controls.Add(footer, 0, 4);

		_subject.Text = _state.Subject;
		_body.Text = _state.Body;
		_version.Text = _state.Version;
		ReloadAttachments();
		_timer.Tick += (_, _) => UpdateStatus();
		_timer.Start();
		FormClosing += (_, _) => SaveEditorState();
		UiText.Apply(this);
		UpdateStatus();
	}

	private static void AddField(TableLayoutPanel table, string label, Control input, int row)
	{
		table.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
		input.Margin = new Padding(3, 5, 3, 5);
		table.Controls.Add(input, 1, row);
	}

	private static Button NewButton(string text, EventHandler handler, int width)
	{
		Button button = new() { Text = text, AutoSize = true, MinimumSize = new Size(width, 39), Margin = new Padding(3, 4, 7, 3) };
		button.Click += handler;
		return button;
	}

	private void AddAttachments(object? sender, EventArgs e)
	{
		using OpenFileDialog dialog = new() { Title = UiText.Get("Seleccionar archivos adjuntos"), Multiselect = true, Filter = UiText.Get("Todos los archivos (*.*)|*.*") };
		if (dialog.ShowDialog(this) != DialogResult.OK) return;
		foreach (string file in dialog.FileNames)
		{
			if (!_state.Attachments.Contains(file, StringComparer.OrdinalIgnoreCase)) _state.Attachments.Add(file);
		}
		ReloadAttachments();
		SaveEditorState();
	}

	private void RemoveAttachment(object? sender, EventArgs e)
	{
		if (_attachments.SelectedItem is not string selected) return;
		_state.Attachments.RemoveAll(path => string.Equals(path, selected, StringComparison.OrdinalIgnoreCase));
		ReloadAttachments();
		SaveEditorState();
	}

	private void ReloadAttachments()
	{
		_attachments.Items.Clear();
		_attachments.Items.AddRange(_state.Attachments.Cast<object>().ToArray());
	}

	private void CreateQueue(object? sender, EventArgs e)
	{
		if (_selectedContacts.Count == 0)
		{
			Warn("Marca primero los contactos en la pantalla principal y vuelve a abrir esta ventana.");
			return;
		}
		if (_state.Queue.Any(item => item.Status is "pending" or "prepared") &&
			MessageBox.Show(this, UiText.Get("La cola guardada todavía tiene contactos pendientes. ¿Deseas sustituirla por una cola nueva?"), UiText.Get("Sustituir cola"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
			return;

		SaveEditorState(save: false);
		HashSet<string> emails = new(StringComparer.OrdinalIgnoreCase);
		List<JsonObject> valid = _selectedContacts
			.Where(IsCurrentlyEligible)
			.Where(contact => !_store.AlreadyInformed(contact, _state.Version.Trim()))
			.Where(contact => emails.Add(ContactStore.Get(contact, "correo").Trim()))
			.ToList();
		if (valid.Count == 0)
		{
			Warn("No hay destinatarios válidos: se han excluido NO CONTACTAR, papelera, duplicados y quienes ya recibieron esta versión.");
			return;
		}

		_state.Queue = valid.Select(contact => new CampaignQueueItem { ContactId = ContactStore.Get(contact, "id") }).ToList();
		_state.CreatedAt = DateTimeOffset.Now;
		_state.CooldownUntil = null;
		_state.Paused = false;
		_stateStore.Save(_state);
		UpdateStatus();
		MessageBox.Show(this, UiText.Format("Cola creada: {0} destinatarios. Se preparará siempre un solo mensaje.", valid.Count), UiText.Get("Cola preparada"), MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void PrepareNext(object? sender, EventArgs e)
	{
		SaveEditorState();
		if (_store.ConfirmedTodayCount() >= DailyLimit)
		{
			Warn("Se ha alcanzado el límite de 45 envíos confirmados de hoy. Continúa mañana.");
			return;
		}
		if (SecondsRemaining() > 0)
		{
			Warn(UiText.Format("Espera {0} segundos antes de preparar el siguiente mensaje.", SecondsRemaining()));
			return;
		}
		if (_state.Queue.Any(item => item.Status == "prepared"))
		{
			Warn("Hay un mensaje preparado pendiente de confirmar. Indica «Confirmar enviado» o «No enviado».");
			return;
		}

		CampaignQueueItem? item;
		JsonObject? contact;
		do
		{
			item = _state.Queue.FirstOrDefault(candidate => candidate.Status == "pending");
			if (item is null)
			{
				Warn("La cola no tiene más destinatarios pendientes.");
				return;
			}
			contact = _store.FindActiveContact(item.ContactId);
			if (contact is null || !IsCurrentlyEligible(contact) || _store.AlreadyInformed(contact, _state.Version.Trim()))
			{
				item.Status = "skipped";
				item.Note = "Excluido al volver a comprobar el contacto antes de preparar.";
				_stateStore.Save(_state);
				contact = null;
			}
		} while (contact is null);

		string recipient = ContactStore.Get(contact, "correo").Trim();
		string subject = Personalize(_state.Subject, contact);
		string body = Personalize(_state.Body, contact);
		if (!EmailDraftLauncher.Open(recipient, subject, body, _state.Attachments, out string detail))
		{
			Warn(detail);
			return;
		}
		item.Status = "prepared";
		item.Note = DateTimeOffset.Now.ToString("O");
		_state.Paused = false;
		_stateStore.Save(_state);
		UpdateStatus();
		MessageBox.Show(this, detail + "\n\n" + UiText.Get("Después de pulsar Enviar, vuelve al gestor y confirma el envío."), UiText.Get("Mensaje preparado"), MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	private void ConfirmSent(object? sender, EventArgs e)
	{
		CampaignQueueItem? item = _state.Queue.FirstOrDefault(candidate => candidate.Status == "prepared");
		JsonObject? contact = item is null ? null : _store.FindActiveContact(item.ContactId);
		if (item is null || contact is null)
		{
			Warn("No hay ningún mensaje preparado pendiente de confirmación.");
			return;
		}
		if (MessageBox.Show(this, UiText.Format("¿Confirmas que has pulsado ENVIAR para {0}?\n\nAbrir el borrador no cuenta como envío.", ContactStore.Get(contact, "correo")), UiText.Get("Confirmar envío REAL"), MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
			return;

		try
		{
			_store.ConfirmIndividualSend(new[] { contact }, _state.Version.Trim());
			item.Status = "confirmed";
			item.Note = DateTimeOffset.Now.ToString("O");
			_state.CooldownUntil = DateTimeOffset.Now.AddSeconds(DelaySeconds);
			_stateStore.Save(_state);
			HistoryChanged = true;
			UpdateStatus();
		}
		catch (Exception ex)
		{
			Warn(UiText.Get("No se pudo guardar el historial:\n") + ex.Message);
		}
	}

	private void ReturnToPending(object? sender, EventArgs e)
	{
		CampaignQueueItem? item = _state.Queue.FirstOrDefault(candidate => candidate.Status == "prepared");
		if (item is null) { Warn("No hay ningún mensaje preparado."); return; }
		item.Status = "pending";
		item.Note = "";
		_stateStore.Save(_state);
		UpdateStatus();
	}

	private void SkipCurrent(object? sender, EventArgs e)
	{
		CampaignQueueItem? item = _state.Queue.FirstOrDefault(candidate => candidate.Status == "prepared")
			?? _state.Queue.FirstOrDefault(candidate => candidate.Status == "pending");
		if (item is null) { Warn("La cola no tiene destinatarios pendientes."); return; }
		item.Status = "skipped";
		item.Note = "Omitido manualmente.";
		_stateStore.Save(_state);
		UpdateStatus();
	}

	private void PauseAndClose(object? sender, EventArgs e)
	{
		_state.Paused = true;
		SaveEditorState();
		Close();
	}

	private bool IsCurrentlyEligible(JsonObject contact)
	{
		return !_store.IsSuppressed(contact)
			&& string.Equals(ContactStore.Get(contact, "canal"), "email", StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(ContactStore.Get(contact, "correo"))
			&& !string.Equals(ContactStore.Get(contact, "estado"), "no_contactar_por_ahora", StringComparison.OrdinalIgnoreCase);
	}

	private void SaveEditorState(bool save = true)
	{
		_state.Subject = _subject.Text;
		_state.Body = _body.Text;
		_state.Version = _version.Text.Trim();
		if (save) _stateStore.Save(_state);
	}

	private void UpdateStatus()
	{
		int pending = _state.Queue.Count(item => item.Status == "pending");
		int confirmed = _state.Queue.Count(item => item.Status == "confirmed");
		int skipped = _state.Queue.Count(item => item.Status == "skipped");
		int today = _store.ConfirmedTodayCount();
		int seconds = SecondsRemaining();
		_summary.Text = UiText.Format("Enviados hoy: {0} de 45    ·    Cola: {1} confirmados, {2} pendientes, {3} omitidos", today, confirmed, pending, skipped);
		CampaignQueueItem? prepared = _state.Queue.FirstOrDefault(item => item.Status == "prepared");
		JsonObject? contact = prepared is null ? null : _store.FindActiveContact(prepared.ContactId);
		_current.Text = contact is null
			? (seconds > 0 ? UiText.Format("Siguiente disponible en {0} segundos.", seconds) : UiText.Get("No hay ningún borrador pendiente de confirmación."))
			: UiText.Format("Pendiente de confirmar: {0} — {1}", ContactStore.Get(contact, "nombre"), ContactStore.Get(contact, "correo"));
		_prepare.Enabled = today < DailyLimit && seconds == 0 && prepared is null && pending > 0;
		_confirm.Enabled = prepared is not null;
		_notSent.Enabled = prepared is not null;
	}

	private int SecondsRemaining()
	{
		if (_state.CooldownUntil is null) return 0;
		return Math.Max(0, (int)Math.Ceiling((_state.CooldownUntil.Value - DateTimeOffset.Now).TotalSeconds));
	}

	private static string Personalize(string template, JsonObject contact)
	{
		Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
		{
			["{nombre}"] = ContactStore.Get(contact, "nombre"),
			["{organizacion}"] = ContactStore.Get(contact, "nombre"),
			["{correo}"] = ContactStore.Get(contact, "correo"),
			["{pais}"] = ContactStore.Get(contact, "pais"),
			["{region}"] = ContactStore.Get(contact, "region")
		};
		foreach ((string token, string value) in values) template = template.Replace(token, value, StringComparison.OrdinalIgnoreCase);
		return template;
	}

	private void Warn(string spanish)
	{
		MessageBox.Show(this, UiText.Get(spanish), UiText.Get("Gestor DNET"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}
}

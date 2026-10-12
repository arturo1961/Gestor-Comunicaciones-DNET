using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text.Json.Nodes;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed class ContactEditorForm : Form
{
	private sealed record FieldDef(string Key, string Caption, string[]? Choices = null, bool Multiline = false);

	private static readonly FieldDef[] CommonFields = new FieldDef[21]
	{
		new FieldDef("id", "ID (historial)"),
		new FieldDef("nombre", "Nombre / entidad"),
		new FieldDef("correo", "Correo electrónico"),
		new FieldDef("pais", "País"),
		new FieldDef("region", "Región / comunidad"),
		new FieldDef("idioma", "Idioma", new string[11]
		{
			"es", "ca", "val", "gl", "eu", "oc-ES", "fr", "en", "de", "it",
			"pt"
		}),
		new FieldDef("tipo", "Tipo de contacto", new string[6] { "prensa", "institucion", "asociacion", "universidad", "empresa", "otros" }),
		new FieldDef("naturaleza", "Organización", new string[5] { "empresa_medio", "organismo_publico", "red_publica", "entidad_civil", "sin_clasificar" }),
		new FieldDef("canal", "Canal", new string[3] { "email", "formulario_web", "otro" }),
		new FieldDef("web_contacto", "Web de contacto"),
		new FieldDef("persona_contacto", "Persona de contacto"),
		new FieldDef("uso_del_correo", "Uso del correo"),
		new FieldDef("verificacion", "Verificación / fuente", null, Multiline: true),
		new FieldDef("estado", "Estado", new string[5] { "pendiente", "enviado_confirmado_usuario", "respondido", "no_contactar_por_ahora", "error" }),
		new FieldDef("ultima_version_comunicada", "Última versión enviada"),
		new FieldDef("fecha_ultimo_envio", "Fecha último envío"),
		new FieldDef("no_contactar", "No contactar"),
		new FieldDef("notas", "Notas", null, Multiline: true),
		new FieldDef("material", "Material utilizado", null, Multiline: true),
		new FieldDef("respuesta_seguimiento", "Respuesta / seguimiento", null, Multiline: true),
		new FieldDef("origen_csv", "Origen del registro")
	};

	private readonly JsonObject _original;

	private readonly bool _creating;

	private readonly Dictionary<string, Control> _controls = new Dictionary<string, Control>();

	public JsonObject? EditedContact { get; private set; }

	public ContactEditorForm(JsonObject? contact = null)
	{
		_creating = contact == null;
		_original = ((contact == null) ? NewContact() : ((JsonObject)contact.DeepClone()));
		Text = (_creating ? "Añadir contacto — todos los campos" : "Modificar contacto — todos los campos");
		base.StartPosition = FormStartPosition.CenterParent;
		Font = new Font("Segoe UI", 10f);
		base.Size = new Size(1050, 765);
		MinimumSize = new Size(850, 600);
		base.FormBorderStyle = FormBorderStyle.Sizable;
		base.ShowInTaskbar = false;
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			RowCount = 3,
			ColumnCount = 1,
			Padding = new Padding(10)
		};
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 62f));
		base.Controls.Add(tableLayoutPanel);
		Label control = new Label
		{
			Dock = DockStyle.Fill,
			TextAlign = ContentAlignment.MiddleLeft,
			Text = "Modifica los datos y pulsa Guardar. El ID se mantiene fijo para proteger el historial de envíos."
		};
		tableLayoutPanel.Controls.Add(control, 0, 0);
		TableLayoutPanel tableLayoutPanel2 = new TableLayoutPanel
		{
			Dock = DockStyle.Fill,
			ColumnCount = 2,
			RowCount = 1
		};
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
		tableLayoutPanel.Controls.Add(tableLayoutPanel2, 0, 1);
		(Panel, TableLayoutPanel) tuple = NewColumn();
		(Panel, TableLayoutPanel) tuple2 = NewColumn();
		tableLayoutPanel2.Controls.Add(tuple.Item1, 0, 0);
		tableLayoutPanel2.Controls.Add(tuple2.Item1, 1, 0);
		List<FieldDef> list = CommonFields.ToList();
		foreach (string key in _original.Select<KeyValuePair<string, JsonNode>, string>((KeyValuePair<string, JsonNode> pair) => pair.Key))
		{
			if (!list.Any((FieldDef d) => d.Key == key))
			{
				list.Add(new FieldDef(key, key));
			}
		}
		for (int num = 0; num < list.Count; num++)
		{
			AddField((num < (list.Count + 1) / 2) ? tuple.Item2 : tuple2.Item2, list[num]);
		}
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			FlowDirection = FlowDirection.RightToLeft,
			Dock = DockStyle.Fill,
			WrapContents = false,
			Padding = new Padding(4)
		};
		tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 2);
		Button button = new Button
		{
			Text = "Cancelar",
			Width = 120,
			Height = 38,
			DialogResult = DialogResult.Cancel
		};
		Button button2 = new Button
		{
			Text = "Guardar contacto",
			Width = 175,
			Height = 38
		};
		button2.Click += SaveChanges;
		flowLayoutPanel.Controls.Add(button);
		flowLayoutPanel.Controls.Add(button2);
		base.CancelButton = button;
		base.AcceptButton = button2;
		UiText.Apply(this);
	}

	private static (Panel Panel, TableLayoutPanel Table) NewColumn()
	{
		Panel obj = new Panel
		{
			Dock = DockStyle.Fill,
			AutoScroll = true,
			Padding = new Padding(4)
		};
		TableLayoutPanel tableLayoutPanel = new TableLayoutPanel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			ColumnCount = 2,
			RowCount = 0,
			Padding = new Padding(2)
		};
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172f));
		tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 282f));
		obj.Controls.Add(tableLayoutPanel);
		return (Panel: obj, Table: tableLayoutPanel);
	}

	private void AddField(TableLayoutPanel table, FieldDef field)
	{
		int row = table.RowCount++;
		bool flag = field.Multiline;
		if (!flag)
		{
			JsonNode jsonNode = _original[field.Key];
			bool flag2 = ((jsonNode is JsonObject || jsonNode is JsonArray) ? true : false);
			flag = flag2;
		}
		bool flag3 = flag;
		int num = (flag3 ? 96 : 36);
		table.RowStyles.Add(new RowStyle(SizeType.Absolute, num));
		table.Controls.Add(new Label
		{
			Text = field.Caption + ":",
			TextAlign = ContentAlignment.MiddleLeft,
			Dock = DockStyle.Fill,
			Margin = new Padding(2)
		}, 0, row);
		Control control;
		if (field.Key == "no_contactar")
		{
			control = new CheckBox
			{
				Checked = ContactStore.Flag(_original, field.Key),
				Dock = DockStyle.Left,
				Margin = new Padding(4, 10, 2, 2),
				AutoSize = true,
				Text = "Sí, excluir de los envíos"
			};
		}
		else if (field.Choices != null)
		{
			ComboBox comboBox = new ComboBox();
			comboBox.DropDownStyle = ComboBoxStyle.DropDown;
			comboBox.Dock = DockStyle.Fill;
			comboBox.Margin = new Padding(3, 5, 3, 5);
			comboBox.Items.AddRange(field.Choices.Cast<object>().ToArray());
			comboBox.Text = ContactStore.Get(_original, field.Key);
			control = comboBox;
		}
		else
		{
			JsonNode jsonNode = _original[field.Key];
			flag = ((jsonNode is JsonObject || jsonNode is JsonArray) ? true : false);
			string text = (flag ? _original[field.Key].ToJsonString() : ContactStore.Get(_original, field.Key));
			control = new TextBox
			{
				Text = text,
				ReadOnly = (field.Key == "id"),
				Dock = DockStyle.Fill,
				Multiline = flag3,
				ScrollBars = (flag3 ? ScrollBars.Vertical : ScrollBars.None),
				Margin = new Padding(3, flag3 ? 3 : 5, 3, flag3 ? 5 : 7),
				BackColor = ((field.Key == "id") ? Color.FromArgb(240, 242, 246) : SystemColors.Window)
			};
		}
		control.TabIndex = _controls.Count + 1;
		_controls[field.Key] = control;
		table.Controls.Add(control, 1, row);
	}

	private void SaveChanges(object? sender, EventArgs e)
	{
		JsonObject jsonObject = (JsonObject)_original.DeepClone();
		foreach (var (text2, control2) in _controls)
		{
			if (text2 == "id")
			{
				continue;
			}
			if (control2 is CheckBox checkBox)
			{
				jsonObject[text2] = checkBox.Checked;
				continue;
			}
			string text3 = control2.Text.Trim();
			JsonNode jsonNode = _original[text2];
			if ((jsonNode is JsonArray || jsonNode is JsonObject) ? true : false)
			{
				try
				{
					jsonObject[text2] = JsonNode.Parse(text3);
				}
				catch
				{
					Error(UiText.Format("El campo '{0}' contiene JSON no válido. Corrígelo antes de guardar.", text2));
					return;
				}
			}
			else if (text3.Length == 0 && jsonNode == null)
			{
				jsonObject[text2] = null;
			}
			else
			{
				jsonObject[text2] = text3;
			}
		}
		string text4 = ContactStore.Get(jsonObject, "nombre").Trim();
		string text5 = ContactStore.Get(jsonObject, "correo").Trim();
		string text6 = ContactStore.Get(jsonObject, "canal").Trim();
		if (text4.Length == 0)
		{
			Error("Es obligatorio introducir el nombre de la empresa, medio o institución.");
			return;
		}
		if (text5.Length > 0)
		{
			try
			{
				if (!string.Equals(new MailAddress(text5).Address, text5, StringComparison.OrdinalIgnoreCase) || text5.Contains(',') || text5.Contains(';'))
				{
					throw new FormatException();
				}
			}
			catch
			{
				Error("El correo no parece válido. Introduce una sola dirección de correo.");
				return;
			}
		}
		if (text6 == "email" && text5.Length == 0)
		{
			Error("El contacto tiene canal 'email': introduce una dirección, o cambia el canal.");
			return;
		}
		string text7 = ContactStore.Get(jsonObject, "fecha_ultimo_envio");
		if (text7.Length > 0 && !DateOnly.TryParseExact(text7, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var _))
		{
			Error("La fecha del último envío debe escribirse como AAAA-MM-DD, o quedar vacía.");
			return;
		}
		EditedContact = jsonObject;
		base.DialogResult = DialogResult.OK;
		Close();
	}

	private static JsonObject NewContact()
	{
		return new JsonObject
		{
			["id"] = "manual_" + Guid.NewGuid().ToString("N"),
			["nombre"] = "",
			["correo"] = "",
			["pais"] = "España",
			["region"] = "",
			["idioma"] = "es",
			["tipo"] = "prensa",
			["naturaleza"] = "empresa_medio",
			["canal"] = "email",
			["web_contacto"] = null,
			["persona_contacto"] = null,
			["uso_del_correo"] = null,
			["verificacion"] = "Pendiente de verificar",
			["estado"] = "pendiente",
			["ultima_version_comunicada"] = null,
			["fecha_ultimo_envio"] = null,
			["no_contactar"] = false,
			["notas"] = "",
			["material"] = null,
			["respuesta_seguimiento"] = null,
			["origen_csv"] = "Introducido en gestor WinForms"
		};
	}

	private void Error(string msg)
	{
		MessageBox.Show(this, UiText.Get(msg), UiText.Get("Revisa los datos"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
	}
}

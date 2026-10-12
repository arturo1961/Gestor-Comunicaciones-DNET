using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GestorComunicacionesDNET;

internal sealed class ContactStore
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		WriteIndented = true
	};

	private readonly string _directory;

	private readonly string _contactsPath;

	private readonly string _historyPath;

	private JsonObject _contactsRoot = new JsonObject();

	private JsonObject _historyRoot = new JsonObject();

	public List<JsonObject> Contacts { get; } = new List<JsonObject>();

	public IReadOnlyList<JsonObject> DeletedContacts => (_contactsRoot["contactos_eliminados"] as JsonArray)?.OfType<JsonObject>().ToList() ?? new List<JsonObject>();

	public IReadOnlyList<JsonObject> HistoryEntries => (_historyRoot["envios"] as JsonArray)?.OfType<JsonObject>().ToList() ?? new List<JsonObject>();

	public string DataDirectory => _directory;

	public ContactStore()
		: this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gestor_Comunicaciones_DNET"))
	{
	}

	internal ContactStore(string dataDirectory)
	{
		_directory = Path.GetFullPath(dataDirectory);
		Directory.CreateDirectory(_directory);
		_contactsPath = Path.Combine(_directory, "contactos.json");
		_historyPath = Path.Combine(_directory, "envios.json");
		CopySeedIfMissing("contactos.json", _contactsPath);
		CopySeedIfMissing("envios.json", _historyPath);
		Load();
	}

	private static void CopySeedIfMissing(string fileName, string destination)
	{
		if (File.Exists(destination))
		{
			return;
		}
		Assembly assembly = typeof(ContactStore).Assembly;
		string text = assembly.GetManifestResourceNames().FirstOrDefault((string name) => name.EndsWith("." + fileName, StringComparison.OrdinalIgnoreCase));
		if (text == null)
		{
			throw new FileNotFoundException("Falta el archivo inicial de datos integrado: " + fileName);
		}
		using Stream stream = assembly.GetManifestResourceStream(text) ?? throw new FileNotFoundException("No se pudo abrir el recurso integrado: " + fileName);
		using FileStream destination2 = File.Create(destination);
		stream.CopyTo(destination2);
	}

	private void Load()
	{
		_contactsRoot = (JsonNode.Parse(File.ReadAllText(_contactsPath)) as JsonObject) ?? throw new InvalidDataException("contactos.json debe contener un objeto JSON.");
		_historyRoot = (JsonNode.Parse(File.ReadAllText(_historyPath)) as JsonObject) ?? throw new InvalidDataException("envios.json debe contener un objeto JSON.");
		if (!(_contactsRoot["contactos"] is JsonArray source) || !(_historyRoot["envios"] is JsonArray))
		{
			throw new InvalidDataException("Los archivos JSON no tienen el formato esperado (contactos / envios).");
		}
		Contacts.Clear();
		Contacts.AddRange(source.OfType<JsonObject>());
	}

	public static string Get(JsonObject obj, string property)
	{
		try
		{
			return obj[property]?.ToString() ?? "";
		}
		catch
		{
			return "";
		}
	}

	public static bool Flag(JsonObject obj, string name)
	{
		bool result;
		return bool.TryParse(Get(obj, name), out result) && result;
	}

	public bool AlreadyInformed(JsonObject contact, string version)
	{
		if (version.Length > 0)
		{
			return string.Equals(Get(contact, "ultima_version_comunicada"), version, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	public void SaveContacts()
	{
		_contactsRoot["contactos"] = new JsonArray(Contacts.Select((JsonObject c) => c.DeepClone()).ToArray());
		AtomicSave(_contactsPath, _contactsRoot);
	}

	public void AddContact(JsonObject contact)
	{
		ValidateForSave(contact);
		if (Contacts.Any((JsonObject c) => Get(c, "id").Equals(Get(contact, "id"), StringComparison.OrdinalIgnoreCase)) || (_contactsRoot["contactos_eliminados"] is JsonArray source && source.OfType<JsonObject>().Any((JsonObject c) => Get(c, "id").Equals(Get(contact, "id"), StringComparison.OrdinalIgnoreCase))))
		{
			throw new InvalidDataException("El ID ya existe en contactos activos o archivados.");
		}
		if (HasDuplicateEmail(contact, null))
		{
			throw new InvalidDataException("El correo ya pertenece a otro contacto; corrígelo para evitar duplicar envíos.");
		}
		Contacts.Add((JsonObject)contact.DeepClone());
		SaveContacts();
	}

	public void UpdateContact(JsonObject original, JsonObject updated)
	{
		int num = Contacts.FindIndex((JsonObject c) => c == original);
		if (num < 0)
		{
			throw new InvalidDataException("El contacto ya no está en la lista.");
		}
		if (!string.Equals(Get(original, "id"), Get(updated, "id"), StringComparison.Ordinal))
		{
			throw new InvalidDataException("El ID no puede modificarse: lo utiliza el historial de envíos.");
		}
		ValidateForSave(updated);
		if (HasDuplicateEmail(updated, original))
		{
			throw new InvalidDataException("Esa dirección ya está asignada a otro contacto.");
		}
		Contacts[num] = (JsonObject)updated.DeepClone();
		SaveContacts();
	}

	public void DeleteContact(JsonObject contact)
	{
		DeleteContacts(new JsonObject[1] { contact });
	}

	public void DeleteContacts(IEnumerable<JsonObject> contacts)
	{
		List<JsonObject> list = contacts.Distinct().ToList();
		if (list.Count == 0)
		{
			return;
		}
		if (list.Any((JsonObject contact) => !Contacts.Contains(contact)))
		{
			throw new InvalidDataException("Uno o varios contactos ya no están en la lista.");
		}
		JsonArray jsonArray = _contactsRoot["contactos_eliminados"] as JsonArray;
		if (jsonArray == null)
		{
			jsonArray = new JsonArray();
			_contactsRoot["contactos_eliminados"] = jsonArray;
		}
		string text = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
		foreach (JsonObject item in list)
		{
			Contacts.Remove(item);
			JsonObject jsonObject = (JsonObject)item.DeepClone();
			jsonObject["fecha_eliminacion_del_gestor"] = text;
			jsonArray.Add(jsonObject);
		}
		SaveContacts();
	}

	public (int restored, int skipped) RestoreDeletedContacts(IEnumerable<JsonObject> deletedContacts)
	{
		if (_contactsRoot["contactos_eliminados"] is not JsonArray archive)
		{
			return (0, 0);
		}
		List<JsonObject> selected = deletedContacts.Distinct().ToList();
		int restored = 0;
		int skipped = 0;
		foreach (JsonObject archived in selected)
		{
			if (!archive.Contains(archived))
			{
				skipped++;
				continue;
			}
			string id = Get(archived, "id").Trim();
			string email = Get(archived, "correo").Trim();
			bool duplicateId = id.Length > 0 && Contacts.Any(c => string.Equals(Get(c, "id").Trim(), id, StringComparison.OrdinalIgnoreCase));
			bool duplicateEmail = email.Length > 0 && Contacts.Any(c => string.Equals(Get(c, "correo").Trim(), email, StringComparison.OrdinalIgnoreCase));
			if (duplicateId || duplicateEmail)
			{
				skipped++;
				continue;
			}
			JsonObject restoredContact = (JsonObject)archived.DeepClone();
			restoredContact.Remove("fecha_eliminacion_del_gestor");
			archive.Remove(archived);
			Contacts.Add(restoredContact);
			restored++;
		}
		if (restored > 0)
		{
			SaveContacts();
		}
		return (restored, skipped);
	}

	public int PermanentlyDeleteArchivedContacts(IEnumerable<JsonObject> deletedContacts)
	{
		if (_contactsRoot["contactos_eliminados"] is not JsonArray archive)
		{
			return 0;
		}
		int removed = 0;
		foreach (JsonObject contact in deletedContacts.Distinct().ToList())
		{
			if (archive.Remove(contact))
			{
				removed++;
			}
		}
		if (removed > 0)
		{
			SaveContacts();
		}
		return removed;
	}

	public IReadOnlyList<ContactIssue> FindContactIssues()
	{
		List<ContactIssue> issues = new List<ContactIssue>();
		foreach (IGrouping<string, JsonObject> group in Contacts
			.Where(c => Get(c, "correo").Trim().Length > 0)
			.GroupBy(c => Get(c, "correo").Trim(), StringComparer.OrdinalIgnoreCase)
			.Where(group => group.Count() > 1))
		{
			issues.Add(new ContactIssue("Correo repetido", string.Join(" · ", group.Select(c => Get(c, "nombre"))), group.Key));
		}
		foreach (IGrouping<string, JsonObject> group in Contacts
			.Where(c => Get(c, "nombre").Trim().Length > 0)
			.GroupBy(c => NormalizeForComparison(Get(c, "nombre")), StringComparer.OrdinalIgnoreCase)
			.Where(group => group.Key.Length > 0 && group.Count() > 1))
		{
			issues.Add(new ContactIssue("Nombre repetido", string.Join(" · ", group.Select(c => Get(c, "nombre"))), string.Join("; ", group.Select(c => Get(c, "correo")).Where(value => value.Length > 0))));
		}
		foreach (JsonObject contact in Contacts)
		{
			string email = Get(contact, "correo").Trim();
			if (email.Length > 0 && !IsValidEmail(email))
			{
				issues.Add(new ContactIssue("Correo posiblemente incorrecto", Get(contact, "nombre"), email));
			}
		}
		return issues
			.OrderBy(issue => issue.Type, StringComparer.CurrentCultureIgnoreCase)
			.ThenBy(issue => issue.Contacts, StringComparer.CurrentCultureIgnoreCase)
			.ToList();
	}

	private static string NormalizeForComparison(string value)
	{
		return new string(value.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());
	}

	private static bool IsValidEmail(string value)
	{
		try
		{
			MailAddress address = new MailAddress(value);
			return string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public void ExportContacts(string target)
	{
		string fullPath = Path.GetFullPath(target);
		if (string.Equals(fullPath, Path.GetFullPath(_contactsPath), StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Elige otra carpeta: ese ya es el archivo de datos activo.");
		}
		SaveContacts();
		File.Copy(_contactsPath, fullPath, overwrite: true);
	}

	public void CreateBackup(string target)
	{
		string fullPath = Path.GetFullPath(target);
		Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("La ruta de destino no es válida."));
		SaveContacts();
		ValidateDataFile(File.ReadAllText(_contactsPath), "contactos", "contactos.json");
		ValidateDataFile(File.ReadAllText(_historyPath), "envios", "envios.json");
		using FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
		using ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Create);
		AddTextEntry(zipArchive, "contactos.json", File.ReadAllText(_contactsPath));
		AddTextEntry(zipArchive, "envios.json", File.ReadAllText(_historyPath));
		JsonObject manifest = new JsonObject
		{
			["formato"] = "Gestor_Comunicaciones_DNET_copia",
			["version_formato"] = 1,
			["fecha_creacion"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz"),
			["incluye"] = new JsonArray("contactos.json", "envios.json")
		};
		AddTextEntry(zipArchive, "manifest.json", manifest.ToJsonString(Options));
	}

	public (string safetyBackupPath, int contacts, int shipments) RestoreBackup(string source)
	{
		string fullPath = Path.GetFullPath(source);
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException("No se encuentra la copia seleccionada.", fullPath);
		}
		string contactsText;
		string historyText;
		using (FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
		using (ZipArchive zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read))
		{
			contactsText = ReadRequiredEntry(zipArchive, "contactos.json");
			historyText = ReadRequiredEntry(zipArchive, "envios.json");
		}
		JsonObject contactsRoot = ValidateDataFile(contactsText, "contactos", "contactos.json");
		JsonObject historyRoot = ValidateDataFile(historyText, "envios", "envios.json");
		int contactCount = (contactsRoot["contactos"] as JsonArray)?.Count ?? 0;
		int shipmentCount = (historyRoot["envios"] as JsonArray)?.Count ?? 0;
		string safetyBackup = Path.Combine(_directory, "copia_automatica_antes_de_recuperar_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".zip");
		CreateBackup(safetyBackup);
		byte[] originalContacts = File.ReadAllBytes(_contactsPath);
		byte[] originalHistory = File.ReadAllBytes(_historyPath);
		try
		{
			AtomicWriteText(_contactsPath, contactsRoot.ToJsonString(Options));
			AtomicWriteText(_historyPath, historyRoot.ToJsonString(Options));
			Load();
		}
		catch
		{
			AtomicWriteBytes(_contactsPath, originalContacts);
			AtomicWriteBytes(_historyPath, originalHistory);
			Load();
			throw;
		}
		return (safetyBackup, contactCount, shipmentCount);
	}

	private static void AddTextEntry(ZipArchive archive, string name, string content)
	{
		ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
		using StreamWriter writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
		writer.Write(content);
	}

	private static string ReadRequiredEntry(ZipArchive archive, string name)
	{
		ZipArchiveEntry? entry = archive.Entries.SingleOrDefault(item => string.Equals(item.FullName, name, StringComparison.OrdinalIgnoreCase));
		if (entry == null)
		{
			throw new InvalidDataException("La copia no contiene " + name + ".");
		}
		using StreamReader reader = new StreamReader(entry.Open(), Encoding.UTF8, true);
		return reader.ReadToEnd();
	}

	private static JsonObject ValidateDataFile(string content, string arrayName, string fileName)
	{
		JsonObject root;
		try
		{
			root = JsonNode.Parse(content) as JsonObject ?? throw new InvalidDataException();
		}
		catch (Exception ex) when (ex is JsonException or InvalidDataException)
		{
			throw new InvalidDataException(fileName + " no contiene un objeto JSON válido.", ex);
		}
		if (root[arrayName] is not JsonArray)
		{
			throw new InvalidDataException(fileName + " no contiene la colección '" + arrayName + "'.");
		}
		return root;
	}

	private static void AtomicWriteText(string path, string content)
	{
		AtomicWriteBytes(path, new UTF8Encoding(false).GetBytes(content));
	}

	private static void AtomicWriteBytes(string path, byte[] content)
	{
		string temp = path + ".restore.tmp";
		try
		{
			File.WriteAllBytes(temp, content);
			File.Move(temp, path, overwrite: true);
		}
		finally
		{
			if (File.Exists(temp))
			{
				File.Delete(temp);
			}
		}
	}

	private static void ValidateForSave(JsonObject contact)
	{
		if (string.IsNullOrWhiteSpace(Get(contact, "id")))
		{
			throw new InvalidDataException("El contacto no tiene ID.");
		}
		if (string.IsNullOrWhiteSpace(Get(contact, "nombre")))
		{
			throw new InvalidDataException("Es necesario indicar el nombre del contacto.");
		}
	}

	private bool HasDuplicateEmail(JsonObject candidate, JsonObject? exclude)
	{
		string email = Get(candidate, "correo").Trim();
		if (email.Length > 0)
		{
			return Contacts.Any((JsonObject c) => c != exclude && string.Equals(Get(c, "correo").Trim(), email, StringComparison.OrdinalIgnoreCase));
		}
		return false;
	}

	public void ConfirmSend(IReadOnlyList<JsonObject> contacts, string version)
	{
		if (!(_historyRoot["envios"] is JsonArray jsonArray))
		{
			throw new InvalidDataException("Falta el historial.");
		}
		string text = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
		foreach (JsonObject contact in contacts)
		{
			contact["estado"] = "enviado_confirmado_usuario";
			contact["ultima_version_comunicada"] = version;
			contact["fecha_ultimo_envio"] = DateTime.Today.ToString("yyyy-MM-dd");
			jsonArray.Add(new JsonObject
			{
				["id_envio"] = Guid.NewGuid().ToString("N"),
				["contacto_id"] = Get(contact, "id"),
				["version"] = version,
				["fecha"] = text,
				["estado"] = "enviado_confirmado_usuario",
				["medio"] = "mailto_manual",
				["observaciones"] = "Confirmado por el usuario tras abrir borrador mailto. No constituye prueba automática de entrega."
			});
		}
		SaveContacts();
		AtomicSave(_historyPath, _historyRoot);
	}

	public (int added, int skipped) ImportContacts(string path)
	{
		JsonNode jsonNode = JsonNode.Parse(File.ReadAllText(path));
		JsonArray jsonArray = ((jsonNode is JsonArray jsonArray2) ? jsonArray2 : ((jsonNode as JsonObject)?["contactos"] as JsonArray));
		if (jsonArray == null)
		{
			throw new InvalidDataException("El JSON debe contener un array 'contactos' o ser un array.");
		}
		HashSet<string> hashSet = (from c in Contacts
			select Get(c, "id") into s
			where s.Length > 0
			select s).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (_contactsRoot["contactos_eliminados"] is JsonArray source)
		{
			foreach (JsonObject item in source.OfType<JsonObject>())
			{
				string text = Get(item, "id");
				if (text != null && text.Length > 0)
				{
					hashSet.Add(text);
				}
			}
		}
		HashSet<string> hashSet2 = (from c in Contacts
			select Get(c, "correo").Trim() into s
			where s.Length > 0
			select s).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
		int num = 0;
		int num2 = 0;
		foreach (JsonObject item2 in jsonArray.OfType<JsonObject>())
		{
			JsonObject jsonObject = (JsonObject)item2.DeepClone();
			string text2 = Get(jsonObject, "id").Trim();
			string text3 = Get(jsonObject, "correo").Trim();
			if (string.IsNullOrWhiteSpace(Get(jsonObject, "nombre")) || (text2.Length > 0 && hashSet.Contains(text2)) || (text3.Length > 0 && hashSet2.Contains(text3)))
			{
				num2++;
				continue;
			}
			if (text2.Length == 0)
			{
				text2 = "importado_" + Guid.NewGuid().ToString("N");
				jsonObject["id"] = text2;
			}
			if (!jsonObject.ContainsKey("naturaleza"))
			{
				jsonObject["naturaleza"] = "sin_clasificar";
			}
			if (!jsonObject.ContainsKey("canal"))
			{
				jsonObject["canal"] = "email";
			}
			if (!jsonObject.ContainsKey("estado"))
			{
				jsonObject["estado"] = "pendiente";
			}
			if (!jsonObject.ContainsKey("no_contactar"))
			{
				jsonObject["no_contactar"] = false;
			}
			if (!jsonObject.ContainsKey("verificacion"))
			{
				jsonObject["verificacion"] = "Importado: verificar dirección y pertinencia";
			}
			Contacts.Add(jsonObject);
			hashSet.Add(text2);
			if (text3.Length > 0)
			{
				hashSet2.Add(text3);
			}
			num++;
		}
		if (num > 0)
		{
			SaveContacts();
		}
		return (added: num, skipped: num2);
	}

	private static void AtomicSave(string path, JsonNode root)
	{
		string text = path + ".tmp";
		if (File.Exists(path))
		{
			File.Copy(path, path + ".bak", overwrite: true);
		}
		File.WriteAllText(text, root.ToJsonString(Options));
		File.Move(text, path, overwrite: true);
	}
}

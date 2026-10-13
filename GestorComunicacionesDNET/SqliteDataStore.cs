using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace GestorComunicacionesDNET;

/// <summary>
/// Almacenamiento transaccional. Cada registro conserva su JSON completo para
/// mantener compatibilidad con campos actuales y futuros.
/// </summary>
internal sealed class SqliteDataStore
{
	private const int SchemaVersion = 1;
	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
	private readonly string _databasePath;
	private readonly string _connectionString;

	public string DatabasePath => _databasePath;

	public SqliteDataStore(string directory, string contactsJsonPath, string historyJsonPath)
	{
		_databasePath = Path.Combine(directory, "gestor_comunicaciones.db");
		_connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = _databasePath,
			Mode = SqliteOpenMode.ReadWriteCreate,
			Pooling = true
		}.ToString();

		if (!File.Exists(_databasePath))
		{
			MigrateFromJson(contactsJsonPath, historyJsonPath);
		}
		ValidateDatabase();
	}

	public (JsonObject contactsRoot, JsonObject historyRoot) Load()
	{
		using SqliteConnection connection = OpenConnection();
		JsonObject contactsRoot = ParseRoot(ReadMetadata(connection, "contacts_header"));
		JsonObject historyRoot = ParseRoot(ReadMetadata(connection, "history_header"));
		JsonArray active = new();
		JsonArray deleted = new();
		JsonArray history = new();

		using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = "SELECT is_deleted, payload FROM contacts ORDER BY is_deleted, position, row_id;";
			using SqliteDataReader reader = command.ExecuteReader();
			while (reader.Read())
			{
				JsonObject item = ParseObject(reader.GetString(1));
				(reader.GetBoolean(0) ? deleted : active).Add(item);
			}
		}

		using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = "SELECT payload FROM history ORDER BY position, row_id;";
			using SqliteDataReader reader = command.ExecuteReader();
			while (reader.Read())
			{
				history.Add(ParseObject(reader.GetString(0)));
			}
		}

		contactsRoot["contactos"] = active;
		contactsRoot["contactos_eliminados"] = deleted;
		historyRoot["envios"] = history;
		return (contactsRoot, historyRoot);
	}

	public void SaveContacts(JsonObject contactsRoot)
	{
		using SqliteConnection connection = OpenConnection();
		using SqliteTransaction transaction = connection.BeginTransaction();
		ReplaceContacts(connection, transaction, contactsRoot);
		transaction.Commit();
	}

	public void SaveAll(JsonObject contactsRoot, JsonObject historyRoot)
	{
		using SqliteConnection connection = OpenConnection();
		using SqliteTransaction transaction = connection.BeginTransaction();
		ReplaceContacts(connection, transaction, contactsRoot);
		ReplaceHistory(connection, transaction, historyRoot);
		transaction.Commit();
	}

	public bool CompatibilityFilesChanged(string contactsJsonPath, string historyJsonPath)
	{
		if (!File.Exists(contactsJsonPath) || !File.Exists(historyJsonPath))
		{
			return false;
		}
		using SqliteConnection connection = OpenConnection();
		string contactsHash = ReadMetadata(connection, "contacts_json_sha256");
		string historyHash = ReadMetadata(connection, "history_json_sha256");
		return !string.Equals(contactsHash, FileHash(contactsJsonPath), StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(historyHash, FileHash(historyJsonPath), StringComparison.OrdinalIgnoreCase);
	}

	public void RecordCompatibilityFiles(string contactsJsonPath, string historyJsonPath)
	{
		using SqliteConnection connection = OpenConnection();
		using SqliteTransaction transaction = connection.BeginTransaction();
		WriteMetadata(connection, transaction, "contacts_json_sha256", FileHash(contactsJsonPath));
		WriteMetadata(connection, transaction, "history_json_sha256", FileHash(historyJsonPath));
		transaction.Commit();
	}

	private void MigrateFromJson(string contactsJsonPath, string historyJsonPath)
	{
		JsonObject contactsRoot = ParseDataFile(contactsJsonPath, "contactos");
		JsonObject historyRoot = ParseDataFile(historyJsonPath, "envios");
		string safetyBackup = CreateMigrationBackup(contactsJsonPath, historyJsonPath);
		string temporaryPath = _databasePath + ".migrating";
		if (File.Exists(temporaryPath))
		{
			File.Delete(temporaryPath);
		}

		string temporaryConnectionString = new SqliteConnectionStringBuilder
		{
			DataSource = temporaryPath,
			Mode = SqliteOpenMode.ReadWriteCreate,
			Pooling = false
		}.ToString();

		try
		{
			using (SqliteConnection connection = new(temporaryConnectionString))
			{
				connection.Open();
				CreateSchema(connection);
				using SqliteTransaction transaction = connection.BeginTransaction();
				ReplaceContacts(connection, transaction, contactsRoot);
				ReplaceHistory(connection, transaction, historyRoot);
				WriteMetadata(connection, transaction, "schema_version", SchemaVersion.ToString());
				WriteMetadata(connection, transaction, "migrated_at", DateTime.Now.ToString("O"));
				WriteMetadata(connection, transaction, "migration_backup", safetyBackup);
				WriteMetadata(connection, transaction, "contacts_json_sha256", FileHash(contactsJsonPath));
				WriteMetadata(connection, transaction, "history_json_sha256", FileHash(historyJsonPath));
				transaction.Commit();
				VerifyCounts(connection, contactsRoot, historyRoot);
				VerifyIntegrity(connection);
			}
			File.Move(temporaryPath, _databasePath);
		}
		catch
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
			throw;
		}
	}

	private SqliteConnection OpenConnection()
	{
		SqliteConnection connection = new(_connectionString);
		connection.Open();
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "PRAGMA foreign_keys=ON; PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL; PRAGMA busy_timeout=5000;";
		command.ExecuteNonQuery();
		return connection;
	}

	private void ValidateDatabase()
	{
		using SqliteConnection connection = OpenConnection();
		string version = ReadMetadata(connection, "schema_version");
		if (!int.TryParse(version, out int value) || value != SchemaVersion)
		{
			throw new InvalidDataException($"La base de datos utiliza un formato no compatible (versión {version}).");
		}
		VerifyIntegrity(connection);
	}

	private static void CreateSchema(SqliteConnection connection)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = """
			CREATE TABLE IF NOT EXISTS metadata (
				key TEXT PRIMARY KEY,
				value TEXT NOT NULL
			);
			CREATE TABLE IF NOT EXISTS contacts (
				row_id INTEGER PRIMARY KEY AUTOINCREMENT,
				is_deleted INTEGER NOT NULL CHECK (is_deleted IN (0, 1)),
				position INTEGER NOT NULL,
				contact_id TEXT,
				email TEXT,
				payload TEXT NOT NULL
			);
			CREATE INDEX IF NOT EXISTS ix_contacts_active_position ON contacts(is_deleted, position);
			CREATE INDEX IF NOT EXISTS ix_contacts_id ON contacts(contact_id COLLATE NOCASE);
			CREATE INDEX IF NOT EXISTS ix_contacts_email ON contacts(email COLLATE NOCASE);
			CREATE TABLE IF NOT EXISTS history (
				row_id INTEGER PRIMARY KEY AUTOINCREMENT,
				position INTEGER NOT NULL,
				shipment_id TEXT,
				contact_id TEXT,
				event_date TEXT,
				payload TEXT NOT NULL
			);
			CREATE INDEX IF NOT EXISTS ix_history_position ON history(position);
			CREATE INDEX IF NOT EXISTS ix_history_contact ON history(contact_id COLLATE NOCASE);
			CREATE INDEX IF NOT EXISTS ix_history_date ON history(event_date);
			""";
		command.ExecuteNonQuery();
	}

	private static void ReplaceContacts(SqliteConnection connection, SqliteTransaction transaction, JsonObject root)
	{
		JsonArray active = root["contactos"] as JsonArray ?? throw new InvalidDataException("Falta la colección 'contactos'.");
		JsonArray deleted = root["contactos_eliminados"] as JsonArray ?? new JsonArray();
		Execute(connection, transaction, "DELETE FROM contacts;");
		InsertContacts(connection, transaction, active, false);
		InsertContacts(connection, transaction, deleted, true);
		WriteMetadata(connection, transaction, "contacts_header", HeaderWithout(root, "contactos", "contactos_eliminados"));
	}

	private static void ReplaceHistory(SqliteConnection connection, SqliteTransaction transaction, JsonObject root)
	{
		JsonArray history = root["envios"] as JsonArray ?? throw new InvalidDataException("Falta la colección 'envios'.");
		Execute(connection, transaction, "DELETE FROM history;");
		using SqliteCommand command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = "INSERT INTO history(position, shipment_id, contact_id, event_date, payload) VALUES ($position, $shipment, $contact, $date, $payload);";
		SqliteParameter position = command.Parameters.Add("$position", SqliteType.Integer);
		SqliteParameter shipment = command.Parameters.Add("$shipment", SqliteType.Text);
		SqliteParameter contact = command.Parameters.Add("$contact", SqliteType.Text);
		SqliteParameter date = command.Parameters.Add("$date", SqliteType.Text);
		SqliteParameter payload = command.Parameters.Add("$payload", SqliteType.Text);
		int index = 0;
		foreach (JsonObject item in history.OfType<JsonObject>())
		{
			position.Value = index++;
			shipment.Value = Value(item, "id_envio");
			contact.Value = Value(item, "contacto_id");
			date.Value = Value(item, "fecha");
			payload.Value = item.ToJsonString();
			command.ExecuteNonQuery();
		}
		WriteMetadata(connection, transaction, "history_header", HeaderWithout(root, "envios"));
	}

	private static void InsertContacts(SqliteConnection connection, SqliteTransaction transaction, JsonArray items, bool deleted)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = "INSERT INTO contacts(is_deleted, position, contact_id, email, payload) VALUES ($deleted, $position, $id, $email, $payload);";
		SqliteParameter isDeleted = command.Parameters.Add("$deleted", SqliteType.Integer);
		SqliteParameter position = command.Parameters.Add("$position", SqliteType.Integer);
		SqliteParameter id = command.Parameters.Add("$id", SqliteType.Text);
		SqliteParameter email = command.Parameters.Add("$email", SqliteType.Text);
		SqliteParameter payload = command.Parameters.Add("$payload", SqliteType.Text);
		int index = 0;
		foreach (JsonObject item in items.OfType<JsonObject>())
		{
			isDeleted.Value = deleted ? 1 : 0;
			position.Value = index++;
			id.Value = Value(item, "id");
			email.Value = Value(item, "correo");
			payload.Value = item.ToJsonString();
			command.ExecuteNonQuery();
		}
	}

	private static string HeaderWithout(JsonObject root, params string[] collections)
	{
		JsonObject header = (JsonObject)root.DeepClone();
		foreach (string collection in collections)
		{
			header.Remove(collection);
		}
		return header.ToJsonString(JsonOptions);
	}

	private static void WriteMetadata(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = "INSERT INTO metadata(key, value) VALUES ($key, $value) ON CONFLICT(key) DO UPDATE SET value=excluded.value;";
		command.Parameters.AddWithValue("$key", key);
		command.Parameters.AddWithValue("$value", value);
		command.ExecuteNonQuery();
	}

	private static string ReadMetadata(SqliteConnection connection, string key)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "SELECT value FROM metadata WHERE key=$key;";
		command.Parameters.AddWithValue("$key", key);
		return command.ExecuteScalar()?.ToString() ?? "";
	}

	private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	private static void VerifyCounts(SqliteConnection connection, JsonObject contacts, JsonObject history)
	{
		long expectedContacts = ((contacts["contactos"] as JsonArray)?.Count ?? 0) + ((contacts["contactos_eliminados"] as JsonArray)?.Count ?? 0);
		long expectedHistory = (history["envios"] as JsonArray)?.Count ?? 0;
		long actualContacts = ScalarLong(connection, "SELECT COUNT(*) FROM contacts;");
		long actualHistory = ScalarLong(connection, "SELECT COUNT(*) FROM history;");
		if (actualContacts != expectedContacts || actualHistory != expectedHistory)
		{
			throw new InvalidDataException("La migración no superó la comprobación de registros. Los JSON originales no se han modificado.");
		}
	}

	private static void VerifyIntegrity(SqliteConnection connection)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = "PRAGMA integrity_check;";
		if (!string.Equals(command.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("La comprobación de integridad de la base de datos no ha sido correcta.");
		}
	}

	private static long ScalarLong(SqliteConnection connection, string sql)
	{
		using SqliteCommand command = connection.CreateCommand();
		command.CommandText = sql;
		return Convert.ToInt64(command.ExecuteScalar());
	}

	private static JsonObject ParseDataFile(string path, string collection)
	{
		JsonObject root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
			?? throw new InvalidDataException(Path.GetFileName(path) + " debe contener un objeto JSON.");
		if (root[collection] is not JsonArray)
		{
			throw new InvalidDataException(Path.GetFileName(path) + $" no contiene la colección '{collection}'.");
		}
		return root;
	}

	private static JsonObject ParseRoot(string json)
	{
		return string.IsNullOrWhiteSpace(json) ? new JsonObject() : ParseObject(json);
	}

	private static JsonObject ParseObject(string json)
	{
		return JsonNode.Parse(json) as JsonObject ?? throw new InvalidDataException("La base de datos contiene un registro JSON no válido.");
	}

	private static string Value(JsonObject item, string property)
	{
		try { return item[property]?.ToString() ?? ""; }
		catch { return ""; }
	}

	private static string FileHash(string path)
	{
		using FileStream stream = File.OpenRead(path);
		return Convert.ToHexString(SHA256.HashData(stream));
	}

	private static string CreateMigrationBackup(string contactsPath, string historyPath)
	{
		string directory = Path.GetDirectoryName(contactsPath) ?? throw new InvalidOperationException("La carpeta de datos no es válida.");
		string path = Path.Combine(directory, "copia_automatica_antes_de_sqlite_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".zip");
		using FileStream stream = new(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
		using ZipArchive archive = new(stream, ZipArchiveMode.Create);
		archive.CreateEntryFromFile(contactsPath, "contactos.json", CompressionLevel.Optimal);
		archive.CreateEntryFromFile(historyPath, "envios.json", CompressionLevel.Optimal);
		JsonObject manifest = new()
		{
			["formato"] = "Gestor_Comunicaciones_DNET_pre_sqlite",
			["fecha_creacion"] = DateTime.Now.ToString("O"),
			["contactos_sha256"] = FileHash(contactsPath),
			["envios_sha256"] = FileHash(historyPath)
		};
		ZipArchiveEntry entry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
		using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false));
		writer.Write(manifest.ToJsonString(JsonOptions));
		return path;
	}
}

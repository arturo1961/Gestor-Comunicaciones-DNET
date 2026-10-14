using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.VisualBasic.FileIO;

namespace GestorComunicacionesDNET;

internal static class ContactImportReader
{
	internal sealed record ReadResult(List<JsonObject> Contacts, int SourceRows, string Format);

	public static ReadResult Read(string path)
	{
		string extension = Path.GetExtension(path).ToLowerInvariant();
		return extension switch
		{
			".json" => ReadJson(path),
			".csv" => ReadCsv(path),
			".vcf" or ".vcard" => ReadVCard(path),
			_ => throw new InvalidDataException("Formato no compatible. Elige un archivo JSON, CSV o vCard (VCF).")
		};
	}

	private static ReadResult ReadJson(string path)
	{
		JsonNode? root = JsonNode.Parse(File.ReadAllText(path));
		JsonArray? array = root is JsonArray direct ? direct : (root as JsonObject)?["contactos"] as JsonArray;
		if (array == null)
		{
			throw new InvalidDataException("El JSON debe contener un array 'contactos' o ser un array.");
		}

		List<JsonObject> contacts = array.OfType<JsonObject>().Select(item => (JsonObject)item.DeepClone()).ToList();
		return new ReadResult(contacts, array.Count, "JSON");
	}

	private static ReadResult ReadCsv(string path)
	{
		using TextFieldParser parser = new TextFieldParser(path, Encoding.UTF8, detectEncoding: true)
		{
			TextFieldType = FieldType.Delimited,
			HasFieldsEnclosedInQuotes = true,
			TrimWhiteSpace = true
		};
		string firstLine = File.ReadLines(path).FirstOrDefault() ?? throw new InvalidDataException("El archivo CSV está vacío.");
		parser.SetDelimiters(DetectDelimiter(firstLine));
		string[] headers = parser.ReadFields() ?? throw new InvalidDataException("El archivo CSV no contiene cabeceras.");
		List<JsonObject> contacts = new List<JsonObject>();
		int sourceRows = 0;
		while (!parser.EndOfData)
		{
			string[]? values;
			try
			{
				values = parser.ReadFields();
			}
			catch (MalformedLineException ex)
			{
				throw new InvalidDataException($"No se pudo leer la línea {ex.Message} del CSV.", ex);
			}
			if (values == null || values.All(string.IsNullOrWhiteSpace)) continue;
			sourceRows++;
			contacts.Add(FromCsv(headers, values));
		}
		return new ReadResult(contacts, sourceRows, "CSV");
	}

	private static string DetectDelimiter(string header)
	{
		char[] candidates = new[] { ',', ';', '\t' };
		return candidates.OrderByDescending(delimiter => header.Count(character => character == delimiter)).First().ToString();
	}

	private static JsonObject FromCsv(string[] headers, string[] values)
	{
		string name = Value(headers, values, "name", "nombre", "display name", "nombre para mostrar", "full name", "nombre completo");
		string given = Value(headers, values, "given name", "first name", "nombre de pila", "nombre");
		string family = Value(headers, values, "family name", "last name", "apellidos", "apellido");
		string organization = Value(headers, values, "organization name", "company", "company name", "empresa", "nombre de la empresa", "nombre de la compania", "nombre de la compañía", "organizacion", "organización", "organizacion 1 nombre", "organización 1 nombre");
		string email = Value(headers, values, "e-mail 1 - value", "email 1 value", "e-mail address", "email address", "correo electronico 1 valor", "correo electrónico 1 valor", "direccion de correo electronico", "dirección de correo electrónico", "correo electronico", "correo electrónico", "correo", "email");
		if (name.Length == 0) name = JoinNonEmpty(given, family);
		if (name.Length == 0) name = organization;
		string country = Value(headers, values, "address 1 - country", "address 1 country", "direccion 1 pais", "dirección 1 país", "country/region", "country", "pais o region", "país o región", "pais", "país");
		string region = Value(headers, values, "address 1 - region", "address 1 region", "direccion 1 region", "dirección 1 región", "state", "province", "state/province", "provincia o estado", "region", "región", "provincia");
		string notes = Value(headers, values, "notes", "note", "notas", "comentarios");
		string website = Value(headers, values, "website 1 - value", "web page", "website", "pagina web", "página web", "sitio web");
		string phone = Value(headers, values, "phone 1 - value", "business phone", "mobile phone", "telefono", "teléfono", "movil", "móvil");
		string language = Value(headers, values, "language", "idioma");
		return NewContact(name, email, organization, country, region, language, website, phone, notes, "Importado desde CSV");
	}

	private static string Value(string[] headers, string[] values, params string[] aliases)
	{
		HashSet<string> normalizedAliases = aliases.Select(NormalizeHeader).ToHashSet(StringComparer.Ordinal);
		for (int index = 0; index < headers.Length && index < values.Length; index++)
		{
			if (normalizedAliases.Contains(NormalizeHeader(headers[index])))
			{
				return values[index].Trim();
			}
		}
		return "";
	}

	private static string NormalizeHeader(string value)
	{
		string decomposed = value.Normalize(NormalizationForm.FormD);
		StringBuilder builder = new StringBuilder();
		foreach (char character in decomposed)
		{
			UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
			if (category != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(character))
			{
				builder.Append(char.ToLowerInvariant(character));
			}
		}
		return builder.ToString();
	}

	private static ReadResult ReadVCard(string path)
	{
		List<string> unfolded = new List<string>();
		foreach (string rawLine in File.ReadAllLines(path))
		{
			if ((rawLine.StartsWith(' ') || rawLine.StartsWith('\t')) && unfolded.Count > 0)
			{
				unfolded[^1] += rawLine[1..];
			}
			else
			{
				unfolded.Add(rawLine);
			}
		}

		List<JsonObject> contacts = new List<JsonObject>();
		Dictionary<string, List<string>>? fields = null;
		int sourceRows = 0;
		foreach (string line in unfolded)
		{
			if (line.Equals("BEGIN:VCARD", StringComparison.OrdinalIgnoreCase))
			{
				fields = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
				continue;
			}
			if (line.Equals("END:VCARD", StringComparison.OrdinalIgnoreCase))
			{
				if (fields != null)
				{
					sourceRows++;
					contacts.Add(FromVCard(fields));
				}
				fields = null;
				continue;
			}
			if (fields == null) continue;
			int colon = line.IndexOf(':');
			if (colon <= 0) continue;
			string keyPart = line[..colon];
			string key = keyPart.Split(';')[0];
			if (key.Contains('.')) key = key[(key.LastIndexOf('.') + 1)..];
			if (!fields.TryGetValue(key, out List<string>? entries))
			{
				entries = new List<string>();
				fields[key] = entries;
			}
			entries.Add(UnescapeVCard(line[(colon + 1)..]));
		}
		if (sourceRows == 0) throw new InvalidDataException("El archivo no contiene contactos vCard reconocibles.");
		return new ReadResult(contacts, sourceRows, "vCard");
	}

	private static JsonObject FromVCard(Dictionary<string, List<string>> fields)
	{
		string name = First(fields, "FN");
		if (name.Length == 0)
		{
			string[] parts = First(fields, "N").Split(';');
			name = JoinNonEmpty(parts.ElementAtOrDefault(1) ?? "", parts.ElementAtOrDefault(0) ?? "");
		}
		string organization = First(fields, "ORG").Replace(';', ' ').Trim();
		if (name.Length == 0) name = organization;
		string[] address = First(fields, "ADR").Split(';');
		string region = address.ElementAtOrDefault(4)?.Trim() ?? "";
		string country = address.ElementAtOrDefault(6)?.Trim() ?? "";
		return NewContact(name, First(fields, "EMAIL"), organization, country, region, First(fields, "LANG"), First(fields, "URL"), First(fields, "TEL"), First(fields, "NOTE"), "Importado desde vCard");
	}

	private static string First(Dictionary<string, List<string>> fields, string key)
	{
		return fields.TryGetValue(key, out List<string>? values) ? values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "" : "";
	}

	private static string UnescapeVCard(string value)
	{
		return value.Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase).Replace("\\,", ",").Replace("\\;", ";").Replace("\\\\", "\\").Trim();
	}

	private static string JoinNonEmpty(params string[] values)
	{
		return string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
	}

	private static JsonObject NewContact(string name, string email, string organization, string country, string region, string language, string website, string phone, string notes, string source)
	{
		email = email.Trim();
		List<string> importedNotes = new List<string>();
		if (notes.Trim().Length > 0) importedNotes.Add(notes.Trim());
		if (organization.Trim().Length > 0) importedNotes.Add("Organización original: " + organization.Trim());
		if (phone.Trim().Length > 0) importedNotes.Add("Teléfono: " + phone.Trim());
		return new JsonObject
		{
			["id"] = "importado_" + Guid.NewGuid().ToString("N"),
			["nombre"] = name.Trim(),
			["correo"] = email,
			["pais"] = country.Trim(),
			["region"] = region.Trim(),
			["idioma"] = language.Trim(),
			["tipo"] = "otros",
			["naturaleza"] = "sin_clasificar",
			["canal"] = email.Length > 0 ? "email" : "otro",
			["web_contacto"] = NullIfEmpty(website),
			["persona_contacto"] = null,
			["uso_del_correo"] = null,
			["verificacion"] = "Importado: verificar dirección y pertinencia",
			["estado"] = "pendiente",
			["ultima_version_comunicada"] = null,
			["fecha_ultimo_envio"] = null,
			["no_contactar"] = false,
			["notas"] = string.Join("\n", importedNotes),
			["material"] = null,
			["respuesta_seguimiento"] = null,
			["origen_csv"] = source
		};
	}

	private static JsonNode? NullIfEmpty(string value)
	{
		return value.Trim().Length == 0 ? null : JsonValue.Create(value.Trim());
	}
}

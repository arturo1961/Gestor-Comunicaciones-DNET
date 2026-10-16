using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GestorComunicacionesDNET;

internal sealed class CampaignQueueItem
{
	public string ContactId { get; set; } = "";
	public string Status { get; set; } = "pending";
	public string Note { get; set; } = "";
}

internal sealed class CampaignState
{
	public string Subject { get; set; } = "";
	public string Body { get; set; } = "";
	public string Version { get; set; } = "";
	public List<string> Attachments { get; set; } = new();
	public List<CampaignQueueItem> Queue { get; set; } = new();
	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
	public DateTimeOffset? CooldownUntil { get; set; }
	public bool Paused { get; set; }
}

internal sealed class CampaignStateStore
{
	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
	private readonly string _path;

	public string Path => _path;

	public CampaignStateStore(string dataDirectory)
	{
		_path = System.IO.Path.Combine(dataDirectory, "campana_actual.json");
	}

	public CampaignState Load()
	{
		if (!File.Exists(_path)) return new CampaignState();
		try
		{
			return JsonSerializer.Deserialize<CampaignState>(File.ReadAllText(_path), Options) ?? new CampaignState();
		}
		catch (JsonException ex)
		{
			throw new InvalidDataException("No se pudo leer la campaña guardada. El archivo no se ha modificado.", ex);
		}
	}

	public void Save(CampaignState state)
	{
		Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
		string temporary = _path + ".tmp";
		try
		{
			File.WriteAllText(temporary, JsonSerializer.Serialize(state, Options));
			File.Move(temporary, _path, true);
		}
		finally
		{
			if (File.Exists(temporary)) File.Delete(temporary);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal sealed record LanguageOption(string Code, string DisplayName, bool RightToLeft, int SortOrder)
{
	public override string ToString() => DisplayName;
}

internal sealed class LanguagePack
{
	public string Code { get; set; } = "";
	public string DisplayName { get; set; } = "";
	public bool RightToLeft { get; set; }
	public int SortOrder { get; set; }
	public Dictionary<string, string> Texts { get; set; } = new Dictionary<string, string>();
}

internal static class UiText
{
	private sealed class OriginalText { public required string Value { get; init; } }
	private static readonly ConditionalWeakTable<object, OriginalText> Originals = new ConditionalWeakTable<object, OriginalText>();
	private static readonly ConditionalWeakTable<TextBox, OriginalText> OriginalPlaceholders = new ConditionalWeakTable<TextBox, OriginalText>();
	private static readonly Dictionary<string, string> Spanish = LoadSpanish();
	private static readonly Dictionary<string, LanguagePack> Packs = LoadPacks();
	private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gestor_Comunicaciones_DNET", "preferencias.json");

	public static IReadOnlyList<LanguageOption> Languages { get; } = Packs.Values
		.Select(pack => new LanguageOption(pack.Code, pack.DisplayName, pack.RightToLeft, pack.SortOrder))
		.Prepend(new LanguageOption("es", "Español", false, 0))
		.OrderBy(option => option.SortOrder)
		.ThenBy(option => option.DisplayName, StringComparer.CurrentCultureIgnoreCase)
		.ToList();

	public static string CurrentLanguage { get; private set; } = "es";

	public static void Initialize()
	{
		string? saved = null;
		try
		{
			if (File.Exists(SettingsPath))
			{
				saved = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsPath))?.GetValueOrDefault("idioma");
			}
		}
		catch { }
		SetLanguage(saved ?? DetectWindowsLanguage(), save: false);
	}

	public static void SetLanguage(string code, bool save = true)
	{
		CurrentLanguage = code == "es" || Packs.ContainsKey(code) ? code : "es";
		if (!save) return;
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
			File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Dictionary<string, string> { ["idioma"] = CurrentLanguage }, new JsonSerializerOptions { WriteIndented = true }));
		}
		catch { }
	}

	public static string Get(string spanish)
	{
		if (CurrentLanguage == "es") return spanish;
		return Packs.TryGetValue(CurrentLanguage, out LanguagePack? pack) && pack.Texts.TryGetValue(spanish, out string? translated) && !string.IsNullOrWhiteSpace(translated)
			? translated : spanish;
	}

	public static string Format(string spanish, params object[] values) => string.Format(CultureInfo.CurrentCulture, Get(spanish), values);

	public static void Apply(Form form)
	{
		TranslateObject(form, () => form.Text, value => form.Text = value);
		ApplyControls(form);
		form.RightToLeft = RightToLeft.No;
		form.RightToLeftLayout = false;
	}

	public static void ApplyAllOpenForms()
	{
		foreach (Form form in Application.OpenForms.Cast<Form>().ToArray()) Apply(form);
	}

	private static void ApplyControls(Control parent)
	{
		foreach (Control control in parent.Controls)
		{
			TranslateObject(control, () => control.Text, value => control.Text = value);
			if (control is TextBox textBox && textBox.PlaceholderText.Length > 0)
			{
				if (!OriginalPlaceholders.TryGetValue(textBox, out OriginalText? placeholder))
				{
					placeholder = new OriginalText { Value = textBox.PlaceholderText };
					OriginalPlaceholders.Add(textBox, placeholder);
				}
				textBox.PlaceholderText = Get(placeholder.Value);
			}
			if (control is MenuStrip menu) foreach (ToolStripItem item in menu.Items) ApplyMenuItem(item);
			if (control is DataGridView grid) foreach (DataGridViewColumn column in grid.Columns)
				TranslateObject(column, () => column.HeaderText, value => column.HeaderText = value);
			ApplyControls(control);
		}
	}

	private static void ApplyMenuItem(ToolStripItem item)
	{
		TranslateObject(item, () => item.Text, value => item.Text = value);
		if (item is ToolStripDropDownItem dropDown) foreach (ToolStripItem child in dropDown.DropDownItems) ApplyMenuItem(child);
	}

	private static void TranslateObject(object target, Func<string> getter, Action<string> setter)
	{
		if (!Originals.TryGetValue(target, out OriginalText? original))
		{
			string source = getter();
			if (source.Length == 0) return;
			original = new OriginalText { Value = source };
			Originals.Add(target, original);
		}
		setter(Get(original.Value));
	}

	private static string DetectWindowsLanguage()
	{
		string code = CultureInfo.CurrentUICulture.Name;
		if (Packs.ContainsKey(code)) return code;
		string neutral = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
		if (neutral == "zh") return code.Contains("Hant", StringComparison.OrdinalIgnoreCase) || code.EndsWith("TW", StringComparison.OrdinalIgnoreCase) ? "zh-Hant" : "zh-Hans";
		return Packs.ContainsKey(neutral) ? neutral : "es";
	}

	private static Dictionary<string, string> LoadSpanish()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GestorComunicacionesDNET.SpanishTexts.json") ?? throw new InvalidDataException("Falta SpanishTexts.json.");
		return JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
	}

	private static Dictionary<string, LanguagePack> LoadPacks()
	{
		Dictionary<string, LanguagePack> result = new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);
		Assembly assembly = Assembly.GetExecutingAssembly();
		foreach (string resource in assembly.GetManifestResourceNames().Where(name => name.Contains(".LanguagePacks.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
		{
			try
			{
				using Stream? stream = assembly.GetManifestResourceStream(resource);
				LanguagePack? pack = stream == null ? null : JsonSerializer.Deserialize<LanguagePack>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (pack != null && pack.Code.Length > 0 && pack.DisplayName.Length > 0 && pack.Texts.Count == Spanish.Count && Spanish.Keys.All(pack.Texts.ContainsKey)) result[pack.Code] = pack;
			}
			catch { }
		}
		return result;
	}
}

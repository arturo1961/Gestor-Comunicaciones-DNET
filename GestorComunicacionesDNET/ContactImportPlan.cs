using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace GestorComunicacionesDNET;

internal sealed record ContactImportPlan(
	IReadOnlyList<JsonObject> Contacts,
	int SourceRows,
	int SkippedMissingName,
	int SkippedDuplicate,
	string Format);

internal sealed record ContactImportResult(int Added, int Skipped, string SafetyBackupPath);

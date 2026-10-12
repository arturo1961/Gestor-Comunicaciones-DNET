using System.Text.Json.Nodes;

namespace GestorComunicacionesDNET;

internal sealed class ContactRow
{
	public required JsonObject Data { get; init; }

	public bool Elegido { get; set; }

	public string Nombre => ContactStore.Get(Data, "nombre");

	public string Correo => ContactStore.Get(Data, "correo");

	public string Pais => ContactStore.Get(Data, "pais");

	public string Region => ContactStore.Get(Data, "region");

	public string Idioma => ContactStore.Get(Data, "idioma");

	public string Clase => ContactStore.Get(Data, "naturaleza");

	public string Tipo => ContactStore.Get(Data, "tipo");

	public string UltimaVersion => ContactStore.Get(Data, "ultima_version_comunicada");

	public string Estado => ContactStore.Get(Data, "estado");
}

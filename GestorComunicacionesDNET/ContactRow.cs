using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace GestorComunicacionesDNET;

internal sealed class ContactRow : INotifyPropertyChanged
{
	private bool _elegido;

	public required JsonObject Data { get; init; }

	public bool Elegido
	{
		get => _elegido;
		set
		{
			if (_elegido == value)
			{
				return;
			}

			_elegido = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Nombre => ContactStore.Get(Data, "nombre");

	public string Correo => ContactStore.Get(Data, "correo");

	public string Pais => ContactStore.Get(Data, "pais");

	public string Region => ContactStore.Get(Data, "region");

	public string Idioma => ContactStore.Get(Data, "idioma");

	public string Clase => ContactStore.Get(Data, "naturaleza");

	public string Tipo => ContactStore.Get(Data, "tipo");

	public string UltimaVersion => ContactStore.Get(Data, "ultima_version_comunicada");

	public string Estado => ContactStore.Get(Data, "estado");

	public string NoContactar => ContactStore.MarkedNoContact(Data) ? "🔴" : "";

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

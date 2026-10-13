using System;
using System.IO;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		ApplicationConfiguration.Initialize();
		UiText.Initialize();
		try
		{
			Application.Run(new MainForm());
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
		{
			MessageBox.Show(
				"No se pudo abrir el almacén de datos. No se ha borrado ningún contacto ni historial.\n\n" +
				"Conserva la carpeta Gestor_Comunicaciones_DNET y la copia automática antes de intentar una reparación.\n\n" + ex.Message,
				"Gestor de Comunicaciones — datos protegidos",
				MessageBoxButtons.OK,
				MessageBoxIcon.Error);
		}
	}
}

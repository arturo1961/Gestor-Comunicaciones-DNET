using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GestorComunicacionesDNET;

internal static class EmailDraftLauncher
{
	public static bool Open(string recipient, string subject, string body, IReadOnlyList<string> attachments, out string detail)
	{
		foreach (string attachment in attachments)
		{
			if (!File.Exists(attachment))
			{
				detail = UiText.Format("No se encuentra el archivo adjunto:\n{0}", attachment);
				return false;
			}
		}

		// Sin adjuntos, mailto respeta siempre la aplicación y la cuenta predeterminadas de Windows.
		// Con adjuntos se intenta Outlook de escritorio, que usa el perfil activo del usuario.
		try
		{
			Type? outlookType = attachments.Count > 0 ? Type.GetTypeFromProgID("Outlook.Application") : null;
			if (outlookType is not null)
			{
				dynamic outlook = Activator.CreateInstance(outlookType)!;
				dynamic message = outlook.CreateItem(0);
				message.To = recipient;
				message.Subject = subject;
				message.Body = body;
				foreach (string attachment in attachments) message.Attachments.Add(attachment);
				message.Display(false);
				detail = UiText.Get("Borrador completo abierto en Outlook. Nada se ha enviado todavía.");
				return true;
			}
		}
		catch
		{
			// Outlook clásico no está disponible: se intenta la aplicación predeterminada.
		}

		try
		{
			string uri = "mailto:" + Uri.EscapeDataString(recipient)
				+ "?subject=" + Uri.EscapeDataString(subject)
				+ "&body=" + Uri.EscapeDataString(body);
			Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
			detail = attachments.Count == 0
				? UiText.Get("Borrador abierto en la aplicación de correo predeterminada. Nada se ha enviado todavía.")
				: UiText.Get("El borrador se ha abierto, pero esta aplicación de correo no admite añadir archivos automáticamente. Añade manualmente los adjuntos indicados.");
			return true;
		}
		catch (Exception ex)
		{
			detail = UiText.Get("No se pudo abrir la aplicación de correo.") + "\n\n" + ex.Message;
			return false;
		}
	}
}

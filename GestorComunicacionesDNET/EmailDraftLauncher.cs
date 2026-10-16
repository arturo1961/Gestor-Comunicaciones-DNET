using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace GestorComunicacionesDNET;

internal static class EmailDraftLauncher
{
	public static bool Open(string recipient, string subject, string body, IReadOnlyList<string> attachments, out string detail)
	{
		// Los textos pegados o recuperados de JSON pueden contener saltos LF o CR.
		// Outlook y algunos controladores mailto de Windows sólo conservan de forma
		// fiable los párrafos cuando el cuerpo utiliza saltos CRLF.
		string normalizedBody = NormalizeLineEndings(body);

		foreach (string attachment in attachments)
		{
			if (!File.Exists(attachment))
			{
				detail = UiText.Format("No se encuentra el archivo adjunto:\n{0}", attachment);
				return false;
			}
		}

		// Sin adjuntos, mailto respeta siempre la aplicación y la cuenta predeterminadas de Windows.
		// Con adjuntos sólo se usa la automatización COM si Outlook clásico es realmente el
		// controlador predeterminado. Así evitamos que una instalación residual de Office 16
		// abra su asistente de configuración cuando el usuario utiliza el nuevo Outlook.
		try
		{
			Type? outlookType = attachments.Count > 0 && IsClassicOutlookDefault()
				? Type.GetTypeFromProgID("Outlook.Application")
				: null;
			if (outlookType is not null)
			{
				dynamic outlook = Activator.CreateInstance(outlookType)!;
				dynamic message = outlook.CreateItem(0);
				message.To = recipient;
				message.Subject = subject;
				message.Body = normalizedBody;
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
				+ "&body=" + Uri.EscapeDataString(normalizedBody);
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

	private static bool IsClassicOutlookDefault()
	{
		try
		{
			const string userChoice = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\Shell\Associations\UrlAssociations\mailto\UserChoice";
			string? progId = Registry.GetValue(userChoice, "ProgId", null) as string;
			return !string.IsNullOrWhiteSpace(progId)
				&& !progId.StartsWith("AppX", StringComparison.OrdinalIgnoreCase)
				&& progId.Contains("Outlook", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	internal static string NormalizeLineEndings(string text)
	{
		return text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n");
	}
}

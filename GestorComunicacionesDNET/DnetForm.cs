using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace GestorComunicacionesDNET;

internal class DnetForm : Form
{
	protected DnetForm()
	{
		using Stream? stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream("GestorComunicacionesDNET.AppIcon.ico");
		if (stream is null) return;
		using var source = new Icon(stream);
		Icon = (Icon)source.Clone();
	}
}

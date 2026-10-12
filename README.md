# Gestor de Comunicaciones DNET

Aplicación personal para Windows que permite organizar contactos, filtrarlos y preparar un correo nuevo con los destinatarios seleccionados en el campo CCO.

El programa no envía mensajes automáticamente, no almacena contraseñas y no incorpora asuntos ni textos privados. El usuario revisa y completa personalmente cada correo en su aplicación de correo predeterminada.

## Funciones

- Alta, modificación y eliminación controlada de contactos.
- Filtros por país, región, idioma, tipo, organización y estado.
- Búsqueda instantánea por nombre, correo, ubicación, organización, estado o notas.
- Selección de destinatarios y copia de direcciones en CCO.
- Apertura de un borrador mediante `mailto`.
- Registro manual y consulta visual del historial de comunicaciones.
- Papelera para recuperar contactos eliminados o borrarlos definitivamente.
- Revisión de posibles contactos repetidos y correos incorrectos.
- Importación y exportación de contactos en JSON.
- Copia de seguridad completa de contactos e historial en ZIP.
- Recuperación validada con copia automática previa del estado actual.

## Privacidad de los datos

Los archivos incluidos en este repositorio están vacíos. En el primer arranque se copian a:

`%LOCALAPPDATA%\Gestor_Comunicaciones_DNET\`

Los contactos y el historial reales permanecen fuera del repositorio. No publiques los archivos de esa carpeta ni las copias ZIP que genere la aplicación.

## Pequeño manual de uso

1. Abre el programa y añade tus contactos con **Nuevo contacto**. También puedes importar contactos desde un archivo JSON.
2. Utiliza el buscador o los filtros para mostrar únicamente los contactos que necesites.
3. Marca las casillas de los destinatarios. **Marcar visibles** selecciona todos los contactos mostrados que pueden recibir correo.
4. Pulsa **Copiar CCO** para copiar las direcciones o **Preparar con mailto** para abrir un correo nuevo con los destinatarios colocados en CCO.
5. Escribe personalmente el asunto y el mensaje, añade los archivos necesarios y revisa el campo CCO antes de enviar.
6. Después de realizar el envío, vuelve al gestor y pulsa **Confirmar envío REAL** para anotarlo en el historial.
7. En **Herramientas** puedes abrir la papelera, consultar el historial y revisar posibles duplicados o correos incorrectos.
8. Utiliza **Copias de seguridad > Hacer copia de seguridad** para guardar juntos los contactos y el historial en un ZIP.
9. Para volver a un estado anterior, utiliza **Recuperar copia de seguridad**. Antes de sustituir los datos, el programa conserva automáticamente otra copia del estado actual.

## Requisitos para compilar

- Windows 10 u 11 de 64 bits.
- SDK de .NET 8.

Compilación normal:

```powershell
dotnet build GestorComunicacionesDNET.csproj -c Release
```

Ejecutable autónomo de Windows x64:

```powershell
dotnet publish GestorComunicacionesDNET.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -p:DebugSymbols=false -o publicar
```

## Uso responsable

Utiliza siempre CCO para grupos de destinatarios, respeta las solicitudes de baja y comprueba que existe una base legítima para cada comunicación. Esta aplicación no evita los límites ni las políticas del proveedor de correo.

## Aportación voluntaria

El programa es gratuito y de código abierto. Si te resulta útil, puedes apoyar voluntariamente su mantenimiento y evolución mediante los canales de aportación que publique el autor. La aportación no es obligatoria, no desbloquea funciones y no modifica los derechos concedidos por la licencia MIT.

También puedes colaborar comunicando errores, proponiendo mejoras o difundiendo el proyecto.

## Licencia

Distribuido bajo la licencia MIT. Consulta [LICENSE](LICENSE).


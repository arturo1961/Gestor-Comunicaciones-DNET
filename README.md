# Gestor de Comunicaciones DNET

Aplicación personal para Windows que permite organizar contactos, filtrarlos y preparar un correo nuevo con los destinatarios seleccionados en el campo CCO.

La versión 1.6.1 incluye **62 idiomas** en un único ejecutable. El idioma se cambia desde el menú superior y queda guardado para el siguiente inicio.

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
- Interfaz, editor, avisos, ayuda y operaciones principales traducidos a 62 idiomas.
- Detección inicial del idioma de Windows y cambio sin reiniciar.
- Importación y exportación de contactos en JSON.
- Copia de seguridad completa de contactos e historial en ZIP.
- Recuperación validada con copia automática previa del estado actual.

## Privacidad de los datos

Los archivos incluidos en este repositorio están vacíos. En el primer arranque se copian a:

`%LOCALAPPDATA%\Gestor_Comunicaciones_DNET\`

Los contactos y el historial reales permanecen fuera del repositorio. No publiques los archivos de esa carpeta ni las copias ZIP que genere la aplicación.

## Pequeño manual de uso

1. Abre el programa y añade tus contactos con **Nuevo contacto**. También puedes importar contactos desde un archivo JSON.
2. Si lo necesitas, cambia la interfaz desde el menú **Idioma**. La elección se conservará.
3. Utiliza el buscador o los filtros para mostrar únicamente los contactos que necesites.
4. Marca las casillas de los destinatarios. **Marcar visibles** selecciona todos los contactos mostrados que pueden recibir correo.
5. Pulsa **Copiar CCO** para copiar las direcciones o **Preparar con mailto** para abrir un correo nuevo con los destinatarios colocados en CCO.
6. Escribe personalmente el asunto y el mensaje, añade los archivos necesarios y revisa el campo CCO antes de enviar.
7. Después de realizar el envío, vuelve al gestor y pulsa **Confirmar envío REAL** para anotarlo en el historial.
8. En **Herramientas** puedes abrir la papelera, consultar el historial y revisar posibles duplicados o correos incorrectos.
9. Utiliza **Copias de seguridad > Hacer copia de seguridad** para guardar juntos los contactos y el historial en un ZIP.
10. Para volver a un estado anterior, utiliza **Recuperar copia de seguridad**. Antes de sustituir los datos, el programa conserva automáticamente otra copia del estado actual.

La lista completa está en [IDIOMAS.md](IDIOMAS.md). Las traducciones iniciales se han generado con ayuda automática y se aceptan correcciones de hablantes nativos.

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

**EL GESTOR DE COMUNICACIONES DNET ES GRATUITO Y LO SEGUIRÁ SIENDO. DONAR O NO DONAR ES DECISIÓN EXCLUSIVA DE CADA PERSONA.** ❤️

Si el programa te resulta útil y quieres apoyar voluntariamente su mantenimiento y evolución, puedes hacerlo por cualquiera de estos medios:

- [PayPal: paypal.me/acarballoflandes](https://paypal.me/acarballoflandes)
- **Bizum:** `+34 662 024 169`
- [GitHub Sponsors de arturo1961](https://github.com/sponsors/arturo1961), cuando el perfil esté disponible.

Donar no desbloquea funciones, no ofrece ventajas especiales y no modifica la licencia MIT ni las condiciones del programa. Consulta los detalles en [DONACIONES.md](DONACIONES.md).

También puedes colaborar comunicando errores, proponiendo mejoras o difundiendo el proyecto.

## Licencia

Distribuido bajo la licencia MIT. Consulta [LICENSE](LICENSE).


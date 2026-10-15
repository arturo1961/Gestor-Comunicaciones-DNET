# Gestor de Comunicaciones DNET

[Español](README.md) | [简体中文](README.zh-CN.md)

Aplicación personal para Windows que permite organizar contactos, filtrarlos y preparar un correo nuevo con los destinatarios seleccionados en el campo CCO.

La versión 1.9.1 incluye **62 idiomas** en un único ejecutable, también para las ventanas de importación, creación de PDF y la función **NO CONTACTAR**. Utiliza una base de datos SQLite para trabajar con listas grandes y permite importar contactos de Google, Outlook y otras agendas mediante CSV o vCard, sin pedir contraseñas. Los filtros de país y región están relacionados: al elegir un país sólo aparecen sus regiones. También crea informes PDF con los contactos visibles o marcados y las columnas elegidas. El idioma se cambia desde el menú superior y queda guardado para el siguiente inicio.

El programa no envía mensajes automáticamente, no almacena contraseñas y no incorpora asuntos ni textos privados. El usuario revisa y completa personalmente cada correo en su aplicación de correo predeterminada.

La descarga ofrece un ejecutable portátil y un instalador para Windows. El instalador detecta automáticamente el idioma visual de Windows, utiliza la traducción compatible correspondiente y conserva la elección en futuras actualizaciones. Si no existe una coincidencia, permite escoger el idioma antes de instalar.

## Funciones

- Alta, modificación y eliminación controlada de contactos.
- Filtros por país, región, idioma, tipo, organización y estado.
- Informes PDF de la lista filtrada o de los contactos marcados, con selección de columnas.
- Búsqueda instantánea por nombre, correo, ubicación, organización, estado o notas.
- Selección de destinatarios y copia de direcciones en CCO.
- Apertura de un borrador mediante `mailto`.
- Registro manual y consulta visual del historial de comunicaciones.
- Papelera para recuperar contactos eliminados o borrarlos definitivamente.
- Revisión de posibles contactos repetidos y correos incorrectos.
- Lista **NO CONTACTAR**, identificada con un punto rojo, que impide seleccionar, preparar o reimportar direcciones excluidas.
- Interfaz, editor, avisos, ayuda y operaciones principales traducidos a 62 idiomas.
- Detección inicial del idioma de Windows y cambio sin reiniciar.
- Importación de contactos desde Google/Outlook CSV, vCard (VCF) y JSON, con vista previa, detección de repetidos y copia automática previa. Sólo se conservan las dos copias automáticas de importación más recientes.
- Exportación de contactos en JSON.
- Copia de seguridad completa de contactos e historial en ZIP.
- Recuperación validada con copia automática previa del estado actual.
- Migración automática de los datos existentes a SQLite, con comprobación de registros e integridad.
- JSON de compatibilidad para poder volver a una versión anterior si fuera necesario.
- Recuperación automática de los cambios realizados al volver temporalmente a una versión anterior.

La guía [INFORMES_PDF.md](INFORMES_PDF.md) explica paso a paso cómo filtrar la tabla y crear un informe.

Consulta [IMPORTAR_CONTACTOS.md](IMPORTAR_CONTACTOS.md) para exportar e incorporar agendas de Google, Outlook u otros programas de forma local.

## Privacidad de los datos

Los archivos incluidos en este repositorio están vacíos. Los datos privados se guardan en:

`%LOCALAPPDATA%\Gestor_Comunicaciones_DNET\`

Desde la versión 1.7.0, `gestor_comunicaciones.db` es la base principal. En el primer arranque se crea automáticamente una copia ZIP de los JSON originales, se migran todos los registros y se comprueba que las cantidades coincidan antes de activar la base nueva. Los JSON siguen actualizándose como formato de compatibilidad y las copias de seguridad conservan el formato conocido. Si una versión anterior modifica esos JSON, la versión nueva lo detecta, protege primero su estado en otro ZIP y reincorpora los cambios. La lista interna **NO CONTACTAR** guarda únicamente la dirección, la fecha y el origen necesarios para evitar futuras comunicaciones; recuperar una copia antigua no reactiva estas direcciones.

Los contactos y el historial reales permanecen fuera del repositorio. No publiques la base de datos, los JSON de esa carpeta ni las copias ZIP que genere la aplicación.

## Pequeño manual de uso

1. Abre el programa y añade tus contactos con **Nuevo contacto**. También puedes importarlos desde Google/Outlook CSV, vCard (VCF) o JSON; revisa la vista previa antes de confirmar.
2. Si lo necesitas, cambia la interfaz desde el menú **Idioma**. La elección se conservará.
3. Utiliza el buscador o los filtros para mostrar únicamente los contactos que necesites.
4. Marca las casillas de los destinatarios. **Marcar visibles** selecciona todos los contactos mostrados que pueden recibir correo.
5. Pulsa **Copiar CCO** para copiar las direcciones o **Preparar con mailto** para abrir un correo nuevo con los destinatarios colocados en CCO.
6. Escribe personalmente el asunto y el mensaje, añade los archivos necesarios y revisa el campo CCO antes de enviar.
7. Después de realizar el envío, vuelve al gestor y pulsa **Confirmar envío REAL** para anotarlo en el historial.
8. Para atender una solicitud de baja, selecciona el contacto y pulsa **Marcar NO CONTACTAR**. El punto rojo confirma que no podrá incluirse en futuros destinatarios ni reaparecer mediante una importación.
9. En **Herramientas** puedes abrir la papelera, consultar el historial y revisar posibles duplicados o correos incorrectos.
10. Utiliza **Copias de seguridad > Hacer copia de seguridad** para guardar juntos los contactos y el historial en un ZIP.
11. Para volver a un estado anterior, utiliza **Recuperar copia de seguridad**. Antes de sustituir los datos, el programa conserva automáticamente otra copia del estado actual y mantiene la lista **NO CONTACTAR** vigente.

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

Para generar también el instalador multilingüe, instala Inno Setup 6 o 7 y ejecuta `Compilar_INSTALADOR.cmd`.

## Uso responsable

Utiliza siempre CCO para grupos de destinatarios, respeta las solicitudes de baja y comprueba que existe una base legítima para cada comunicación. Esta aplicación no evita los límites ni las políticas del proveedor de correo.

## Aportación voluntaria

**EL GESTOR DE COMUNICACIONES DNET ES GRATUITO Y LO SEGUIRÁ SIENDO. DONAR O NO DONAR ES DECISIÓN EXCLUSIVA DE CADA PERSONA.** ❤️

Si el programa te resulta útil y quieres apoyar voluntariamente su mantenimiento y evolución, puedes hacerlo por cualquiera de estos medios:

- [PayPal: paypal.me/acarballoflandes](https://paypal.me/acarballoflandes)
- [Ko-fi: ko-fi.com/arturocarballo](https://ko-fi.com/arturocarballo)
- **Bizum:** `+34 662 024 169`
- [GitHub Sponsors de arturo1961](https://github.com/sponsors/arturo1961), cuando el perfil esté disponible.

Donar no desbloquea funciones, no ofrece ventajas especiales y no modifica la licencia MIT ni las condiciones del programa. Consulta los detalles en [DONACIONES.md](DONACIONES.md).

También puedes colaborar comunicando errores, proponiendo mejoras o difundiendo el proyecto.

## Licencia

Distribuido bajo la licencia MIT. Consulta [LICENSE](LICENSE).


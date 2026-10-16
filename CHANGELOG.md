# Historial de cambios

## 1.10.0

- Nueva ventana **Campaña individual** con asunto, cuerpo, versión y selección de varios archivos adjuntos.
- Cola persistente de destinatarios que excluye contactos **NO CONTACTAR**, eliminados, duplicados y quienes ya recibieron la misma versión.
- Preparación supervisada de un solo borrador cada vez; abrirlo nunca se registra como envío.
- Confirmación manual del envío antes de actualizar el contacto y el historial.
- Espera mínima de 20 segundos entre confirmaciones y límite de 45 destinatarios confirmados por día, incluidos los reenvíos.
- Pausa y reanudación de la cola después de cerrar el programa.
- Uso de la aplicación y cuenta predeterminadas de Windows; con Outlook de escritorio compatible se añaden automáticamente los archivos.
- Personalización opcional mediante `{nombre}`, `{organizacion}`, `{correo}`, `{pais}` y `{region}`.
- La campaña guardada se incorpora a las copias de seguridad y se recupera junto con los contactos y el historial.
- Ventana, botones, contadores, confirmaciones y avisos traducidos en los 62 idiomas.

## 1.9.1

- Nuevo icono propio para el ejecutable, las ventanas, los accesos directos y el instalador.
- Nuevo instalador multilingüe que adopta automáticamente el idioma visual de Windows y conserva la elección al actualizar.
- La vista previa de importación muestra por separado cuántos registros se omiten por pertenecer a **NO CONTACTAR**.
- La barra inferior muestra permanentemente la cantidad de contactos incluidos en **NO CONTACTAR**.
- Nueva marca roja **NO CONTACTAR** para conservar y reconocer las direcciones que han solicitado no recibir comunicaciones.
- Los contactos excluidos no pueden seleccionarse, copiarse a CCO ni añadirse a un borrador mediante `mailto`.
- Las importaciones omiten las direcciones excluidas aunque vuelvan a aparecer en archivos JSON, CSV o vCard.
- La lista de exclusión se conserva al recuperar una copia de seguridad anterior y almacena únicamente el correo necesario para reconocerlo.
- Las copias automáticas creadas antes de importar se limitan a las dos más recientes; las copias manuales no se eliminan.
- Botón, columna, confirmaciones y avisos nuevos traducidos en los 62 idiomas.

## 1.9.0

- Creación de informes PDF a partir de los filtros aplicados en la pantalla principal.
- Elección entre todos los contactos visibles o solamente los contactos marcados.
- Selección de las columnas que aparecerán en el informe.
- El PDF incluye título, fecha, filtros aplicados, cantidad de contactos y páginas numeradas.
- Los botones, ventanas, avisos de importación e informes PDF nuevos están traducidos en los 62 idiomas.

## 1.8.1

- El filtro Región muestra únicamente las regiones pertenecientes al país seleccionado.
- Al cambiar de país, una región que ya no corresponde se restablece automáticamente a «Todos».
- La opción «Todos» en País sigue mostrando todas las regiones disponibles.

## 1.8.0

- Importación local de contactos desde Google Contacts y Outlook en formato CSV.
- Importación de libretas de direcciones vCard (`.vcf`) y mantenimiento de la importación JSON.
- Vista previa con cantidades y una muestra de los contactos antes de confirmar.
- Detección de correos e identificadores repetidos sin sobrescribir contactos existentes.
- Copia de seguridad ZIP automática inmediatamente antes de cada importación confirmada.
- No se solicitan contraseñas ni se conecta directamente con cuentas de correo.

## 1.7.1

- Corregido el botón «Marcar visibles» para que las casillas se actualicen inmediatamente.
- La tabla recibe ahora una notificación explícita por cada cambio de selección.
- La detección de contactos aptos acepta `email` y estados equivalentes sin distinguir mayúsculas y minúsculas.

## 1.7.0

- Nueva base de datos SQLite preparada para listas de contactos e historiales grandes.
- Migración automática y comprobada desde los JSON existentes, sin pedir pasos técnicos al usuario.
- Copia ZIP automática de los JSON originales antes de crear la base de datos.
- Verificación del número de registros y de la integridad de SQLite antes de aceptar la migración.
- Operaciones transaccionales: un cambio se guarda completo o no se guarda, evitando estados incompletos.
- JSON de compatibilidad actualizados después de cada cambio para poder regresar a una versión anterior.
- Detección protegida de cambios hechos con una versión anterior, con copia automática antes de reincorporarlos.
- Importación, exportación y copias ZIP conservan el mismo formato JSON de las versiones anteriores.

## 1.6.1

- Editor de contactos rediseñado con una presentación más limpia y moderna.
- Dos secciones visuales equilibradas para datos del contacto y seguimiento.
- Un único desplazamiento vertical, campos más anchos y etiquetas con espacio suficiente.
- Cabecera destacada y botones de guardar y cancelar más claros.
- Tamaño inicial adaptado automáticamente al área disponible de la pantalla.

## 1.6.0

- 62 idiomas integrados en el mismo ejecutable.
- Detección inicial del idioma de Windows.
- Selector de idioma que cambia la interfaz sin reiniciar y recuerda la elección.
- Traducción de la ventana principal, editor, papelera, historial, revisión, ayuda, confirmaciones y avisos principales.
- Validación automática de que todos los paquetes contienen los mismos textos y conservan sus marcadores.
- Las escrituras de derecha a izquierda mantienen el texto natural sin invertir la distribución de la aplicación.

## 1.5.0

- Buscador instantáneo compatible con los filtros existentes.
- Papelera visual para recuperar contactos eliminados.
- Eliminación definitiva protegida por una confirmación expresa.
- Ventana de consulta del historial de comunicaciones.
- Revisión de correos posiblemente incorrectos, direcciones repetidas y nombres duplicados.
- Ayuda y manual actualizados.
- El formato de los archivos de datos y de las copias de seguridad sigue siendo compatible con la versión 1.4.0.

## 1.4.0

- Copias de seguridad completas en ZIP.
- Recuperación validada con copia automática previa.
- Publicación inicial con datos vacíos y licencia MIT.

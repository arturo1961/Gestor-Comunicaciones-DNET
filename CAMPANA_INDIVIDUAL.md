# Campaña individual supervisada

Esta función prepara un solo mensaje cada vez. El gestor nunca pulsa **Enviar** ni guarda contraseñas.

## Preparación

1. Filtra y marca los contactos en la pantalla principal.
2. Pulsa **Campaña individual...**.
3. Escribe el asunto, el cuerpo y la versión que deseas registrar.
   Puedes elegir cualquier campo del selector y pulsar **＋** para insertarlo donde esté el cursor, tanto en el asunto como en el cuerpo. Los códigos entre corchetes también pueden seguir escribiéndose manualmente.
   Si la documentación está en Google Drive, pega su enlace en **URL** y pulsa el botón **＋** de esa fila para insertar `{enlace_documentacion}` en el asunto o el cuerpo.
4. Añade uno o varios archivos si son necesarios.
5. Pulsa **Crear cola con los marcados**.

La cola excluye automáticamente direcciones repetidas, contactos eliminados, **NO CONTACTAR** y contactos que ya recibieron esa versión.

## Envío supervisado

1. Pulsa **Preparar siguiente**.
2. Revisa el destinatario, el asunto, el cuerpo y los adjuntos en tu aplicación de correo.
3. Pulsa **Enviar** personalmente.
4. Regresa al gestor y pulsa **Confirmar enviado**.
5. Elige la espera entre envíos (de 1 a 3.600 segundos). Después de confirmar cada envío, espera esa cuenta atrás antes de preparar el siguiente.

Abrir o cerrar un borrador no lo registra como enviado. Si no lo has enviado, pulsa **No enviado** para devolverlo a pendientes. **Omitir** lo retira de esa cola sin cambiar el contacto.

## Cuenta de correo y adjuntos

Sin adjuntos, Windows abre la aplicación y cuenta de correo predeterminadas mediante `mailto`. Si se han indicado adjuntos, el gestor sólo intenta crear un borrador completo mediante Outlook clásico cuando este es el correo predeterminado. Con el nuevo Outlook u otra aplicación predeterminada abre el borrador sin iniciar el instalador de Office y avisa de que los archivos deben añadirse manualmente.

## Pausa, límite y recuperación

- **Pausar y cerrar** conserva la cola en `%LOCALAPPDATA%\Gestor_Comunicaciones_DNET\campana_actual.json`.
- El tiempo de espera, el límite diario y el enlace de documentación elegidos se conservan en la campaña y en las copias de seguridad.
- Puedes utilizar todos los campos presentes en la tabla, por ejemplo `{persona_contacto}`, `{web_contacto}`, `{tipo}`, `{notas}` o `{material}`.
- Se cuentan todos los destinatarios confirmados ese día, también los reenvíos y los confirmados mediante el sistema CCO existente.
- El límite diario comienza en 45, pero puedes modificarlo en **✉ / 24 h** ahora o dentro de varias semanas. Al alcanzarlo, no se prepara otro destinatario hasta el día siguiente.
- Las copias ZIP incluyen la campaña guardada además de los contactos y el historial.

El límite organiza un uso prudente, pero no evita ni elude las políticas antispam del proveedor de correo.

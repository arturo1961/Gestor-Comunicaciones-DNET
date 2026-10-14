# Importar contactos

Gestor de Comunicaciones DNET 1.8.0 puede importar contactos sin conectarse a ninguna cuenta y sin solicitar contraseñas.

## Formatos compatibles

- Google Contacts en CSV.
- Outlook o Microsoft 365 en CSV.
- Agendas en formato vCard (`.vcf`).
- Copias compatibles del Gestor en JSON.

## Procedimiento

1. Exporta los contactos desde el servicio o programa de origen.
2. En el Gestor, pulsa **Importar JSON/CSV/VCF**.
3. Selecciona el archivo exportado.
4. Revisa la cantidad de registros, los repetidos detectados y la muestra presentada.
5. Confirma únicamente si la vista previa es correcta.

Antes de incorporar contactos, el programa crea automáticamente una copia ZIP completa del estado actual. Los contactos que ya tengan el mismo correo o identificador se omiten y nunca sobrescriben los existentes.

Después de importar, conviene revisar los registros desde **Herramientas > Revisar duplicados y correos**. Los contactos importados quedan en estado pendiente y su dirección debe verificarse antes de utilizarla.

## Privacidad

La importación se realiza completamente en el equipo. El Gestor no inicia sesión en Google, Microsoft ni otros servicios, no guarda contraseñas y no transmite el archivo seleccionado.

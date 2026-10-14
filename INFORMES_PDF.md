# Crear informes PDF

Gestor de Comunicaciones DNET 1.9.0 puede crear un PDF sin modificar los contactos ni el historial.

## Procedimiento

1. En la pantalla principal, selecciona el país y, si lo necesitas, una región.
2. Aplica otros filtros de idioma, tipo, organización o estado, o utiliza el cuadro de búsqueda.
3. Comprueba que la tabla muestra los contactos que deseas incluir.
4. Pulsa **Crear informe PDF**.
5. Elige entre **todos los visibles después de filtrar** o **solamente los marcados**.
6. Marca las columnas que deseas mostrar y escribe un título opcional.
7. Pulsa **Crear PDF** y elige dónde guardarlo.

El documento contiene la fecha, los filtros aplicados, la cantidad de contactos y la numeración de páginas. La creación del informe sólo lee los registros seleccionados: no cambia la base SQLite, los JSON ni el historial.

La función utiliza **Microsoft Print to PDF**, incluida normalmente en Windows 10 y Windows 11.

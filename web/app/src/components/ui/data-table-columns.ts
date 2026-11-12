import { createColumnHelper, type RowData, rowPaginationFeature, tableFeatures } from '@tanstack/react-table';

/** Register sorting or filtering here when a screen needs them. Paging is the server's: DataTable never slices rows. */
export const features = tableFeatures({ rowPaginationFeature });

export type Features = typeof features;

/** The column helper for rows of TRow: `const notes = columnsOf<Note>()`, then `notes.columns([notes.accessor('text', ...)])`. */
export const columnsOf = <TRow extends RowData>() => createColumnHelper<Features, TRow>();

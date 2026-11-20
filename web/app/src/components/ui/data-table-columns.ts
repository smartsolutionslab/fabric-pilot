import {
  columnVisibilityFeature,
  createColumnHelper,
  type RowData,
  rowPaginationFeature,
  tableFeatures,
} from '@tanstack/react-table';

/**
 * Column visibility, for the table's Columns control, and paging, which is the server's: DataTable never slices rows.
 * Register sorting or filtering here when a screen needs them.
 */
export const features = tableFeatures({ columnVisibilityFeature, rowPaginationFeature });

export type Features = typeof features;

/** The column helper for rows of TRow: `const notes = columnsOf<Note>()`, then `notes.columns([notes.accessor('text', ...)])`. */
export const columnsOf = <TRow extends RowData>() => createColumnHelper<Features, TRow>();

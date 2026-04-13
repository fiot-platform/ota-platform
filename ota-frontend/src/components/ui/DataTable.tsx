'use client'

import * as React from 'react'
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight, Inbox } from 'lucide-react'
import { clsx } from 'clsx'
import { PaginationInfo } from '@/types'

// ─── Column Definition ────────────────────────────────────────────────────────

export interface Column<T> {
  key: string
  header: string
  accessor?: keyof T
  cell?: (row: T) => React.ReactNode
  className?: string
  headerClassName?: string
  sortable?: boolean
}

interface DataTableProps<T> {
  columns: Column<T>[]
  data: T[]
  pagination?: PaginationInfo
  onPageChange?: (page: number) => void
  onPageSizeChange?: (pageSize: number) => void
  isLoading?: boolean
  emptyMessage?: string
  emptyIcon?: React.ReactNode
  keyExtractor: (row: T) => string
  onRowClick?: (row: T) => void
  rowClassName?: (row: T) => string
  stickyHeader?: boolean
}

// ─── Loading Skeleton ─────────────────────────────────────────────────────────

function TableSkeleton({ columns, rows = 5 }: { columns: number; rows?: number }) {
  return (
    <>
      {Array.from({ length: rows }).map((_, rowIdx) => (
        <tr key={rowIdx} className="border-b border-slate-100">
          {Array.from({ length: columns }).map((_, colIdx) => (
            <td key={colIdx} className="px-4 py-3">
              <div className="h-4 bg-slate-200 rounded animate-pulse" style={{ width: `${60 + Math.random() * 30}%` }} />
            </td>
          ))}
        </tr>
      ))}
    </>
  )
}

// ─── Pagination Controls ──────────────────────────────────────────────────────

function PaginationControls({
  pagination,
  onPageChange,
  onPageSizeChange,
}: {
  pagination: PaginationInfo
  onPageChange?: (page: number) => void
  onPageSizeChange?: (pageSize: number) => void
}) {
  const { page, pageSize, totalCount, totalPages } = pagination
  const start = Math.min((page - 1) * pageSize + 1, totalCount)
  const end = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-slate-200 bg-white rounded-b-xl">
      <div className="flex items-center gap-2">
        <span className="text-sm text-slate-500">Rows per page:</span>
        <select
          value={pageSize}
          onChange={(e) => onPageSizeChange?.(Number(e.target.value))}
          className="text-sm border border-slate-200 rounded-lg px-2 py-1 focus:outline-none focus:ring-2 focus:ring-accent-500 bg-white"
        >
          {[10, 25, 50, 100].map((size) => (
            <option key={size} value={size}>{size}</option>
          ))}
        </select>
      </div>

      <div className="flex items-center gap-4">
        <span className="text-sm text-slate-500">
          {totalCount === 0 ? '0' : `${start}–${end}`} of {totalCount}
        </span>
        <div className="flex items-center gap-1">
          <button
            onClick={() => onPageChange?.(1)}
            disabled={page === 1}
            className={clsx(
              'p-1.5 rounded-lg hover:bg-slate-100 transition-colors',
              page === 1 ? 'text-slate-300 cursor-not-allowed' : 'text-slate-600'
            )}
            aria-label="First page"
          >
            <ChevronsLeft className="w-4 h-4" />
          </button>
          <button
            onClick={() => onPageChange?.(page - 1)}
            disabled={!pagination.hasPreviousPage}
            className={clsx(
              'p-1.5 rounded-lg hover:bg-slate-100 transition-colors',
              !pagination.hasPreviousPage ? 'text-slate-300 cursor-not-allowed' : 'text-slate-600'
            )}
            aria-label="Previous page"
          >
            <ChevronLeft className="w-4 h-4" />
          </button>

          {/* Page numbers */}
          {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
            let pageNum: number
            if (totalPages <= 5) {
              pageNum = i + 1
            } else if (page <= 3) {
              pageNum = i + 1
            } else if (page >= totalPages - 2) {
              pageNum = totalPages - 4 + i
            } else {
              pageNum = page - 2 + i
            }
            return (
              <button
                key={pageNum}
                onClick={() => onPageChange?.(pageNum)}
                className={clsx(
                  'w-8 h-8 flex items-center justify-center rounded-lg text-sm font-medium transition-colors',
                  pageNum === page
                    ? 'bg-accent-600 text-white'
                    : 'text-slate-600 hover:bg-slate-100'
                )}
              >
                {pageNum}
              </button>
            )
          })}

          <button
            onClick={() => onPageChange?.(page + 1)}
            disabled={!pagination.hasNextPage}
            className={clsx(
              'p-1.5 rounded-lg hover:bg-slate-100 transition-colors',
              !pagination.hasNextPage ? 'text-slate-300 cursor-not-allowed' : 'text-slate-600'
            )}
            aria-label="Next page"
          >
            <ChevronRight className="w-4 h-4" />
          </button>
          <button
            onClick={() => onPageChange?.(totalPages)}
            disabled={page === totalPages || totalPages === 0}
            className={clsx(
              'p-1.5 rounded-lg hover:bg-slate-100 transition-colors',
              page === totalPages || totalPages === 0 ? 'text-slate-300 cursor-not-allowed' : 'text-slate-600'
            )}
            aria-label="Last page"
          >
            <ChevronsRight className="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>
  )
}

// ─── Main DataTable ───────────────────────────────────────────────────────────

export function DataTable<T>({
  columns,
  data,
  pagination,
  onPageChange,
  onPageSizeChange,
  isLoading = false,
  emptyMessage = 'No data found',
  emptyIcon,
  keyExtractor,
  onRowClick,
  rowClassName,
  stickyHeader = false,
}: DataTableProps<T>) {
  return (
    <div className="w-full rounded-xl border border-slate-200 bg-white shadow-card overflow-hidden">
      <div className={clsx('w-full overflow-x-auto', stickyHeader && 'max-h-[600px] overflow-y-auto')}>
        <table className="w-full text-sm">
          <thead className={clsx('bg-slate-50 border-b border-slate-200', stickyHeader && 'sticky top-0 z-10')}>
            <tr>
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={clsx(
                    'px-4 py-3 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide whitespace-nowrap',
                    col.headerClassName
                  )}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <TableSkeleton columns={columns.length} />
            ) : data.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-4 py-16 text-center">
                  <div className="flex flex-col items-center gap-3">
                    {emptyIcon ?? <Inbox className="w-12 h-12 text-slate-300" />}
                    <p className="text-slate-500 font-medium">{emptyMessage}</p>
                  </div>
                </td>
              </tr>
            ) : (
              data.map((row) => (
                <tr
                  key={keyExtractor(row)}
                  onClick={() => onRowClick?.(row)}
                  className={clsx(
                    'border-b border-slate-100 last:border-b-0 transition-colors duration-100',
                    onRowClick ? 'cursor-pointer hover:bg-accent-50' : 'hover:bg-slate-50',
                    rowClassName?.(row)
                  )}
                >
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={clsx('px-4 py-3 text-primary-800', col.className)}
                    >
                      {col.cell
                        ? col.cell(row)
                        : col.accessor
                          ? String(row[col.accessor] ?? '')
                          : null}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {pagination && (
        <PaginationControls
          pagination={pagination}
          onPageChange={onPageChange}
          onPageSizeChange={onPageSizeChange}
        />
      )}
    </div>
  )
}

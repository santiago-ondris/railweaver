/** One `label: value` row of a `.detail-list` (DESIGN.md › Components). */
export function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt>{label}</dt>
      <dd>{value}</dd>
    </div>
  )
}

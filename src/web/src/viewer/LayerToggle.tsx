export function LayerToggle({
  name,
  detail,
  pressed,
  toggle,
}: {
  name: string
  detail: string
  pressed: boolean
  toggle: () => void
}) {
  return (
    <li>
      <button type="button" className="layer-toggle" aria-pressed={pressed} onClick={toggle}>
        <span className="layer-box" aria-hidden="true" />
        <span className="layer-text">
          <span className="layer-name">{name}</span>
          <span className="layer-detail">{detail}</span>
        </span>
      </button>
    </li>
  )
}

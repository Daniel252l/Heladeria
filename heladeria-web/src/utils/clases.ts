export function clases(
  ...valores: Array<string | false | null | undefined>
) {
  return valores.filter(Boolean).join(' ')
}

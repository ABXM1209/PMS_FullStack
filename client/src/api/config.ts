const apiBase = import.meta.env.VITE_API_BASE_URL ?? ''
export const api = (path: string) => `${apiBase}${path}`
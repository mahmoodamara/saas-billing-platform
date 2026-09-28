import type { Session } from '../types'

const SESSION_KEY = 'billing-session'
export const getSession = (): Session | null => {
  const raw = localStorage.getItem(SESSION_KEY)
  return raw ? JSON.parse(raw) : null
}
export const setSession = (s: Session | null) => s ? localStorage.setItem(SESSION_KEY, JSON.stringify(s)) : localStorage.removeItem(SESSION_KEY)

export async function api<T>(path:string, options:RequestInit = {}):Promise<T> {
  const session = getSession()
  const headers = new Headers(options.headers)
  headers.set('Content-Type', 'application/json')
  if (session?.token) headers.set('Authorization', `Bearer ${session.token}`)
  const res = await fetch(`/api${path}`, { ...options, headers })
  if (!res.ok) {
    let message = `${res.status} ${res.statusText}`
    try { const body = await res.json(); message = body.title ?? body.message ?? message } catch {}
    throw new Error(message)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

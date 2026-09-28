import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, setSession } from '../services/api'
import type { Session } from '../types'

export default function Login(){
  const [email,setEmail]=useState('customer@demo.local'); const [password,setPassword]=useState('Customer123!'); const [error,setError]=useState(''); const nav=useNavigate()
  async function submit(e:FormEvent){e.preventDefault(); setError(''); try{const s=await api<Session>('/auth/login',{method:'POST',body:JSON.stringify({email,password})});setSession(s);nav(s.role==='Admin'?'/admin':'/')}catch(e){setError((e as Error).message)}}
  return <div className="flex min-h-screen items-center justify-center p-5"><form onSubmit={submit} className="card w-full max-w-md">
    <h1 className="text-2xl font-bold">Sign in</h1><p className="mt-1 text-sm text-slate-400">Demo customer is pre-filled. Admin: admin@demo.local / Admin123!</p>
    <div className="mt-6"><label className="label">Email</label><input className="input" value={email} onChange={e=>setEmail(e.target.value)}/></div>
    <div className="mt-4"><label className="label">Password</label><input className="input" type="password" value={password} onChange={e=>setPassword(e.target.value)}/></div>
    {error&&<div className="mt-4 rounded-lg bg-red-950 p-3 text-sm text-red-300">{error}</div>}
    <button className="btn-primary mt-5 w-full">Sign in</button>
  </form></div>
}

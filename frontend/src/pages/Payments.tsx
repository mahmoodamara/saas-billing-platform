import { useEffect,useState } from 'react'
import { api } from '../services/api'
import type { Payment } from '../types'

export default function Payments(){const [p,setP]=useState<Payment[]>([]);useEffect(()=>{api<Payment[]>('/payments').then(setP)},[]);return <div><h1 className="text-3xl font-bold">Payments</h1><p className="mb-6 text-slate-400">Every provider attempt is persisted for auditability.</p><div className="space-y-3">{p.map(x=><div className="card flex flex-wrap items-center justify-between gap-3" key={x.id}><div><div className="font-bold">${x.amount.toFixed(2)} {x.currency}</div><div className="text-xs text-slate-500">{x.providerReference||'No provider reference'}</div></div><div className="text-right"><span className="badge">{x.status}</span><div className="mt-1 text-xs text-slate-500">Attempts: {x.attempts}</div></div></div>)}</div></div>}

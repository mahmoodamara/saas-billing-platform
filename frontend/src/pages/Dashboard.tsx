import { useEffect,useState } from 'react'
import { api } from '../services/api'
import type { Dashboard as D } from '../types'
import Stat from '../components/Stat'

export default function Dashboard(){const [d,setD]=useState<D|null>(null);const [error,setError]=useState('');useEffect(()=>{api<D>('/dashboard').then(setD).catch(e=>setError(e.message))},[])
if(error)return <div className="card text-red-300">{error}</div>;if(!d)return <div className="card">Loading…</div>;const pct=d.usage?Math.min(100,Math.round(d.usage.used/d.usage.included*100)):0
return <div className="space-y-6"><div><h1 className="text-3xl font-bold">{d.organization}</h1><p className="text-slate-400">Billing overview</p></div>
<div className="grid gap-4 md:grid-cols-4"><Stat label="Current plan" value={d.subscription?.plan.name??'None'} sub={d.subscription?`$${d.subscription.plan.monthlyPrice}/month`:undefined}/><Stat label="Usage" value={d.usage?.used.toLocaleString()??0} sub={`${d.usage?.included.toLocaleString()??0} included`}/><Stat label="Overage" value={d.usage?.overage.toLocaleString()??0} sub={`Est. $${d.usage?.estimatedOverageCost.toFixed(2)??'0.00'}`}/><Stat label="Latest invoice" value={d.latestInvoice?`$${d.latestInvoice.total.toFixed(2)}`:'—'} sub={d.latestInvoice?.status}/></div>
{d.usage&&<div className="card"><div className="mb-2 flex justify-between"><span>Monthly usage</span><span>{pct}%</span></div><div className="h-3 overflow-hidden rounded-full bg-slate-800"><div className="h-full bg-indigo-500" style={{width:`${pct}%`}}/></div><div className="mt-5 grid gap-3 md:grid-cols-3">{d.usage.byChannel.map(x=><div className="rounded-xl bg-slate-950 p-4" key={x.channel}><div className="text-sm text-slate-400">{x.channel}</div><div className="text-xl font-bold">{x.quantity.toLocaleString()}</div></div>)}</div></div>}
</div>}

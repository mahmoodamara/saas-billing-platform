import { FormEvent,useEffect,useState } from 'react'
import { api } from '../services/api'

type Audit={action:string;entityType:string;entityId:string;dataJson:string;createdAt:string}
type AdminPayment={id:string;organization:string;invoiceId:string;amount:number;currency:string;status:string;attempts:number;refunded:number;createdAt:string}

export default function Admin(){
  const [q,setQ]=useState('Payment'); const [audits,setAudits]=useState<Audit[]>([]); const [payments,setPayments]=useState<AdminPayment[]>([]); const [msg,setMsg]=useState('')
  const loadPayments=()=>api<AdminPayment[]>('/admin/payments').then(setPayments).catch(e=>setMsg(e.message))
  useEffect(()=>{void loadPayments()},[])
  async function search(e:FormEvent){e.preventDefault();setMsg('');try{setAudits(await api<Audit[]>(`/audit/search?q=${encodeURIComponent(q)}`))}catch(e){setMsg((e as Error).message)}}
  async function retry(id:string){try{await api(`/admin/payments/${id}/retry`,{method:'POST'});setMsg('Retry executed.');loadPayments()}catch(e){setMsg((e as Error).message)}}
  async function refund(p:AdminPayment){const remaining=p.amount-p.refunded;const raw=prompt(`Refund amount (max ${remaining.toFixed(2)}):`,remaining.toFixed(2));if(!raw)return;const amount=Number(raw);try{await api(`/admin/payments/${p.id}/refund`,{method:'POST',body:JSON.stringify({amount,reason:'Admin demo refund'})});setMsg('Refund processed.');loadPayments()}catch(e){setMsg((e as Error).message)}}
  return <div className="space-y-8"><div><h1 className="text-3xl font-bold">Admin</h1><p className="text-slate-400">Payment operations and Elasticsearch-backed audit search.</p></div>{msg&&<div className="card">{msg}</div>}
  <section><h2 className="mb-3 text-xl font-bold">Payments</h2><div className="space-y-3">{payments.length===0&&<div className="card text-slate-400">No payments yet. Use the customer account to create/pay an invoice.</div>}{payments.map(p=><div className="card flex flex-wrap items-center justify-between gap-4" key={p.id}><div><div className="font-bold">{p.organization} · ${p.amount.toFixed(2)} {p.currency}</div><div className="text-xs text-slate-500">{p.id}</div><div className="mt-1 text-sm text-slate-400">Status {p.status} · attempts {p.attempts} · refunded ${p.refunded.toFixed(2)}</div></div><div className="flex gap-2">{p.status==='Failed'&&<button className="btn-secondary" onClick={()=>retry(p.id)}>Retry</button>}{(p.status==='Succeeded'||p.status==='PartiallyRefunded')&&p.refunded<p.amount&&<button className="btn-primary" onClick={()=>refund(p)}>Refund</button>}</div></div>)}</div></section>
  <section><h2 className="mb-3 text-xl font-bold">Audit search</h2><form className="card flex gap-3" onSubmit={search}><input className="input" value={q} onChange={e=>setQ(e.target.value)} placeholder="Payment, Invoice, Subscription…"/><button className="btn-primary">Search</button></form><div className="mt-4 space-y-3">{audits.map((x,i)=><div className="card" key={`${x.entityId}-${i}`}><div className="flex justify-between gap-3"><b>{x.action}</b><span className="text-xs text-slate-500">{new Date(x.createdAt).toLocaleString()}</span></div><div className="mt-1 text-sm text-slate-400">{x.entityType} · {x.entityId}</div><pre className="mt-3 overflow-auto rounded-lg bg-slate-950 p-3 text-xs text-slate-300">{x.dataJson}</pre></div>)}</div></section></div>
}

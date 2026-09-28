import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { getSession, setSession } from '../services/api'

export default function Layout(){
  const session = getSession(); const nav = useNavigate()
  const links = [['/','Dashboard'],['/plans','Plans'],['/usage','Usage'],['/invoices','Invoices'],['/payments','Payments']]
  if(session?.role === 'Admin') links.push(['/admin','Admin'])
  return <div className="min-h-screen">
    <header className="border-b border-slate-800 bg-slate-900/70 backdrop-blur">
      <div className="mx-auto flex max-w-7xl items-center justify-between px-5 py-4">
        <div><div className="text-xl font-bold">SaaS Billing Platform</div><div className="text-xs text-slate-400">ASP.NET Core · React · SQL Server · Redis</div></div>
        <div className="flex items-center gap-3 text-sm"><span className="text-slate-400">{session?.email}</span><button className="btn-secondary" onClick={()=>{setSession(null); nav('/login')}}>Sign out</button></div>
      </div>
    </header>
    <div className="mx-auto grid max-w-7xl grid-cols-1 gap-6 px-5 py-6 md:grid-cols-[210px_1fr]">
      <aside className="card h-fit p-3">{links.map(([to,label])=><NavLink key={to} to={to} end={to==='/'} className={({isActive})=>`mb-1 block rounded-lg px-3 py-2 ${isActive?'bg-indigo-500 text-white':'text-slate-300 hover:bg-slate-800'}`}>{label}</NavLink>)}</aside>
      <main><Outlet/></main>
    </div>
  </div>
}

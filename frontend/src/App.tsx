import { Navigate,Route,Routes } from 'react-router-dom'
import Layout from './components/Layout'
import Dashboard from './pages/Dashboard'
import Login from './pages/Login'
import Plans from './pages/Plans'
import Usage from './pages/Usage'
import Invoices from './pages/Invoices'
import Payments from './pages/Payments'
import Admin from './pages/Admin'
import { getSession } from './services/api'

function Protected(){return getSession()?<Layout/>:<Navigate to="/login" replace/>}
export default function App(){return <Routes><Route path="/login" element={<Login/>}/><Route element={<Protected/>}><Route path="/" element={<Dashboard/>}/><Route path="/plans" element={<Plans/>}/><Route path="/usage" element={<Usage/>}/><Route path="/invoices" element={<Invoices/>}/><Route path="/payments" element={<Payments/>}/><Route path="/admin" element={<Admin/>}/></Route><Route path="*" element={<Navigate to="/" replace/>}/></Routes>}

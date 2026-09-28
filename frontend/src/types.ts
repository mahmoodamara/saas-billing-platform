export type Plan = { id:string; name:string; monthlyPrice:number; includedMessages:number; extraMessagePrice:number; isActive:boolean }
export type Subscription = { id:string; status:string; plan:Plan; scheduledPlan?:Plan|null; billingPeriodStart:string; billingPeriodEnd:string }
export type Usage = { used:number; included:number; overage:number; estimatedOverageCost:number; byChannel:{channel:string; quantity:number}[]; periodStart:string; periodEnd:string }
export type InvoiceItem = { id:string; type:string; description:string; quantity:number; unitPrice:number; amount:number }
export type Invoice = { id:string; status:string; periodStart:string; periodEnd:string; total:number; dueAt:string; items:InvoiceItem[] }
export type Payment = { id:string; invoiceId:string; amount:number; currency:string; status:string; providerReference:string; attempts:number; createdAt:string }
export type Dashboard = { organization:string; subscription?:Subscription|null; usage?:Usage|null; latestInvoice?:Invoice|null; recentPayments:Payment[] }
export type Session = { token:string; email:string; role:string; organizationId?:string|null }

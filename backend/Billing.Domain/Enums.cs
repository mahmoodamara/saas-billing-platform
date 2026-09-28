namespace Billing.Domain;

public enum UserRole { Customer, Admin }
public enum SubscriptionStatus { Active, PastDue, Cancelled, Suspended }
public enum InvoiceStatus { Draft, Open, Paid, PastDue, Cancelled }
public enum PaymentStatus { Pending, Succeeded, Failed, Refunded, PartiallyRefunded }
public enum RefundStatus { Succeeded, Failed }
public enum InvoiceItemType { Subscription, Overage, Proration, Credit }

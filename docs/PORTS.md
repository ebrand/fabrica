# Port Allocation Scheme

A consistent port layout helps keep UX apps, MFEs, BFFs, domain services, and integration layers easy to reason about during local development.

---

## 1. Shell UXs (Root Frontend Apps)
**Range:** `3000–3099`

| Purpose | Port |
|--------|------|
| Storefront shell | `3000` |
| Admin shell | `3001` |
| Partner/merchant shell | `3002` |
| Experimental shells | `3010+` |

---

## 2. MFEs (Micro-Frontends / Remotes)
**Range:** `3100–3199`

| MFE | Port |
|-----|------|
| Catalog MFE | `3110` |
| Cart / Checkout MFE | `3120` |
| Account / Profile MFE | `3130` |
| Orders MFE | `3140` |
| Admin Catalog MFE | `3150` |
| Admin Orders MFE | `3160` |
| Admin Customers MFE | `3170` |
| Content MFE | `3180` |

---

## 3. BFFs (Backend-for-Frontend APIs)
**Range:** `3200–3299`

| BFF | Port |
|-----|------|
| Storefront BFF | `3200` |
| Admin BFF | `3210` |
| Partner/merchant BFF | `3220` |
| Mobile BFF | `3230` |
| Content BFF | `3240` |
| Additional capability-based BFFs | `3250+` |

---

## 4. Domain Services (DDD Bounded Contexts)
**Range:** `3400–3499`

### Customer Domain (`3410–3419`)
| Service | Port |
|---------|------|
| Customer API | `3410` |
| Customer Command/Write Service | `3411` |

### Product / Catalog Domain (`3420–3429`)
| Service | Port |
|---------|------|
| Product API | `3420` |
| Pricing API | `3421` |

### Order Management Domain (`3430–3439`)
| Service | Port |
|---------|------|
| Orders API | `3430` |
| Fulfillment/Shipping | `3431` |

### Payments / Billing Domain (`3440–3449`)
| Service | Port |
|---------|------|
| Payment Orchestration | `3440` |
| Invoicing | `3441` |

### Inventory Domain (`3450–3459`)
| Service | Port |
|---------|------|
| Inventory Service | `3450` |
| Warehouse Allocation | `3451` |

### Content Domain (`3460–3469`)
| Service | Port |
|---------|------|
| Content API (CMS) | `3460` |
| Media Service | `3461` |
| Translation Service | `3462` |

---

## 5. ESB / Integration / Federation Layer
**Range:** `3500–3599`

| Service | Port |
|---------|------|
| ESB HTTP façade / router | `3500` |
| Event ingress adapter | `3510` |
| ERP outbound integration | `3520` |
| CRM outbound integration | `3530` |
| Search indexer | `3540` |
| Analytics ETL / data pump | `3550` |

---

## 6. Shared / Cross-Cutting Services
**Range:** `3600–3699`

| Service | Port |
|---------|------|
| Auth / IAM | `3600` |
| Notification Service | `3610` |
| File / Media Service | `3620` |
| Configuration / Feature Flags | `3630` |
| Audit / Compliance API | `3640` |

---

## 7. Observability / Internal Tools
**Range:** `3700–3799`

| Tool | Port |
|------|------|
| Internal Ops Dashboard | `3700` |
| Tracing UI | `3710` |
| Metrics Dashboard | `3720` |
| Log Viewer / Log Search | `3730` |

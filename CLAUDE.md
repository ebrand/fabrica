# Fabrica Commerce Cloud

## Logistics

This is the "Fabrica Commerce Cloud" project. The fully-qualified path to the root of this project is:

`/Users/eric.brand/Documents/source/github/Eric-Brand_swi/fabrica/`

The following initial folder will be followed to keep projects and files organized:
```
.
├── .claude
│   └── agents.json
├── .DS_Store
├── docs
│   ├── CLAUDE.md
│   └── PORTS.md
├── domain
│   ├── admin
│   │   ├── acl
│   │   ├── database
│   │   └── esb
│   ├── common
│   │   └── model
│   ├── content
│   ├── customer
│   ├── finance
│   ├── fulfillment
│   ├── legal
│   ├── order-mgmt
│   ├── product
│   └── sales-mktg
├── infrastructure
└── ux
    ├── bff
    ├── mfe
    └── shell
```

There is a `/.claude/agents.json` file describing the overall architecture and various guidelines, etc. for the various Claude agents that will be in-use for this project. There is also this file (`/docs/CLAUDE.md`) to be used for project notes and also a `/docs/PORTS.md` file to be used as a map for the ports used for various components in this project.

## Overview

This will be a multi-tenant commerce cloud plaform with each tenant context sub-divided into multiple domains, implementing event-based data federation between domains, providing OAuth authentication via Google (and others) with a fully customizable RBAC system. To begin, I would like to run everything locally except for infrastructural pieces like the database, messaging, service mesh, api gateway being hosted a Docker containers.

## Domains

The following discreet domain boundaries will be supported:
- Admin
- Customer
- Content
- Finance
- Fulfillment
- Legal
- Order-Mgmt
- Product
- Sales-Mktg

## Database Structure

This section describes the backend database system that will be put in place. Each domain will employ a separate database instance named using the following format:

- `fabrica-{domain}-db` and will contain the following schemata:

|Schema|Description|
|:-|:-|
|`fabrica`|This schema will contain tables describing the promary domain aggregate and it's related data.|
|`cdc`|This schema will contain an `outbox` table used to store events describing data maintenance queued for publishing to the enterprise service bus (ESB).|

For example, the relational database for Fabrica's `customer` domain will be configured and installed as:

### Database Name

`fabrica-customer-db`

### Schema/Tables

|Schema.Table|Description|
|:-|:-|
|`fabrica`.`customer`|The core table for the `customer` domain's aggregate object.|
|`fabrica`.`customer_address`|A related table containing customer address records.|
|`cdc`.`outbox`|The table to which to write data maintenance events in order to create ESB event for publishing.|

## Naming Things

### Code Objects

All database schemata and tables, all code objects referring to domain objects will be **SINGULAR** to avoid confusion and mismatches in infrastructure and code.

- One-word names:
  - `customer`
  - `product`

- Multi-word names:
  - `shipping_address_id`
  - `shipping-address-id`
  - `shippingAddressId`
  - `ShippingAddressId`

### Docker Containers

Docker containers running custom code for the **Fabrica Commerce Cloud** will be named using the following convention:

[ `shell` | `mfe` | `bff` | `acl` ] - [ `admin` | `customer` | `content` | `finance` | `fulfillment` | `legal` | `order-mgmt` | `product` | `sales-mktg` ]

Infrastructure Docker containers should be named simply `postgres`,   `consul`, `redis`, etc.

All **Fabrica Commerce Cloud** containers will be members of a customer `fabrica` group.

## Coding Style

### [C# style guide](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)

### [JavaScript style guide](https://google.github.io/styleguide/jsguide.html)

## URL and Port Acquisition

No component should ever contain any configuration for other components' URLs. They should, instead, call into the `acl-admin` service to acquire the appropriate URL for the BFF or ACL service to whcih they are trying to connect.
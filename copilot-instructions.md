# FrostTrack Web Copilot Instructions

You are GitHub Copilot using Raptor mini.

Workspace: `FrostTrack_Web` on branch `utc-time`.

Primary focus:

- Preserve cold storage functionality.
- Remove legacy POS entities, services, routes, database tables, and UI features.
- Keep Booking, Delivery, RecurringCharge, Transaction, DailyStockBook, StockReport, Product, Customer, Employee, Company, Branch, Bank, Asset, PaymentMethod, Organization, PrintSettings.
- Do not recreate deleted POS features like Purchase, Sales, Supplier, Stock, SupplierPayment, SaleReturn, Damage.

Guidelines:

- Use concise markdown with headings in final responses.
- Apply minimal source edits and verify with build/tool checks when possible.
- Avoid heavy or unnecessary changes outside the current user request.
- Respect the existing clean architecture layering: Domain, Application, Persistence, Infrastructure, FrostTrack.Server, frosttrack.client.

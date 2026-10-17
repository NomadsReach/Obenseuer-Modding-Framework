---
title: What you can't do (yet)
parent: Recipes and crafting
grand_parent: Items and crafting
nav_order: 5
---

# What you can't do (yet)

- **You can't read the state of the growing panel.** Its only plain data is a live reference to a station, and the rest is screen drawing. `Crafting.All()` already lists the growing stations.
  - Technical: the class is `GrowingPanel`, its only plain data is `ActiveManu`, a live `Growing` reference.
- **You can't read the input or output of a put-item machine.** The only machine of that kind builds its options from invoices, not from a list (see the [Invoices](../invoices.md) page). A dishwasher is a manufacturing station, not that kind of machine.
  - Technical: the class is `PutItemSelection`, and its only subclass is `InvoicePaymentMachine`. A `Dishwasher` is a `manufacturing` subclass.
- **You can't read liquid volumes.** `Count` is the recipe amount, which can be fractional (for example 0.4 for a liquid).

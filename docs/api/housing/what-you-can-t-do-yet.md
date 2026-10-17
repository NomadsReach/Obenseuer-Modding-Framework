---
title: What you can't do (yet)
parent: Housing
grand_parent: Places and travel
nav_order: 7
---

# What you can't do (yet)

- You can't read rent templates. `CharacterRentDatabase` holds rent templates for the game's characters, not rent that was paid. The room's real rent comes from the apartment and is in `ApartmentInfo.Rent`.
- You can't read invoices for rent here. The game only uses invoices as a deposit when an eviction fails the money check. It is not a rent or room event. <span class="tech">(`InvoiceController` is not wrapped.)</span>
- You can't use the janitor here. The janitor's actions are for storage and triggers, not housing. <span class="tech">(`JanitorController` is not wrapped.)</span>
- You can't read the scene's room settings. Nothing in the game reads a live room setting, so rooms come from the building's apartment list. <span class="tech">(`RoomData` and `ApartmentFurniture` are scene and prefab settings.)</span>
- You can't use the blueprint screens. They only show information. The game's blueprint data comes from its furniture blueprint controller, which `Building` uses. <span class="tech">(`BlueprintManager` and `BuildingSystemMenu` are not wrapped.)</span>
- You can't read an apartment's condition, because the game has no single condition value. The cleaning timer is on the [Cleaning](../cleaning.md) page, and the furniture clear percent is not covered.

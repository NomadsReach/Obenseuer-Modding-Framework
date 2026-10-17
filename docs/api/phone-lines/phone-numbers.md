---
title: Phone numbers
parent: Phone lines
grand_parent: Phones, mail and media
nav_order: 2
---

# Phone numbers

A phone number here is a string of digits, with `*` standing for key 10 and `#` for key 11, the same form `Phones.All()` gives you. The number has to be one the game's phone book knows, so an empty string, any other character, or a number that matches nothing makes the call fail.

## Limits

- A number that is hidden in the phone book can't be dialled. The game does not let a phone reach a hidden entry either. <span class="tech">(The string is turned into a `PhoneNumber` by searching `PhoneNumberDatabase.database` with the game's own `PhoneNumber.ValidateNumber`. An entry marked `notIndexed` can't be found that way, as in the game.)</span>

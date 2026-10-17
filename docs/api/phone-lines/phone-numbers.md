---
title: Phone numbers
parent: Phone lines
grand_parent: Phones, mail and media
nav_order: 2
---

# Phone numbers

This is the full list of what the API gives you. In every code block, the comments say what each member gives you or does. A line starting with `Technical:` holds extra detail for experienced modders. You can skip it on a first read.

A phone number here is a string of digits, with `*` standing for key 10 and `#` for key 11. This is the same form `Phones.All()` gives you. The number has to be one the game's phone book knows. An empty string, any other character, or a number that matches nothing makes the call fail.

- **A number that is hidden in the phone book can't be dialled.** The game does not let a phone reach a hidden entry either.
  - Technical: the string is turned into a `PhoneNumber` by searching `PhoneNumberDatabase.database` with the game's own `PhoneNumber.ValidateNumber`. An entry marked `notIndexed` can't be found that way, as in the game.

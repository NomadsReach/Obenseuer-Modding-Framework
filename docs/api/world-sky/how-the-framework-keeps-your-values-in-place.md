---
title: How the framework keeps your values in place
parent: World sky
grand_parent: Places and travel
nav_order: 5
---

# How the framework keeps your values in place

You do not need to know this to use the API. It helps when your values look wrong.

## Things to know

- The game rewrites these values itself, so the framework writes yours again after the game. The game updates the ambient light every frame, the sun light, the sky and the fog colour on a timer, and the fog when the player goes under water. The framework puts your values back just before the frame is drawn. <span class="tech">(ambient light is written by `AzureEnvironmentController`, and the sun light, skybox and fog colour by `SunDirection`. The framework writes the override again in `Application.onBeforeRender`, and also before the sky shader values are pushed (`AzureSkyRenderController.UpdateShaderUniforms`), because the stars, clouds and exposure of the Azure sky are read in that push.)</span>
- The framework remembers the value the game last wrote for each property. When you clear your values, it writes that remembered value back, so the sky returns to what the game would show. If a part of the game that the framework does not watch has written a newer value in the meantime, the framework leaves it alone. <span class="tech">(around each game writer, the framework puts the remembered value back, lets the writer run, and reads the result as the new remembered value, so a game write that equals your override is not lost. The writers it watches are `UnderwaterEffect.EnableEffect` and `DisableEffect`, `SunDirection.Update`, `LightsController.Update` and `LightsController.StartChangeAmbientColor`.)</span>
- The framework looks for sky objects in the scene at most once every 120 frames (about every 2 seconds at 60 frames a second), and only while one of your values is set or still being held. A sun light that is no longer found, a sky controller that was replaced and a skybox material that was replaced all get their remembered value written back.

## Limits

- A game script or a mod that writes the same value the framework last wrote cannot be told apart from no write. The earlier remembered value is then kept.

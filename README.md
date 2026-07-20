# The Bright Knight

## Game concept

The Bright Knight is a third-person 3D dungeon-action game. A lone knight enters a lightless dungeon, defeats the Shadow Wardens guarding each chamber, and physically recovers the lightbulbs they drop. Every room must be fully cleared and every dropped bulb collected before the deeper door will open. Reaching the final door restores light to the dungeon.

The hub contains a goblin shop where recovered lightbulbs can be traded for permanent upgrades during the current play session.

## Controls

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move | `WASD` or arrow keys | Left stick |
| Attack | Left mouse button or `Enter` | West button |
| Parry / lower guard | `Q` | Not currently bound |
| Jump | `Space` | South button |
| Interact with doors/shop | `X` or `E` | North button |
| Restart after winning/losing | `R` or the Restart button | Use the Restart button |

Parry is a toggle: press `Q` once to raise the guard and again to lower it. Attacking also lowers the guard.

## Objective

1. Enter the dungeon from the hub.
2. Defeat every Shadow Warden in the current room.
3. Walk over every glowing lightbulb the enemies drop to pick it up.
4. Use the newly unsealed Deeper door.
5. Clear all four rooms and use the Final door to win.

The Exit door remains available as a retreat. Collected currency stays with the knight, so it can be spent at the hub shop before starting a new run.

## Gameplay systems

- **Four-room dungeon run:** Torch Hall, Split Library, Spike Gallery, and the final Vault are generated at runtime with distinct layouts, props, lighting, and increasing enemy counts.
- **Combat:** Sword attacks consume mana and damage enemies in front of the knight. The Spark Spell shop upgrade adds attack damage.
- **Enemy AI:** Shadow Wardens pursue the player, steer around nearby obstacles, display health bars, attack at close range, and grow tougher deeper in the dungeon.
- **Parry and counter:** Guarding blocks a Warden's melee hit and counterattacks it. Radiant Guard improves counter damage.
- **Physical collectibles:** Defeated enemies drop spinning, glowing lightbulb objects. There are 14 collectible drops across a complete four-room run; currency is never awarded merely for entering a door.
- **Locked progression doors:** Deeper and Final doors remain sealed until the room has no enemies and all its dropped lightbulbs have been picked up. Door prompts and colors show their current state.
- **Resources and progress tracking:** Health, regenerating mana, wallet currency, room number, remaining enemies, room pickups, total run pickups, and the current objective are shown in the UI.
- **Goblin shop:** Lightbulbs buy repeatable max-health and max-mana improvements plus the one-time Spark Spell and Radiant Guard upgrades.
- **Challenges:** Enemy combat/AI, damaging spike floors, room obstacles, locked areas, and limited health/mana all affect the route.
- **Win/lose/restart flow:** Health reaching zero or falling out of the world causes a loss. Clearing the Final door causes a victory. Both states show an ending overlay and allow a full restart.
- **Audio and presentation:** The project generates original dungeon ambience and event sounds at runtime. Emissive pickups, enemy eyes, hit/death particles, torch and crystal lights, fog, styled HUD panels, enemy health bars, and door-state colors provide visual feedback.



## External assets and resources

- [Toon RTS Units - Demo by Polygon Blacksmith](https://assetstore.unity.com/packages/3d/characters/toon-rts-units-demo-69687) — knight model and knight animations; Unity Asset Store license.
- [Polygon City Pack - Environment and Interior (Free) by WAND AND CIRCLES](https://assetstore.unity.com/packages/3d/polygon-city-pack-environment-and-interior-free-101685) — optional barrels, boxes, shelves, tables, pillars, and stones; Unity Asset Store license.
- [Kenney Platformer Kit](https://kenney.nl/assets/platformer-kit) — source of the existing platformer FBX props, including the spike model used in the Spike Gallery; CC0.
- [Unity Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.4/manual/index.html), [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/index.html), and [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) — rendering, controls, and UI text.

All new Shadow Warden geometry, lightbulb pickup geometry, materials, particles, UI styling, ambience, and sound effects in this update are generated inside Unity and use no additional external asset files.

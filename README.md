# Prey 2 Project — Movement / Perk / Inventory Foundation

A scriptable basis for a sci-fi FPS parkour game (spiritually: the cancelled Human Head *Prey 2*, with heavier RPG systems). Unity **6.3 LTS**, C#, new Input System, `CharacterController`-based movement.

## What's here

| System | Files | Notes |
|---|---|---|
| **Movement** | `Movement/PlayerMotor.cs`, `Movement/PlayerMovementController.*.cs` | One component split into partial files, one per mechanic. |
| **Abilities / gating** | `Player/AbilityId.cs`, `Player/AbilityUnlocks.cs` | Every advanced move checks `AbilityUnlocks.Has(...)`. |
| **Perks (RPG backbone)** | `Perks/PerkDefinition.cs`, `Perks/PerkEffect.cs`, `Perks/PerkTree.cs` | Perk nodes grant abilities, buff stats, or mod weapons. |
| **Stats** | `Player/PlayerStats.cs` | Modifier stack perks feed; movement reads *effective* values. |
| **Input / camera** | `Player/PlayerInputReader.cs`, `Player/FirstPersonLook.cs` | Code-defined bindings, no `.inputactions` asset needed. |
| **Inventory** | `Inventory/*.cs` | Quick-bar (combat) + manual RPG grid. |
| **Tuning** | `Config/MovementConfig.cs` | All movement numbers, authorable as an asset. |
| **Combat / weapons** | `Combat/*.cs` | Weapons, damage, health — perk-scaled, hitscan + projectile. |

### Base kit vs. perk-gated
- **Base (always on):** Sprint, Crouch, **Slide** (momentum-based), Jump, **Mantle**, **Wall-run**, **Wall-jump**.
- **Perk-gated:** Double-jump, Air-dash, Grapple, Ground-slam — plus any stat/weapon augments you author.

## Scene setup (5 minutes)

1. **Player object:** create a GameObject, add `CharacterController` (e.g. height 1.8, radius 0.35), tag it Player.
2. Add these components to it: `PlayerMotor`, `PlayerInputReader`, `PlayerStats`, `AbilityUnlocks`, `PerkTree`, `PlayerMovementController`, `InventorySystem`, `InventoryInputBridge`. (Optional `LineRenderer` for the grapple rope.)
3. **Camera:** child a `Camera` under a "CameraPivot" empty (at eye height ~1.6). Add `FirstPersonLook` to the player; assign `playerBody` = the player transform, `cameraPivot` = the pivot, `cam` = the camera, `input` = the `PlayerInputReader`.
4. **Config:** right-click in Project ▸ *Create ▸ FPS Parkour ▸ Movement Config*, assign it to the controller's `Config` field.
5. Set the config's **World Mask** / **Grapple Mask** to your environment layer(s).
6. Wire the remaining serialized refs on the controller (motor/input/config/look/stats/unlocks) — most auto-resolve in `Awake`, but assign `look` explicitly.
7. **Script Execution Order** (Project Settings ▸ Script Execution Order): put `PlayerInputReader` *before* `PlayerMovementController` so input is fresh each frame.

## Controls (default bindings)

| Action | Key |
|---|---|
| Move | WASD |
| Look | Mouse |
| Jump / Double-jump / Wall-jump | Space |
| Sprint | Left Shift |
| Crouch / Slide / Ground-slam (in air) | Left Ctrl |
| Air-dash | Left Alt |
| Grapple (hold) | Q |
| Fire | Left Mouse |
| Aim down sights | Right Mouse |
| Reload | R |
| Switch weapon | 1–4 / Mouse wheel |
| Inventory (full grid) | Tab |
| Quick-bar slots | 1–4 |
| Interact / ask / detain | E |
| Throw snare gadget | G |
| Scan a stranger (hold) | V |
| Perk tree | P |

> Grapple moved off Right Mouse (now **Q**) so aiming can use Right Mouse — the standard FPS binding. Movement code is unchanged; only the binding moved.

## Authoring perks (the RPG layer)

Create a perk: *Create ▸ FPS Parkour ▸ Perk*. Give it a cost, prerequisites (tree edges), and one or more **effects**. Grant points with `PerkTree.AwardSkillPoints(n)`, then `PerkTree.TryUnlock(perk)`. Example sci-fi augments this system supports out of the box:

| Perk | Effect(s) |
|---|---|
| **Kinetic Dampers** | `GrantAbility: GroundSlam` |
| **Neuro-Grapple** | `GrantAbility: Grapple` |
| **Reflex Booster** | `GrantAbility: AirDash` + `PlayerStat DashCharges +1` |
| **Overclocked Legs** | `PlayerStat WallRunDurationMult +40%`, `PlayerStat SprintSpeedMult +15%` |
| **Grav-Weave Musculature** | `GrantAbility: DoubleJump` + `PlayerStat GravityMult ×0.9` |
| **Dermal Plating** | `PlayerStat MaxHealth +50`, `PlayerStat FallDamageResist +0.5` |
| **Smart-Link Optics** | `WeaponStat SpreadMult ×0.7`, `WeaponStat FireRateMult +10%` |
| **Extended Mags** | `WeaponStat MagSizeAdd +8` |

Adding a brand-new *kind* of effect = add an enum value in `PerkEffect.PerkEffectKind` and a case in `PerkTree.ApplyEffects`.

## Weapon system

Files in `Combat/`:

| File | Role |
|---|---|
| `WeaponDefinition` | ScriptableObject: fire mode (semi/auto/burst), hitscan vs projectile, damage + type, ammo/reload, spread, recoil, ADS, pellets, VFX hooks. Authorable via *Create ▸ FPS Parkour ▸ Weapon*. |
| `WeaponInstance` | Runtime ammo state for one owned weapon (so ammo persists across switches). |
| `WeaponController` | The brain: switching, ADS, reload, fire-mode logic, spread, recoil. Reads every stat through `PlayerStats.ModifyWeapon`, so weapon-mod perks apply live. |
| `Projectile` | Travelling round (energy bolt); step-raycast so fast shots don't tunnel. |
| `DamageInfo` / `IDamageable` | The damage contract. Damage types: Kinetic/Energy/EMP/Explosive/Corrosive. |
| `Health` / `Hitbox` | Generic target health w/ per-type resistances; `Hitbox` on a child collider = weak points / headshots. `PlayerStats` also implements `IDamageable`. |

**Setup:** add `WeaponController` to the player; assign `input`, `look`, `stats`, and (optional) an `aimOrigin` (camera) and `weaponSocket`. Author a couple of `WeaponDefinition` assets and drop them in **Starting Loadout** — it's playable immediately. Give enemies a `Collider` + `Health` (and child `Hitbox` colliders for headshots). All wired via events (`OnAmmoChanged`, `OnFired`, `OnWeaponChanged`, `OnReloadStarted/Completed`, `OnAimChanged`) for a HUD to bind.

**Perk hook:** the `WeaponStat` effects from the perk tree (Smart-Link Optics, Extended Mags, etc.) flow straight into `WeaponController`'s effective damage / fire-rate / mag / reload / spread — no weapon code changes needed.

## How the layers connect

```
PerkTree.TryUnlock(perk)
   ├─ effect GrantAbility  → AbilityUnlocks.Grant(id)   → movement mechanic becomes usable
   ├─ effect PlayerStat    → PlayerStats.AddModifier(...) → movement reads new effective value
   └─ effect WeaponStat    → PlayerStats.AddWeaponModifier(...) → (future) weapon system reads it
```

Movement never asks "is this perk owned?" — only "is this ability unlocked / what's my effective speed?". That keeps perks and mechanics decoupled.

## Built 2026-09-04 (see `~/Local Docs/Prey 2 Project/decisions/`)
This is now a real Unity project (HDRP, 6000.3.7f1) with **13 assembly definitions**.

| System | Assembly | Notes |
|---|---|---|
| **Enemy AI** | `FPSParkour.AI` | Senses (sight cone + hearing + LoS), state brain on a `NavMeshAgent`, ranged weapon, and **`Subduable`** — the non-lethal downed state. |
| **Bounty layer** | `FPSParkour.Bounty` | Contracts, registry, credits, reputation, contract board. Capture / kill / release. |
| **Narrative** | `FPSParkour.Narrative` | `StoryFlags` registry, `DialogueGraph` + `DialogueRunner` with `[CHOICE]` branches, weighted flag-gated barks. |
| **Save/load** | `FPSParkour.Save` | `ISaveable` across flags, bounties, credits, reputation. |
| **Open world** | `FPSParkour.World` | Additive district streaming with hysteresis, capped recycling crowd, ambient bark zones. |
| **HUD/UI** | `FPSParkour.UI` | Health, ammo, credits, dialogue, contract board, resolution prompt, perk tree, inventory. |
| **Ground-slam AoE** | `FPSParkour.Movement` | `GroundSlamImpact` — the hook this README left open. |

| **Investigation** | `FPSParkour.Investigation` | Identity marks, crowd identities, the dossier, district heat, the scanner, bystanders/informants, and the hunt director that joins stalking to chasing. |

**The tech tree is designed and authored (ADR 0010):** 23 nodes, four branches, four tiers, 58 points
to own all of it. `PerkTreeBuilder` is the single source — `GymContentBuilder` no longer carries its
own perk list. Points come from `ContractProgression`: captured 2, killed 1, released 0. Press **P**.

**Character art (ADR 0011):** first-person arms + rifle viewmodel, and bodies for the crowd, guards
and bounty target — imported from the existing `Unity Conversions` library via
*FPS Parkour ▸ Art ▸ 1/2*. All of it is optional; the builders fall back to capsules when it is
absent. View-layer code lives in `FPSParkour.Presentation`.

**Wired 2026-09-05 (ADR 0012):** the capture/kill/release prompt, HUD, contract board, save/load,
bark subtitles, hit markers and player death — all previously written and instantiated nowhere.
`RosterImporter` turns `BOUNTY_ROSTER.md` into **39 contract assets**.

**Still not built:** audio of any kind (zero `AudioSource` in the project), and alt-fire / weapon
abilities. Deliberately skipped rather than cut into
`WeaponController` — see ADR 0006.

## Verification note (resolved, and superseded)
The old note here said these scripts had never been compiled in a Unity project. **They have now,
and they were clean.** The only blocker was a vendor package: the HDRP template pins Input System
**1.12.0**, which does not compile on 6000.3.7f1 (`BuildTarget.ReservedCFE`). Pinned to **1.18.0**.

Unity batchmode now reports **exit 0, zero errors, all 13 assemblies built**.

**But nothing has ever been run.** There is no scene, no prefab and no content asset. Compiling is
not behaviour — see ADR 0006 for exactly what is and is not verified.
```

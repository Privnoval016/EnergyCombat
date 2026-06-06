# Combat System — User Guide

A layered, data-driven combat framework for the EnergyCombat rogue-like. Everything lives in `namespace Combat`.

---

## Architecture

```text
Input Buffer  →  Ability Selector  →  Ability Pipeline  →  Modifiers  →  Presentation
(CombatInputBuffer  (combo tree +        (async phases:      (stats,       (IAnimationDriver
 buffered events)    loadout scan)        startup/active/     structural,   IHitboxController)
                                          recovery + custom)  dynamic)
```

All layers communicate through `CombatContext` — one instance per ability run, discarded on completion.

Key entry points:
| File | Role |
|---|---|
| [CombatController.cs](Orchestration/CombatController.cs) | MonoBehaviour facade — own all subsystems, public API |
| [AbilitySelector.cs](Orchestration/AbilitySelector.cs) | Input → ability resolution; owns combo + interrupt state |
| [AbilityExecutor.cs](Execution/AbilityExecutor.cs) | Builds and runs the pipeline; notifies selector on finish |
| [AbilityDefinition.cs](Abilities/AbilityDefinition.cs) | ScriptableObject — designer-facing ability data |
| [CombatContext.cs](Core/CombatContext.cs) | Per-run mutable state passed through every phase |

---

## Quick-Start: Your First Ability

### 1. Create an Ability Definition

`Assets → Create → Combat → Ability Definition`

| Field | What to set |
|---|---|
| **AbilityName** | `"LightSlash"` |
| **Tags** | `CombatTag.Melee` |
| **PrimaryInput** | `LightAttack` |
| **BaseStats** | `Damage = 15`, `Knockback = 5` |
| **AnimationRequest.Clip** | Drag in the animation clip |
| **Phases** | `[+]` → `StartupPhase`, then `ActivePhase`, then `RecoveryPhase` |

### 2. Configure the Phases

**StartupPhase** — frames before the hit lands
- `StartupEndEvent`: `"StartupEnd"` (animation event name)
- `FallbackDuration`: `0.15`

**ActivePhase** — the hit window
- `Hitboxes[0].HitboxId`: `"Sword"` (must match `WeaponHitboxController.HitboxId`)
- `Hitboxes[0].Config`: shape=Sphere, Radius=0.4, TargetLayers=Enemy
- `FallbackDuration`: `0.2`

**RecoveryPhase** — returning to neutral
- `FallbackDuration`: `0.25`
- `AllowComboCancel`: ✓

### 3. Create an Ability Loadout

`Assets → Create → Combat → Ability Loadout`

Drag your `AbilityDefinition` assets into `ActiveAbilities`. Create one per weapon type.

### 4. Wire Up the Character Prefab

On the **Player** GameObject:
- Add `CombatController` component
- Set `_defaultLoadouts[0]` → your `AbilityLoadout`

On each **weapon bone** (e.g. `RightHand/Sword`):
- Add `WeaponHitboxController`
- Set `HitboxId` = `"Sword"`
- Optionally add `HitboxDebugDrawer` for Gizmos

`CombatController` auto-discovers all `WeaponHitboxController` components at Awake — no manual wiring needed.

### 5. Route Input

`PlayerController` already calls `combatController.PushInput(...)`. No additional wiring required.

---

## Multiple Loadouts (Dual-Wield)

`CombatController` supports any number of simultaneous loadouts. All are searched in order — first ability match wins.

```csharp
// At start — assign via Inspector _defaultLoadouts[], or at runtime:
combatController.AddLoadout(swordLoadout);
combatController.AddLoadout(axeLoadout);

// Drop a weapon
combatController.RemoveLoadout(axeLoadout);

// Full kit swap
combatController.SetLoadouts(new[] { newLoadout });

// Single-weapon convenience (replaces all, resets combo)
combatController.EquipLoadout(greatswordLoadout);
```

---

## Combo System

### Data Structure

Combos use a **flat node list** on one `ComboDefinition` ScriptableObject. Transitions reference other nodes by integer index — no object recursion, no Unity serializer warnings.

```
ComboDefinition (ScriptableObject)
  Nodes[0]: Ability=LightSlash2,  WindowDuration=0.6
    Transitions[0]: Button=Light → TargetNodeIndex=1
    Transitions[1]: Button=Heavy → TargetNodeIndex=2
  Nodes[1]: Ability=LightSlash3,  WindowDuration=0.5
    Transitions=[]                   ← combo ends here
  Nodes[2]: Ability=HeavyFinisher, WindowDuration=0
    Transitions=[]
  MaxConcurrentInterrupts: 2
```

- Index **0** is always the root (first follow-up after the opener).
- `TargetNodeIndex = -1` means the combo ends after that step.

### Setting Up a Combo

1. Create a `ComboDefinition` asset (`Assets → Create → Combat → Combo Definition`)
2. Add nodes to `Nodes` list in the Inspector
3. On each node set `Ability`, `ComboWindowDuration`, and `Transitions`
4. In each `Transition` set `Button` and `TargetNodeIndex`
5. Assign the `ComboDefinition` to `LightSlash.FollowUpCombo`

### Pause Combos (e.g. "Light, pause, Light")

On a `ComboTransition`:
- `MinPauseDuration: 0.3`
- `MaxPauseDuration: 0.8`

The selector only follows this edge if the player waited 0.3–0.8 seconds between inputs.

---

## Combo Interrupt Behavior

`AbilityDefinition.InterruptBehavior` controls what happens to the active combo chain when a new ability fires mid-execution.

| Value | Effect |
|---|---|
| `BreakCombo` (default) | Resets the combo chain immediately. Use for specials, launches, and dodges. |
| `PreserveCombo` | Keeps the combo chain alive. Timer keeps running. Use for secondary-weapon basics. |

Example — off-hand axe basic attack that doesn't destroy the main-hand sword combo:
1. Set the axe basic ability `InterruptBehavior = PreserveCombo`
2. On the sword's `ComboDefinition` set `MaxConcurrentInterrupts = 2`

`MaxConcurrentInterrupts` caps how many consecutive `PreserveCombo` interrupts are tolerated before the chain resets anyway. `0` means unlimited.

---

## Ability Phases — The Extension Point

Every ability's execution timeline is a list of `AbilityPhase` objects assigned via `[SerializeReference]` in the Inspector. Phases are `[Serializable]` inline classes — no separate assets, no boilerplate.

```csharp
[Serializable]
public class MyCustomPhase : AbilityPhase
{
    [Range(0f, 5f)] public float Duration = 1f;

    public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        // ... do multi-frame async work ...
        await UniTask.WaitForSeconds(Duration, cancellationToken: token);
    }
}
```

The moment you write this class it appears in the `[+]` type picker on any `AbilityDefinition.Phases` field. Parameters are tunable per-ability in the Inspector.

### Key rules for custom phases

1. **Always `token.ThrowIfCancellationRequested()`** at the top — this is how you respect ability cancellation.
2. **Always `finally` your cleanup** — hitboxes, applied forces, particle systems. They must clean up even when cancelled.
3. **Phases hold config, not state** — all runtime data goes in `CombatContext` or local variables inside `ExecuteAsync`.
4. **Never await without forwarding the token** — use `cancellationToken: token` on every `UniTask.WaitForSeconds`, `UniTask.Delay`, etc.

### Built-in Phases

| Phase | File | Purpose |
|---|---|---|
| `StartupPhase` | [Phases/StartupPhase.cs](Pipeline/Phases/StartupPhase.cs) | Waits for an animation event or fallback timer before the hit |
| `ActivePhase` | [Phases/ActivePhase.cs](Pipeline/Phases/ActivePhase.cs) | Activates named hitboxes; deactivates them in `finally` |
| `RecoveryPhase` | [Phases/RecoveryPhase.cs](Pipeline/Phases/RecoveryPhase.cs) | Waits for neutral return; sets `ComboWindowOpen` tag |

### Sample Phases

All in [Pipeline/Phases/Samples/](Pipeline/Phases/Samples/).

| Phase | Key Parameters | Use Case |
|---|---|---|
| `WaitPhase` | `Duration` | Fixed pause between phases |
| `ApplyTagPhase` | `TagsToApply[]` | Set context tags for downstream gating |
| `ApplyForceToHitsPhase` | `ForceVector`, `Mode`, `UseAttackerOrientation` | Knockback all hit targets |
| `LaunchPhase` | `LaunchSelf`, `LaunchForce`, `LaunchHitTargets`, `TargetLaunchForce`, `HangDuration` | Aerial-combo opener — launches attacker and targets |

### Example: Aerial Launch Ability

Pipeline for a "Uppercut Launch" that sends both player and targets into the air for follow-up air combos:

```
Phases:
  [0] StartupPhase   FallbackDuration=0.1
  [1] ActivePhase    Hitboxes=[{HitboxId="Fist", Config=Sphere r=0.5}]  FallbackDuration=0.15
  [2] LaunchPhase    LaunchSelf=true  LaunchForce=12
                     LaunchHitTargets=true  TargetLaunchForce=9
                     HangDuration=0.4
  [3] RecoveryPhase  FallbackDuration=0.2  AllowComboCancel=true
```

`FollowUpCombo`: assign a `ComboDefinition` containing your air-combo nodes.
`Conditions`: add `GroundedCondition` so this can only trigger on the ground.

---

## Stats System

Stats are identified by `StatId` (serializable struct). Built-in constants:

```
StatId.Damage   StatId.AttackSpeed   StatId.Range
StatId.Knockback   StatId.StaminaCost   StatId.HitStun
```

On `AbilityDefinition.BaseStats` add entries: `Stat: Damage | BaseValue: 20`.

### Modifiers

```csharp
// +5 flat bonus
combatController.AddModifier(new DamageStatModifier(StatModifier.Additive(5f, "Sword Upgrade")));

// 20% multiplier
combatController.AddModifier(new DamageStatModifier(StatModifier.Multiplicative(0.2f, "Fire Rune")));

// Time-limited (expires after 10s)
combatController.AddModifier(new TemporalCombatModifier(
    new DamageStatModifier(StatModifier.Additive(15f, "Rage Buff")), duration: 10f));
```

Evaluation order (deterministic): `base + Σ(Additive)` × `(1 + Σ(Multiplicative))` → highest-priority `Override` wins if present.

---

## Modifier Types

Three injection points, applied in this order:

| Interface | When | Use Case |
|---|---|---|
| `IStatContributor` | Before pipeline build | Damage bonuses, speed buffs |
| `IStructuralModifier` | Before pipeline snapshot | Insert extra steps, wrap existing ones |
| `IDynamicModifier` | During / after execution | On-hit effects, tag reactions, VFX triggers |

### Custom Dynamic Modifier

```csharp
public class BurnOnHitModifier : IDynamicModifier
{
    public Tag[] Tags => Array.Empty<Tag>();
    public bool IsExpired => false;
    public string DebugLabel => "Burn On Hit";

    public void OnCombatEvent(ICombatEvent evt, CombatContext context)
    {
        if (evt is not HitEvent hit) return;
        // apply burn status to hit.Hit.Target
    }
}

combatController.AddModifier(new BurnOnHitModifier());
```

---

## Conditions

Add to `AbilityDefinition.Conditions` in the Inspector to gate execution.

| Condition | Passes when |
|---|---|
| `GroundedCondition` | Character is on the ground |
| `AirborneCondition` | Character is not grounded |
| `SprintingCondition` | Character is sprinting |
| `HasTagCondition` | CombatContext has a specific tag |

Conditions compose without code via `AndCondition`, `OrCondition`, `NotCondition` from `Extensions.Logic`.

---

## Multiple Hitboxes

One attack can activate multiple hitboxes simultaneously. In `ActivePhase.Hitboxes`:

```
Hitboxes[0]: HitboxId="Sword"  Config=(Sphere, Radius=0.4)
Hitboxes[1]: HitboxId="Fist"   Config=(Sphere, Radius=0.25)
```

Both activate at phase entry; both deactivate in `finally`.

---

## Animation Integration

### Default (NullAnimationDriver)

`CombatController` uses `NullAnimationDriver` by default — all animations complete immediately. Lets the full pipeline work without animation setup.

### Integrating Animancer

1. Import Animancer via Package Manager
2. Add `AnimancerAnimationDriver` component to the weapon/character
3. Follow TODO comments in [Animation/AnimancerAnimationDriver.cs](Animation/AnimancerAnimationDriver.cs)
4. Assign to `CombatController._animationDriverSource`

### Animation Event Names

| Phase | Default event string | Meaning |
|---|---|---|
| Startup | `"StartupEnd"` | Hitbox becomes active |
| Active | `"ActiveEnd"` | Hitbox deactivates |
| Recovery | `"RecoveryEnd"` | Character returns to neutral |

---

## Debug Overlay

Add `CombatDebugOverlay` to any GameObject. Assign `Target` → your `CombatController`. Press **F2** to toggle.

Shows: active ability, combo window state, attack tags, stat breakdown with modifier contributions, active modifiers, last 5 input events.

---

## Design Rules

1. **Phases hold config, not state** — runtime data goes in `CombatContext` or local variables.
2. **Always `finally` your cleanup** — hitboxes, forces, and timers must clean up even on cancellation.
3. **Never mutate the pipeline during execution** — structural modifiers run before `ExecuteAsync`.
4. **One `CombatContext` per execution** — never reuse contexts across ability runs.
5. **Conditions are cheap** — evaluated synchronously before execution; keep them fast.
6. **Token discipline** — every `await` must forward `cancellationToken: token`.

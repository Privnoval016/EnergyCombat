# Combat System — Setup & Debug Guide

---

## Minimal Working Setup

Follow these steps in order. At the end you'll have attacks triggering, hitboxes activating, and the state machine switching into `AttackingState`.

### Step 1 — Create an Ability Definition

`Assets → Create → Combat → Ability Definition`

Fill in the Inspector:
- **AbilityName**: `"LightSlash"`
- **PrimaryInput**: `LightAttack`
- **Tags**: add `Melee` (Odin dropdown button lists all options)
- **Phases** (click `[+]` on the list to add each):
  1. `StartupPhase` — `FallbackDuration = 0.15`
  2. `ActivePhase` — expand Hitboxes, add one entry: `HitboxId = "Sword"`, Config shape=Sphere Radius=0.4
  3. `RecoveryPhase` — `FallbackDuration = 0.25`, `AllowComboCancel = true`

> **No animation clip yet?** Leave `AnimationRequest.Clip` empty. `NullAnimationDriver` takes over and the full pipeline still runs — the state machine **will** switch into `AttackingState`.

### Step 2 — Create an Ability Loadout

`Assets → Create → Combat → Ability Loadout`

Drag your `AbilityDefinition` into `Active Abilities[0]`.

### Step 3 — Wire Up the Player Prefab

On the **Player** root GameObject:
- Add `CombatController` component
- Set `_defaultLoadouts[0]` → your new `AbilityLoadout`
- Leave `_animationDriverSource` empty (uses `NullAnimationDriver` automatically)

### Step 4 — Add a Hitbox Controller

On a **child** GameObject (e.g. `Player/RightHand/Sword`):
- Add `WeaponHitboxController` component
- Set `HitboxId` = `"Sword"` (must match what you typed in Step 1)
- Optionally add `HitboxDebugDrawer` to see the sphere in the Scene view

`CombatController` scans all child `WeaponHitboxController` components at Awake — no manual assignment needed.

### Step 5 — Press Play and Test

Hit the **LightAttack** button. The `CombatController` picks up the input, resolves the ability, and runs the pipeline. The player state machine enters `AttackingState` for the duration.

Press **F2** to open the `CombatDebugOverlay` — it shows the active ability, combo state, stat sheet, active modifiers, and last 5 input events in real time.

---

## Stats — What They Are and How to Use Them

Stats in this system are **per-execution scratch values**. They live on `CombatContext.Stats` (a `StatSheet`) and are created fresh for each ability run, then discarded.

They are NOT persistent character stats. Think of them as the final calculated numbers for one swing of the sword.

### Predefined stat names

```
StatId.Damage       StatId.AttackSpeed    StatId.Range
StatId.Knockback    StatId.StaminaCost    StatId.HitStun
```

### Assigning base values (Inspector)

On `AbilityDefinition.BaseStats`, click `[+]` and enter:

| Stat | Base Value |
|---|---|
| `Damage` | `20` |
| `Knockback` | `6` |

Type the stat name directly — it's a free string field backed by `StatId`.

### Applying modifiers in code

```csharp
// +5 flat damage from a sword upgrade rune
combatController.AddModifier(new DamageStatModifier(
    StatModifier.Additive(5f, source: "Sword Upgrade")));

// 20% damage multiplier from a fire enchantment
combatController.AddModifier(new DamageStatModifier(
    StatModifier.Multiplicative(0.2f, source: "Fire Rune")));

// Expires after 10 seconds
combatController.AddModifier(new TemporalCombatModifier(
    new DamageStatModifier(StatModifier.Additive(15f, "Rage Buff")), duration: 10f));
```

Evaluation order: `(base + Σ additive) × (1 + Σ multiplicative)`. Override with highest priority wins if present.

### Reading stats during execution

In a custom `AbilityPhase` or `IDynamicModifier`:

```csharp
float damage = context.Stats.Get(StatId.Damage);
```

In `WeaponHitboxController.OnHit` (your hit handler):

```csharp
void OnHitTarget(HitData hit)
{
    float damage = hit.Context.Stats.Get(StatId.Damage);
    // Apply damage to hit.Target's health component
}
```

Stats intentionally do nothing on their own — the system stays out of health and status logic.

---

## Events

All events are dispatched through the global `EventBus<T>` (from `Extensions.EventBus`). Subscribe from any MonoBehaviour:

```csharp
using Extensions.EventBus;
using Combat;

public class MyListener : MonoBehaviour
{
    void OnEnable()
    {
        EventBus<AbilityStartedEvent>.Subscribe(OnAbilityStarted);
        EventBus<AbilityEndedEvent>.Subscribe(OnAbilityEnded);
        EventBus<HitEvent>.Subscribe(OnHit);
    }

    void OnDisable()
    {
        EventBus<AbilityStartedEvent>.Unsubscribe(OnAbilityStarted);
        EventBus<AbilityEndedEvent>.Unsubscribe(OnAbilityEnded);
        EventBus<HitEvent>.Unsubscribe(OnHit);
    }

    void OnAbilityStarted(AbilityStartedEvent e)
    {
        Debug.Log($"Started: {e.Ability.AbilityName}");
    }

    void OnAbilityEnded(AbilityEndedEvent e)
    {
        Debug.Log($"Ended: {e.Ability.AbilityName}, interrupted={e.WasInterrupted}");
    }

    void OnHit(HitEvent e)
    {
        // e.Hit contains: Source, Target, Point, Normal, Damage, AttackTags, Context
        float dmg = e.Hit.Context.Stats.Get(StatId.Damage);
        Debug.Log($"Hit {e.Hit.Target.name} for {dmg} damage");
    }
}
```

### Event reference

| Event | Payload | When |
|---|---|---|
| `AbilityStartedEvent` | `Ability`, `Context` | Pipeline begins executing |
| `AbilityEndedEvent` | `Ability`, `WasInterrupted` | Pipeline ends (normally or cancelled) |
| `HitEvent` | `Hit` (HitData) | Each physics contact during `ActivePhase` |

---

## Debug Checklist — Why Nothing Is Happening

Work through this list top-to-bottom.

**1. No loadout assigned**
Open the `CombatController` Inspector. `_defaultLoadouts` must have at least one entry. If the array is empty, no inputs will ever resolve to an ability.

**2. Loadout has no abilities**
Click the `AbilityLoadout` asset. `ActiveAbilities` must be non-empty.

**3. Input not reaching the controller**
Press **F2** (CombatDebugOverlay) and check "last 5 inputs". If no inputs appear, the `PlayerController` is not calling `combatController.PushInput(...)`. Confirm `PlayerController._combatController` is assigned in the Inspector.

**4. Ability found but conditions block it**
The overlay shows "active ability: —" while inputs are registering. Open the `AbilityDefinition` in the Inspector and check `Conditions`. A `GroundedCondition` while airborne, or a `HasTagCondition` for a tag that isn't set, will silently block execution. Remove or disable conditions while testing.

**5. State machine not entering AttackingState**
`PlayerController.IsAttacking` reads `combatController.IsExecuting`. `IsExecuting` is `true` from the moment `AbilityExecutor.ExecuteAsync` starts — even with no animation clip. If the state never enters `AttackingState`, the ability pipeline is not running. Open the CombatDebugOverlay and verify "active ability" shows your ability name during the expected window.

**6. Hitboxes not activating**
The `ActivePhase` looks up hitboxes by ID via `context.Controller.GetHitboxController(hitboxId)`. If the ID in `ActivePhase.Hitboxes[0].HitboxId` doesn't match the `HitboxId` field on the `WeaponHitboxController` component, the lookup returns null and nothing activates silently. Check both spellings are identical (case-sensitive).

**7. Hits registering but damage not applied**
The combat system records hits in `context.RegisteredHits` but does NOT apply damage automatically. You must subscribe to `HitEvent` and apply damage in your listener (see Events section above).

---

## Useful Console Checks

```
// In CombatController, all exceptions from the pipeline are logged via:
Debug.LogException(ex);   // located in AbilityExecutor.ExecuteAsync catch block

// Enable verbose logging temporarily by adding Debug.Log calls inside:
// AbilitySelector.Resolve()   — to see what ability was resolved
// AbilityExecutor.ExecuteAsync() — to see pipeline start/end
```

---

## Wall Running & Ledge Grabbing

Both are registered automatically. You don't need any extra wiring.

**Wall Run** — auto-activates when airborne, fast, and near a wall at the right angle. Press **Jump** while wall running for a wall jump. Configure via `PlayerLocomotionConfig.WallRun` (WallRunSettings).

**Ledge Grab** — auto-activates when airborne, slow/falling, and in front of a climbable ledge edge. The character hangs briefly then auto-climbs. Configure via `PlayerLocomotionConfig.LedgeGrab` (LedgeGrabSettings).

**Post-state dash boost** — after a slide, wall run, or ledge climb, if the player is holding forward within `PostStateBoost.AngleThreshold` degrees of their facing direction, a short `DashAbility` fires automatically. Toggle with `PostStateBoost.Enabled` and tune the angle with `PostStateBoost.AngleThreshold`.

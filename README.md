# EnergyCombat

A Unity project featuring a layered combat simulation framework built on top of a physics-driven locomotion system.

---

## Overview

EnergyCombat separates combat into five independent but tightly coordinated subsystems that compose at runtime. The result is a fully data-driven ability framework where designers configure moves, combos, and modifiers in the Inspector without touching code.

**What it is:**

- An async, phase-based ability execution pipeline
- A strongly-typed tag system shared between physics and combat
- A stat evaluation system with composable modifier contributions
- A combo tree configurable per ability in ScriptableObjects
- A hitbox system decoupled from weapon geometry through interfaces

**What it is not:**

- An animation framework (animation is injected via `IAnimationDriver`)
- A networking layer
- A damage/health system (that belongs on `IHittable` implementors)

---

## Architecture

```text
┌─────────────────────────────────────────────────────────────┐
│                        PlayerController                      │
│            (routes input, owns StateMachine)                 │
└────────────────────────┬────────────────────────────────────┘
                         │ PushInput(CombatInputEvent)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 1 — Input       CombatInputBuffer                     │
│                        (buffered, timestamped events)        │
└────────────────────────┬────────────────────────────────────┘
                         │ Resolve(buffer, context)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 2 — Selection   AbilitySelector                       │
│                        (combo tree + library fallback)       │
└────────────────────────┬────────────────────────────────────┘
                         │ ExecuteAsync(definition, ...)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 3 — Pipeline    AbilityPipeline                       │
│                        (startup → active → recovery)        │
└────────────────────────┬────────────────────────────────────┘
                         │ apply, evaluate
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 4 — Modifiers   ModifierContainer                     │
│                        (structural / stat / dynamic)         │
└────────────────────────┬────────────────────────────────────┘
                         │ Play / Activate
                         ▼
┌─────────────────────────────────────────────────────────────┐
│  Layer 5 — Presentation  IAnimationDriver + IHitboxController│
│                          (injected, not hardcoded)           │
└─────────────────────────────────────────────────────────────┘
```

All layers communicate through `CombatContext` — a per-execution runtime object — and the shared `Tag` type system.

---

## Tag System

### `Tag` struct

```csharp
// Combat/Core/Tags/Tag.cs
public readonly struct Tag : IEquatable<Tag>
```

`Tag` is a strongly-typed wrapper around a string. It has value equality and an `implicit operator Tag(string)` so it blends seamlessly with legacy string code.

```csharp
Tag t = "Melee";            // implicit conversion
string s = (string)t;       // explicit conversion
```

### `TagQuery`

```csharp
// Usage: match targets that have Fire, do not have Boss, and have at least Stunned or Frozen
var query = TagQuery.Require(CombatTag.Fire)
                    .And(TagQuery.Exclude(CombatTag.Boss))
                    .And(TagQuery.Any(CombatTag.Stunned, CombatTag.Frozen));

bool hit = query.Matches(targetTagContainer);
```

`TagQuery` has three sets:

- `Required` — all must be present
- `Excluded` — none may be present
- `AnyOf` — at least one must be present (if array is non-empty)

### `ITagContainer`

Both `MotionContext` (physics) and `CombatContext` (combat) implement `ITagContainer`:

```csharp
public interface ITagContainer
{
    bool HasTag(Tag tag);
    void SetTag(Tag tag);
    void RemoveTag(Tag tag);
    void ClearTags();
}
```

### `MotionTag` migration

`MotionTag` constants were migrated from `const string` to `static readonly Tag`. All call sites compiled without changes because of the implicit conversion.

### `CombatTag` constants

```csharp
Combat.Core.CombatTag.Melee
Combat.Core.CombatTag.Ranged
Combat.Core.CombatTag.Projectile
Combat.Core.CombatTag.Grounded
Combat.Core.CombatTag.Aerial
Combat.Core.CombatTag.Fire
Combat.Core.CombatTag.Ice
Combat.Core.CombatTag.Lightning
Combat.Core.CombatTag.Charged
Combat.Core.CombatTag.Crit
Combat.Core.CombatTag.ComboStarter
Combat.Core.CombatTag.ComboFollowUp
Combat.Core.CombatTag.ComboWindowOpen
Combat.Core.CombatTag.Boss
Combat.Core.CombatTag.Burning
Combat.Core.CombatTag.Frozen
Combat.Core.CombatTag.Stunned
```

---

## Stats System

### `StatId`

```csharp
StatId.Damage
StatId.AttackSpeed
StatId.Range
StatId.Knockback
StatId.StaminaCost
StatId.HitStun
StatId.BlockStun
```

`StatId` is a `readonly struct` — same pattern as `Tag`. Create custom stat IDs with `new StatId("MyCustomStat")`.

### `StatSheet`

`StatSheet` stores base values and modifier lists. Evaluation is always deterministic:

```text
final = base
      + sum(Additive modifiers)
      × (1 + sum(Multiplicative modifiers))
      → Override with highest Priority (if any)
```

```csharp
var sheet = new StatSheet();
sheet.SetBase(StatId.Damage, 10f);
sheet.AddModifier(StatId.Damage, StatModifier.Additive(5f, "sword"));        // +5
sheet.AddModifier(StatId.Damage, StatModifier.Multiplicative(0.5f, "buff")); // ×1.5
float result = sheet.Evaluate(StatId.Damage, context);  // (10 + 5) × 1.5 = 22.5
```

Modifiers can be conditional on tags:

```csharp
// only applies if context has CombatTag.Burning
StatModifier.Additive(20f, "burn bonus", TagQuery.Require(CombatTag.Burning));
```

---

## Ability Definitions

Create via `Assets > Create > Combat > Ability Definition`.

| Field | Purpose |
| --- | --- |
| `AbilityName` | Display name |
| `Tags` | Combat tags applied to context (Melee, Fire, etc.) |
| `PrimaryInput` | Which button triggers this ability |
| `RequireHold` | Tap vs hold to activate |
| `HoldThreshold` | Seconds required for a hold |
| `Conditions` | `ICondition<CombatContext>[]` — AND'd together; all must pass |
| `BaseStats` | `AbilityStatEntry[]` — stat ID + base value pairs |
| `AnimationRequest` | Clip, speed, fade, layer, event names |
| `Phases` | Ordered pipeline phases (startup/active/recovery ScriptableObjects) |
| `ComboDefinition` | Optional combo tree for follow-up attacks |

### Creating an ability step-by-step

1. Right-click in Project → Create → Combat → Ability Definition
2. Name it (e.g. `LightAttack_01`)
3. Set `PrimaryInput = LightAttack`, `RequireHold = false`
4. Add `Tags`: drag in `CombatTag.Melee`
5. Set `BaseStats`: add entry `Damage = 15`
6. Set `Phases`: drag in a `StartupPhase`, `ActivePhase`, and `RecoveryPhase` ScriptableObject
7. Add a `HitboxConfig` to the `ActivePhase` asset (radius, layers, tag filter)
8. Assign the `AbilityDefinition` to an `AbilityLibrary` ScriptableObject
9. Assign the `AbilityLibrary` to `CombatController` in the prefab

---

## Combo System

Combos are a tree of `ComboNode` objects rooted at a `ComboDefinition` ScriptableObject.

```text
ComboDefinition
  └── RootNode (LightAttack_01)
        └── Transitions[]
              ├── [Button=Light, MaxPause=0.6s] → ComboNode (LightAttack_02)
              │     └── Transitions[]
              │           └── [Button=Heavy, MinPause=0.3s, MaxPause=0.8s] → ComboNode (HeavyFinisher)
              └── [Button=Heavy, RequireHold=true] → ComboNode (ChargedHeavy)
```

### `ComboTransition` fields

| Field | Purpose |
| --- | --- |
| `Button` | Which input advances the combo |
| `RequireHold` | Must the button be held |
| `MinPauseDuration` | Earliest the input can arrive (rhythm check) |
| `MaxPauseDuration` | Latest it can arrive (0 = no upper bound) |
| `Target` | The next `ComboNode` |

### Example: Light → Light → [pause 0.3-0.8s] → Heavy (hold)

```text
RootNode (Light_1, window=0.6s)
  Transition: Button=Light, MaxPause=0.6s → Node (Light_2, window=1.0s)
    Transition: Button=Heavy, RequireHold=true, MinPause=0.3s, MaxPause=0.8s → Node (HeavyFinish)
```

The `AbilitySelector` manages the active `ComboNode` pointer and a `CountdownTimer` for the combo window. On ability completion, `OnAbilityCompleted` advances the pointer if a follow-up was found. On interruption or timeout, `ResetCombo` returns to root.

---

## Pipeline & Phases

### Execution flow

```text
AbilityExecutor.ExecuteAsync
  1. Build CombatContext
  2. Populate StatSheet from AbilityDefinition.BaseStats
  3. Apply stat-contributing modifiers
  4. Build List<IPipelineStep> from AbilityDefinition.Phases
  5. Call ModifierContainer.ApplyStructural(steps, context)   ← mutates list
  6. SNAPSHOT the step list                                    ← no more structural changes
  7. Create CancellationTokenSource
  8. await AbilityPipeline.ExecuteAsync(context, token)
  9. finally: raise AbilityEndedEvent, reset IsExecuting
```

### Built-in phases

| Phase | Waits for | Cancellable |
| --- | --- | --- |
| `StartupPhase` | `"StartupEnd"` animation event OR fallback duration | No (by default) |
| `ActivePhase` | `"ActiveEnd"` animation event OR fallback duration | Yes; hitbox deactivated in `finally` |
| `RecoveryPhase` | `"RecoveryEnd"` OR combo-cancel | Yes; `ComboWindowOpen` tag removed in `finally` |

### Creating a custom phase

```csharp
[CreateAssetMenu(menuName = "Combat/Pipeline/My Phase")]
public class MyPhase : AbilityPhase
{
    public override async UniTask ExecuteAsync(CombatContext context, CancellationToken token)
    {
        // do something
        await UniTask.Delay(500, cancellationToken: token);
        // do something else
    }
}
```

### Cancellation

Every `await` passes the `CancellationToken`. Cancellation is propagated from `CombatController.CancelCurrentAbility()` through `AbilityExecutor`'s `CancellationTokenSource`. All `finally` blocks run on cancellation — hitboxes are deactivated, tags are cleaned up, events are fired.

---

## Modifiers

Three modifier types compose without coupling:

### Structural modifiers (`IStructuralModifier`)

Mutate the pipeline step list *before* execution begins:

```csharp
public class PipelineStepInserter : IStructuralModifier
{
    void ModifyPipeline(List<IPipelineStep> steps, CombatContext ctx)
        => steps.Insert(_index, _step);
}
```

### Stat modifiers (`IStatContributor`)

Contribute to `StatSheet` before pipeline runs:

```csharp
public class DamageStatModifier : ICombatModifier, IStatContributor
{
    public void ContributeStats(StatSheet stats, CombatContext context)
        => stats.AddModifier(StatId.Damage, _modifier);
}
```

### Dynamic modifiers (`IDynamicModifier`)

React to `ICombatEvent`s fired through `EventBus<T>`:

```csharp
public class TagGainModifier<TEvent> : IDynamicModifier where TEvent : ICombatEvent
{
    public void OnCombatEvent(ICombatEvent evt, CombatContext ctx)
    {
        if (evt is TEvent) ctx.SetTag(_tagToGain);
    }
}
```

### Temporal wrapping

Any modifier can be wrapped to expire after a duration:

```csharp
controller.AddModifier(new TemporalCombatModifier(myModifier, duration: 5f));
```

`ModifierContainer.RemoveExpired()` is called by `CombatController` each frame.

---

## Animation

### `IAnimationDriver`

```csharp
public interface IAnimationDriver
{
    AnimationHandle Play(AnimationRequest request, CancellationToken token);
    void Stop();
}
```

Two implementations provided:

- `NullAnimationDriver` — completes immediately; use for testing or headless builds
- `AnimancerAnimationDriver` — stub; wire in Animancer when the package is added

### `AnimationHandle`

Pipeline phases `await` the handle for specific animation events:

```csharp
await context.Animation.WaitForEventAsync("StartupEnd", token);
await context.Animation.WaitForNormalizedTimeAsync(0.75f, token);
await context.Animation.WaitForCompletionAsync(token);
```

The driver calls `handle.TriggerEvent(name)` from an animation event callback. Hold frames work naturally because `TriggerEvent` resolves the `UniTaskCompletionSource` for that name.

### Swapping animation backends

1. Implement `IAnimationDriver` on a MonoBehaviour
2. Assign it to `CombatController._animationDriverSource` in the Inspector
3. `CombatController.Awake` reads the interface from that component

---

## Hitbox Detection

### `IHitboxController`

```csharp
public interface IHitboxController
{
    void Activate(HitboxConfig config, CombatContext context);
    void Deactivate();
    event Action<HitData> OnHit;
    bool IsActive { get; }
}
```

### `HitboxConfig`

Configures what shape to sweep and what to hit:

```csharp
[Serializable]
public class HitboxConfig
{
    public HitboxShape Shape;      // Sphere, Capsule, Box
    public Vector3 Offset;
    public float Radius;           // Sphere / Capsule
    public float HalfHeight;       // Capsule
    public Vector3 BoxHalfExtents; // Box
    public LayerMask TargetLayers;
    public TagQuery TargetFilter;  // optional tag-based filtering
    public int MaxHitsPerSwing;
}
```

### `WeaponHitboxController` setup

1. Add `WeaponHitboxController` to the weapon bone Transform in the prefab
2. Set `WeaponTip` and `WeaponBase` transforms (for capsule sweep direction)
3. Assign it to `CombatController._hitboxControllerSource` in the Inspector
4. Configure `HitboxConfig` on the `ActivePhase` ScriptableObject asset

`WeaponHitboxController` runs `Physics.OverlapSphereNonAlloc` (or Capsule/Box) each `FixedUpdate` while active. It deduplicates hits per execution using a `HashSet<GameObject>` that is cleared on `Activate` and fires `EventBus<HitEvent>` for each new hit.

### Filtering hit targets

```csharp
// Only hit enemies that are on fire and not bosses
config.TargetFilter = TagQuery.Require(CombatTag.Burning)
                               .And(TagQuery.Exclude(CombatTag.Boss));
```

`WeaponHitboxController` calls `query.Matches(target.GetComponent<ITagContainer>())` before registering a hit.

---

## State Machine Integration

### `AttackingState`

`AttackingState` is a sibling of `GroundedState` and `AirborneState` under `ActiveState`. It has no sub-states.

- **Entry**: any grounded or airborne state transitions here when `host.IsAttacking` becomes true
- **Stay**: while `host.IsAttacking` is true
- **Exit**: when `host.IsAttacking` becomes false → returns to grounded (→ Idle) or airborne (→ Fall) based on `host.IsGrounded`

`AttackingState` does **not** drive combat logic. It only mirrors the state of `CombatController.IsExecuting` in the locomotion state machine.

### Attack cancels dash and slide

All leaf states (Idle, Move, Sprint, Slide, Dash, Jump, Fall) check `if (host.IsAttacking) return host.GetState<AttackingState>()` as their first transition. This means a `LightAttack` input mid-dash will immediately exit the dash animation and enter `AttackingState`.

---

## Events

All combat events implement `ICombatEvent` (which extends `IEvent` from `Extensions.EventBus`).

| Event | Fields |
| --- | --- |
| `AbilityStartedEvent` | `Ability`, `Context` |
| `AbilityEndedEvent` | `Ability`, `WasInterrupted` |
| `HitEvent` | `Hit`, `Context` |
| `KillEvent` | `Target`, `Context` |

Subscribe from anywhere:

```csharp
EventBus<HitEvent>.Subscribe(OnHit);

void OnHit(HitEvent evt)
{
    Debug.Log($"Hit {evt.Hit.Target.name} for {evt.Hit.Damage}");
}
```

Unsubscribe in `OnDestroy`. No custom event system — the existing `Extensions.EventBus` handles everything.

---

## Setup Walkthrough

### Minimum viable combat player

1. Add `CombatController` component to the player prefab
2. Create an `AbilityLibrary` asset; assign at least one `AbilityDefinition`
3. Assign the library to `CombatController.AbilityLibrary` in Inspector
4. Choose an animation driver:
   - Testing: add a `NullAnimationDriver` component, assign to `CombatController`
   - Production: implement `IAnimationDriver`, assign your component
5. Add `WeaponHitboxController` to the weapon bone; assign to `CombatController`
6. `PlayerController` already routes `LightAttack` / `HeavyAttack` button events to `CombatController.PushInput`
7. Hit Play; press the configured attack button

### Scene debug overlay

Add `CombatDebugOverlay` to any GameObject. Assign the `CombatController` target. Press `F2` to toggle visibility at runtime.

---

## Adding a New Ability

1. Create → Combat → Ability Definition
2. Fill in Identity and Input fields
3. Create phase assets (or reuse existing ones):
   - Create → Combat → Pipeline → Startup Phase
   - Create → Combat → Pipeline → Active Phase (set `HitboxConfig` here)
   - Create → Combat → Pipeline → Recovery Phase
4. Assign phases to `AbilityDefinition.Phases` in order
5. Set base stats in `BaseStats[]`
6. Add `AbilityDefinition` to an `AbilityLibrary` asset
7. Test: `NullAnimationDriver` will skip all animation waits and complete immediately

---

## Adding a New Modifier

### Stat modifier example

```csharp
public class FreezeDebuffModifier : MonoBehaviour, ICombatModifier, IStatContributor
{
    public Tag[] Tags => new[] { CombatTag.Frozen };
    public bool IsExpired => false;
    public string DebugLabel => "Freeze (-30% AttackSpeed)";

    public void ContributeStats(StatSheet stats, CombatContext context)
    {
        stats.AddModifier(StatId.AttackSpeed,
            StatModifier.Multiplicative(-0.3f, "freeze debuff"));
    }
}
```

```csharp
combatController.AddModifier(new TemporalCombatModifier(freezeModifier, duration: 3f));
```

### Dynamic modifier example

```csharp
public class OnKillHealModifier : IDynamicModifier
{
    public Tag[] Tags => Array.Empty<Tag>();
    public bool IsExpired => false;
    public string DebugLabel => "On Kill: Heal 10";

    public void OnCombatEvent(ICombatEvent evt, CombatContext ctx)
    {
        if (evt is KillEvent kill)
            healthSystem.Heal(10f);
    }
}
```

Register dynamic modifier events via `CombatController.Modifiers.DispatchEvent(evt)` — this is called automatically for all `ICombatEvent` types fired by `AbilityExecutor`.

---

## Debug Overlay

Toggle with `F2` (configurable via `ToggleKey` in Inspector).

```text
COMBAT SYSTEM
── Execution ──
Executing: YES
Ability: LightAttack_01
Combo Window: OPEN

── Attack Tags ──
[Melee]  [Fire]  [Charged]

── Stats ──
Damage: base=15 +add=5 ×mul=1.5 → 30.0
AttackSpeed: base=1 → 1.0
Range: base=2 → 2.0
Knockback: base=3 → 3.0

── Modifiers ──
• Freeze (-30% AttackSpeed)
• On Kill: Heal 10

── Input Buffer (last 5) ──
LightAttack [Performed] (0.12s ago)
LightAttack [Started] (0.14s ago)
```

---

## Key Design Rules

**Do not modify the pipeline during execution.** Structural modifiers run before `ExecuteAsync`. After the step list is snapshotted, it is immutable for the duration of that ability. Mutating it mid-execution will have no effect and may panic.

**Do not hold references to `CombatContext` past execution.** `CombatContext` is created per-execution and recycled. Storing a reference to it after `AbilityEndedEvent` fires will give you stale data.

**Do not call `CombatController.PushInput` from inside a pipeline step.** Input should flow from the player input layer only. Recursion through the pipeline is not supported.

**Stat evaluation is always contextual.** `StatSheet.Evaluate(statId, context)` evaluates conditional modifiers against the current context tags. Call it during execution, not at ability load time.

**`finally` blocks are the only safe place for cleanup.** Any hitbox deactivation, tag removal, or state reset that must happen even on cancellation must be in a `finally` block inside the phase's `ExecuteAsync`.

**Structural modifiers run once, in registration order.** If two structural modifiers both insert a step at index 0, the second one will insert before the first one's step. Order of `AddModifier` calls matters for structural modifiers.

**`NullAnimationDriver` completes immediately.** During testing, all `WaitForEventAsync` and `WaitForCompletionAsync` calls return on the same frame. If phases appear to run instantly, this is expected.

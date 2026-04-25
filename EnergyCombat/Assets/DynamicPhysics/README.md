# DynamicPhysics — Modular Velocity-Based Locomotion Engine

A standalone, rigidbody-based physics controller framework for Unity, designed for high-speed parkour combat. Fully namespace-isolated under `DynamicPhysics` with zero coupling to external game systems.

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Core Concepts](#core-concepts)
- [Setup Guide](#setup-guide)
- [API Reference](#api-reference)
- [Abilities](#abilities)
- [Modifiers](#modifiers)
- [Constraints](#constraints)
- [Examples](#examples)
- [Performance](#performance)
- [Extending the System](#extending-the-system)

---

## Architecture Overview

The system is split into four strictly separated layers:

```
┌─────────────────────────────────────────┐
│  Layer 1: Gameplay API                  │
│  RequestJump() / RequestDash() / etc.   │
├─────────────────────────────────────────┤
│  Layer 2: Orchestration                 │
│  MotionOrchestrator → ProfileBuilder    │
│  → RuntimeMotionConfig                  │
├─────────────────────────────────────────┤
│  Layer 3: Motion Pipeline               │
│  InputSteering → Abilities → Forces     │
│  → Gravity → Modifiers → Constraints    │
├─────────────────────────────────────────┤
│  Layer 4: Physics Motor                 │
│  PhysicsMotor → Rigidbody               │
└─────────────────────────────────────────┘
```

**Key principle**: No subsystem directly sets final velocity. All systems contribute to a shared `MotionContext` additively or transformationally. The only class that touches `Rigidbody` is `PhysicsMotor`.

### Pipeline Execution Order

Each `FixedUpdate`, stages execute in this deterministic order:

| Priority | Stage | Responsibility |
|----------|-------|---------------|
| 100 | **InputSteeringStage** | Converts input to velocity with momentum/inertia |
| 200 | **AbilityInfluenceStage** | Applies ability velocity contributions |
| 300 | **ExternalForceStage** | Integrates knockback/wind/explosion forces |
| 400 | **GravityStage** | Applies scaled gravity |
| 500 | **ModifierStage** | Applies drag, speed caps, steering overrides |
| 600 | **ConstraintStage** | Enforces rope limits, ground snap, speed limits |

---

## Core Concepts

### MotionContext

The shared mutable state object that flows through the entire pipeline. Contains velocity, position, flags, input, and physics parameters. Allocated once and reused every frame — zero GC pressure.

### MotionFlags

Bitfield flags for fast contextual queries:
```csharp
if ((context.Flags & MotionFlags.Grounded) != 0) { /* on ground */ }
if ((context.Flags & MotionFlags.Dashing) != 0) { /* mid-dash */ }
```

Available flags: `Grounded`, `Airborne`, `Sliding`, `WallContact`, `Swinging`, `Dashing`, `InCombat`, `Stunned`, `SlidingCrouch`, `WallRunning`.

### MovementProfile (ScriptableObject)

Data-only tuning presets — zero runtime state. Create them via `Assets > Create > DynamicPhysics > Movement Profile`.

Parameters include:
- **Steering**: max speed, acceleration, deceleration, turn rate, speed-dependent control
- **Physics**: gravity scale, friction, air control

### MovementProfileBuilder

Fluent API for composing runtime configurations from profiles + code overrides:

```csharp
var config = orchestrator.CreateBuilder()
    .FromProfile(airProfile)
    .WithGravityScale(0.5f)
    .WithModifier(new DragModifier(0.1f))
    .Build();
```

---

## Setup Guide

### Step 1: Add Components

1. Add a `Rigidbody` to your character GameObject (the orchestrator requires it).
2. Add the `MotionOrchestrator` component.
3. Create a `MovementProfile` asset: `Assets > Create > DynamicPhysics > Movement Profile`.
4. Assign the profile to the orchestrator's **Default Profile** field.
5. Configure the **Ground Detector** settings (radius, distance, layers).

### Step 2: Implement Input Provider

Create a class that implements `IMotionInputProvider`:

```csharp
using DynamicPhysics.Input;
using UnityEngine;

public class MyInputProvider : MonoBehaviour, IMotionInputProvider
{
    public MotionInputData GetInput()
    {
        var cam = Camera.main.transform;
        
        return new MotionInputData
        {
            MoveInput = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")),
            JumpHeld = Input.GetButton("Jump"),
            CameraForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized,
            CameraRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized
        };
    }
}
```

### Step 3: Wire It Up

```csharp
using DynamicPhysics.Abilities;
using DynamicPhysics.Orchestration;
using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [SerializeField] private MotionOrchestrator orchestrator;
    [SerializeField] private MyInputProvider inputProvider;

    private void Start()
    {
        // Set input
        orchestrator.SetInputProvider(inputProvider);

        // Register abilities
        orchestrator.RegisterAbility(new JumpAbility(
            jumpHeight: 2.5f,
            coyoteTime: 0.12f,
            jumpBufferTime: 0.1f
        ));

        orchestrator.RegisterAbility(new DashAbility(
            dashSpeed: 25f,
            dashDuration: 0.15f,
            cooldown: 0.8f
        ));

        orchestrator.RegisterAbility(new WallRunAbility(
            wallDetectionDistance: 0.7f,
            maxDuration: 1.2f,
            wallRunSpeed: 10f
        ));

        orchestrator.RegisterAbility(new SlideAbility(
            speedBoostMultiplier: 1.3f,
            slideFriction: 12f
        ));
    }

    private void Update()
    {
        // Route discrete inputs to requests
        if (Input.GetButtonDown("Jump")) orchestrator.RequestJump();
        if (Input.GetButtonDown("Dash")) orchestrator.RequestDash();
        if (Input.GetButtonDown("Slide")) orchestrator.RequestSlide();
    }
}
```

### Step 4: Tune via Profiles

Create multiple `MovementProfile` assets for different movement modes:
- **Ground Profile**: High acceleration, high friction, full control
- **Air Profile**: Lower acceleration, low friction, reduced air control
- **Combat Profile**: Reduced max speed, high deceleration

Switch profiles at runtime:
```csharp
orchestrator.SetProfile(combatProfile);
```

---

## API Reference

### MotionOrchestrator — Public API

#### Requests (Discrete Actions)
| Method | Description |
|--------|-------------|
| `RequestJump()` | Queues a jump request (supports buffering + coyote time) |
| `RequestDash(Vector3 dir)` | Queues a directional dash |
| `RequestSlide()` | Queues a ground slide |
| `AttachRope(RopeEffector)` | Attaches to a rope anchor for swinging |
| `DetachRope()` | Detaches from the current rope |

#### Forces
| Method | Description |
|--------|-------------|
| `AddForce(Vector3)` | Adds an external force (integrated as F*dt/m) |
| `AddImpulse(Vector3)` | Adds immediate velocity (bypasses force integration) |

#### Configuration
| Method | Description |
|--------|-------------|
| `SetInputProvider(IMotionInputProvider)` | Sets the continuous input source |
| `SetProfile(MovementProfile)` | Switches movement profile |
| `RegisterAbility(IMotionAbility)` | Adds a persistent ability |
| `UnregisterAbility<T>()` | Removes an ability by type |
| `AddModifier(IMotionModifier)` | Adds a persistent modifier |
| `AddTemporalModifier(IMotionModifier, float)` | Adds a time-limited modifier |
| `AddConstraint(IMotionConstraint)` | Adds a persistent constraint |
| `CreateBuilder()` | Creates a fluent runtime config builder |

#### Read-Only State
| Property | Description |
|----------|-------------|
| `IsGrounded` | Whether the character is on a walkable surface |
| `Velocity` | Current velocity vector |
| `Context` | Full motion context (for debugging) |

---

## Abilities

### JumpAbility

Full-featured jump with input-timing systems:

- **Coyote time**: Jump remains valid briefly after leaving ground edge
- **Jump buffering**: Jump pressed before landing fires on contact
- **Variable height**: Release early for short hops, hold for full height
- **Apex hang**: Reduced gravity at jump peak for floaty feel

Jump velocity is derived from desired height using kinematics: `v₀ = √(2gh)`

### DashAbility

Directional burst with cooldown:
- Overrides velocity in dash direction for a short duration
- Heavily reduces gravity during dash
- Suppresses steering input via `MotionFlags.Dashing`

### WallRunAbility

Auto-activating wall run when airborne near walls:
- Detects walls via left/right raycasts relative to velocity direction
- Reduces gravity, projects movement along wall surface
- Grip decays over time (gradual downward pull)
- Supports wall jumping with configurable up/out forces

### SlideAbility

Ground slide with momentum:
- Speed boost on entry, friction-based deceleration
- Height reduction via callback delegate (decoupled from collider type)
- Suppresses steering for committed slide direction

### RopeSwingAbility + RopeEffector

Pendulum swing system:
- `RopeEffector`: Place on world objects as anchor points (configurable length, offset)
- `RopeSwingAbility`: Manages attachment/detachment lifecycle
- `RopeConstraint`: Enforces pendulum distance, removes outward velocity component
- Input adds tangential force for swing steering
- Optional rope length adjustment (reel in/out)

---

## Modifiers

| Modifier | Description |
|----------|-------------|
| `GravityScaleModifier` | Multiplies gravity (apex hang, fast-fall) |
| `SpeedCapModifier` | Clamps horizontal speed |
| `DragModifier` | Exponential velocity decay (air resistance) |
| `SteeringOverrideModifier` | Adjusts input responsiveness (reduce during attacks) |
| `TemporalModifier` | Wraps any modifier with auto-expiring duration |

### Temporal Modifiers Example

```csharp
// Reduce control for 0.5 seconds during a hit reaction
orchestrator.AddTemporalModifier(
    new SteeringOverrideModifier(0.2f),
    duration: 0.5f
);

// Slow the character for 2 seconds
orchestrator.AddTemporalModifier(
    new SpeedCapModifier(5f),
    duration: 2f
);
```

---

## Constraints

| Constraint | Description |
|------------|-------------|
| `GroundSnapConstraint` | Projects velocity onto slopes, prevents floating |
| `RopeConstraint` | Enforces pendulum distance from anchor |
| `SpeedLimitConstraint` | Hard cap on total velocity magnitude |

Constraints always run last and may destructively correct velocity to maintain physical invariants.

---

## Examples

### Knockback

```csharp
// Apply a knockback force from an explosion
Vector3 knockbackDir = (player.transform.position - explosionCenter).normalized;
orchestrator.AddForce(knockbackDir * 500f);

// Temporarily reduce player control during knockback
orchestrator.AddTemporalModifier(
    new SteeringOverrideModifier(0.1f),
    duration: 0.3f
);
```

### Grapple Swing

```csharp
// Attach to a rope effector when player activates grapple
public void OnGrappleActivated(RopeEffector target)
{
    orchestrator.AttachRope(target);
}

// Detach and launch
public void OnGrappleReleased()
{
    orchestrator.DetachRope();
    // Player launches with current swing velocity — momentum preserved automatically
}
```

### Dynamic Profile Switching

```csharp
// Switch to combat profile when entering combat
public void OnCombatEntered()
{
    orchestrator.SetProfile(combatProfile);
    orchestrator.AddModifier(new SpeedCapModifier(8f));
}

public void OnCombatExited()
{
    orchestrator.SetProfile(defaultProfile);
    orchestrator.RemoveModifier(combatSpeedCap);
}
```

### Slide with Collider Adjustment

```csharp
var slideAbility = new SlideAbility(speedBoostMultiplier: 1.3f);
slideAbility.OnHeightChange = (multiplier) =>
{
    // Adjust capsule collider height
    capsuleCollider.height = originalHeight * multiplier;
    capsuleCollider.center = Vector3.up * (originalHeight * multiplier * 0.5f);
};
orchestrator.RegisterAbility(slideAbility);
```

---

## Performance

The system is designed for near-zero runtime overhead:

- **Zero GC in the hot path**: `MotionContext` and `RuntimeMotionConfig` are allocated once and reused. Request buffer is a fixed-size array. No LINQ or lambda allocations in pipeline execution.
- **Cache-friendly iteration**: Pipeline stages are sorted once into a flat array and iterated linearly. Modifiers and constraints use `List<T>` with pre-allocated capacity.
- **Minimal math**: Uses `sqrMagnitude` instead of `magnitude` where possible. Avoids unnecessary `Vector3.Normalize()` calls. Caches `Physics.gravity` at construction.
- **Constant-time flag checks**: `MotionFlags` is a `[Flags]` enum using bitwise operations — no string comparisons or dictionary lookups.
- **No virtual call overhead on data**: All data containers are concrete classes/structs. Interface dispatch is limited to the stage/modifier/constraint/ability iteration (typically 6-10 calls per frame).

---

## Extending the System

### Custom Ability

```csharp
using DynamicPhysics.Abilities;
using DynamicPhysics.Core;
using UnityEngine;

public class GlideAbility : IMotionAbility
{
    public bool IsActive { get; private set; }

    public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
    {
        // Activate when airborne and falling
        return (context.Flags & MotionFlags.Airborne) != 0 
            && context.Velocity.y < -1f 
            && context.Input.JumpHeld;
    }

    public void Activate(MotionContext context)
    {
        IsActive = true;
    }

    public void Tick(MotionContext context, float deltaTime)
    {
        // Heavily reduce gravity for gliding
        context.GravityScale *= 0.15f;

        // Deactivate when landing or releasing
        if ((context.Flags & MotionFlags.Grounded) != 0 || !context.Input.JumpHeld)
            Deactivate(context);
    }

    public Vector3 GetVelocityInfluence(MotionContext context) => Vector3.zero;

    public void Deactivate(MotionContext context)
    {
        IsActive = false;
    }
}
```

### Custom Modifier

```csharp
using DynamicPhysics.Core;
using DynamicPhysics.Pipeline.Modifiers;
using UnityEngine;

public class WindModifier : IMotionModifier
{
    public int Order => 60;
    public Vector3 WindDirection;
    public float WindStrength;

    public void Apply(MotionContext context)
    {
        context.Velocity += WindDirection * (WindStrength * context.DeltaTime);
    }
}
```

### Custom Pipeline Stage

```csharp
using DynamicPhysics.Core;
using DynamicPhysics.Orchestration;
using DynamicPhysics.Pipeline;

public class MagnetismStage : IMotionStage
{
    public int Priority => 250; // After abilities, before forces
    public Transform MagnetTarget;
    public float PullStrength;

    public void Execute(MotionContext context, RuntimeMotionConfig config)
    {
        if (MagnetTarget == null) return;
        Vector3 toTarget = MagnetTarget.position - context.Position;
        context.Velocity += toTarget.normalized * (PullStrength * context.DeltaTime);
    }
}
```

---

## Directory Structure

```
DynamicPhysics/
├── README.md
├── Core/
│   ├── MotionContext.cs          — Shared mutable pipeline state
│   ├── MotionFlags.cs            — Bitfield contextual flags
│   └── MotionRequest.cs          — Discrete gameplay intents
├── Pipeline/
│   ├── IMotionStage.cs           — Stage interface
│   ├── MotionPipeline.cs         — Ordered stage executor
│   ├── Stages/
│   │   ├── InputSteeringStage.cs — Momentum-based steering
│   │   ├── GravityStage.cs       — Scaled gravity
│   │   ├── AbilityInfluenceStage.cs
│   │   ├── ExternalForceStage.cs — Force integration
│   │   ├── ModifierStage.cs      — Modifier application
│   │   └── ConstraintStage.cs    — Constraint enforcement
│   └── Modifiers/
│       ├── IMotionModifier.cs    — Modifier interface
│       ├── GravityScaleModifier.cs
│       ├── SpeedCapModifier.cs
│       ├── DragModifier.cs
│       ├── SteeringOverrideModifier.cs
│       └── TemporalModifier.cs   — Time-limited wrapper
├── Constraints/
│   ├── IMotionConstraint.cs      — Constraint interface
│   ├── GroundSnapConstraint.cs   — Slope adherence
│   ├── RopeConstraint.cs         — Pendulum distance
│   └── SpeedLimitConstraint.cs   — Hard speed cap
├── Profiles/
│   ├── MovementProfile.cs        — ScriptableObject tuning data
│   └── SteeringSettings.cs       — Steering configuration
├── Orchestration/
│   ├── MotionOrchestrator.cs     — Hub MonoBehaviour
│   ├── MovementProfileBuilder.cs — Fluent config builder
│   ├── RuntimeMotionConfig.cs    — Resolved per-frame config
│   └── InfluencePriority.cs      — Priority constants
├── Motor/
│   ├── PhysicsMotor.cs           — Sole Rigidbody accessor
│   └── GroundDetector.cs         — Spherecast ground sensing
├── Input/
│   ├── IMotionInputProvider.cs   — Input abstraction
│   └── MotionInputData.cs        — Per-frame input snapshot
└── Abilities/
    ├── IMotionAbility.cs         — Ability interface
    ├── JumpAbility.cs            — Jump with coyote/buffer/variable/apex
    ├── DashAbility.cs            — Directional burst
    ├── WallRunAbility.cs         — Auto-activating wall run
    ├── SlideAbility.cs           — Ground slide with momentum
    ├── RopeSwingAbility.cs       — Pendulum swing management
    └── RopeEffector.cs           — World anchor component
```

---

## Portability

This system is fully contained within the `DynamicPhysics` namespace and depends only on `UnityEngine`. To move it to another project:

1. Copy the entire `DynamicPhysics/` folder into the target project's `Assets/`.
2. All references will resolve automatically — no external dependencies.

If you later want compilation isolation, add an Assembly Definition (`.asmdef`) at the root with:
- **Name**: `DynamicPhysics`
- **Root Namespace**: `DynamicPhysics`
- **References**: None (only auto-referenced Unity assemblies)

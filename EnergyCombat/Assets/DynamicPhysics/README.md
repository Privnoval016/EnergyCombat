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
│  orchestrator.Request("Jump")           │
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

**Key principles**:
- No subsystem directly sets final velocity — all contribute additively via `MotionContext`.
- Only `PhysicsMotor` touches `Rigidbody`.
- The orchestrator has **zero ability-specific logic** — all request handling is routed through abilities.

### Request Routing

All gameplay interaction flows through a single `Request()` method. Requests are routed in two passes:

1. **Intercept pass**: Active abilities receive each request via `TryConsumeRequest()`. If consumed, the request stops here. (e.g., WallRunAbility intercepts Jump → wall jump.)
2. **Activation pass**: Unconsumed requests are passed to inactive abilities via `CanActivate()`.

### Pipeline Execution Order

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

The shared mutable state object that flows through the entire pipeline. Contains velocity, position, tags, input, and physics parameters. Allocated once and reused every frame.

### MotionTag (String-Based Tags)

Extensible string-based tag system replacing a hardcoded flags enum. Any system can define custom tags:

```csharp
// Built-in tags
context.SetTag(MotionTag.Grounded);
context.HasTag(MotionTag.Dashing);
context.RemoveTag(MotionTag.Airborne);

// Custom tags — no core code changes needed
context.SetTag("Burning");
context.SetTag("SpeedBoosted");
if (context.HasTag("Frozen")) { /* ... */ }
```

Built-in tags: `Grounded`, `Airborne`, `Sliding`, `WallContact`, `Swinging`, `Dashing`, `InCombat`, `Stunned`, `SlidingCrouch`, `WallRunning`.

### MotionRequestType (String-Based Requests)

```csharp
// Built-in request types
orchestrator.Request(MotionRequestType.Jump);
orchestrator.Request(MotionRequestType.Dash, dashDirection);

// Custom request types
orchestrator.Request("DoubleJump");
orchestrator.Request("GrappleShot");
```

### MovementProfile (ScriptableObject)

Data-only tuning presets — zero runtime state. Create via `Assets > Create > DynamicPhysics > Movement Profile`.

---

## Setup Guide

### Step 1: Add Components

1. Add a `Rigidbody` to your character GameObject.
2. Add the `MotionOrchestrator` component.
3. Create a `MovementProfile` asset: `Assets > Create > DynamicPhysics > Movement Profile`.
4. Assign the profile and configure ground detection settings.

### Step 2: Implement Input Provider

```csharp
using DynamicPhysics;
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
using DynamicPhysics;
using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [SerializeField] private MotionOrchestrator orchestrator;
    [SerializeField] private MyInputProvider inputProvider;

    private void Start()
    {
        orchestrator.SetInputProvider(inputProvider);

        orchestrator.RegisterAbility(new JumpAbility(jumpHeight: 2.5f, coyoteTime: 0.12f));
        orchestrator.RegisterAbility(new DashAbility(dashSpeed: 25f, cooldown: 0.8f));
        orchestrator.RegisterAbility(new WallRunAbility(maxDuration: 1.2f));
        orchestrator.RegisterAbility(new SlideAbility(speedBoostMultiplier: 1.3f));
    }

    private void Update()
    {
        // All requests go through the same generic API
        if (Input.GetButtonDown("Jump")) orchestrator.Request(MotionRequestType.Jump);
        if (Input.GetButtonDown("Dash")) orchestrator.Request(MotionRequestType.Dash);
        if (Input.GetButtonDown("Slide")) orchestrator.Request(MotionRequestType.Slide);
    }
}
```

---

## API Reference

### MotionOrchestrator

#### Generic Request API
| Method | Description |
|--------|-------------|
| `Request(MotionRequest)` | Queues any request — routed through abilities automatically |
| `Request(string type)` | Convenience: queues a request by type string |
| `Request(string type, Vector3 dir)` | Convenience: queues a directional request |

#### Forces
| Method | Description |
|--------|-------------|
| `AddForce(Vector3)` | External force (integrated as F*dt/m) |
| `AddImpulse(Vector3)` | Immediate velocity addition |

#### Configuration
| Method | Description |
|--------|-------------|
| `SetInputProvider(IMotionInputProvider)` | Sets continuous input source |
| `SetProfile(MovementProfile)` | Switches movement profile |
| `RegisterAbility(IMotionAbility)` | Adds a persistent ability |
| `UnregisterAbility<T>()` | Removes an ability by type |
| `AddModifier(IMotionModifier)` | Adds a persistent modifier |
| `AddTemporalModifier(IMotionModifier, float)` | Adds a time-limited modifier |
| `AddConstraint(IMotionConstraint)` | Adds a persistent constraint |

---

## Abilities

### IMotionAbility Interface

Every ability implements three key methods:

| Method | Called On | Purpose |
|--------|-----------|---------|
| `TryConsumeRequest()` | Active abilities | Intercept requests (e.g., wall run intercepts Jump → wall jump) |
| `CanActivate()` | Inactive abilities | Check if unconsumed requests + conditions warrant activation |
| `GetVelocityInfluence()` | Active abilities | Return additive velocity contribution |

### Built-In Abilities

- **JumpAbility**: Coyote time, buffering, variable height, apex hang. `v₀ = √(2gh)`.
- **DashAbility**: Directional burst with cooldown. Reduced gravity.
- **WallRunAbility**: Auto-activating. **Intercepts Jump requests** for wall jumps.
- **SlideAbility**: Speed boost, friction deceleration, height change callback.
- **RopeSwingAbility**: Pendulum swing. **Intercepts Jump and RopeDetach requests**.

---

## Modifiers

| Modifier | Description |
|----------|-------------|
| `GravityScaleModifier` | Multiplies gravity |
| `SpeedCapModifier` | Clamps horizontal speed |
| `DragModifier` | Exponential velocity decay |
| `SteeringOverrideModifier` | Adjusts input responsiveness |
| `TemporalModifier` | Wraps any modifier with auto-expiring duration |

---

## Constraints

| Constraint | Description |
|------------|-------------|
| `GroundSnapConstraint` | Projects velocity onto slopes, prevents floating |
| `RopeConstraint` | Enforces pendulum distance from anchor |
| `SpeedLimitConstraint` | Hard cap on total velocity magnitude |

---

## Examples

### Knockback

```csharp
orchestrator.AddForce(knockbackDir * 500f);
orchestrator.AddTemporalModifier(new SteeringOverrideModifier(0.1f), duration: 0.3f);
```

### Grapple Swing

```csharp
// Attach — request routes to RopeSwingAbility via CanActivate
orchestrator.Request(new MotionRequest(MotionRequestType.RopeAttach) { Target = ropeEffector });

// Detach — RopeSwingAbility intercepts this via TryConsumeRequest
orchestrator.Request(MotionRequestType.RopeDetach);
```

### Wall Jump (Automatic)

```csharp
// Player presses jump while wall running:
orchestrator.Request(MotionRequestType.Jump);
// WallRunAbility (active) intercepts → performs wall jump
// If NOT wall running, JumpAbility (inactive) activates → normal jump
```

### Custom Ability with Custom Request

```csharp
public class TeleportAbility : IMotionAbility
{
    public const string TeleportRequest = "Teleport";

    public bool TryConsumeRequest(MotionContext context, MotionRequest request) => false;

    public bool CanActivate(MotionContext context, MotionRequest[] requests, int requestCount)
    {
        for (int i = 0; i < requestCount; i++)
            if (requests[i].Type == TeleportRequest) return true;
        return false;
    }

    // ... rest of implementation
}

// Usage:
orchestrator.Request("Teleport");
```

---

## Performance

- **Zero GC in hot path**: MotionContext allocated once, fixed-size request buffers
- **Cache-friendly execution**: Stages sorted once into flat array
- **Tag lookups**: HashSet<string> with O(1) average-case Contains
- **No LINQ** in any runtime code path
- **sqrMagnitude** over `magnitude` where possible

---

## Extending the System

### Custom Tags

```csharp
// Define anywhere — no registration needed
public static class MyTags
{
    public const string Burning = "Burning";
    public const string SpeedBoosted = "SpeedBoosted";
}

// Use in abilities, modifiers, or gameplay code
context.SetTag(MyTags.Burning);
if (context.HasTag(MyTags.SpeedBoosted)) { /* ... */ }
```

### Custom Request Types

```csharp
public static class MyRequests
{
    public const string DoubleJump = "DoubleJump";
    public const string GroundPound = "GroundPound";
}

orchestrator.Request(MyRequests.GroundPound);
```

### Custom Pipeline Stage

```csharp
public class MagnetismStage : IMotionStage
{
    public int Priority => 250;
    public Transform Target;
    public float PullStrength;

    public void Execute(MotionContext context, RuntimeMotionConfig config)
    {
        if (Target == null) return;
        Vector3 toTarget = Target.position - context.Position;
        context.Velocity += toTarget.normalized * (PullStrength * context.DeltaTime);
    }
}
```

---

## Directory Structure

```
DynamicPhysics/                       (all files use namespace DynamicPhysics)
├── README.md
├── Core/
│   ├── MotionContext.cs              — Shared mutable pipeline state (HashSet tags)
│   ├── MotionFlags.cs                — MotionTag string constants
│   └── MotionRequest.cs              — Requests + MotionRequestType string constants
├── Pipeline/
│   ├── IMotionStage.cs
│   ├── MotionPipeline.cs
│   ├── Stages/                       — 6 concrete pipeline stages
│   └── Modifiers/                    — IMotionModifier + 5 concrete modifiers
├── Constraints/                      — IMotionConstraint + 3 concrete constraints
├── Profiles/                         — MovementProfile (SO) + SteeringSettings
├── Orchestration/                    — MotionOrchestrator, Builder, Config, Priority
├── Motor/                            — PhysicsMotor + GroundDetector
├── Input/                            — IMotionInputProvider + MotionInputData
└── Abilities/                        — IMotionAbility + 5 abilities + RopeEffector
```

---

## Portability

This system depends only on `UnityEngine` and lives in a single flat `DynamicPhysics` namespace. To move to another project, copy the entire folder. To add compilation isolation later, create a single `.asmdef` at the root.

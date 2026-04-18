# Ability System V7 — Workflow Guide

## Architecture at a Glance

The system is split into two independent layers:

```
Player Input
     │
     ▼
┌─────────────────┐       ┌──────────────────┐
│ MovesetController│──────▶│ AbilityController │
│  (combo routing) │       │  (single move)    │
└─────────────────┘       └──────────────────┘
         │                         │
         ▼                         ▼
  MovesetDefinition         AbilityDefinition
  (paths, steps)            (modules, timing)
```

**AbilityController** handles one ability at a time (startup, active, recovery, charge).
**MovesetController** decides *which* ability plays next based on input and combo state.

Both layers are optional — `AbilityController` works standalone for simple cases, `MovesetController` adds combo routing on top.

---

## Step 1 — Create Ability Modules (ScriptableObjects)

Ability modules define individual effects: damage, animation, VFX, SFX, etc. These are existing assets — the module system is unchanged from V5.

Each module is a ScriptableObject derived from `AbilityModuleDefinition`. Create them via **Assets > Create > NobunAtelier > Ability > Modules > ...**.

---

## Step 2 — Create an AbilityDefinition (ScriptableObject)

Create via **Assets > Create > NobunAtelier > Ability > Ability Definition**.

An `AbilityDefinition` represents a single move. It contains:

### Default ActionModel

The core of every ability. An `ActionModel` holds two module lists and timing:

| Field | Purpose |
|---|---|
| **Driven Modules** | Follow the execution driver's timing (Initiate → Execute → Update → Stop). These trigger state transitions. |
| **Overlay Modules** | Tied to the ActionModel's lifetime. Start immediately, stop on teardown. No state transitions. |
| **Execution Driver Module** | (Optional) A module that controls timing via animation events. If empty, the awaitable timer is used. |
| **Awaitable Execution Context** | Timer-based timing: `ExecutionDelay` → `UpdateDuration` → `RecoveryDuration`. Only shown when no driver module is set. |

**Driver resolution rules:**
- Driver module set → animation-driven (events control lifecycle)
- Driver module empty + driven modules present → timer-driven (`AwaitableExecutionContext`)
- No driven modules → overlay-only (used for charge phases)

### Charge Configuration (Optional)

Enable `CanBeCharged` to configure hold-and-release mechanics:

| Field | Purpose |
|---|---|
| `ChargeStart` | Overlay-only ActionModel played while charging begins |
| `ChargedAbilityLevels[]` | Each level has a threshold duration, an `OnLevelReached` overlay, and an `OnChargeReleased` ActionModel |
| `ChargeCancel` | Overlay-only ActionModel played when charge is cancelled |
| `ChargeConstraint` | `None`, `ReleaseOnMaxChargeReached`, `ReleaseOnTimeout`, `CancelOnTimeout` |

---

## Step 3 — Set Up the Scene

### Standalone (No Combos)

Add an **AbilityController** component to your character. Assign a default `AbilityDefinition`.

```
Character GameObject
 └── AbilityController
      └── Default Ability: MyAbility.asset
```

Call the API directly:

```csharp
abilityController.TryExecute(someAbility);
abilityController.Cancel();
abilityController.StartCharge(chargeableAbility);
abilityController.ReleaseCharge();
```

### With Combos (Moveset)

Additionally create `InputSlot` and `MovesetDefinition` assets, then add a **MovesetController**.

```
Character GameObject
 ├── AbilityController
 └── MovesetController
      ├── Ability Controller: (ref to above)
      ├── Moveset: MyMoveset.asset
      └── Input Buffer Duration: 0.2
```

The player controller calls `MovesetController` instead of `AbilityController`:

```csharp
movesetController.PressSlot(lightAttackSlot);   // tap
movesetController.HoldSlot(heavyAttackSlot);    // hold start
movesetController.ReleaseSlot(heavyAttackSlot); // hold release
```

---

## Step 4 — Create InputSlots (ScriptableObjects)

Create via **Assets > Create > NobunAtelier > Moveset > Input Slot**.

These are identity tokens that decouple the moveset from the Input System. One per logical input: `LightAttack.asset`, `HeavyAttack.asset`, etc.

Your player controller maps `InputAction` callbacks to these slots.

---

## Step 5 — Create a MovesetDefinition (ScriptableObject)

Create via **Assets > Create > NobunAtelier > Moveset > Moveset Definition**.

A `MovesetDefinition` contains an array of **Paths**. Each path is a combo sequence:

```yaml
MovesetDefinition:
  Paths:
    - Priority: 0
      ResetMode: OnTimeout
      ResetTimeout: 1.5
      Steps:
        - InputSlot: LightAttack, Ability: Slash_X1,  IsChargeInput: false
        - InputSlot: LightAttack, Ability: Slash_X2,  IsChargeInput: false
        - InputSlot: LightAttack, Ability: Slash_X3,  IsChargeInput: false

    - Priority: 1
      ResetMode: OnCompletion
      Steps:
        - InputSlot: HeavyAttack, Ability: ChargedSmash, IsChargeInput: true
```

### Path Fields

| Field | Purpose |
|---|---|
| `Priority` | Higher priority paths can interrupt lower ones. |
| `ResetMode` | `OnCompletion` = reset after any step completes. `OnTimeout` = reset after timer expires between steps. `Loop` = wrap from last step back to first, enabling infinite repeating combos (resets on timeout if the player stops). |
| `ResetTimeout` | Seconds before the combo resets (`OnTimeout` and `Loop` modes). |

### Step Fields

| Field | Purpose |
|---|---|
| `InputSlot` | Which input triggers this step. |
| `Ability` | The `AbilityDefinition` to play. |
| `IsChargeInput` | If true, `HoldSlot` starts a charge instead of a tap. |

---

## Execution Lifecycle

### State Machine

```
Ready ──▶ Starting ──▶ InProgress ──▶ Recovery ──▶ Ready
  │                                       │
  │          (chain: TryExecute)          │
  │◀──────────────────────────────────────┘
  │
  └──▶ Charging ──▶ (ReleaseCharge) ──▶ Starting ──▶ ...
```

### Events

| Event | When | Typical Use |
|---|---|---|
| `OnAbilityStarted` | A new ability enters `Starting` | Update state machine, camera |
| `OnAbilityStartCharge` | Charge begins | Show charge UI |
| `OnRecoveryWindowOpen` | Driven modules stopped, recovery window open | Moveset flushes buffered input |
| `OnAbilityCompleted` | Full lifecycle finished, back to `Ready` | Reset to idle |
| `OnAbilityCancelled` | Hard cancel (stun, death) | Reset to idle, clear combo |

### Combo Chain Flow

1. Player presses `LightAttack` → Moveset resolves Step 0 → plays `Slash_X1`
2. Player presses `LightAttack` again during `InProgress` → rejected → buffered
3. `Slash_X1` enters `Recovery` → `OnRecoveryWindowOpen` fires → buffer flushed → `Slash_X2` starts
4. Combo continues until reset timeout or completion

### Teardown vs Cancel

- **Teardown** (chaining): Current ability silently stops when a new one starts during Recovery. No events fire for the outgoing ability.
- **Cancel** (external interrupt): Everything stops immediately. `OnAbilityCancelled` fires. Combo resets.

---

## Quick Reference

### AbilityController API

```csharp
bool TryExecute(AbilityDefinition ability, AbilityExecutionContext? context = null)
void Cancel()
bool StartCharge(AbilityDefinition ability)
void ReleaseCharge()
void CancelCharge()
```

**Read-only state:** `CurrentAbility`, `CurrentState`, `IsCharging`, `IsInRecovery`, `ExecutionContext`

### MovesetController API

```csharp
void PressSlot(InputSlot slot)    // tap confirmed
void HoldSlot(InputSlot slot)     // hold confirmed
void ReleaseSlot(InputSlot slot)  // button released
void SetMoveset(MovesetDefinition moveset)
void ResetAllPaths()
```

**Read-only state:** `ActivePathIndex`, `GetComboStep(pathIndex)`, `CurrentMoveset`

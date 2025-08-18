# @Character Architecture Summary

## Core Responsibilities
- Manages character movement, rotation, and abilities through modular components
- Coordinates different module types with priority-based execution
- Provides physics integration and lifecycle management
- Acts as the central hub connecting controllers, physics, and behavioral modules

## Dependencies
- @CharacterControllerBase: Interface for character input control
- @CharacterPhysicsModule: Handles physical movement and collision
- @CharacterVelocityModuleBase: Controls velocity modification
- @CharacterRotationModuleBase: Handles rotation logic
- @CharacterAbilityModuleBase: Implements special character abilities
- Unity Animator: Manages character animations

## System Architecture
- Part of NobunAtelier namespace
- Uses a modular, priority-based execution system
- Modules are categorized by function (physics, velocity, rotation, ability)
- Supports runtime module queries, execution, and management

## Modularity & Extensibility
- Module lists for velocity, rotation, and abilities with priority sorting
- Generic TryGet methods to retrieve specific module types
- Auto-capture system for gathering modules in the hierarchy
- Module initialization system for setup and configuration

## Key Methods & Events
- Events: OnPreUpdate, OnPostUpdate
- Movement: Move(), GetMoveVector(), GetMoveSpeed(), IsMoving
- Rotation: Rotate()
- Module Access: TryGetAbilityModule<T>(), TryGetVelocityModule<T>(), TryGetRotationModule<T>()
- State Management: ResetCharacter()
- Controller: SetController()

## Module Types
- @CharacterPhysicsModule: Handles physical movement, ground detection, collision
- @CharacterVelocityModuleBase: Modifies character velocity based on inputs and state
- @CharacterRotationModuleBase: Controls character facing direction and orientation
- @CharacterAbilityModuleBase: Implements character abilities and actions

## Execution Flow
1. Physics modules handle collision and movement
2. Best rotation module is selected based on priority and availability
3. Ability modules are executed based on priority
4. Velocity is calculated from all available velocity modules
5. Final velocity is applied through the physics module 
# Project DeadCell - Agents Guidelines

This document provides context and guidelines for AI agents working on this Unity project.

## 1. Project Overview
- **Type**: 2D Action/Platformer Game (inspired by Dead Cells).
- **Engine**: Unity 3D/2D
- **Unity Version**: `2022.3.62f3`
- **Main Programming Language**: C#

## 2. Code Architecture & Folder Structure
The source code is primarily located in `Assets/Scripts/`. The project is modular, relying heavily on component-based design and common game programming patterns.

### Key Directories and Systems:
- **`Assets/Scripts/Player/`**: Contains all player-related logic.
  - **State Machine**: The player logic utilizes a State Machine pattern. Check `Assets/Scripts/Player/States/` (e.g., `PlayerStateMachine.cs`, `PlayerState.cs`, `PlayerMoveState.cs`, `PlayerAttackState.cs`, `PlayerRollState.cs`).
  - Other essential components: `Combat.cs`, `PlayerEffects.cs`, `PlayerRespawn.cs`, `PlayerProjectile.cs`.
- **`Assets/Scripts/Core/`**: Contains core gameplay systems and managers.
  - **Health & Combat**: `Health.cs`, `WeaponSystem.cs`, `WeaponPickup.cs`, `StanceManager.cs`.
  - **Buff System**: `BuffReceiver.cs`, `TemporaryBuff.cs`.
  - **Level Flow**: `LevelFlowManager.cs`, `SceneTransitionManager.cs`.
- **`Assets/Scripts/Enemies/`**: Contains enemy AI and behaviors.
  - Base classes: `EnemyController.cs`, `BossController.cs`.
  - Organized into types/biomes: `Autumn/`, `Boss/`, `Types/`.
- **`Assets/Scripts/Camera/`**: Contains camera logic, mainly `CameraFollow.cs`.
- **Other folders**: `UI/` for user interfaces, `Environment/` for level elements, `Items/` for interactables/loot.

## 3. Coding Guidelines & Best Practices for AI
When writing or modifying code in this project, adhere to the following rules:

1. **Follow the State Machine Pattern**: When adding new player behaviors, create new states extending `PlayerState` rather than bloating the main player script.
2. **Component-Based Design**: Keep scripts focused on a single responsibility. Use `GetComponent` (preferably cached in `Awake()` or `Start()`) to interact with other systems (e.g., `Health`, `Combat`).
3. **Unity Best Practices**:
   - Cache references instead of using `GameObject.Find` or `GetComponent` in `Update()`.
   - Use `SerializeField` for inspector variables instead of making fields `public` unless they need to be accessed by other scripts.
   - Use object pooling for frequently instantiated objects (like projectiles or hit effects) if performance becomes an issue.
4. **Naming Conventions**:
   - Classes and Methods: `PascalCase`.
   - Private fields: `camelCase` or `_camelCase`.
   - Public fields / Properties: `PascalCase`.
5. **Preserve Existing Logic**: Do not aggressively refactor working systems unless specifically requested by the user. Maintain existing comments and logic flow.

## 4. Workflows
- **Adding a new Weapon**: Update `WeaponSystem.cs` and `WeaponPickup.cs`, ensuring it integrates with the player's `Combat` component.
- **Adding a new Enemy**: Extend `EnemyController` (or use it directly) and add it to the `Assets/Scripts/Enemies/Types/` folder.
- **Adding a new State**: Create a new class in `Player/States/` extending `PlayerState`, then add a reference and transition logic in `PlayerStateMachine` and the `Player` class.

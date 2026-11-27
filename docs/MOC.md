# Projeto-rac — Map of Content (MOC)

Indice central do projeto **Projeto-rac** (Unity `6000.6.5f1`, URP `17.6.0`, Input System `1.20.0`, AI Navigation `2.0.15`, Unity Pipeline `1.0.0-pre.1`).

## Grafo de Arquitetura

```mermaid
flowchart TD
    MOC["MOC (Indice Central)"]
    PL["[[player-systems]]<br/>Movimento, Camera, Input, Stamina, Vida, Interacao"]
    WP["[[weapons-and-combat]]<br/>IWeapon, IDamageable, Inventario, Estilingue, Escopeta"]
    EN["[[enemy-ai]]<br/>EnemyMeleeAI (NavMesh), AIState, EnemyHealth"]
    UI["[[ui-and-hud]]<br/>Crosshair, Ammo, HealthBar, StaminaBar, Charge"]
    ED["[[editor-tooling]]<br/>Builders de Cena, HUD, Inimigos e Prefabs"]
    CM["[[unity-codemode-workflow]]<br/>Unity Pipeline, script run/eval, AgentScripts"]

    MOC --> PL
    MOC --> WP
    MOC --> EN
    MOC --> UI
    MOC --> ED
    MOC --> CM

    PL -->|stamina / vida / alvo| UI
    PL -->|IInteractable (E)| WP
    WP -->|IDamageable| EN
    EN -->|IDamageable| PL
    WP -->|municao / carga| UI
    ED -.->|gera cena e prefabs| PL
    ED -.->|gera Canvas uGUI| UI
    CM -.->|executa C# sem reload| ED
```

## Notas de Arquitetura

| Nota | Dominio | Scripts Principais |
| :--- | :--- | :--- |
| [[player-systems]] | Controle FPS, camera PS2, input, stamina, vida, pos-processamento e interacao (portas, gavetas, armarios pela mira e notas) | `FirstPersonController`, `FirstPersonCameraLook`, `PS2CameraFeel`, `PlayerInputHandler`, `PlayerStamina`, `PlayerHealth`, `PlayerInteraction`, `DoorController`, `DrawerController`, `CabinetController`, `InspectableNote`, `StaminaExhaustionPostProcess` |
| [[weapons-and-combat]] | Contratos de combate, inventario (slots 1-2), estilingue, escopeta e pools | `IDamageable`, `IWeapon`, `PlayerWeaponInventory`, `SlingshotWeapon`, `ShotgunWeapon`, pools e projeteis |
| [[enemy-ai]] | IA corpo a corpo e a distancia com NavMesh, percepcao LOS e pool de projeteis | `EnemyMeleeAI`, `EnemyRangedAI`, `EnemyPerception`, `EnemyRangedVisuals`, `EnemyProjectile`, `EnemyProjectilePool`, `EnemyHealth` |
| [[ui-and-hud]] | HUD modular em uGUI com sprites configuraveis, modal de notas e dialogo inicial estilo Visual Novel | `CrosshairHUD`, `AmmoHUD`, `PlayerHealthBarUI`, `StaminaBarUI`, `SlingshotChargeHUD`, `NoteInspectionUI`, `VisualNovelDialogueUI`, `VisualNovelDialogueData`, `VisualNovelGameplayLock`, `VisualNovelPortraitStage` |
| [[editor-tooling]] | Scripts de construcao procedural de arena, mansao de 2 andares (com corrimao no Hall e varandas cobertas), visual PS2, colisores, props, HUDs, Visual Novel e prefabs no Editor | `TestArenaBuilder`, `DoorArenaBuilder`, `PS2VisualSetupBuilder`, `MansionMapBuilder`, `MansionLayoutData`, `MansionDoorFactory`, `MansionRailingFactory`, `MansionColliderFixer`, `MansionPropsTestBuilder`, `VisualNovelDialogueBuilder`, `VisualNovelDialogueUIEditor`, `VisualNovelPlaceholderArt`, `HUDCanvasBuilder`, `AmmoHUDBuilder`, `HealthHUDBuilder`, `EnemyBuilder`, `SlingshotPrefabGenerator` |
| [[unity-codemode-workflow]] | Operacao do Unity Editor aberto via Code Mode (`script run` / `eval`) | `My project/AgentScripts/ProjectProbe.cs`, `ApplySceneFixes.cs`, `SpawnRangedEnemyInScene.cs`, `BuildMansionInScene.cs`, `SetupMansionFixesAndProps.cs`, `ApplyPS2StyleInScene.cs`, `ApplyFloor2BalconiesAndRailings.cs`, `SetupVisualNovelDialogueInScene.cs`, `ApplyPlayerArmSlingshotInScene.cs`, `unity` CLI + `com.unity.pipeline` |

## Contratos Centrais

- `ProjectRac.Combat.IDamageable`: `void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)` — implementado por `PlayerHealth` e `EnemyHealth`.
- `ProjectRac.Interaction.IInteractable`: `string GetInteractionPrompt()`, `void Interact()` — implementado por `ShotgunPickup`, `DoorController`, `DrawerController`, `CabinetController`, `InspectableNote` e `InteractableTestObject`.
- `ProjectRac.Weapons.IWeapon`: `WeaponName`, `SlotIndex`, `IsEquipped`, `Equip()`, `Unequip()` — implementado por `SlingshotWeapon` e `ShotgunWeapon`.

## Knowledge Items (KIs)

- **KI-01 (Limite de 300 linhas — `rennan-dev-cycle`)**: `EnemyMeleeAI.cs` esta em 307L; `TestArenaBuilder.cs` e `ShotgunWeapon.cs` estao em 298L. Novos obstaculos da arena devem usar builders dedicados (como `DoorArenaBuilder.cs`).
- **KI-02 (Exaustao de Stamina isolada)**: `StaminaExhaustionPostProcess` ativa efeitos URP (lens distortion, vignette, film grain) estritamente quando a stamina cai abaixo de `35` (ver [[player-systems]]).
- **KI-03 (Pickup da Escopeta e Portas)**: `ShotgunPickup` e `DoorController` exigem pressionar `E` via `PlayerInteraction` e sao imunes a disparo de projeteis; `Shotgun_Pedestal` usa `BoxCollider` rente ao topo para nao bloquear o raycast de interacao.
- **KI-04 (Code Mode sem Domain Reload)**: Scripts auxiliares para o agente devem ficar em `My project/AgentScripts/` (fora de `Assets/`) para rodar via `unity command script run` sem disparar reimportacao nem domain reload (ver [[unity-codemode-workflow]]).
- **KI-05 (PhysX `IgnoreCollision` em Pools)**: `Physics.IgnoreCollision` deve ser registrado apenas apos `gameObject.SetActive(true)`, pois o PhysX ignora chamadas em colisores inativos (`ShotgunPellet`, `ShotgunPelletPool`, `SlingshotProjectile`).

## Customizacoes de Agente Ativas

- **Workflow**: `.agents/skills/rennan-dev-cycle/SKILL.md` (5-Gate Protocol + Quick-Fix automatico).
- **Skills GameDev**: `.agents/skills.json` apontando para as 80 skills de `.agents/vendor/awesome-gamedev-agent-skills/`.
- **Plugin Oficial Unity**: `~/.gemini/config/plugins/unity/` (`Unity-Technologies/unity-agent-plugin`, 34 skills oficiais).

# Player Systems

Voltar ao indice: [[MOC]] | Relacionados: [[weapons-and-combat]], [[enemy-ai]], [[ui-and-hud]]

## Escopo

Concentra movimentacao em primeira pessoa, camera, leitura de input, stamina com pos-processamento de exaustao, saude com invulnerabilidade temporaria e deteccao de objetos interativos.

## Componentes (`Assets/Scripts/Player/` e `Effects/`)

| Arquivo | Linhas | Responsabilidade | API / Eventos Principais |
| :--- | :---: | :--- | :--- |
| `PlayerInputHandler.cs` | 209 | Ponte para o Unity Input System (`InputActionAsset`) | `JumpTriggered`, `IsSprinting`, `InteractTriggered`, `PrimaryActionTriggered/Held/Released`, `QuickSlot1Triggered`, `QuickSlot2Triggered`, `SetActionAsset(...)` |
| `FirstPersonController.cs` | 135 | Locomocao, corrida e pulo unico (`jumpHeight 0.85m`, `OverlapSphereNonAlloc` ignorando proprio colisor + lockout) acoplados a `PlayerStamina` | `IsGrounded`, `JumpHeight`, `SetJumpHeight(float)` |
| `FirstPersonCameraLook.cs` | 121 | Rotacao de camera FPS com suavizacao sutil (`24`), recuo vertical, roll lateral e trava do cursor | `AddRecoil(float)`, `SetLookSmoothing(float)`, `LockCursor()`, `UnlockCursor()` |
| `PS2CameraFeel.cs` | 136 | Camera FPS estilo PS2 sutil (FOV 58°, head-bob moderado, respiracao e strafe tilt `0.8°`) | `CurrentRollAngle`, `SetRetroFov(float)`, `ApplySubtlePreset()` |
| `PlayerStamina.cs` | 123 | Recurso de stamina (0-100), consumo em sprint/salto e estado de exaustao | `TryConsumeSprint`, `TryConsumeJump`, `RestoreStamina`, eventos `OnStaminaChanged`, `OnExhaustionChanged` |
| `PlayerHealth.cs` | 106 | Vida do jogador (`IDamageable`), janela de invulnerabilidade e respawn | `TakeDamage`, `Heal`, `SetRespawnPosition`, eventos `OnHealthChanged`, `OnPlayerDamaged`, `OnPlayerDied` |
| `PlayerInteraction.cs` | 126 | Raycast de interacao (tecla `E`, pausado durante leitura de notas) contra `IInteractable` e prompt no HUD | `BindCrosshairHUD(CrosshairHUD)` |
| `DoorController.cs` (`Interaction/`) | 176 | Porta articulada (`IInteractable`) com abertura suave para o lado oposto ao jogador e `NavMeshObstacle` dinamico | `IsOpen`, `Open`, `Close`, `Toggle`, `Configure(pivot, obstacle)` |
| `DrawerController.cs` (`Interaction/`) | 110 | Gaveta deslizante (`IInteractable`) ao longo do eixo local com damping exponencial | `IsOpen`, `Open`, `Close`, `Toggle`, `Configure(...)` |
| `CabinetController.cs` (`Interaction/`) | 285 | Armario com abertura independente pela mira no centro da camera (`ViewportPointToRay`) | `IsOpen`, `IsViewerLeftOpen`, `IsViewerRightOpen`, `ResolveCameraSide`, `ToggleSide`, `OpenSide`, `CloseSide` |
| `InspectableNote.cs` (`Interaction/`) | 51 | Nota legivel (`IInteractable`) com titulo e texto configuraveis no Inspector que abre `NoteInspectionUI` | `NoteTitle`, `NoteBody`, `Configure(...)` |
| `StaminaExhaustionPostProcess.cs` | 229 | Volume URP de exaustao (lens distortion, vignette, film grain) abaixo de stamina `35` | `CurrentWeight`, `TargetWeight`, `BindPlayerStamina(PlayerStamina)` |

## Fluxo de Dados

```mermaid
flowchart LR
    IN["PlayerInputHandler<br/>(Unity Input System)"]
    MOVE["FirstPersonController"]
    CAM["FirstPersonCameraLook"]
    STAM["PlayerStamina"]
    FX["StaminaExhaustionPostProcess<br/>(URP Volume, stamina &lt; 35)"]
    INT["PlayerInteraction"]
    HP["PlayerHealth<br/>(IDamageable)"]

    IN -->|move / sprint / jump| MOVE
    IN -->|look| CAM
    IN -->|interact E| INT
    MOVE -->|TryConsumeSprint / Jump| STAM
    STAM -->|OnStaminaChanged| FX
    STAM -->|OnStaminaChanged| HUD1["[[ui-and-hud]] StaminaBarUI"]
    HP -->|OnHealthChanged| HUD2["[[ui-and-hud]] PlayerHealthBarUI"]
    INT -->|SetTargetingInteractable| HUD3["[[ui-and-hud]] CrosshairHUD"]
    INT -->|Interact| PICK["[[weapons-and-combat]] ShotgunPickup"]
```

## Notas de Integracao

- `PlayerInteraction` depende apenas da interface `ProjectRac.Interaction.IInteractable` (`GetInteractionPrompt()`, `Interact()`), mantendo desacoplamento entre jogador, `InteractableTestObject` e `ShotgunPickup`.
- `PlayerHealth` implementa `ProjectRac.Combat.IDamageable`, recebendo dano direto de [[enemy-ai]] (`EnemyMeleeAI`) respeitando `IsInvulnerable`.
- `FirstPersonCameraLook.AddRecoil` e chamado pelas armas em [[weapons-and-combat]] para aplicar coice de camera.

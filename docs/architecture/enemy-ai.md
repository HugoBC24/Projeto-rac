# Enemy AI

Voltar ao indice: [[MOC]] | Relacionados: [[player-systems]], [[weapons-and-combat]], [[editor-tooling]]

## Escopo

Controla inimigos corpo a corpo e a distancia com navegacao baseada em `NavMeshAgent` (`com.unity.ai.navigation` `2.0.15`), percepcao com linha de visao (LOS), posicionamento tatico (patrulha, perseguicao, ataque, recuo), pooling de projeteis inimigos e ciclo de dano/morte via `IDamageable`.

## Componentes (`Assets/Scripts/Enemy/`)

| Arquivo | Linhas | Responsabilidade | API / Eventos Principais |
| :--- | :---: | :--- | :--- |
| `EnemyMeleeAI.cs` | 307 | Maquina de estados `AIState` (`Patrol`, `Chase`, `Attack`) corpo a corpo sobre `NavMeshAgent` | `CurrentState`, `detectionRadius` (`14m`), `loseTargetRadius` (`22m`), `attackRange` (`1.8m`), `attackDamage` (`20`) |
| `EnemyRangedAI.cs` | 289 | Maquina de estados `RangedAIState` (`Patrol`, `Chase`, `Attack`, `Retreat`) com LOS em timer (`0.15s`), windup (`0.35s`) e recuo | `CurrentState`, `HasLineOfSight`, `FirePoint`, `BindProjectilePool(...)` |
| `EnemyPerception.cs` | 84 | Sensor de linha de visao (peito/cabeca) e amostragem de pontos de patrulha/recuo no `NavMesh` | `HasLineOfSight(...)`, `TrySamplePatrolPoint(...)`, `TrySampleRetreatPoint(...)` |
| `EnemyRangedVisuals.cs` | 173 | Modelo procedural distinto do `RangedEnemy` (armadura azul-petroleo, visor ambar, blaster + `FirePoint` + brilho de carregamento) | `EnsureVisuals(...)`, `SetMuzzleColor(...)` |
| `EnemyProjectile.cs` | 146 | Projetil fisico (`ContinuousDynamic`, sem gravidade) com `IgnoreCollision` pos-ativacao (`KI-05`), dano em `PlayerHealth` e reciclagem | `Launch(...)`, `Recycle()` |
| `EnemyProjectilePool.cs` | 124 | Pool reutilizavel de projeteis inimigos (padrao `20`) com materiais URP compartilhados | `Configure(prefab, 20)`, `SpawnProjectile(...)`, `ReturnToPool(...)` |
| `EnemyHealth.cs` | 153 | Vida do inimigo (`IDamageable`), flash visual de impacto e sinalizacao de morte | `MaxHealth`, `CurrentHealth`, `IsDead`, `SetMaxHealth(...)`, `RefreshRenderers()`, `TakeDamage(...)`, eventos `OnHealthChanged`, `OnDamageTaken`, `OnDeath` |

## Maquina de Estados (`EnemyRangedAI`)

```mermaid
stateDiagram-v2
    [*] --> Patrol
    Patrol --> Chase: "Distancia <= 20m e LOS livre (ou dano sofrido)"
    Chase --> Patrol: "Distancia > 28m ou perdeu alvo na ultima posicao vista"
    Chase --> Attack: "Distancia <= 15m e LOS livre"
    Attack --> Chase: "Distancia > 18m ou perdeu LOS"
    Attack --> Retreat: "Distancia < 5.5m e LOS livre"
    Retreat --> Attack: "Distancia >= 8.0m ou sem saida no NavMesh"
    Retreat --> Chase: "Perdeu LOS"
    Patrol --> Dead: "EnemyHealth.OnDeath"
    Chase --> Dead: "EnemyHealth.OnDeath"
    Attack --> Dead: "EnemyHealth.OnDeath"
    Retreat --> Dead: "EnemyHealth.OnDeath"
    Dead --> [*]
```

## Notas de Integracao e Saude Tecnica

- `EnemyRangedAI` e `EnemyMeleeAI` exigem `[RequireComponent(typeof(NavMeshAgent))]` e `[RequireComponent(typeof(EnemyHealth))]`.
- `EnemyProjectile` ignora outros `EnemyHealth` (sem fogo amigo), ignora `DoorController`/`ShotgunPickup` e aplica dano somente em `PlayerHealth` ([[player-systems]]).
- Paredes, portas fechadas (`WallWithDoor`) e caixas de fisica bloqueiam tanto o sensor `EnemyPerception.HasLineOfSight` quanto os projeteis inimigos, funcionando como cobertura real.
- **Debito tecnico registrado (KI-01)**: `EnemyMeleeAI.cs` permanece em **307 linhas**; todos os novos modulos de `RangedEnemy` foram estruturados abaixo de 300 linhas.

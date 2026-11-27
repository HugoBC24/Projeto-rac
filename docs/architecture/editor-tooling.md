# Editor Tooling

Voltar ao indice: [[MOC]] | Relacionados: [[player-systems]], [[weapons-and-combat]], [[enemy-ai]], [[ui-and-hud]], [[unity-codemode-workflow]]

## Escopo

Scripts exclusivos do Unity Editor (`Assets/Scripts/Editor/`) usados para montar de forma reproduzivel a arena de teste (`SampleScene`), os inimigos, os prefabs de armas e a hierarquia completa do Canvas uGUI.

## Builders (`Assets/Scripts/Editor/`)

| Arquivo | Linhas | Menu / Funcao | O que Constroi |
| :--- | :---: | :--- | :--- |
| `TestArenaBuilder.cs` | 300 | `[MenuItem("Tools/Build FPAA Test Arena")]` | Monta a arena de teste completa em `SampleScene` (plataformas incluindo `Platform_High` a `2.2m`, parede com porta via `DoorArenaBuilder`, jogador, pedestal da escopeta, `MeleeEnemy` + `RangedEnemy` + `EnemyProjectilePool`, NavMesh e HUD) |
| `DoorArenaBuilder.cs` | 215 | Helper estatico | Constroi `WallWithDoor` (`10m x 4m`) com batente, `Door_Pivot`, `Door_Leaf` (`DoorController` + `NavMeshObstacle` + `NavMeshModifier`), macanetas e materiais URP persistentes em `Assets/Materials/Arena/` |
| `Map/MansionMapBuilder.cs` | 256 | `[MenuItem("Tools/Build Mansion Map (Center)")]` / `Upgrade Mansion Floor 2 (Railings & Balconies)` | Constroi `MansionMap` (`30m x 21.2m`, 2 andares, escada no HALL, 12 portas interativas, guarda-corpo no mezanino do Hall, 2 varandas cobertas sem porta no 2o andar, teto `Roof`, iluminacao e `NavMeshSurface`), suportando atualizacao in-place do `Floor2` preservando escala e props |
| `Map/MansionLayoutData.cs` | 245 | Dados estaticos | Coordenadas calibradas da planta (1o e 2o andar, recortes de laje, `Floor2Railings`, `Floor2BalconyColumns`, vaos sem porta das varandas, escada e luzes) |
| `Map/MansionDoorFactory.cs` | 191 | Helper estatico | Gera paredes (em espaco local), vaos abertos (`Archway`) e portas interativas completas (`DoorController` + `NavMeshObstacle` + `NavMeshModifier`) em qualquer orientacao |
| `Map/MansionRailingFactory.cs` | 134 | Helper estatico | Gera guarda-corpos de madeira escura (`TopRail`, `BottomCurb`, postes, balaustres e `Railing_GuardCollider` unico de `1.15m`) com suporte a recorte livre para a escada, alem das colunas de canto das varandas cobertas |
| `Map/MansionColliderFixer.cs` | 214 | `[MenuItem("Tools/Fix Mansion Colliders After Resize")]` | Gera `MeshCollider` convexo sem cisalhamento para a rampa (`StairRampMesh.asset`) e corrimao (`StairHandrailMesh.asset`), imune a escala nao-uniforme da `MansionMap`, e refaz bake do `NavMesh` |
| `Map/MansionPropsTestBuilder.cs` | 263 | `[MenuItem("Tools/Build Mansion Test Props (Drawer, Cabinet, Note)")]` | Cria 1 comoda com gaveta (`DrawerController`), 1 armario de 2 portas (`CabinetController`), 1 nota (`InspectableNote`) no `HALL` e o painel `NoteInspectionUI` no `HUD_Canvas` |
| `PS2VisualSetupBuilder.cs` | 272 | `[MenuItem("Tools/Apply PS2 Visual & Camera Style")]` | Configura shader `PS2RetroPostProcess` suavizado (`640×360`, dither `0.45`, scanlines `0.08`), `PS2_GlobalVolume` (prio 5, vinheta `0.14`), iluminacao interna sem lanterna, `PS2CameraFeel` (FOV 58°) e `jumpHeight 0.85m` |
| `HUDCanvasBuilder.cs` | 283 | Helper estatico | Cria o Canvas uGUI principal, `CrosshairHUD`, `SlingshotChargeHUD`, `StaminaBarUI`, `NoteInspectionUI` e `VisualNovelDialoguePanel`, ligando referencias |
| `UI/VisualNovelDialogueBuilder.cs` | 228 | `[MenuItem("Tools/Build Visual Novel Intro Dialogue UI")]` | Monta `VisualNovelDialoguePanel` em 3 camadas no `HUD_Canvas` (caixa preta com borda branca, retratos altos sobre a borda, texto com margens seguras e seta `▼`) |
| `UI/VisualNovelDialogueUIEditor.cs` | 209 | `[CustomEditor(typeof(VisualNovelDialogueUI))]` | Inspector customizado com campo `Quantidade de Dialogos`, botoes `+ Adicionar` / `- Remover`, caixas multi-linha de texto, sprite, posicao e reordenacao |
| `UI/VisualNovelPlaceholderArt.cs` | 134 | Helper estatico | Gera sprites placeholder PNG em `Assets/Art/UI/VN/` (`vn_placeholder_maid`, `vn_placeholder_guest`, `vn_continue_arrow`) |
| `AmmoHUDBuilder.cs` | 126 | Helper estatico | Constroi o painel de municao (`AmmoHUD`) e conecta ao inventario/armas |
| `HealthHUDBuilder.cs` | 93 | Helper estatico | Constroi a barra de vida (`PlayerHealthBarUI`) e vincula a `PlayerHealth` |
| `EnemyBuilder.cs` | 156 | Helper estatico | Instancia `MeleeEnemy` (`80 HP`), `RangedEnemy` (`60 HP` com materiais URP persistentes em `Assets/Materials/Enemies/`) e `EnemyProjectilePool` sob `ShootPools` |
| `SlingshotPrefabGenerator.cs` | 99 | `[MenuItem("Tools/Generate Slingshot Prefab")]` | Gera `Assets/Prefabs/Slingshot_Viewmodel.prefab` |
| `TestCubeCreator.cs` | 22 | Helper estatico | Utilitario minimo de criacao de cubo de teste |

## Relacao com Code Mode

```mermaid
flowchart LR
    AGENT["Agente Antigravity<br/>(Code Mode)"] -->|unity command script run| PROBE["AgentScripts/*.cs<br/>(fora de Assets/)"]
    PROBE -->|chama APIs de Editor| BUILDERS["Assets/Scripts/Editor/*Builder.cs"]
    BUILDERS -->|atualiza cena ativa| SCENE["Assets/Scenes/SampleScene.unity"]
```

## Saude Tecnica

- **Debito tecnico registrado (KI-01)**: `TestArenaBuilder.cs` possui **307 linhas**. Caso novas areas ou obstaculos sejam adicionados a arena, separar a construcao de geometria da configuracao de entidades.
- Sempre que um Editor estiver aberto, prefira acionar builders ou editar a cena via [[unity-codemode-workflow]] em vez de editar arquivos `.unity` ou `.prefab` manualmente em texto.

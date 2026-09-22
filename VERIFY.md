# Sigilwoven — VERIFY

## 1. dotnet build
Command: `cd ~/dev/sigilwoven && dotnet build Sigilwoven.csproj`
Result: **Build succeeded. 0 Warning(s) 0 Error(s).**
TargetFramework: **net9.0** (менять на net8.0 НЕ потребовалось; Godot.NET.Sdk/4.7.2 + SDK 10.0.111 собрал net9.0 штатно).
Фикс по ходу: `DirectionalLight3D` shadow mode — правильное имя enum `DirectionalLight3D.ShadowMode.Parallel4Splits` (это PSSM4).

## 2. Headless-прогон
Commands:
- `timeout 60 godot-mono --headless --path ~/dev/sigilwoven --import` → IMPORT_EXIT=0
- `timeout 25 godot-mono --headless --path ~/dev/sigilwoven --quit-after 5` → EXIT=0

Output (обрезанный):
```
Godot Engine v4.7.2.stable.mono.arch_linux.ed1daf0bf
[Sigilwoven] Building world...
[Sigilwoven] World ready.
EXIT=0
```
Script Error / ERROR: / CS-ошибок — **нет**.

## 3. Что работоспособно
- Мир строится в рантайме из C# (Main._Ready): DirectionalLight PSSM4, WorldEnvironment+ProceduralSky, земля 60x1x60, игрок, 4 врага-мишени в группе "enemies".
- PlayerController: капсула, камера TPS за плечом (pivot + mouse orbit, Q/E), WASD MoveAndSlide ~6, ЛКМ raycast к плоскости Y=0 + каст, hint Label.
- SkillCatalog: ровно 25 скиллов (meteor, chain_lightning, lightning_elemental, lightning_wisp, dread_hound, bone_golem, phoenix + остальные).
- LinkCatalog: 6 связок (lightning_form, fire_link, cold_touch, chain_extension, stun_impacts, persist_aura).
- LinkSystem.ResolveCast: lightning_form → LightningForm+стан 50%/0.5с; fire_link/chain_extension на AreaAttack → ground fire 1.5с; chain count, burn, persist.
- SkillCaster: 4 слота (1: meteor+fire, 2: elemental+lightning_form, 3: chain+chainExt, 4: golem), клавиши 1-4, ЛКМ каст (саммон/area+burning ground/projectile+chain/buff-print).
- Minion: следует за игроком, бьёт врагов; молниеформа — циановый emissive + гало, урон молнией + стан через TakeDamage(resolved).
- Meteor: взрыв-визуал + урон по радиусу 4 + BurningGround (Area3D, тики 0.25с, 1.5с).
- LinkMenuUI: клавиша C открывает, Esc закрывает, OptionButton скиллов + 6 CheckBox линков, Apply в слот 1.
- Дамаг-числа: Label3D с твином при попадании, burn-тики отдельно.

## 4. Состояние после патча №010

- `Cold Touch` теперь действительно накладывает 38% Chill на 1.4 с;
  `Venom Seal` добавляет зелёный DoT, `Execution Mark` усиливает попадания
  по врагам с HP ниже 35%.
- Последняя проверка: `dotnet build` — 0 warnings / 0 errors; headless-запуск
  строит мир и запускает ритуал без C# и Script Error.
- На потом: баланс, звук, сохранения билдов и полноценные анимации моделей.

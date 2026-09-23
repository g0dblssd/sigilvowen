# Патч №038 — автономные сборки Linux и Windows

Дата: 2026-09-22

## Реализация

- Добавлены Godot .NET export presets `Linux x86_64` и `Windows x86_64`.
- Добавлен обязательный `Sigilwoven.sln`, используемый .NET export pipeline.
- Добавлены автоматические сборщики:
  - `tools/build-release.sh` для Linux/macOS shell;
  - `tools/build-release.ps1` для Windows PowerShell.
- Каждый сборщик компилирует Release, экспортирует обе платформы, проверяет
  исполняемый файл и `Sigilwoven.dll` внутри платформенного .NET runtime,
  после чего создаёт архивы распространения.
- `build/` исключён из Git: бинарники пересобираются, а не хранятся в истории.
- README описывает запуск, сборку и расположение сохранений на обеих ОС.
- Окно получило базовый viewport 1600×900 и stretch для разных разрешений.

## Созданные дистрибутивы

- `build/packages/Sigilwoven-linux-x86_64.tar.gz` — около 74 MiB.
- `build/packages/Sigilwoven-windows-x86_64.zip` — около 84 MiB.
- Оба пакета self-contained: пользователю не требуется Godot или .NET.

## Проверка

- Release C# build: 0 warnings, 0 errors.
- Linux: ELF 64-bit x86-64, runtime и игровая assembly присутствуют.
- Windows: PE32+ x86-64 GUI, runtime и игровая assembly присутствуют.
- Автономная Linux-сборка запущена напрямую без редактора и успешно создала
  мир, импортированного героя и стартовый ритуал.
- Windows-бинарник сформирован официальным Godot 4.7.2 .NET template; запуск
  необходимо дополнительно повторить на настоящей Windows-машине/CI runner.
- `git diff --check`: выполняется финальной проверкой.

## План контента

По запросу добавлен отдельный backlog из 72 пунктов:
`tasks/GLOBAL_CONTENT_AND_DIFFICULTY_ROADMAP.md`.

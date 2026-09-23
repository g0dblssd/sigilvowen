# Sigilwoven

Изометрическая ARPG на Godot 4.7.2 .NET. Проект и готовые сборки поддерживают
64-битные Linux и Windows.

## Скачать

Готовые установщики находятся на странице
[GitHub Releases](https://github.com/g0dblssd/sigilvowen/releases/latest):

- `Sigilwoven-Setup-Windows-x86_64.exe` — обычный Windows Setup;
- `Sigilwoven-Installer-Linux-x86_64.run` — автономный Linux-установщик;
- ZIP/TAR.GZ — portable-версии без установки.

## Запуск готовой игры

Готовой сборке не нужны отдельно установленные Godot или .NET.

- Linux: распаковать архив, разрешить запуск файла `Sigilwoven.x86_64`
  (`chmod +x Sigilwoven.x86_64`) и запустить его.
- Windows 10/11 x64: распаковать весь архив и запустить `Sigilwoven.exe`.
  Нельзя вынимать только `.exe`: рядом находятся PCK и .NET-библиотеки игры.

Для установщика Linux:

```bash
chmod +x Sigilwoven-Installer-Linux-x86_64.run
./Sigilwoven-Installer-Linux-x86_64.run
```

Сохранения платформенно-независимы и записываются через Godot `user://`:

- Linux: `~/.local/share/godot/app_userdata/Sigilwoven/`;
- Windows: `%APPDATA%\Godot\app_userdata\Sigilwoven\`.

## Сборка релиза

Нужны Godot **4.7.2 .NET editor**, соответствующие .NET export templates и
.NET SDK, способный собрать `net9.0`. Export templates устанавливаются через
`Editor → Manage Export Templates`.

Linux/macOS shell:

```bash
./tools/build-release.sh
```

Windows PowerShell:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\tools\build-release.ps1
```

Оба скрипта сначала проверяют C#-сборку, затем создают:

- `build/linux/Sigilwoven.x86_64` и Linux runtime;
- `build/windows/Sigilwoven.exe` и Windows runtime;
- архивы для распространения в `build/packages/`.

Сборку можно выполнить вручную командами:

```bash
godot-mono --headless --path . --export-release "Linux x86_64" build/linux/Sigilwoven.x86_64
godot-mono --headless --path . --export-release "Windows x86_64" build/windows/Sigilwoven.exe
```

## Запуск исходного проекта

```bash
dotnet build
godot-mono --path .
```

На Windows бинарник редактора обычно называется
`Godot_v4.7.2-stable_mono_win64.exe`; путь к нему можно передать скрипту через
переменную `GODOT_BIN`.

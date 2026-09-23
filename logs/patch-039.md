# Патч №039 — установщики и GitHub Releases

Дата: 2026-09-22

## Загрузка для игроков

На странице GitHub Releases публикуются четыре готовых варианта:

- `Sigilwoven-Setup-Windows-x86_64.exe` — Windows Setup без прав администратора;
- `Sigilwoven-Installer-Linux-x86_64.run` — self-extracting Linux installer;
- `Sigilwoven-windows-x86_64.zip` — portable Windows;
- `Sigilwoven-linux-x86_64.tar.gz` — portable Linux;
- `SHA256SUMS.txt` — контрольные суммы установщиков и архивов.

## Windows installer

- Устанавливает игру в `%LOCALAPPDATA%\Programs\Sigilwoven`.
- Создаёт ярлыки рабочего стола и меню «Пуск».
- Регистрирует штатный деинсталлятор в Windows Apps.
- Не запрашивает права администратора.

## Linux installer

- Устанавливает игру в `$XDG_DATA_HOME/sigilwoven` или `~/.local/share/sigilwoven`.
- Создаёт команду `~/.local/bin/sigilwoven`.
- Добавляет desktop entry в меню приложений.
- Не требует root и не изменяет системные каталоги.

## Автоматизация GitHub

Workflow `.github/workflows/release-installers.yml` при push тега `v*`:

1. Загружает официальный Godot 4.7.2 .NET editor и export templates.
2. Восстанавливает Linux/Windows .NET runtime packs.
3. Экспортирует обе self-contained версии.
4. Собирает Linux `.run` и Windows NSIS `Setup.exe`.
5. Создаёт SHA-256 суммы.
6. Публикует отдельный GitHub Release и прикрепляет все загрузки.

# LumberjackGame — «Лесоруб»

3D survival-игра на Unity: игрок в роли лесоруба валит деревья топором, перерабатывает брёвна на пилораме в доски, продаёт ресурсы и улучшает дом, топор и инвентарь. Процедурно генерируемый мир с циклом день/ночь.

## Особенности

- **Игровой цикл** — рубка деревьев → переноска → распиловка на пилораме → продажа → улучшения
- **Процедурная генерация мира** — террейн, деревья, трава, дома; всё строится кодом в рантайме
- **Система улучшений** — 4 уровня топора (урон/скорость), 4 уровня инвентаря, 5 уровней дома (бонус к продаже)
- **Квесты и сохранения** — система заданий, автосохранение прогресса
- **День/ночь** — динамический цикл освещения
- **Миникарта и HUD** — UI на uGUI
- **Процедурные текстуры** — UV-развёртка моделей и генерация текстур через PIL-скрипты в `Tools/`

## Стек

- **Unity 6000.6 (Unity 6)**, C#
- Процедурная генерация террейна и мешей
- Unity UI (uGUI), Input System
- Python/PIL — генерация текстур для проекта

## Запуск

1. Открыть проект в Unity Hub (версия **6000.6.7f1**)
2. Открыть основную сцену и нажать Play

Готовая сборка под macOS: `Builds/Mac/LumberjackGame.app` (не коммитится, собирается через `Build → Build`)

## Структура

```
Assets/Scripts/
  Core/      — GameManager, GameBootstrap, QuestManager, SaveManager,
              DayNightCycle, AudioManager, Noise
  Player/    — PlayerController, AxeController, Interaction, PlayerTag
  World/     — TerrainGenerator, TerrainMath, WorldBuilder, Tree,
              GrassField, Sawmill, HouseManager, SellPoint, Door,
              Pickup, TextureLibrary
  UI/        — UIManager, Minimap
  Editor/    — GameSetup (автонастройка сцены из редактора)
Tools/       — Python-скрипты генерации текстур/экспорта
```

## Скриншоты

<!-- screenshots -->

## Автор

Смирнов Александр — студент 4 курса, БГТУ «ВОЕНМЕХ» (ИСиТ)

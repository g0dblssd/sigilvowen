using System.Collections.Generic;
using Godot;

namespace Sigilwoven;

/// <summary>Controls the birth ritual and places the dormant surface packs.</summary>
public partial class SpawnSequence : Node
{
    private const float RitualDuration = 5f;
    private float _elapsed;
    private bool _packsSpawned;
    private bool _ready;
    private bool _ritualStarted;
    private bool _classConfirmed;
    private bool _populationPaused;
    private bool _dungeonUnlocked;
    private int _initialPacksRemaining;
    private int _lastObjectiveValue = -1;
    private float _worldClock;
    private PlayerController? _player;
    private Node3D? _world;
    private readonly List<SurfacePackDefinition> _packDefinitions = new();
    public event System.Action? SurfaceCleared;

    private sealed class SurfacePackDefinition
    {
        public string Name = "";
        public Vector3 Position;
        public float BaseHp;
        public EnemyArchetype[] Archetypes = System.Array.Empty<EnemyArchetype>();
        public bool FirstClearComplete;
        public float RespawnAt = -1f;
        public MobPack? ActivePack;
    }

    public void Setup(PlayerController player, Node3D world)
    {
        _player = player;
        _world = world;
        StartRitualIfReady();
    }

    public override void _Ready()
    {
        _ready = true;
        StartRitualIfReady();
    }

    public void ConfirmClassChoice()
    {
        _classConfirmed = true;
        StartRitualIfReady();
    }

    private void StartRitualIfReady()
    {
        if (!_ready || !_classConfirmed || _ritualStarted || _player == null)
        {
            return;
        }
        _ritualStarted = true;
        _player.BeginBirth(1.5f, RitualDuration);
        _player.SetObjectiveStatus($"RITUAL  {Mathf.CeilToInt(RitualDuration)}");
        GD.Print("[Spawn] Ritual started: stun 1.5s, damage -30% for 5s.");
    }

    public override void _Process(double delta)
    {
        if (_packsSpawned)
        {
            UpdatePopulation((float)delta);
            return;
        }
        _elapsed += (float)delta;
        if (_ritualStarted && _player != null)
        {
            int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(RitualDuration - _elapsed));
            if (secondsLeft != _lastObjectiveValue)
            {
                _lastObjectiveValue = secondsLeft;
                _player.SetObjectiveStatus($"RITUAL  {secondsLeft}");
            }
        }
        if (_ritualStarted && _elapsed >= RitualDuration && _player != null && _world != null)
        {
            SpawnSurfacePacks();
        }
    }

    public void SetPopulationPaused(bool paused)
    {
        _populationPaused = paused;
        if (!paused)
        {
            _player?.SetObjectiveStatus("WORLD HUNT  •  PACKS ARE REFORMING");
            return;
        }
        foreach (SurfacePackDefinition definition in _packDefinitions)
        {
            if (definition.ActivePack != null && IsInstanceValid(definition.ActivePack))
            {
                definition.ActivePack.Cleared -= OnSurfacePackCleared;
                definition.ActivePack.QueueFree();
                definition.ActivePack = null;
                definition.RespawnAt = _worldClock + 12f;
            }
        }
    }

    private void SpawnSurfacePacks()
    {
        PlayerController? player = _player;
        Node3D? world = _world;
        if (player == null || world == null)
        {
            return;
        }
        _packsSpawned = true;
        _packDefinitions.AddRange(new[]
        {
            CreateDefinition("EMBER RAIDERS", new Vector3(15f, 0f, 7f), 62f, EnemyArchetype.Raider, EnemyArchetype.Ghoul, EnemyArchetype.Brute),
            CreateDefinition("VEIL HEXERS", new Vector3(-18f, 0f, -13f), 58f, EnemyArchetype.Hexer, EnemyArchetype.SkeletonArcher, EnemyArchetype.Raider),
            CreateDefinition("BROKEN VOW", new Vector3(8f, 0f, 27f), 68f, EnemyArchetype.Brute, EnemyArchetype.Berserker, EnemyArchetype.Leaper, EnemyArchetype.Ghoul),
            CreateDefinition("ASHEN TEETH", new Vector3(34f, 0f, -22f), 74f, EnemyArchetype.Shieldbearer, EnemyArchetype.Brute, EnemyArchetype.Vampire, EnemyArchetype.Necromancer),
            CreateDefinition("PALE CIRCLE", new Vector3(-36f, 0f, 21f), 72f, EnemyArchetype.Hexer, EnemyArchetype.Necromancer, EnemyArchetype.SkeletonArcher, EnemyArchetype.Vampire),
            CreateDefinition("HOLLOW OATH", new Vector3(2f, 0f, -40f), 78f, EnemyArchetype.Brute, EnemyArchetype.Berserker, EnemyArchetype.Ghoul, EnemyArchetype.SkeletonArcher, EnemyArchetype.Vampire),
        });
        _initialPacksRemaining = _packDefinitions.Count;
        foreach (SurfacePackDefinition definition in _packDefinitions)
        {
            SpawnPack(world, player, definition);
        }
        player.SetObjectiveStatus($"WORLD HUNT  •  {_packDefinitions.Count} DORMANT PACKS");
        player.ShowCombatMessage("PACKS AWAKEN WHEN YOU ENTER THEIR SIGILS");
        GD.Print($"[Spawn] Ritual complete. {_packDefinitions.Count} respawning surface packs placed.");
    }

    private static SurfacePackDefinition CreateDefinition(string name, Vector3 position, float baseHp, params EnemyArchetype[] archetypes)
    {
        return new SurfacePackDefinition { Name = name, Position = position, BaseHp = baseHp, Archetypes = archetypes };
    }

    private void SpawnPack(Node3D world, PlayerController player, SurfacePackDefinition definition)
    {
        var pack = new MobPack();
        pack.Setup(player, definition.Name, 6f);
        for (int i = 0; i < definition.Archetypes.Length; i++)
        {
            float angle = Mathf.Tau * i / definition.Archetypes.Length;
            EliteModifier elite = i == 0 ? Enemy.RollEliteModifier() : EliteModifier.None;
            Enemy enemy = pack.AddMember(definition.BaseHp, definition.Archetypes[i], new Vector3(Mathf.Cos(angle) * 1.8f, 0f, Mathf.Sin(angle) * 1.8f), elite);
            float healthScale = 1f + (player.Progression.Level - 1) * 0.011f;
            float damageScale = 1f + (player.Progression.Level - 1) * 0.0045f;
            float rarityBonus = Mathf.Min(0.3f, player.Progression.Level / 1000f);
            enemy.ApplyDifficulty(healthScale, damageScale, rarityBonus);
        }
        pack.Cleared += OnSurfacePackCleared;
        world.AddChild(pack);
        pack.GlobalPosition = definition.Position;
        definition.ActivePack = pack;
        definition.RespawnAt = -1f;
    }

    private void OnSurfacePackCleared(MobPack pack)
    {
        pack.Cleared -= OnSurfacePackCleared;
        SurfacePackDefinition? clearedDefinition = null;
        foreach (SurfacePackDefinition definition in _packDefinitions)
        {
            if (definition.ActivePack == pack)
            {
                clearedDefinition = definition;
                break;
            }
        }
        if (clearedDefinition == null)
        {
            return;
        }
        clearedDefinition.ActivePack = null;
        clearedDefinition.RespawnAt = _worldClock + 14f;
        if (!clearedDefinition.FirstClearComplete)
        {
            clearedDefinition.FirstClearComplete = true;
            _initialPacksRemaining = Mathf.Max(0, _initialPacksRemaining - 1);
        }
        if (_player == null)
        {
            return;
        }
        if (!_dungeonUnlocked && _initialPacksRemaining > 0)
        {
            _player.SetObjectiveStatus($"WORLD HUNT  •  {_initialPacksRemaining} FIRST-CLEAR PACKS REMAIN");
            return;
        }
        if (!_dungeonUnlocked)
        {
            _dungeonUnlocked = true;
            _player.RestoreMana(_player.MaxMana);
            _player.SetObjectiveStatus("WORLD FIRST CLEAR  •  RAID UNLOCKED");
            _player.ShowCombatMessage("THE DESCENDING RAID IS NOW AVAILABLE");
            SurfaceCleared?.Invoke();
            GD.Print("[Spawn] Surface first-clear complete. Raid unlocked.");
        }
    }

    private void UpdatePopulation(float delta)
    {
        if (_populationPaused || _world == null || _player == null)
        {
            return;
        }
        _worldClock += delta;
        int activeCount = 0;
        foreach (SurfacePackDefinition definition in _packDefinitions)
        {
            if (definition.ActivePack != null && IsInstanceValid(definition.ActivePack))
            {
                activeCount++;
            }
            else if (definition.RespawnAt >= 0f && _worldClock >= definition.RespawnAt)
            {
                SpawnPack(_world, _player, definition);
                activeCount++;
            }
        }
        if (_dungeonUnlocked && activeCount != _lastObjectiveValue)
        {
            _lastObjectiveValue = activeCount;
            _player.SetObjectiveStatus($"OPEN WORLD  •  {activeCount} ACTIVE PACKS  •  RAID READY");
        }
    }
}

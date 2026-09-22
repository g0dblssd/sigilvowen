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
    private int _surfacePacksRemaining;
    private int _lastObjectiveValue = -1;
    private PlayerController? _player;
    private Node3D? _world;
    public event System.Action? SurfaceCleared;

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

    private void StartRitualIfReady()
    {
        if (!_ready || _ritualStarted || _player == null)
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

    private void SpawnSurfacePacks()
    {
        PlayerController? player = _player;
        Node3D? world = _world;
        if (player == null || world == null)
        {
            return;
        }
        _packsSpawned = true;
        _surfacePacksRemaining = 3;
        SpawnPack(world, player, "EMBER RAIDERS", new Vector3(10f, 0f, 2f), 62f,
            new[] { EnemyArchetype.Raider, EnemyArchetype.Raider, EnemyArchetype.Brute });
        SpawnPack(world, player, "VEIL HEXERS", new Vector3(-10f, 0f, -8f), 58f,
            new[] { EnemyArchetype.Hexer, EnemyArchetype.Hexer, EnemyArchetype.Raider });
        SpawnPack(world, player, "BROKEN VOW", new Vector3(5f, 0f, 13f), 68f,
            new[] { EnemyArchetype.Brute, EnemyArchetype.Raider, EnemyArchetype.Hexer, EnemyArchetype.Raider });
        player.SetObjectiveStatus("SURFACE HUNT  •  3 DORMANT PACKS");
        player.ShowCombatMessage("PACKS AWAKEN WHEN YOU ENTER THEIR SIGILS");
        GD.Print("[Spawn] Ritual complete. Three dormant surface packs placed.");
    }

    private void SpawnPack(Node3D world, PlayerController player, string name, Vector3 position, float baseHp, EnemyArchetype[] archetypes)
    {
        var pack = new MobPack();
        pack.Setup(player, name, 5.2f);
        for (int i = 0; i < archetypes.Length; i++)
        {
            float angle = Mathf.Tau * i / archetypes.Length;
            pack.AddMember(baseHp, archetypes[i], new Vector3(Mathf.Cos(angle) * 1.55f, 0f, Mathf.Sin(angle) * 1.55f));
        }
        pack.Cleared += OnSurfacePackCleared;
        world.AddChild(pack);
        pack.GlobalPosition = position;
    }

    private void OnSurfacePackCleared(MobPack pack)
    {
        pack.Cleared -= OnSurfacePackCleared;
        _surfacePacksRemaining = Mathf.Max(0, _surfacePacksRemaining - 1);
        if (_player == null)
        {
            return;
        }
        if (_surfacePacksRemaining > 0)
        {
            _player.SetObjectiveStatus($"SURFACE HUNT  •  {_surfacePacksRemaining} PACKS REMAIN");
            return;
        }
        _player.RestoreMana(PlayerController.MaxMana);
        _player.SetObjectiveStatus("SURFACE CLEARED  •  DUNGEON UNLOCKED");
        _player.ShowCombatMessage("ECHOING VAULT IS NOW AVAILABLE");
        SurfaceCleared?.Invoke();
        GD.Print("[Spawn] Surface packs cleared. Dungeon unlocked.");
    }
}

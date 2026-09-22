using Godot;

namespace Sigilwoven;

/// <summary>Controls the five-second hero birth ritual and the opening ambush.</summary>
public partial class SpawnSequence : Node
{
    private const float RitualDuration = 5f;
    private float _elapsed;
    private bool _ambushStarted;
    private bool _ready;
    private bool _ritualStarted;
    private int _wave;
    private float _nextWaveTimer = -1f;
    private bool _encounterComplete;
    private int _lastObjectiveValue = -1;
    private PlayerController? _player;
    private Node3D? _world;

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
        if (_ambushStarted)
        {
            UpdateWaves((float)delta);
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
            StartAmbush();
        }
    }

    private void StartAmbush()
    {
        PlayerController? player = _player;
        Node3D? world = _world;
        if (player == null || world == null)
        {
            return;
        }
        _ambushStarted = true;
        _wave = 1;
        Vector3[] offsets =
        {
            new Vector3(7f, 0f, 1f), new Vector3(-6f, 0f, 3f),
            new Vector3(2f, 0f, -7f), new Vector3(-4f, 0f, -6f),
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            var enemy = new Enemy();
            EnemyArchetype archetype = i == offsets.Length - 1 ? EnemyArchetype.Hexer : EnemyArchetype.Raider;
            enemy.Configure(60f, false, archetype);
            world.AddChild(enemy);
            enemy.GlobalPosition = player.GlobalPosition + offsets[i];
            enemy.JoinAmbush(player);
        }
        _lastObjectiveValue = offsets.Length;
        player.SetObjectiveStatus($"WAVE 1  •  {offsets.Length} HOSTILES");
        GD.Print("[Spawn] Ritual complete. Opening ambush spawned: 4 enemies.");
    }

    private void UpdateWaves(float delta)
    {
        if (_encounterComplete || _world == null || _player == null)
        {
            return;
        }
        int hostileCount = 0;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion() && !enemy.IsTrainingDummy)
            {
                hostileCount++;
            }
        }
        if (hostileCount > 0)
        {
            _nextWaveTimer = -1f;
            if (hostileCount != _lastObjectiveValue)
            {
                _lastObjectiveValue = hostileCount;
                _player.SetObjectiveStatus($"WAVE {_wave}  •  {hostileCount} HOSTILES");
            }
            return;
        }
        if (_wave >= 3)
        {
            _encounterComplete = true;
            _player.RestoreMana(PlayerController.MaxMana);
            _player.SetObjectiveStatus("ARENA CLEARED  •  AETHER RESTORED");
            _player.ShowCombatMessage("VICTORY — THE SIGIL ACCEPTS YOU");
            GD.Print("[Spawn] Arena cleared. Encounter complete.");
            return;
        }
        if (_nextWaveTimer < 0f)
        {
            _nextWaveTimer = 3f;
            _lastObjectiveValue = 3;
            _player.SetObjectiveStatus("WAVE CLEARED  •  NEXT IN 3");
            _player.ShowCombatMessage("NEXT WAVE IN 3");
            return;
        }
        _nextWaveTimer -= delta;
        int countdown = Mathf.Max(0, Mathf.CeilToInt(_nextWaveTimer));
        if (countdown != _lastObjectiveValue)
        {
            _lastObjectiveValue = countdown;
            _player.SetObjectiveStatus($"WAVE CLEARED  •  NEXT IN {countdown}");
        }
        if (_nextWaveTimer > 0f)
        {
            return;
        }
        _wave++;
        _nextWaveTimer = -1f;
        int count = _wave == 2 ? 5 : 6;
        float hp = _wave == 2 ? 80f : 105f;
        for (int i = 0; i < count; i++)
        {
            float angle = Mathf.Tau * i / count + _wave * 0.4f;
            var enemy = new Enemy();
            EnemyArchetype archetype = _wave == 2
                ? (i == count - 1 ? EnemyArchetype.Hexer : EnemyArchetype.Raider)
                : (i % 3 == 0 ? EnemyArchetype.Brute : (i % 3 == 1 ? EnemyArchetype.Hexer : EnemyArchetype.Raider));
            enemy.Configure(hp, false, archetype);
            _world.AddChild(enemy);
            enemy.GlobalPosition = _player.GlobalPosition + new Vector3(Mathf.Cos(angle) * 9f, 0f, Mathf.Sin(angle) * 9f);
            enemy.JoinAmbush(_player);
        }
        _player.ShowCombatMessage($"WAVE {_wave} — MIXED HOSTILES x{count}");
        _lastObjectiveValue = count;
        _player.SetObjectiveStatus($"WAVE {_wave}  •  {count} HOSTILES");
        GD.Print($"[Spawn] Wave {_wave} spawned: {count} mixed enemies, base {hp} HP.");
    }
}

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
        if (_ritualStarted && _elapsed >= RitualDuration && _player != null && _world != null)
        {
            StartAmbush();
        }
    }

    private void StartAmbush()
    {
        _ambushStarted = true;
        _wave = 1;
        Vector3[] offsets =
        {
            new Vector3(7f, 0f, 1f), new Vector3(-6f, 0f, 3f),
            new Vector3(2f, 0f, -7f), new Vector3(-4f, 0f, -6f),
        };
        foreach (Vector3 offset in offsets)
        {
            var enemy = new Enemy();
            enemy.Configure(60f);
            _world!.AddChild(enemy);
            enemy.GlobalPosition = _player!.GlobalPosition + offset;
            enemy.JoinAmbush(_player);
        }
        GD.Print("[Spawn] Ritual complete. Opening ambush spawned: 4 enemies.");
    }

    private void UpdateWaves(float delta)
    {
        if (_wave >= 3 || _world == null || _player == null)
        {
            return;
        }
        bool hostileAlive = false;
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && IsInstanceValid(enemy) && !enemy.IsQueuedForDeletion() && !enemy.IsTrainingDummy)
            {
                hostileAlive = true;
                break;
            }
        }
        if (hostileAlive)
        {
            _nextWaveTimer = -1f;
            return;
        }
        if (_nextWaveTimer < 0f)
        {
            _nextWaveTimer = 3f;
            _player.ShowCombatMessage("NEXT WAVE IN 3");
            return;
        }
        _nextWaveTimer -= delta;
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
            enemy.Configure(hp);
            _world.AddChild(enemy);
            enemy.GlobalPosition = _player.GlobalPosition + new Vector3(Mathf.Cos(angle) * 9f, 0f, Mathf.Sin(angle) * 9f);
            enemy.JoinAmbush(_player);
        }
        _player.ShowCombatMessage($"WAVE {_wave} — {count} RAIDERS");
        GD.Print($"[Spawn] Wave {_wave} spawned: {count} enemies, {hp} HP.");
    }
}
